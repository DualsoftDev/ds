// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Concurrent
open DocumentFormat.OpenXml.Packaging
open DocumentFormat.OpenXml.Drawing
open DocumentFormat.OpenXml
open System.IO
open System
open PptUtil
open Dual.Common.Core.FS
open Engine.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Presentation
open Engine.Core.MapperDataModule
open System.Xml.Serialization
open System.IO.Packaging
open PLC.Mapper.FS

[<AutoOpen>]
module PptDocModule =

    let [<Literal>] pptHeadPage = 1

    let getSystemName (name: string) =
        let fileName = PathManager.getFileNameWithoutExtension (name.ToFile())

        if fileName.Contains(" ") then
            Office.ErrorPpt(ErrorCase.Name, ErrID._57, $"{fileName}", 0, 0u)

        if fileName.IsQuotationRequired() then
            Office.ErrorPpt(ErrorCase.Name, ErrID._58, $"{fileName}", 0, 0u)

        fileName

    let updateAliasPpt
        (
            nodes: Dictionary<string, PptNode>,
            pages: Dictionary<SlidePart, PptPage>,
            parents: Dictionary<PptNode, seq<PptNode>>
        ) =

        let pptNodes = nodes.Values

        let dicFlowNodes =
            pages.Values
            |> Seq.filter (fun page -> page.IsUsing)
            |> Seq.groupBy (fun page -> page.Title)
            |> Seq.map (fun (groupTitle, _) -> groupTitle, pptNodes |> Seq.filter (fun node -> node.FlowName = groupTitle))
            |> dict

        let updateAlias (aliasCheckNodes: PptNode seq) (allNodes: PptNode seq) (nameNums: (string*int) seq) (isExFlow:bool) =
            (aliasCheckNodes, nameNums)
            ||> Seq.map2 (fun node nameSet -> node, nameSet)
            |> Seq.iter (fun (node, (name, aliasNumber)) ->
                if (aliasNumber > 0) then
                    let orgNode =
                        if isExFlow
                        then
                             match  allNodes.TryFind(fun f -> $"{f.PageTitle}.{f.Name}" = name) with
                             | Some f -> f
                             | None -> node.Shape.ErrorName( $"해당 Flow.Work를 찾을 수 없습니다 {name}", node.PageNum)

                        else aliasCheckNodes |> Seq.filter (fun f ->
                                                    let keyName =
                                                        match f.NodeType with
                                                            | CALL  when not(f.IsFunction) -> f.Job.Combine()
                                                            | _ -> f.Name
                                                    keyName = name)
                                                    |> Seq.head

                    node.Alias <- Some(orgNode)
                    node.AliasNumber <- aliasNumber)

        let settingAlias (nodes: PptNode seq) (exFlowReal:bool) =
            let nodes = nodes.OrderByDescending(fun o -> parents.ContainsKey(o))
            let filterNodes, allNodes =
                let findNodes =
                    if  exFlowReal
                    then nodes |> Seq.filter (fun f -> f.NodeType = REALExF)
                    else nodes |> Seq.filter (fun f -> f.NodeType <> REALExF)

                if  not(exFlowReal)
                then findNodes, findNodes
                else findNodes, nodes

            let nameNums= GetAliasNumber
                            (filterNodes
                                |> Seq.map (fun f ->
                                    match f.NodeType with
                                    | CALL -> if f.IsFunction
                                                then f.Name 
                                                else f.Job.Combine()
                                    | _ -> f.Name 
                                    )
                            )

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
                dicFlowNodes.[real.FlowName]
                |> Seq.filter (fun node -> node.NodeType.IsCall)
                |> Seq.filter (fun node -> parents.[real].Contains(node)))

        realSet |> Seq.iter(fun f-> settingAlias f false)
        realSet |> Seq.collect(fun xs-> xs) |> fun f-> settingAlias f true
        callInFlowSet |> Seq.iter(fun f-> settingAlias f false)
        callInRealSet |> Seq.iter(fun f-> settingAlias f false)

    let getGroupParentsChildren (page: int, subG: GroupShape, nodes: Dictionary<string, PptNode>) =

        // Recursively get IDs of real and call shapes used in the group and its subgroups
        let rec getGroupMembers (subG: GroupShape, shapeIds: HashSet<uint32>) =
            subG.Descendants<Shape>()
            |> Seq.filter (fun shape -> shape.IsRectangle()
                                        || shape.IsEllipse()
                                        || shape.IsFlowChartPreparation())
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

    let getMapperData(doc: PresentationDocument):UserTagConfig option=
        let customXmlParts =
            doc.PresentationPart.CustomXmlParts
            |> Seq.map(fun f->
                use reader = new StreamReader(f.GetStream())
                reader.ReadToEnd()
                )
        if customXmlParts.length() > 0 then
            Some(PowerPointMapperXml.LoadMapperData  customXmlParts)
        else
            None
            

    type PptDoc private(path: string, parameter: DeviceLoadParameters option, doc: PresentationDocument, target,
        pages:IDictionary<SlidePart, PptPage>,
        nodes:IDictionary<string, PptNode>,
        parents:IDictionary<PptNode, seq<PptNode>>,
        edges:PptEdge seq,
        dummys:ISet<PptDummy>
    ) =
        member x.GetTables(colCnt: int) = doc.GetTablesWithPageNumbers colCnt
        member x.GetLayouts() = doc.GetLayouts()
        member x.SaveSlideImage() = doc.SaveSlideImage(PathManager.getFileName (path|>DsFile))

        member x.GetPage(pageNum: int) =
            pages.Values |> Seq.find (fun p -> p.PageNum = pageNum)

        member val Pages = pages.Values |> Seq.sortBy (fun p -> p.PageNum) |> toArray
        member val PageNames = pages.Values.Select(fun f -> f.Title).ToArray()
        member val NodesHeadPage = nodes.Values |> Seq.filter (fun p -> p.PageNum = pptHeadPage) |> toArray
        member val Nodes = nodes.Values |> filter (fun p -> p.PageNum <> pptHeadPage) |> Seq.sortBy (fun p -> p.PageNum) |> toArray
        member val Edges = edges |> Seq.sortBy (fun p -> p.PageNum) |> toArray

        member val DicNodes = nodes
        member val Parents = parents
        member val Dummys = dummys

        member val Name = (pages.Keys |> Seq.find (fun f -> f.GetPage() = pptHeadPage)).PageTitle()
        member val Path = path
        member val DirectoryName = PathManager.getDirectoryName (DsFile path)

        member val DicFlow = Dictionary<int, Flow>() // page , flow
        member val DicVertex = Dictionary<string, Vertex>()
        member val IsBuilded = false with get, set

        member x.Parameter: DeviceLoadParameters = parameter.Value
        member x.Doc = doc

        member x.UserDeviceTags : List<UserDeviceTag> =
            match getMapperData doc with
            | Some mapperData -> mapperData.UserDeviceTags.ToList()
            | None -> new List<UserDeviceTag>()

        member x.UserTagsFromMapper : List<UserMonitorTag> =
            match getMapperData doc with
            | Some mapperData -> mapperData.UserMonitorTags.ToList()
            | None -> new List<UserMonitorTag>()

    type PptDoc with
        static member Create(path: string, parameter: DeviceLoadParameters option, doc: PresentationDocument, target) =

            let pages = Dictionary<SlidePart, PptPage>()
            let nodes = Dictionary<string, PptNode>()
            let parents = Dictionary<PptNode, seq<PptNode>>()
            let dummys = HashSet<PptDummy>()
            let edges = HashSet<PptEdge>()

            // 숨김 페이지, blank page 제외한 모든 페이지
            let validSlidesAll =
                Office.SlidesSkipHide(doc)
                |> Seq.filter (fun (s, _) -> not (s.IsSlideLayoutBlankType()))


            if validSlidesAll |> Seq.exists (fun (slidePart, page) -> page = pptHeadPage) |> not then
                Office.ErrorPpt(Page, ErrID._12, "Title Slide", 0, 0u)

            // pages 구성 (및 제목 누락 페이지 에러)
            validSlidesAll
            |> Seq.iter (fun (slidePart, page) ->
                if slidePart.PageTitle() = "" then
                    Office.ErrorPpt(Page, ErrID._59, "Title Error", page, 0u)
                else
                    pages.Add(slidePart, PptPage (slidePart, page, true)) |> ignore)

            // 페이지 별 group 정보
            let allGroups =
                Groups(doc)
                |> Seq.filter (fun (slide, _) -> pages.ContainsKey(slide))
                |> Seq.map (fun (slide, groupSet) -> pages.[slide].PageNum, groupSet)

            let shapes =
                Office.PageShapes(doc)
                |> Seq.filter (fun (shape, page, _) ->
                    if page = pptHeadPage then
                        shape.IsBevelShape()
                    else
                        pages.Values |> Seq.exists (fun w -> w.PageNum = page))

            pages.Values
            |> Seq.iter (fun pptPage ->
                let ableShapes = shapes |> Seq.filter (fun (shape, page, _) -> pptPage.PageNum = page) |> Seq.map(fun (shape,_,_) -> shape)
                pptPage.SlidePart.CheckValidShapes(pptPage.PageNum, ableShapes))

            let connections =
                Connections(doc)
                |> Seq.filter (fun (slide, _) -> slide.GetPage() <> pptHeadPage)
                |> Seq.filter (fun (slide, _) -> pages.ContainsKey(slide))

            let slideSize = Office.SlideSize(doc)
            let headSlide = validSlidesAll |> Seq.find (fun (slidePart, page) -> page = pptHeadPage) |> fst

            let masterMacros =
                Office.PagePlaceHolderShapes(doc)
                |> Seq.map(fun (master, value, page) -> {Macro = master; MacroRelace = value; Page = page} )

            shapes
            |> Seq.iter (fun (shape, page, _) ->
                let pagePpt = pages.Values |> Seq.find (fun w -> w.PageNum = page)
                let headPageName = headSlide.PageTitle()
                let sysName, flowName = GetSysNFlow(headPageName, pagePpt.Title, pagePpt.PageNum)
                let headPage = page = pptHeadPage

                let node = PptNode.Create(shape, page, flowName, slideSize, headPage, masterMacros)

                if node.Name = "" then
                    shape.ErrorName(ErrID._13, page)

                if nodes.ContainsKey node.Key
                then
                    node.Shape.ErrorShape(ErrID._20, page)
                else
                    nodes.Add(node.Key, node) |> ignore

                )

            let dicParentCheck = Dictionary<string, int>()

            allGroups
            |> Seq.iter (fun (page, groups) ->
                groups
                |> getValidGroup
                |> Seq.iter (fun group ->
                    let groupAllNodes = getGroupParentsChildren (page, group, nodes)

                    if groupAllNodes |> Seq.isEmpty |> not then
                        let pptGroup = PptRealGroup (page, groupAllNodes)

                        match pptGroup.Parent with
                        | Some parent ->
                            if dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum) then
                                parents.Add(parent, pptGroup.Children)
                            else
                                Office.ErrorPpt(
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
                node.UpdateNodeRoot(isRoot)
                )

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

                        let sKey = Objkey(iPage, startId)
                        let eKey = Objkey(iPage, endId)
                        if not(nodes.ContainsKey(sKey) && nodes.ContainsKey(eKey))
                        then
                            conn.ErrorConnect(ErrID._14, "", "", iPage)


                        let sNode = nodes.[sKey]
                        let eNode = nodes.[eKey]


                        let sName = if nodes.ContainsKey(sNode.Key) then sNode.Name else ""
                        let eName = if nodes.ContainsKey(eNode.Key) then eNode.Name else ""

                        if not (nodes.ContainsKey(sNode.Key)) then
                            conn.ErrorConnect(ErrID._14, sName, "", iPage)

                        if not (nodes.ContainsKey(eNode.Key)) then
                            conn.ErrorConnect(ErrID._14, eName, "", iPage)

                        if conn.IsNonDirectional() then
                            dummys.AddDummys(sNode, eNode)
                        else
                            edges.Add(PptEdge (conn, Id, iPage, sNode, eNode)) |> ignore))

            updateAliasPpt (nodes, pages, parents)
            PptDoc(path, parameter, doc, target,
                pages, nodes, parents, edges, dummys)


[<Extension>]
type PptDocExt =

    [<Extension>]
    static member GetCopyPathNName(doc: PptDoc) =

        let callJobDic =
            doc.Nodes
            |> Seq.filter (fun node -> node.IsCall)
            |> Seq.filter (fun node -> not (node.IsFunction))
            |> Seq.map (fun node -> node.DevName, node.JobParam.TaskDevCount)
            |> dict

        let getDevCount (devName) =
            if callJobDic.ContainsKey(devName)
            then callJobDic.[devName]
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
