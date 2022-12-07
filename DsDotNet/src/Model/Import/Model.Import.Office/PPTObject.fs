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
        $"{flowName}_{TrimSpace (name.Split('$').[0])}", TrimSpace(name.Split('$').[1])
    let GetSysNFlow(fileName:string, name:string, pageNum:int) = 
            if(name.StartsWith("$"))
                then 
                    if name.Contains(".")
                    then (TrimSpace(name.Split('.').[0]).TrimStart('$')), TrimSpace(name.Split('.').[1])
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
    let IsDummyShape(shape:Shape) = shape.IsDashShape() && (shape.CheckRectangle()||shape.CheckEllipse())

    type pptPage(slidePart:SlidePart, iPage:int , bShow:bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape:Presentation.Shape, iPage:int, pageTitle:string, isDummy:bool)  =
        let copySystems = Dictionary<string, string>() //copyName, orgiName
        let safeties    = HashSet<string>()
        let jobInfos = Dictionary<string, HashSet<string>>()  // jobBase, api SystemNames
        
        let mutable name = ""
        let mutable ifName    = ""
        let mutable ifTXs    = HashSet<string>()
        let mutable ifRXs    = HashSet<string>()
        let mutable nodeType:NodeType = NodeType.REAL
        let mutable btnType:BtnType option = None 

        let trimSpace(text:string) =   text.TrimStart(' ').TrimEnd(' ')
        let trimStartEndSeq(texts:string seq) =  texts  |> Seq.map(fun name -> trimSpace name)
        let updateSafety(barckets:string)  = barckets.Split(';') |> Seq.iter(fun f-> safeties.Add (f) |> ignore )
        let updateCopySys(barckets:string, orgiSysName:string, groupJob:int) =
            
            if  (groupJob > 0)
            then
                let jobBaseName = $"{pageTitle}_{barckets}" //jobBaseName + apiName = JobName
                jobInfos.Add(jobBaseName , HashSet<string>())
                let copys = 
                    [for i in [1..groupJob] do 
                        yield sprintf "%s%d" jobBaseName i]
                
                copys
                |> Seq.iter(fun copy ->
                    copySystems.Add(copy, orgiSysName) 
                    jobInfos[jobBaseName].Add(copy)|>ignore)

            else
                let copys = 
                    barckets.Split(';') 
                    |> trimStartEndSeq
                    |> Seq.map(fun sys -> $"{pageTitle}_{sys}")

                copys
                |> Seq.iter(fun copy -> 
                    copySystems.Add(copy, orgiSysName)
                    jobInfos.Add(copy, [copy]|>HashSet))

        let updateIF(text:string)      =
            ifName <- GetBracketsReplaceName(text) |> trimSpace
            let txrx = GetSquareBrackets(shape.InnerText, false)
            if(txrx.Contains('~'))
            then
                let txs = (txrx.Split('~')[0])
                let rxs = (txrx.Split('~')[1])
                ifTXs  <- txs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet
                ifRXs  <- rxs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet

        do
            name <-  GetBracketsReplaceName(shape.InnerText)  |> trimSpace
            nodeType <-
                if isDummy then DUMMY

                elif(shape.CheckRectangle())    then  REAL
                elif(shape.CheckHomePlate())    then  IF
                elif(shape.CheckFoldedCorner()) then  COPY

                elif(shape.CheckDonutShape()
                    || shape.CheckBlockArc()
                    || shape.CheckNoSmoking()
                    || shape.CheckBevelShape()) then  BUTTON

                elif(shape.CheckEllipse())
                then CALL
                    //if(name.Split('.').Count() <> 2)
                    //then shape.ErrorName(ErrID._46, iPage)
                    //if((txCnt = 0 && rxCnt = 0) || txCnt < 0 || rxCnt < 0)
                    //then shape.ErrorName(ErrID._2, iPage)
                    //else 
                    //    if (txCnt > 0 && rxCnt > 0) then TR
                    //    elif (txCnt = 0) then RX
                    //    elif (rxCnt = 0) then TX
                    //    else shape.ErrorName(ErrID._2, iPage)

                else  shape.ErrorName(ErrID._1, iPage)

            match nodeType with
            |CALL|REAL ->
                     //if(nodeType =MY|>not) 
                     //then GetSquareBrackets(shape.InnerText, false) |> fun text -> if text = ""|>not then updateTxRx text
                     GetSquareBrackets(shape.InnerText, true )      |> fun text -> if text = ""|>not then updateSafety text
            |IF ->   updateIF shape.InnerText
            |COPY -> 
                     let name, number = GetTailNumber(shape.InnerText)
                     GetSquareBrackets(name, false)
                        |> fun text -> 
                            updateCopySys  (text ,(GetBracketsReplaceName(name) |> trimSpace), number)
            |_ -> ()

            let btn =  if shape.CheckNoSmoking() then Some(BtnType.EmergencyBTN)
                       elif shape.CheckDonutShape() then Some(BtnType.AutoBTN)
                       elif shape.CheckBevelShape() then Some(BtnType.ResetBTN)
                       else None
            btnType    <- btn
               

        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys  = copySystems
        member x.JobInfos  = jobInfos
        member x.Safeties = safeties
        member x.IfName      = ifName
        member x.IfTXs       = ifTXs
        member x.IfRXs       = ifRXs
        member x.BtnType = btnType
        member x.NodeType = nodeType
        member x.IsDummy  = nodeType = DUMMY
        member x.PageTitle    = pageTitle
        member x.IsAlias :bool   = x.Alias.IsSome

        member val Id =  shape.GetId()
        member val Key =  Objkey(iPage, shape.GetId())
        member val Name =   name with get, set
        member val NameOrg =   shape.InnerText
        member val Alias :pptNode  option = None with get, set
        member x.GetRectangle (sildeSize:int*int) =  shape.GetPosition(sildeSize)

    and
        pptEdge(conn:Presentation.ConnectionShape,  iEdge:UInt32Value, iPage:int ,sNode:pptNode, eNode:pptNode) =
        let mutable reverse = false
        let mutable causal:ModelingEdgeType = ModelingEdgeType.StartEdge
        do
            
            GetCausal(conn, iPage, sNode.Name, eNode.Name) 
            |> fun(c, r) -> causal <- c;reverse <- r

        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.StartNode:pptNode = if(reverse) then eNode else sNode
        member x.EndNode:pptNode =   if(reverse) then sNode else eNode
        member x.ParentId = 0 //reserve


        member val Name =  conn.EdgeName()
        member val Key =  Objkey(iPage, iEdge)
        member x.Text =
                            let sName = if sNode.Alias.IsSome then  sNode.Alias.Value.Name else sNode.Name
                            let eName = if eNode.Alias.IsSome then  eNode.Alias.Value.Name else eNode.Name
                            if(reverse)
                            then $"{iPage};{eName}{causal.ToText()}{sName}";
                            else $"{iPage};{sName}{causal.ToText()}{eName}";

        member val Causal:ModelingEdgeType = causal
    and
        pptRealGroup(iPage:int,  nodes: pptNode seq) =
        let mutable parent:pptNode option = None
        let childSet =  ConcurrentHash<pptNode>()
        let nodeNames(nodes :pptNode seq) = nodes.Select(fun s->s.Name).JoinWith(", ")

        do
            let reals  = nodes.Where(fun w -> w.NodeType.IsReal)
            let calls  = nodes.Where(fun w -> w.NodeType.IsCall)

            if(reals.Count() > 1) 
            then  Office.ErrorPPT(Group, ErrID._23, $"Reals:{reals|>nodeNames}", iPage)

            if(reals.Count() = 0 ) 
            then  Office.ErrorPPT(Group, ErrID._25, $"Nodes:{nodes|>nodeNames}", iPage)

            parent <-
                if(reals.Any()|>not) then None
                else Some(reals |> Seq.head)

            let children = nodes
                            |> Seq.filter (fun node ->node.NodeType = REAL |> not)
                            |> Seq.filter (fun node ->node.NodeType = DUMMY|>not)

            if(children.Any() |> not) 
            then  Office.ErrorPPT(Group, ErrID._22,  $"Nodes:{nodes|>nodeNames}", iPage)

            children |> Seq.iter(fun child -> childSet.TryAdd(child)|>ignore)

        member x.RealKey = sprintf "%d;%s"  iPage (parent.Value.Name)
        member x.PageNum = iPage

        member x.Parent:pptNode option = parent
        member x.Children =  childSet.Values 
    and
        pptDummyGroup(iPage:int, nodes: pptNode seq) =
        let mutable dummy :pptNode option = None
        let childSet =  ConcurrentHash<pptNode>()
        let nodeNames(nodes :pptNode seq) = nodes.Select(fun s->s.Name).JoinWith(", ")

        do
            let reals  = nodes.Where(fun w -> w.NodeType.IsReal)
            let calls  = nodes.Where(fun w -> w.NodeType.IsCall)
            let dummys = nodes.Where(fun w -> w.NodeType = DUMMY)

            //1 child 는 dummy로 구성 불가
            if dummys.Count() = 1 &&  reals.Count() = 1 && calls.Count() = 0  
            then Office.ErrorPPT(Group, ErrID._19, $"{reals.First().Name}", nodes.First().PageNum) 
            if dummys.Count() = 1 &&  reals.Count() = 0 && calls.Count() = 1  
            then Office.ErrorPPT(Group, ErrID._19, $"{calls.First().Name}", nodes.First().PageNum) 


            if(dummys.Count() > 1) 
            then  Office.ErrorPPT(Group, ErrID._24, $"부모수:{dummys.Count()}", iPage)

            if(reals.Count() = 0 && dummys.Count() = 0 ) 
            then  Office.ErrorPPT(Group, ErrID._25, $"도형 타입확인", iPage)

           
            dummy <-
                if(dummys.Any()|>not) then None
                else Some(dummys |> Seq.head)

            let children = nodes  |> Seq.filter (fun node ->node.NodeType = DUMMY|>not)
            
            if(children.Any() |> not) 
            then  Office.ErrorPPT(Group, ErrID._12, $"자식수:0", iPage)

            children |> Seq.iter(fun child -> childSet.TryAdd(child)|>ignore)

        member x.PageNum = iPage

        member x.DummyParent:pptNode option = dummy
        member x.Children =  childSet.Values 
        