// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open System.IO
open System
open PPTUtil
open Dual.Common.Core.FS
open Engine.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices



[<AutoOpen>]
module PPTDocModule =

    let getSystemName(name:string) =
        let fileName = PathManager.getFileNameWithoutExtension(name.ToFile())
        if fileName.Contains(" ")            
        then Office.ErrorPPT(ErrorCase.Name, ErrID._57, $"{fileName}", 0 , 0u)
        if fileName.IsQuotationRequired()
        then Office.ErrorPPT(ErrorCase.Name, ErrID._58, $"{fileName}", 0 , 0u)
        fileName

    let updateAliasPPT (
          nodes:Dictionary<string, pptNode>
        , pages:Dictionary<SlidePart, pptPage>
        , parents:Dictionary<pptNode, seq<pptNode>>) =

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


    let getGroupParentsChildren(page:int, subG:Presentation.GroupShape, nodes:Dictionary<string, pptNode>) =

        //group 에 사용된 real, call ID를 재귀적으로 모든 하위그룹까지 가져옴
        let rec getGroupMembers(subG:Presentation.GroupShape, shapeIds:HashSet<uint32>) =
                subG.Descendants<Presentation.Shape>()
                |> Seq.filter(fun shape -> shape.CheckRectangle() || shape.CheckEllipse())
                |> Seq.iter(fun shape -> shapeIds.Add(shape.GetId().Value)|>ignore )

                subG.Descendants<Presentation.GroupShape>()
                |> Seq.iter(fun childGroup -> getGroupMembers(childGroup, shapeIds) |> ignore)

        let shapeIds = HashSet<uint32>()
        getGroupMembers(subG, shapeIds)
        let groupNodes = shapeIds |> Seq.map (fun id -> nodes.[ Objkey(page, id) ])
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
    let rec SubGroup(page, subG:GroupShape, dicUsedSub:HashSet<GroupShape>) =
            subG.Descendants<Presentation.GroupShape>()
            |> Seq.iter(fun childGroup ->

            dicUsedSub.Add(childGroup) |>ignore

            SubGroup(page, childGroup, dicUsedSub)
            )
    type pptDoc(path:string, parameter:DeviceLoadParameters option, doc:PresentationDocument)  =

        let headPages =  Dictionary<SlidePart, int>()
        let pages =  Dictionary<SlidePart, pptPage>()
        let masterPages =  Dictionary<int, DocumentFormat.OpenXml.Presentation.SlideMaster>()
        let nodes =  Dictionary<string, pptNode>()
        let parents = Dictionary<pptNode, seq<pptNode>>()
        let dummys =  HashSet<pptDummy>()
        let edges =   HashSet<pptEdge>()

        do
            let sildesAll = Office.SildesAll(doc)
            let shapes = Office.PageShapes(doc)
            let connections = Connections(doc)
            let allGroups =  Groups(doc) |> Seq.map(fun (slide, groupSet) -> pages.[slide].PageNum, groupSet)
            let sildeSize = Office.SildeSize(doc)
            let sildeMasters = Office.SildesMasterAll(doc)

            sildeMasters |> Seq.iter (fun slideMaster -> masterPages.Add(masterPages.Count+1, slideMaster) |>ignore )
            
            sildesAll    
                |> Seq.filter(fun (slidePart, show, page) -> not( slidePart.IsSlideLayoutBlanckType()))
                |> Seq.iter  (fun (slidePart, show, page) ->
                        if (slidePart.PageTitle(false) = "" && slidePart.PageTitle(true) = "")
                        then Office.ErrorPPT(Page, ErrID._59 , "Title Error" , page, 0u))


            sildesAll    
                |> Seq.iter  (fun (slidePart, show, page) ->
                        if (slidePart.PageTitle(false) <> "")
                            then pages.Add(slidePart, pptPage(slidePart, page, show)) |>ignore 
                        else if (slidePart.PageTitle(true) <> "")
                            then headPages.Add(slidePart, page) |> ignore
                )

            let dicShape = Dictionary<int, HashSet<Tuple<Shape,bool>>>()
            shapes
            |> Seq.where (fun (shape, page, geometry, isDash) -> not(headPages.Values.Contains(page)))
            |> Seq.iter (fun (shape, page, geometry, isDash) ->
                        if(dicShape.ContainsKey(page)|>not)
                        then dicShape.Add(page, HashSet<Tuple<Shape,bool>>()) |> ignore
                        dicShape.[page].Add(shape, isDash) |> ignore
            )
            if(headPages.IsEmpty())
            then Office.ErrorPPT(Page, ErrID._12 , "Title Slide" , 0, 0u)

            let name = headPages.Keys.First().PageTitle(true)
            shapes
            |> Seq.where (fun (shape, page, geometry, isDash) -> not(headPages.Values.Contains(page)))
            |> Seq.iter (fun (shape, page, geometry, isDash) ->

                        let pagePPT = pages.Values.Filter(fun w->w.PageNum = page).First()
                        let sysName, flowName = GetSysNFlow(name, pagePPT.Title, pagePPT.PageNum)

                        let node = pptNode(shape, page, flowName)
                        if(node.Name ="") then shape.ErrorName(ErrID._13, page)
                        nodes.Add(node.Key, node)  |>ignore )

            let dicParentCheck = Dictionary<string, int>()
            allGroups
            |> Seq.iter (fun (page, groups) ->
                groups|> getValidGroup
                |> Seq.iter (fun group ->
                    let groupAllNodes = getGroupParentsChildren(page, group, nodes)
                    if groupAllNodes.any()
                    then
                        let pptGroup = pptRealGroup(page, groupAllNodes)
                        match pptGroup.Parent with
                        |Some parent -> 
                            if(dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum)) then
                                parents.Add(parent, pptGroup.Children)
                            else
                                Office.ErrorPPT(Group, ErrID._17, $"{dicParentCheck.[pptGroup.RealKey]}-{parent.Name}", pptGroup.PageNum, parent.Shape.ShapeID())
                        |None  -> ()
                    )
            )

            
            connections
            |> Seq.where (fun (slide, conns) -> not(headPages.ContainsKey(slide)))
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

            //with ex -> doc.Close()
            //           failwithf  $"{ex.Message}"


        member x.GetTables(colCnt:int) = doc.GetTablesWithPageNumbers colCnt     
        member x.GetPage(pageNum:int) = pages.Values |> Seq.filter(fun p -> p.PageNum = pageNum) |> Seq.head
        member val Pages = pages.Values.OrderBy(fun p -> p.PageNum)
        member val Nodes = nodes.Values.OrderBy(fun p -> p.PageNum)
        member val Edges = edges.OrderBy(fun p -> p.PageNum)

        member val DicNodes = nodes
        member val Parents = parents
        member val Dummys  = dummys

        member val Name = headPages.Keys.First().PageTitle(true)
        member val Path = path
        member val DirectoryName =  PathManager.getDirectoryName (path.ToFile())

        member val DicFlow = Dictionary<int, Flow>() // page , flow
        member val DicVertex = Dictionary<string, Vertex>()
        member val IsBuilded = false with get, set
        member x.Parameter:DeviceLoadParameters = parameter.Value
        member x.Doc = doc
[<Extension>]
type PPTDocExt =
        [<Extension>]
        static member GetCopyPathNName(doc:pptDoc) =
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType.IsLoadSys)
            |> Seq.collect(fun node ->
            node.CopySys.Select(fun copy ->
                let loadFilePath =  if copy.Value.EndsWith(".pptx") then copy.Value else copy.Value+".pptx"
                loadFilePath , copy.Key.Trim(), node 
                    )
        )
