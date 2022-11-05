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
module PPTObjectModule =

    let Objkey(iPage, Id) = $"{iPage}page{Id}"
    let TrimSpace(name:string) = name.TrimStart(' ').TrimEnd(' ')
    let CopyName(name:string, cnt) = sprintf "Copy%d_%s" cnt (name.Replace(".", "_")) 


    let GetSysNApi(flowName:string, name:string) = 
        if(name.StartsWith("$"))
            then (TrimSpace(name.Split('.').[0]).TrimStart('$')), name.Split('.').[1]
            else  $"{flowName}_{name.Split('.').[0]}", name.Split('.').[1]
            
    let GetSysNFlow(fileName:string, name:string, pageNum:int) = 
            if(name.StartsWith("$"))
                then 
                    if name.Contains(".")
                    then (TrimSpace(name.Split('.').[0]).TrimStart('$')), name.Split('.').[1]
                    else (TrimSpace(name.TrimStart('$'))), "_"

            elif(name = "")        then fileName, sprintf "Page%d" pageNum
            else                        fileName, TrimSpace(name)
            
    
    ///전체 사용된 화살표 반환 (앞뒤연결 필수)
    let Connections(doc:PresentationDocument) =
                    Office.SildesSkipHide(doc)
                    |> Seq.map (fun slide ->  slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<ConnectionShape>())
                    |> Seq.map(fun (slide, connects) ->
                        slide, connects |> Seq.map(fun conn ->
                            let Id = conn.Descendants<NonVisualDrawingProperties>().First().Id;
                            let startNode = conn.Descendants<NonVisualConnectionShapeProperties>().First().Descendants<StartConnection>().FirstOrDefault()
                            let endNode   = conn.Descendants<NonVisualConnectionShapeProperties>().First().Descendants<EndConnection>().FirstOrDefault()

                            let connStartId =  if(startNode = null) then 0u else startNode.Id.Value
                            let connEndId =  if(endNode = null) then 0u else endNode.Id.Value

                            conn, Id, connStartId, connEndId))

    ///전체 사용된 도형간 그룹지정 정보
    let Groups(doc:PresentationDocument) =
                    Office.SildesSkipHide(doc)
                    |> Seq.filter (fun slide ->  slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>().Any())
                    |> Seq.map (fun slide ->  slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>() |> Seq.toList )

    let rec SubGroup(subG:GroupShape, dicUsedSub:ConcurrentHash<GroupShape>) =
                            subG.Descendants<Presentation.GroupShape>()
                            |> Seq.iter(fun childGroup ->
                                dicUsedSub.TryAdd(childGroup) |>ignore
                                SubGroup(childGroup, dicUsedSub) )

    let IsDummyGroup(subG:GroupShape) =
                    let haveDummy =
                        subG.Descendants<Presentation.Shape>()
                        |> Seq.filter(fun shape -> shape.CheckEllipse()||shape.CheckRectangle())
                        |> Seq.filter(fun shape -> shape.IsDashShape())
                        |> Seq.filter(fun shape -> shape.InnerText.Length = 0)
                        |> Seq.length > 0
                    let haveReal =
                        subG.Descendants<Presentation.Shape>()
                        |> Seq.filter(fun shape -> shape.CheckRectangle())
                        |> Seq.filter(fun shape -> shape.IsDashShape()|>not)
                        |> Seq.filter(fun shape -> shape.InnerText.Length > 0)
                        |> Seq.length > 0
                    haveDummy && haveReal|>not

    let GetCausal(conn:ConnectionShape, iPage, startName, endName) =
                    let shapeProperties = conn.Descendants<ShapeProperties>().FirstOrDefault();
                    let outline = shapeProperties.Descendants<Outline>().FirstOrDefault();
                    let tempHead  = if(outline.Descendants<HeadEnd>().FirstOrDefault() = null)
                                    then LineEndValues.None
                                    else outline.Descendants<HeadEnd>().FirstOrDefault().Type.Value
                    let tempTail  = if(outline.Descendants<TailEnd>().FirstOrDefault() = null)
                                    then LineEndValues.None
                                    else outline.Descendants<TailEnd>().FirstOrDefault().Type.Value

                    let isChangeHead = (tempHead = LineEndValues.None|>not) && (tempTail = LineEndValues.None)

                    let headShape = if(isChangeHead) then tempTail else tempHead
                    let tailShape = if(isChangeHead) then tempHead else tempTail

                    let existHead = headShape = LineEndValues.None|> not
                    let existTail = tailShape = LineEndValues.None|> not

                    let headArrow = headShape = LineEndValues.Triangle || headShape = LineEndValues.Arrow || headShape = LineEndValues.Stealth
                    let tailArrow = tailShape = LineEndValues.Triangle || tailShape = LineEndValues.Arrow || tailShape = LineEndValues.Stealth

                    let dashLine = Office.IsDashLine(conn)

                    let single = outline.CompoundLineType = null || outline.CompoundLineType.Value = CompoundLineValues.Single;
                    let edgeProperties = conn.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault();
                    let connStart = edgeProperties.Descendants<StartConnection>().FirstOrDefault();
                    let connEnd = edgeProperties.Descendants<EndConnection>().FirstOrDefault();


                    //연결오류 찾아서 예외처리
                    if (connStart = null && connEnd = null) then  conn.ErrorConnect(ErrID._4, startName, endName, iPage)
                    if (connStart = null) then  conn.ErrorConnect(ErrID._5, startName, endName, iPage)
                    if (connEnd = null) then  conn.ErrorConnect(ErrID._6, startName, endName, iPage)
                    if (not existHead && not existTail) then  conn.ErrorConnect(ErrID._7, startName, endName, iPage)
                    if (existHead && existTail) 
                    then
                        if(not dashLine)
                        then 
                            if(headArrow && tailArrow) then  conn.ErrorConnect(ErrID._8, startName, endName, iPage)           
                            if((headArrow || tailArrow)|>not)   then  conn.ErrorConnect(ErrID._9, startName, endName, iPage)  
                            if(not headArrow && not tailArrow)  then  conn.ErrorConnect(ErrID._10, startName, endName, iPage) 


                    //인과 타입과 <START, END> 역전여부
                    match existHead, existTail, dashLine with
                    |true, true, true  ->   Interlock , false
                    |true, true, false  ->  if(not headArrow &&  tailArrow) then StartReset , false
                                            else StartReset, true //반대로 뒤집기 필요
                    // dashLine 점선라인, single 한줄라인
                    |_->  match single, tailArrow, dashLine with
                            | true, true, false ->  StartEdge, isChangeHead
                            | false,true, false ->  StartPush, isChangeHead
                            | true, true, true  ->  ResetEdge, isChangeHead
                            | false,true, true  ->  ResetPush, isChangeHead
                            | _ -> conn.ErrorConnect(ErrID._3, startName, endName, iPage)


    let rec ValidGroup(subG:Presentation.GroupShape, shapeIds:ConcurrentHash<uint32>) =
        subG.Descendants<Presentation.Shape>()
        |> Seq.filter(fun shape -> shape.CheckRectangle() || shape.CheckEllipse())
        |> Seq.iter(fun shape -> shapeIds.TryAdd(shape.GetId().Value)|>ignore )
        subG.Descendants<Presentation.GroupShape>()
        |> Seq.iter(fun childGroup ->
            ValidGroup(childGroup, shapeIds) |> ignore)
        true

    let GetAliasName(names:string seq) =
        let usedNames = HashSet<string>()
        seq {

                let newName(testName) =
                    if  names |> Seq.filter (fun name -> name = testName) |> Seq.length = 1
                    then usedNames.Add(testName) |>ignore
                         testName
                    else
                        let mutable cnt = 0
                        let mutable copy = testName
                        while usedNames.Contains(copy) do
                            if(cnt > 0)
                            then copy <- CopyName(testName, cnt)
                            cnt <- cnt + 1

                        usedNames.Add(copy) |>ignore
                        copy

                for name in names do
                    yield name, newName(name)
            }


    type pptPage(slidePart:SlidePart, iPage:int , bShow:bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape:Presentation.Shape, iPage:int, dashOutline:bool , sildeSize, pageTitle:string)  =
        let mutable txCnt = 1
        let mutable rxCnt = 1
        let mutable name = ""
        let mutable copySystems = ConcurrentDictionary<string, string>() //copyName, orgiName
        let mutable safeties    = HashSet<string>()
        let mutable ifName    = ""
        let mutable ifTXs    = HashSet<string>()
        let mutable ifRXs    = HashSet<string>()
        let mutable bEmg = false
        let mutable bAuto = false
        let mutable bStart = false
        let mutable bReset = false
        let mutable nodeType:NodeType = NodeType.MY
        let trimStartEnd(text:string) =   text.TrimStart(' ').TrimEnd(' ')
        let trimStartEndSeq(texts:string seq) =  texts  |> Seq.map(fun name -> trimStartEnd name)
        let updateTxRx(tailBarckets:string) =
                if(tailBarckets.Split(';').Count() > 1)
                then
                    txCnt <- tailBarckets.Split(';').[0] |> Convert.ToInt32
                    rxCnt <- tailBarckets.Split(';').[1] |> Convert.ToInt32
                else 
                    shape.ErrorName(ErrID._22, iPage)

        let updateSafety(barckets:string)  = safeties <- barckets.Split(';')  |> HashSet
                                            //             |> Seq.map(fun name -> $"{pageTitle}_{name}") |> HashSet
        let updateCopySys(barckets:string, orgiSysName:string) =
            if  (trimStartEnd barckets).All(fun c -> Char.IsDigit(c))
            then
                [for i in [1..Convert.ToInt32(barckets)] do yield sprintf "%s%d" name i]
                |> Seq.map(fun sys -> $"{pageTitle}_{sys}" , orgiSysName)
                |> Seq.iter(fun (copy, orgi) -> copySystems.TryAdd(copy, orgiSysName) |> ignore)

            else
                barckets.Split(';') |> trimStartEndSeq
                |> Seq.map(fun sys -> $"{pageTitle}_{sys}" , orgiSysName)
                |> Seq.iter(fun (copy, orgi) -> copySystems.TryAdd(copy, orgiSysName) |> ignore)

        let updateIF(text:string)      =
            ifName <- GetBracketsReplaceName(text) |> trimStartEnd
            let txrx = GetSquareBrackets(shape.InnerText, false)
            if(txrx.Contains('~'))
            then
                let txs = (txrx.Split('~')[0])
                let rxs = (txrx.Split('~')[1])
                ifTXs  <- txs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet
                ifRXs  <- rxs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet

        do
            nodeType <-
                if(shape.CheckRectangle())      then  MY
                elif(shape.CheckHomePlate())    then  IF
                elif(shape.CheckFoldedCorner()) then  COPY
                elif(shape.CheckDonutShape()
                    || shape.CheckBlockArc()
                    || shape.CheckNoSmoking()
                    || shape.CheckBevelShape()) then  BUTTON
                elif(shape.CheckEllipse())
                then
                    if(dashOutline)
                    then DUMMY
                    else
                        if((txCnt = 0 && rxCnt = 0) || txCnt < 0 || rxCnt < 0)
                        then shape.ErrorName(ErrID._2, iPage)
                        else 
                            if (txCnt > 0 && rxCnt > 0) then TR
                            elif (txCnt = 0) then RX
                            elif (rxCnt = 0) then TX
                            else shape.ErrorName(ErrID._2, iPage)

                else  shape.ErrorName(ErrID._1, iPage)

            name <-  GetBracketsReplaceName(shape.InnerText)
            match nodeType with
            |TX|RX|TR|MY ->
                     if(nodeType =MY|>not) 
                     then GetSquareBrackets(shape.InnerText, false) |> fun text -> if text = ""|>not then updateTxRx text
                     GetSquareBrackets(shape.InnerText, true )      |> fun text -> if text = ""|>not then updateSafety text
            |IF ->   updateIF shape.InnerText
            |COPY -> GetSquareBrackets(shape.InnerText, false)
                        |> fun text ->
                            if text = ""|>not
                            then updateCopySys  (text ,(GetBracketsReplaceName(shape.InnerText) |> trimStartEnd))
            |_ -> ()

            bEmg    <- shape.CheckNoSmoking()
            bStart  <- false//not use shape.CheckBlockArc()
            bAuto   <- shape.CheckDonutShape()
            bReset  <- shape.CheckBevelShape()

        member x.PageNum = iPage
        member x.Shape = shape
        member x.DashOutline = dashOutline
        member x.Safeties = safeties
        member x.CopySys  = copySystems
        member x.IfName      = ifName
        member x.IfTXs       = ifTXs
        member x.IfRXs       = ifRXs
        member x.IsEmgBtn = bEmg
        member x.IsAutoBtn= bAuto
        member x.IsStartBtn = bStart
        member x.IsResetBtn = bReset
        member x.NodeType = nodeType
        member x.IsDummy  = nodeType = DUMMY
        member x.PageTitle    = pageTitle
        member x.IsAlias :bool   = x.Alias.IsSome

        member val Id =  shape.GetId()
        member val Key =  Objkey(iPage, shape.GetId())
        member val Name =   name with get, set
        member val NameOrg =   shape.InnerText
        member val CntTX =  txCnt
        member val CntRX =  rxCnt
        member val Alias :pptNode  option = None with get, set
        member val Rectangle :System.Drawing.Rectangle =   shape.GetPosition(sildeSize)

    and
        pptEdge(conn:Presentation.ConnectionShape,  iEdge:UInt32Value, iPage:int ,startId:uint32, endId:uint32, nodes:ConcurrentDictionary<string, pptNode>) =
        let mutable reverse = false
        let mutable causal:ModelingEdgeType = ModelingEdgeType.StartEdge
        let startKey = Objkey(iPage, startId)
        let endKey   = Objkey(iPage, endId)
        let startNode = nodes.[startKey]
        let endNode   = nodes.[endKey]
        do
            if conn.IsOutlineConnectionExist()|>not
            then conn.ErrorConnect(ErrID._40, nodes.[startKey].Name, nodes.[endKey].Name, iPage)
            GetCausal(conn, iPage, nodes.[startKey].Name, nodes.[endKey].Name) 
            |> fun(c, r) -> causal <- c;reverse <- r

        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.StartNode:pptNode = if(reverse) then endNode else startNode
        member x.EndNode:pptNode =   if(reverse) then startNode else endNode
        member x.ParentId = 0 //reserve


        member val Name =  conn.EdgeName()
        member val Key =  Objkey(iPage, iEdge)
        member x.Text =
                            let sName = if startNode.Alias.IsSome then  startNode.Alias.Value.Name else startNode.Name
                            let eName = if endNode.Alias.IsSome   then  endNode.Alias.Value.Name   else endNode.Name
                            if(reverse)
                            then $"{iPage};{eName}{causal.ToText()}{sName}";
                            else $"{iPage};{sName}{causal.ToText()}{eName}";

        member val Causal:ModelingEdgeType = causal

    and
        pptGroup(iPage:int, ids:uint32 seq, nodes:ConcurrentDictionary<string, pptNode>) =
        let mutable parent:pptNode option = None
        let mutable dummy :pptNode option = None
        let childSet =  ConcurrentHash<pptNode>()

        do
            let parents =
                ids
                |> Seq.map (fun id -> nodes.[ Objkey(iPage, id) ])
                |> Seq.filter (fun node -> node.NodeType = MY)
            if(parents.Count() > 1) 
            then  Office.ErrorPPT(Group, ErrID._23, $"부모수:{parents.Count()}", iPage)
            let dummys = 
                ids 
                |> Seq.map (fun id -> nodes.[ Objkey(iPage, id) ])
                |> Seq.filter (fun node -> node.NodeType = DUMMY)
            if(parents.Count() = 0 && dummys.Count() > 1) 
            then  Office.ErrorPPT(Group, ErrID._24, $"부모수:{dummys.Count()}", iPage)
            if(parents.Count() = 0 && dummys.Count() = 0 ) 
            then  Office.ErrorPPT(Group, ErrID._25, $"도형 타입확인", iPage)

            parent <-
                if(parents.Any()|>not) then None
                else Some(parents |> Seq.head)


            dummy <-
                if(dummys.Any()|>not) then None
                else Some(dummys |> Seq.head)

            let children =
                ids
                |> Seq.map (fun id -> nodes.[Objkey(iPage, id) ])
                |> Seq.filter (fun node ->node.NodeType = MY |> not)
                |> Seq.filter (fun node ->(node.NodeType = DUMMY|>not ||  parent.IsSome))

            if(children.Any() |> not) 
            then  Office.ErrorPPT(Group, ErrID._12, $"자식수:0", iPage)
         

            children |> Seq.iter(fun child -> childSet.TryAdd(child)|>ignore)

        member x.RealKey = sprintf "%d;%s"  iPage (parent.Value.Name)
        member x.PageNum = iPage

        member x.Parent:pptNode option = parent
        member x.DummyParent:pptNode option = dummy
        member x.Children =  childSet.Values 
        
    type pptDoc(path:string)  =
        let doc = Office.Open(path)
        let name =  let fileName = Path.GetFileNameWithoutExtension(path)
                    if fileName.IsQuotationRequired()
                    then Office.ErrorPPT(ErrorCase.Name, ErrID._44, $"SystemNamePath : {path}", 0, $"SystemName : {fileName}")
                    fileName
        let pages =  ConcurrentDictionary<SlidePart, pptPage>()
        let masterPages =  ConcurrentDictionary<int, DocumentFormat.OpenXml.Presentation.SlideMaster>()
        let nodes =  ConcurrentDictionary<string, pptNode>()
        let parents = ConcurrentDictionary<pptNode, seq<pptNode>>()
        let edges =  ConcurrentHash<pptEdge>()
        let updateAliasPPT () =
            let pptNodes = nodes.Values
            let dicFlowNodes = pages.Values
                                |> Seq.filter(fun page -> page.IsUsing)
                                |> Seq.map  (fun page ->
                                    page.PageNum, pptNodes |> Seq.filter(fun node -> node.PageNum = page.PageNum)
                                    ) |> dict

            let settingAlias(nodes:pptNode seq) =
                let nodes = nodes.OrderByDescending(fun o-> parents.ContainsKey(o))  //부모지정
                let names  = nodes |> Seq.map(fun f->f.Name)
                (nodes, GetAliasName(names))
                ||> Seq.map2(fun node  nameSet -> node,  nameSet)
                |> Seq.iter(fun (node, (name, newName)) ->
                                if(name  = newName |> not)
                                then    let orgNode = nodes |> Seq.filter(fun f->f.Name = name) |> Seq.head
                                        node.Alias <- Some(orgNode)
                                        node.Name  <- newName
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

        do
            let sildesAll = Office.SildesAll(doc)
            let shapes = Office.PageShapes(doc)
            let connections = Connections(doc)
            let groups = Groups(doc)
            let allGroups =  groups |> Seq.collect(fun (slide, groupSet) -> groupSet)
            let sildeSize = Office.SildeSize(doc)
            let sildeMasters = Office.SildesMasterAll(doc)

            try

                sildeMasters
                |> Seq.iter (fun slideMaster ->
                    masterPages.TryAdd(masterPages.Count+1, slideMaster) |>ignore )

                sildesAll
                |> Seq.iter (fun (slidePart, show, page) ->
                    pages.TryAdd(slidePart, pptPage(slidePart, page, show)) |>ignore )

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

                            let node = pptNode(shape, page,  isDash,  sildeSize, flowName)
                            if(node.Name ="" && node.NodeType = DUMMY|>not) then shape.ErrorName(ErrID._13, page)
                            nodes.TryAdd(node.Key, node)  |>ignore )

                let dicFakeSub = ConcurrentHash<Presentation.GroupShape>()
                allGroups |> Seq.iter (fun group -> SubGroup(group, dicFakeSub))

                let dicParentCheck = ConcurrentDictionary<string, int>()
                let makeRealGroup ( pptGroup:pptGroup ) =
                    if(pptGroup.Parent.IsNone)
                    then Office.ErrorPPT(Group, ErrID._18, "", pptGroup.PageNum)
                    else 
                        let parent = pptGroup.Parent.Value;
                        if(dicParentCheck.TryAdd(pptGroup.RealKey, pptGroup.PageNum))
                        then
                            parents.TryAdd(parent, pptGroup.Children)|>ignore
                        else 
                            Office.ErrorPPT(Group, ErrID._17, $"{dicParentCheck.[pptGroup.RealKey]}-{parent.Name}", pptGroup.PageNum) 
                
                let makeDummyGroup ( pptGroup:pptGroup ) =
                        if(pptGroup.DummyParent.IsSome)
                        then
                            parents.TryAdd(pptGroup.DummyParent.Value, pptGroup.Children)|>ignore

                groups
                |> Seq.iter (fun (slide, groupSet) ->
                    groupSet
                    |> Seq.iter (fun group ->
                        let shapeIds = ConcurrentHash<uint32>()
                        ValidGroup(group, shapeIds) |> ignore

                        if(IsDummyGroup(group))
                        then makeDummyGroup(pptGroup(pages.[slide].PageNum, shapeIds.Values, nodes))
                        else
                            if(dicFakeSub.ContainsKey(group)|>not)
                            then makeRealGroup (pptGroup(pages.[slide].PageNum, shapeIds.Values, nodes))
                    )
                )

                connections
                |> Seq.iter (fun (slide, conns) ->
                    conns
                          |> Seq.iter (fun (conn, Id, startId, endId) ->
                            let iPage = pages.[slide].PageNum
                            let nodeName =
                                if(nodes.ContainsKey(Objkey(iPage, endId)))
                                then nodes.[Objkey(iPage, endId)].Name else ""

                            if(startId = 0u && endId = 0u) then  conn.ErrorConnect(ErrID._4, "","", iPage)
                            if(nodes.ContainsKey(Objkey(iPage, startId))|>not) then  conn.ErrorConnect(ErrID._14, "",$"{nodeName}", iPage) 
                            if(nodes.ContainsKey(Objkey(iPage, endId))|>not)   then  conn.ErrorConnect(ErrID._14, $"{nodeName}", "", iPage) 
                            if(startId = 0u) then  conn.ErrorConnect(ErrID._15, "", $"{nodeName}", iPage) 
                            if(endId = 0u) then    conn.ErrorConnect(ErrID._16, $"{nodeName}", "", iPage) 
                            edges.TryAdd(pptEdge(conn, Id, iPage ,startId, endId,  nodes)) |>ignore 
                        ))


                updateAliasPPT()
                doc.Close()

            with ex -> doc.Close()
                       failwithf  $"{ex.Message}"


        member x.GetPage(pageNum:int) = pages.Values |> Seq.filter(fun p -> p.PageNum = pageNum) |> Seq.head
        member val Pages = pages.Values.OrderBy(fun p -> p.PageNum)

        member val Nodes = nodes.Values.OrderBy(fun p -> p.PageNum)
        member val DicNodes = nodes
        member val Edges = edges.Values.OrderBy(fun p -> p.PageNum)
        member val Parents = parents

        member val Name =  name
        member val FullPath =  path


