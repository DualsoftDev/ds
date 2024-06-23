// Copyright (c) Dualsoft  All Rights Reserved.
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
open DocumentFormat.OpenXml.Presentation

[<AutoOpen>]
module PPTDocModule =

    let pptHeadPage = 1

    let getSystemName (name: string) =
        let fileName = PathManager.getFileNameWithoutExtension (name.ToFile())

        if fileName.Contains(" ") then
            Office.ErrorPPT(ErrorCase.Name, ErrID._57, $"{fileName}", 0, 0u)

        if fileName.IsQuotationRequired() then
            Office.ErrorPPT(ErrorCase.Name, ErrID._58, $"{fileName}", 0, 0u)

        fileName

    let updateAliasPPT
        (
            nodes: Dictionary<string, pptNode>,
            pages: Dictionary<SlidePart, pptPage>,
            parents: Dictionary<pptNode, seq<pptNode>>
        ) =

        let pptNodes = nodes.Values

        let dicFlowNodes =
            pages.Values
            |> Seq.filter (fun page -> page.IsUsing)
            |> Seq.map (fun page -> page.PageNum, pptNodes |> Seq.filter (fun node -> node.PageNum = page.PageNum))
            |> dict

        let updateAlias (aliasCheckNodes: pptNode seq) (allNodes: pptNode seq) (nameNums: (string*int) seq) (isExFlow:bool) =
            (aliasCheckNodes, nameNums)
            ||> Seq.map2 (fun node nameSet -> node, nameSet)
            |> Seq.iter (fun (node, (name, aliasNumber)) ->
                if (aliasNumber > 0) then
                    let orgNode = 
                        if isExFlow
                        then
                             match  allNodes.TryFind(fun f -> $"{f.PageTitle}.{f.Name}" = name) with
                             | Some f -> f
                             | None -> failWithLog $"{name} 해당 Flow.Work를 찾을 수 없습니다."

                        else aliasCheckNodes |> Seq.filter (fun f -> f.Name = name) |> Seq.head

                    node.Alias <- Some(orgNode)
                    node.AliasNumber <- aliasNumber)

        let settingAlias (nodes: pptNode seq) (exFlowReal:bool) =
            let nodes = nodes.OrderByDescending(fun o -> parents.ContainsKey(o)) 
            let filterNodes, allNodes = 
                let findNodes =
                    if  exFlowReal
                    then nodes |> Seq.filter (fun f -> f.NodeType = REALExF) 
                    else nodes |> Seq.filter (fun f -> f.NodeType <> REALExF) 

                if  not(exFlowReal)
                then findNodes, findNodes
                else findNodes, nodes

            let nameNums= GetAliasNumber(filterNodes|> Seq.map (fun f -> f.Name))
            updateAlias filterNodes allNodes nameNums exFlowReal


        let children = parents |> Seq.collect (fun parentSet -> parentSet.Value)

        let callInFlowSet =
            dicFlowNodes
            |> Seq.map (fun flowNodes ->
                flowNodes.Value
                |> Seq.filter (fun node -> node.NodeType.IsCall && children.Contains(node) |> not))

        let realSet =
            dicFlowNodes
            |> Seq.map (fun flowNodes -> flowNodes.Value |> Seq.filter (fun node -> node.NodeType.IsReal))

        let callInRealSet =
            realSet
            |> Seq.collect id
            |> Seq.filter parents.ContainsKey
            |> Seq.map (fun real ->
                dicFlowNodes.[real.PageNum]
                |> Seq.filter (fun node -> node.NodeType.IsCall)
                |> Seq.filter (fun node -> parents.[real].Contains(node)))

        realSet |> Seq.iter(fun f-> settingAlias f false)
        realSet |> Seq.collect(fun xs-> xs) |> fun f-> settingAlias f true
        callInFlowSet |> Seq.iter(fun f-> settingAlias f false)
        callInRealSet |> Seq.iter(fun f-> settingAlias f false)

    let getGroupParentsChildren (page: int, subG: GroupShape, nodes: Dictionary<string, pptNode>) =

        // Recursively get IDs of real and call shapes used in the group and its subgroups
        let rec getGroupMembers (subG: GroupShape, shapeIds: HashSet<uint32>) =
            subG.Descendants<Shape>()
            |> Seq.filter (fun shape -> shape.CheckRectangle() 
                                        || shape.CheckEllipse() 
                                        || shape.CheckFlowChartPreparation())
            |> Seq.iter (fun shape -> shapeIds.Add(shape.GetId().Value) |> ignore)

            subG.Descendants<GroupShape>()
            |> Seq.iter (fun childGroup -> getGroupMembers (childGroup, shapeIds) |> ignore)
        
        let shapeIds = HashSet<uint32>()
        getGroupMembers (subG, shapeIds)
        shapeIds |> Seq.map (fun id -> nodes.[Objkey(page, id)])

    let getValidGroup (groupShapes: seq<GroupShape>) =
        let rec getGroups (subG: GroupShape, names: HashSet<string>) =
            subG.Descendants<GroupShape>()
            |> Seq.iter (fun childGroup ->
                names.Add(childGroup.GroupName()) |> ignore
                getGroups (childGroup, names) |> ignore)
            names

        let groupSubs =
            groupShapes
            |> Seq.map (fun group -> getGroups (group, HashSet<string>()))
            |> Seq.collect id

        groupShapes |> Seq.filter (fun f -> not (groupSubs.Contains(f.GroupName())))

    (* 사용 안함 *)
    //// Save subgroups to dicUsedSub recursively
    //let rec SubGroup (page, subG: GroupShape, dicUsedSub: HashSet<GroupShape>) =
    //    subG.Descendants<GroupShape>()
    //    |> Seq.iter (fun childGroup ->
    //        dicUsedSub.Add(childGroup) |> ignore
    //        SubGroup(page, childGroup, dicUsedSub))

    type pptDoc(path: string, parameter: DeviceLoadParameters option, doc: PresentationDocument) =

        let pages = Dictionary<SlidePart, pptPage>()
        let nodes = Dictionary<string, pptNode>()
        let parents = Dictionary<pptNode, seq<pptNode>>()
        let dummys = HashSet<pptDummy>()
        let edges = HashSet<pptEdge>()

        //let _masterPages = Dictionary<int, SlideMaster>()

        do
            (* 사용 안함 *)
            //let _slideMasters = Office._slidesMasterAll(doc)
            //_slideMasters
            //|> Seq.iter (fun slideMaster -> _masterPages.Add(_masterPages.Count + 1, slideMaster) |> ignore)

            let validSlidesAll =  
                Office.SlidesSkipHide(doc)
                |> Seq.filter (fun (s, _) -> not (s.IsSlideLayoutBlanckType()))
            

            if validSlidesAll |> Seq.exists (fun (slidePart, page) -> page = pptHeadPage) |> not then
                Office.ErrorPPT(Page, ErrID._12, "Title Slide", 0, 0u)

            let headSlide = validSlidesAll |> Seq.find (fun (slidePart, page) -> page = pptHeadPage) |> fst

            validSlidesAll 
            |> Seq.iter (fun (slidePart, page) ->
                if slidePart.PageTitle() = "" then
                    Office.ErrorPPT(Page, ErrID._59, "Title Error", page, 0u)
                else 
                    pages.Add(slidePart, pptPage (slidePart, page, true)) |> ignore)

            let allGroups =
                Groups(doc)
                |> Seq.filter (fun (slide, _) -> pages.ContainsKey(slide))
                |> Seq.map (fun (slide, groupSet) -> pages.[slide].PageNum, groupSet)

            let shapes =
                Office.PageShapes(doc)
                |> Seq.filter (fun (shape, page, _) -> 
                    page <> pptHeadPage && pages.Values |> Seq.exists (fun w -> w.PageNum = page)
                    || page = pptHeadPage && shape.CheckBevelShape())

            pages.Values
            |> Seq.iter (fun pptPage -> 
                let ableShapes = shapes |> Seq.filter (fun (shape, page, _) -> pptPage.PageNum = page) |> Seq.map(fun (shape,_,_) -> shape)
                pptPage.SlidePart.CheckValidShapes(pptPage.PageNum, ableShapes))

            let connections =
                Connections(doc)
                |> Seq.filter (fun (slide, _) -> slide.GetPage() <> pptHeadPage)
                |> Seq.filter (fun (slide, _) -> pages.ContainsKey(slide))
            
            let dicShape = Dictionary<int, HashSet<Shape>>()

            shapes 
            |> Seq.iter (fun (shape, page, _) ->
                if not (dicShape.ContainsKey(page)) then
                    dicShape.Add(page, HashSet<Shape>()) |> ignore
                dicShape.[page].Add(shape) |> ignore)

            let slideSize = Office.SlideSize(doc)
            shapes
            |> Seq.iter (fun (shape, page, _) ->
                let pagePPT = pages.Values |> Seq.find (fun w -> w.PageNum = page)
                let headPageName = headSlide.PageTitle()
                let sysName, flowName = GetSysNFlow(headPageName, pagePPT.Title, pagePPT.PageNum)
                let headPage = page = pptHeadPage
                let node = pptNode (shape, page, flowName, slideSize, headPage)

                if node.Name = "" then
                    shape.ErrorName(ErrID._13, page)

                nodes.Add(node.Key, node) |> ignore)

            let dicParentCheck = Dictionary<string, int>()

            allGroups
            |> Seq.iter (fun (page, groups) ->
                groups
                |> getValidGroup
                |> Seq.iter (fun group ->
                    let groupAllNodes = getGroupParentsChildren (page, group, nodes)

                    if groupAllNodes |> Seq.isEmpty |> not then
                        let pptGroup = pptRealGroup (page, groupAllNodes)

                        match pptGroup.Parent with
                        | Some parent ->
                            if dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum) then
                                parents.Add(parent, pptGroup.Children)
                            else
                                Office.ErrorPPT(
                                    Group,
                                    ErrID._17,
                                    $"{dicParentCheck.[pptGroup.RealKey]}-{parent.Name}",
                                    pptGroup.PageNum,
                                    parent.Shape.ShapeID()
                                )
                        | None -> ()))

            let children = parents.Values |> Seq.collect id
            nodes.Values
            |> Seq.iter (fun node ->
                let isRoot = not (children |> Seq.contains node)
                node.UpdateCallDevParm(isRoot))

            connections
            |> Seq.iter (fun (slide, conns) -> 
                conns
                |> Seq.iter (fun (conn, Id, startId, endId) ->
                    let iPage = pages.[slide].PageNum

                    match startId, endId with
                    | 0u, 0u -> conn.ErrorConnect(ErrID._4, "", "", iPage)                         
                    | 0u, _ ->
                        conn.ErrorConnect(ErrID._15, "", $"{nodes.[Objkey(iPage, endId)].Name}", iPage)
                    | _, 0u ->
                        conn.ErrorConnect(ErrID._15, $"{nodes.[Objkey(iPage, startId)].Name}", "", iPage)
                    | _ ->
                        let sNode = nodes.[Objkey(iPage, startId)]
                        let eNode = nodes.[Objkey(iPage, endId)]
                        let sName = if nodes.ContainsKey(sNode.Key) then sNode.Name else ""
                        let eName = if nodes.ContainsKey(eNode.Key) then eNode.Name else ""

                        if not (nodes.ContainsKey(sNode.Key)) then
                            conn.ErrorConnect(ErrID._14, sName, "", iPage)

                        if not (nodes.ContainsKey(eNode.Key)) then
                            conn.ErrorConnect(ErrID._14, eName, "", iPage)

                        if conn.IsNonDirectional() then
                            dummys.AddDummys(sNode, eNode)
                        else
                            edges.Add(pptEdge (conn, Id, iPage, sNode, eNode)) |> ignore))

            updateAliasPPT (nodes, pages, parents)

        member x.GetTables(colCnt: int) = doc.GetTablesWithPageNumbers colCnt
        member x.GetLayouts() = doc.GetLayouts()
        member x.SaveSlideImage() = doc.SaveSlideImage(PathManager.getFileName (path|>DsFile))
        
        member x.GetPage(pageNum: int) =
            pages.Values |> Seq.find (fun p -> p.PageNum = pageNum)

        member val Pages = pages.Values |> Seq.sortBy (fun p -> p.PageNum)
        member val PageNames = pages.Values.Select(fun f -> f.Title)
        member val NodesHeadPage = nodes.Values |> Seq.filter (fun p -> p.PageNum = pptHeadPage)
        member val Nodes = nodes.Values |> Seq.filter (fun p -> p.PageNum <> pptHeadPage) |> Seq.sortBy (fun p -> p.PageNum)
        member val Edges = edges |> Seq.sortBy (fun p -> p.PageNum)

        member val DicNodes = nodes
        member val Parents = parents
        member val Dummys = dummys

        member val Name = (pages.Keys |> Seq.find (fun f -> f.GetPage() = pptHeadPage)).PageTitle()    
        member val Path = path
        member val DirectoryName = PathManager.getDirectoryName (path|>DsFile)

        member val DicFlow = Dictionary<int, Flow>() // page , flow
        member val DicVertex = Dictionary<string, Vertex>()
        member val IsBuilded = false with get, set

        member x.Parameter: DeviceLoadParameters = parameter.Value
        member x.Doc = doc

[<Extension>]
type PPTDocExt =

    [<Extension>]
    static member GetCopyPathNName(doc: pptDoc) =

        let callJobDic = 
            doc.Nodes
            |> Seq.filter (fun node -> node.IsCall && not (node.IsFunction))
            |> Seq.map (fun node ->
                    if doc.PageNames.Contains(node.FlowName) then
                        let flowNodes = doc.Nodes
                                           .Where(fun f->f.PageTitle = node.FlowName)
                                           .Where(fun f->f.NodeType = CALL)
                        if flowNodes.Any(fun n->n.CallDevName = node.CallDevName) then
                            node.CallName, node.JobOption
                        else
                            failWithLog $"{node.CallDevName} 해당 디바이스가 없습니다."
                    else
                        failWithLog $"{node.FlowName} 해당 페이지가 없습니다.")
            |> dict

        let getDevCount (devName) = 
            if callJobDic.ContainsKey(devName) 
            then callJobDic.[devName].DeviceCount 
            else 1
            
        doc.Nodes
        |> Seq.filter (fun node -> node.NodeType.IsLoadSys)
        |> Seq.collect (fun node ->
            node.CopySys |> Seq.collect (fun copy ->
                let devCount = getDevCount(copy.Key)
                let loadFilePath = 
                    if copy.Value.EndsWith(".pptx") then copy.Value
                    else copy.Value + ".pptx"

                if devCount > 1 then
                    seq {
                        for i in 1 .. devCount do
                            let multiName = getMultiDeviceName (copy.Key.Trim()) i
                            yield loadFilePath, multiName, node
                    }
                else
                    seq {
                        yield loadFilePath, copy.Key.Trim(), node
                    }
            )
        )
