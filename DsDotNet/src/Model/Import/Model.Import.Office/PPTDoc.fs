// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open System.IO
open System
open PPTUtil
open Engine.Common.FS
open Model.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices



[<AutoOpen>]
module PPTDocModule =

    let getSystemDirectoryName(path:string) = FileInfo(path).DirectoryName
    let getSystemName(name:string) =
        let fileName = Path.GetFileNameWithoutExtension(name)
        if fileName.IsQuotationRequired()
        then Office.ErrorPPT(ErrorCase.Name, ErrID._44, $"SystemNamePath : {name}", 0, $"SystemName : {fileName}")
        fileName

    let updateAliasPPT (
          nodes:ConcurrentDictionary<string, pptNode>
        , pages:ConcurrentDictionary<SlidePart, pptPage>
        , parents:ConcurrentDictionary<pptNode, seq<pptNode>>) =

            let pptNodes = nodes.Values
            let dicFlowNodes = pages.Values
                                |> Seq.filter(fun page -> page.IsUsing)
                                |> Seq.map  (fun page ->
                                    page.PageNum, pptNodes |> Seq.filter(fun node -> node.PageNum = page.PageNum)
                                    ) |> dict

            let settingAlias(nodes:pptNode seq) =
                let nodes = nodes.OrderByDescending(fun o-> parents.ContainsKey(o))  //부모지정
                let names  = nodes |> Seq.map(fun f->f.Name)
                (nodes, GetAliasNumber(names))
                ||> Seq.map2(fun node  nameSet -> node,  nameSet)
                |> Seq.iter(fun (node, (name, aliasNumber)) ->
                                if(aliasNumber > 0)
                                then    let orgNode = nodes |> Seq.filter(fun f->f.Name = name) |> Seq.head
                                        node.Alias <- Some(orgNode)
                                        node.AliasNumber  <- aliasNumber
                                        )

            let children = parents |> Seq.collect(fun parentSet -> parentSet.Value)
            let callInFlowSet = dicFlowNodes
                                |> Seq.map(fun flowNodes ->
                                            flowNodes.Value
                                            |> Seq.filter(fun node -> node.NodeType.IsCall && children.Contains(node)|>not)
                                            )
            let realSet = dicFlowNodes
                                |> Seq.map(fun flowNodes ->
                                            flowNodes.Value |> Seq.filter(fun node -> node.NodeType.IsReal))
            let callInRealSet = realSet
                                    |> Seq.collect(fun reals -> reals)
                                    |> Seq.filter(fun real -> parents.ContainsKey(real))
                                    |> Seq.map(fun real ->
                                                dicFlowNodes.[real.PageNum]
                                                |> Seq.filter(fun node -> node.NodeType.IsCall)
                                                |> Seq.filter(fun node -> parents.[real].Contains(node) ))

            realSet |> Seq.iter settingAlias
            callInFlowSet |> Seq.iter settingAlias
            callInRealSet |> Seq.iter settingAlias


    let getGroupParentsChildren(page:int, subG:Presentation.GroupShape, nodes:ConcurrentDictionary<string, pptNode>) =

        //group 에 사용된 real, call ID를 재귀적으로 모든 하위그룹까지 가져옴
        let rec getGroupMembers(subG:Presentation.GroupShape, shapeIds:ConcurrentHash<uint32>) =
                subG.Descendants<Presentation.Shape>()
                |> Seq.filter(fun shape -> shape.CheckRectangle() || shape.CheckEllipse())
                |> Seq.iter(fun shape -> shapeIds.TryAdd(shape.GetId().Value)|>ignore )

                subG.Descendants<Presentation.GroupShape>()
                |> Seq.iter(fun childGroup -> getGroupMembers(childGroup, shapeIds) |> ignore)

        let shapeIds = ConcurrentHash<uint32>()
        getGroupMembers(subG, shapeIds)
        let groupNodes = shapeIds.Values |> Seq.map (fun id -> nodes.[ Objkey(page, id) ])
        groupNodes

    let getValidGroup(groupShapes:GroupShape seq) =
            let rec getGroups(subG:Presentation.GroupShape, names:HashSet<string>) =
                subG.Descendants<Presentation.GroupShape>()
                |> Seq.iter(fun childGroup ->
                                names.Add(childGroup.GroupName()) |>ignore
                                getGroups(childGroup, names) |> ignore)
                names

            let groupSubs =
                groupShapes
                |> Seq.map(fun group->  getGroups(group, HashSet<string>()))
                |> Seq.collect(fun groups-> groups)

            groupShapes
            |> Seq.filter(fun f-> groupSubs.Contains(f.GroupName())|>not)


    //하부의 재귀적 중복 그룹 항목을 dicUsedSub 저장한다
    let rec SubGroup(page, subG:GroupShape, dicUsedSub:ConcurrentHash<GroupShape>) =
            subG.Descendants<Presentation.GroupShape>()
            |> Seq.iter(fun childGroup ->

            dicUsedSub.TryAdd(childGroup) |>ignore

            SubGroup(page, childGroup, dicUsedSub)
            )

    type pptDoc(path:string, parameter:DeviceLoadParameters)  =
        let doc = Office.Open(path)
        let name = getSystemName path
        let pages =  ConcurrentDictionary<SlidePart, pptPage>()
        let masterPages =  ConcurrentDictionary<int, DocumentFormat.OpenXml.Presentation.SlideMaster>()
        let nodes =  ConcurrentDictionary<string, pptNode>()
        let parents = ConcurrentDictionary<pptNode, seq<pptNode>>()
        let dummys =  HashSet<pptDummy>()
        let edges =   HashSet<pptEdge>()

        do
            let sildesAll = Office.SildesAll(doc)
            let shapes = Office.PageShapes(doc)
            let connections = Connections(doc)
            let allGroups =  Groups(doc) |> Seq.map(fun (slide, groupSet) -> pages.[slide].PageNum, groupSet)
            let sildeSize = Office.SildeSize(doc)
            let sildeMasters = Office.SildesMasterAll(doc)

            try

                sildeMasters |> Seq.iter (fun slideMaster -> masterPages.TryAdd(masterPages.Count+1, slideMaster) |>ignore )
                sildesAll    |> Seq.iter (fun (slidePart, show, page) -> pages.TryAdd(slidePart, pptPage(slidePart, page, show)) |>ignore )

                let dicShape = ConcurrentDictionary<int, HashSet<Tuple<Shape,bool>>>()
                shapes
                |> Seq.iter (fun (shape, page, geometry, isDash) ->
                            if(dicShape.ContainsKey(page)|>not)
                            then dicShape.TryAdd(page, HashSet<Tuple<Shape,bool>>()) |> ignore
                            dicShape.[page].Add(shape, isDash) |> ignore
                )

                shapes
                |> Seq.iter (fun (shape, page, geometry, isDash) ->

                            let pagePPT = pages.Values.Filter(fun w->w.PageNum = page).First()
                            let sysName, flowName = GetSysNFlow(name, pagePPT.Title, pagePPT.PageNum)

                            let node = pptNode(shape, page, flowName)
                            if(node.Name ="") then shape.ErrorName(ErrID._13, page)
                            nodes.TryAdd(node.Key, node)  |>ignore )

                let dicParentCheck = ConcurrentDictionary<string, int>()
                allGroups
                |> Seq.iter (fun (page, groups) ->
                    groups|> getValidGroup
                    |> Seq.iter (fun group ->
                        let groupAllNodes = getGroupParentsChildren(page, group, nodes)
                        if groupAllNodes.any()
                        then
                            let pptGroup = pptRealGroup(page, groupAllNodes)
                                
                            let parent = pptGroup.Parent.Value;
                            if(dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum))
                            then parents.TryAdd(parent, pptGroup.Children)|>ignore
                            else Office.ErrorPPT(Group, ErrID._17, $"{dicParentCheck.[pptGroup.RealKey]}-{parent.Name}", pptGroup.PageNum)
                    )
                )


                connections
                |> Seq.iter (fun (slide, conns) ->
                    conns
                          |> Seq.iter (fun (conn, Id, startId, endId) ->
                            let iPage = pages.[slide].PageNum

                            
                            if(startId = 0u && endId = 0u) then  conn.ErrorConnect(ErrID._4, "","", iPage)
                            if(startId = 0u) then  conn.ErrorConnect(ErrID._15, "", $"{nodes.[Objkey(iPage, endId)].Name}", iPage)
                            if(endId = 0u)   then  conn.ErrorConnect(ErrID._16, $"{nodes.[Objkey(iPage, startId)].Name}", "", iPage)

                            let sNode = nodes.[Objkey(iPage, startId)]
                            let eNode = nodes.[Objkey(iPage, endId)]
                            let sName = if(nodes.ContainsKey(sNode.Key)) then sNode.Name else ""
                            let eName = if(nodes.ContainsKey(eNode.Key)) then eNode.Name else ""

                            if(nodes.ContainsKey(sNode.Key)|>not) then  conn.ErrorConnect(ErrID._14, $"{sName}", "", iPage)
                            if(nodes.ContainsKey(eNode.Key)|>not) then  conn.ErrorConnect(ErrID._14, $"{eName}", "", iPage)
                           
                            if conn.IsNonDirectional()
                            then dummys.AddDummys([|sNode; eNode|])
                            else edges.Add(pptEdge(conn, Id, iPage ,sNode, eNode)) |>ignore
                        ))


                updateAliasPPT(nodes, pages, parents)
                doc.Close()

            with ex -> doc.Close()
                       failwithf  $"{ex.Message}"


        member x.GetPage(pageNum:int) = pages.Values |> Seq.filter(fun p -> p.PageNum = pageNum) |> Seq.head
        member val Pages = pages.Values.OrderBy(fun p -> p.PageNum)
        member val Nodes = nodes.Values.OrderBy(fun p -> p.PageNum)
        member val Edges = edges.OrderBy(fun p -> p.PageNum)

        member val DicNodes = nodes
        member val Parents = parents
        member val Dummys  = dummys

        member val Name =  name
        member val DirectoryName =  getSystemDirectoryName path

        member val DicFlow = Dictionary<int, Flow>() // page , flow
        member val DicVertex = Dictionary<string, Vertex>() 
        member val IsBuilded = false with get, set
        member val Parameter:DeviceLoadParameters = parameter
    
[<Extension>]
type PPTDocExt =
        [<Extension>]
        static member GetCopyPathNName(doc:pptDoc) =
            doc.Nodes 
            |> Seq.filter(fun node -> node.NodeType = COPY_VALUE || node.NodeType = COPY_REF)
            |> Seq.collect(fun node ->
            node.CopySys.Select(fun copy -> 
                let path = Path.GetFullPath(Path.Combine(doc.DirectoryName, copy.Value))+".pptx"
                if File.Exists(path) |> not
                then node.Shape.ErrorPath(ErrID._29, node.PageNum, path)
                                
                copy.Value , copy.Key, node)
        )
     