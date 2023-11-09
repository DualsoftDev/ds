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
module PPTObjectModule =

    let Objkey(iPage, Id) = $"{iPage}page{Id}"
    let TrimSpace(name:string) = name.TrimStart(' ').TrimEnd(' ')
    let CopyName(name:string, cnt) = sprintf "Copy%d_%s" cnt (name.Replace(".", "_"))

    let GetSysNApi(flowName:string, name:string) =

        if name.Contains '$'
        then failwithf $"not support '$' replace '.' {name}"

        $"{flowName}_{TrimSpace (name.Split('.').[0])}", TrimSpace(name.Split('.').[1])
            
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

    let GetAliasNumber(names:string seq) =
        let usedNames = HashSet<string>()
        seq {

                let Number(testName) =
                    if  names |> Seq.filter (fun name -> name = testName) |> Seq.length = 1
                    then usedNames.Add(testName) |>ignore
                         0
                    else
                        let mutable cnt = 0
                        let mutable copy = testName
                        while usedNames.Contains(copy) do
                            if(cnt > 0)
                            then copy <- CopyName(testName, cnt)
                            cnt <- cnt + 1

                        usedNames.Add(copy) |>ignore
                        cnt

                for name in names do
                    yield name, Number(name)-1
            }

    let nameCheck(shape:Shape, nodeType:NodeType, iPage:int) =
        let name = GetBracketsRemoveName(shape.InnerText) |> trimSpace
        if name.Contains(";")
            then shape.ErrorName(ErrID._18, iPage)

        //REAL other flow 아니면 이름에 '.' 불가
        let checkDotErr() =
            if nodeType <> REALExF && name.Contains(".")
            then  shape.ErrorName(ErrID._19, iPage)

        match nodeType with
        | REALExF     -> if  name.Contains(".")|>not then  shape.ErrorName(ErrID._54, iPage)
        | REALExS     -> if  name.Contains("$")|>not then  shape.ErrorName(ErrID._55, iPage)
        | CALL        -> if  not (name.Contains("$") || name.Contains(".")) then shape.ErrorName(ErrID._56, iPage)

        | OPEN_EXSYS_CALL
        | OPEN_EXSYS_LINK
        | COPY_DEV  ->   let name, number = GetTailNumber(shape.InnerText)
                         if GetSquareBrackets(name, false).IsNone
                         then  shape.ErrorName(ErrID._7, iPage)
                         try
                            GetBracketsRemoveName(name)+".pptx" |> PathManager.getValidFile |> ignore
                         with
                         | ex -> shape.ErrorName(ex.Message, iPage)

        | IF_DEVICE
        | IF_LINK
        | REAL
        | DUMMY
        | BUTTON
        | CONDITION
        | LAMP        -> checkDotErr()

    


    let IsDummyShape(shape:Shape) = shape.IsDashShape() && (shape.CheckRectangle()||shape.CheckEllipse())

    type pptPage(slidePart:SlidePart, iPage:int , bShow:bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle(false)

    type pptNode(shape:Presentation.Shape, iPage:int, pageTitle:string)  =
        let copySystems = Dictionary<string, string>() //copyName, orgiName
        let safeties    = HashSet<string>()
        let jobInfos = Dictionary<string, HashSet<string>>()  // jobBase, api SystemNames
        let btnDefs = Dictionary<string, BtnType>()
        let lampDefs   = Dictionary<string, LampType>()
        let condiDefs   = Dictionary<string, ConditionType>()

        let mutable name = ""
        let mutable ifName    = ""
        let mutable ifTXs    = HashSet<string>()
        let mutable ifRXs    = HashSet<string>()
        let mutable nodeType:NodeType = NodeType.REAL

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
                let copyRows = barckets.Split(';').Select(fun s->s.Trim())
                let copys = copyRows.Select(fun sys -> $"{pageTitle}_{sys}")

                if copys.Distinct().length() <> copys.length()
                then Office.ErrorName(shape, ErrID._33, iPage)

                copys
                |> Seq.iter(fun copy ->
                    copySystems.Add(copy, orgiSysName)
                    jobInfos.Add(copy, [copy]|>HashSet))

        let updateDeviceIF(text:string)      =
            ifName <- GetBracketsRemoveName(text) |> trimSpace
            match GetSquareBrackets(shape.InnerText, false)  with
            |Some txrx ->
                if(txrx.Contains('~'))
                then
                    let txs = (txrx.Split('~')[0])
                    let rxs = (txrx.Split('~')[1])
                    ifTXs  <- txs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet
                    ifRXs  <- rxs.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet
                else
                    shape.ErrorName(ErrID._43, iPage)
            |None ->
                    shape.ErrorName(ErrID._53, iPage)

        let updateLinkIF(text:string)      =
            ifName <- GetBracketsRemoveName(text) |> trimSpace
            let txrx = GetSquareBrackets(shape.InnerText, false)
            if(txrx.IsSome)
            then
                ifTXs  <- txrx.Value.Split(';').Where(fun f->f=""|>not) |> trimStartEndSeq |> Seq.filter(fun f->f="_"|>not) |> HashSet
            else
                shape.ErrorName(ErrID._53, iPage)

        let getBracketItems (name:string) =
            name.Split('[').Where(fun w->w <> "")
            |> Seq.map(fun f-> 
                    match   GetSquareBrackets("["+f, true) with
                    |Some item -> GetBracketsRemoveName("["+f), item
                    |None ->      GetBracketsRemoveName("["+f), ""
                            )

        do

            nodeType <-
                if(shape.CheckRectangle())
                then
                    if   shape.InnerText.Contains(".") then REALExF
                    elif shape.InnerText.Contains("$") then REALExS
                    else REAL
                elif(shape.CheckHomePlate())
                then
                    match GetSquareBrackets(shape.InnerText, false) with
                    |Some text -> if text.Contains("~") then IF_DEVICE else IF_LINK
                    |None ->IF_LINK
                   

                elif(shape.CheckFoldedCornerPlate())
                then OPEN_EXSYS_CALL
                    //if shape.InnerText.Contains("/")
                    //then OPEN_EXSYS_CALL
                    //else OPEN_EXSYS_LINK //test ahn link 통신 처리 대응
                elif(shape.CheckFoldedCornerRound()) then COPY_DEV
                elif(shape.CheckEllipse())           then CALL
                elif(shape.CheckBevelShapePlate())   then LAMP
                elif(shape.CheckBevelShapeRound())   then BUTTON
                elif(shape.CheckCondition())         then CONDITION
                else  shape.ErrorName(ErrID._1, iPage)
            
            if nodeType = CALL
            then name <-  GetHeadBracketRemoveName(shape.InnerText)  |> trimSpace
            else name <-  GetBracketsRemoveName(shape.InnerText)  |> trimSpace

            nameCheck (shape, nodeType, iPage)


            match nodeType with
            |CALL|REAL ->
                     match GetSquareBrackets(shape.InnerText, true) with
                     | Some text ->updateSafety text
                     | None -> ()
            |IF_DEVICE ->   updateDeviceIF  shape.InnerText
            |IF_LINK   ->   updateLinkIF    shape.InnerText
            |OPEN_EXSYS_CALL
            |OPEN_EXSYS_LINK
            |COPY_DEV ->
                     let name, number = GetTailNumber(shape.InnerText)
                     match GetSquareBrackets(name, false) with
                     | Some text -> updateCopySys  (text ,(GetBracketsRemoveName(name) |> trimSpace), number)
                     | None -> ()
                           

            |BUTTON ->    getBracketItems(shape.InnerText).ForEach(fun (n, t) -> btnDefs.Add(n|>TrimSpace, t|> getBtnType))
            |LAMP   ->    getBracketItems(shape.InnerText).ForEach(fun (n, t) -> lampDefs.Add(n|>TrimSpace, t|> getLampType))
            |CONDITION -> getBracketItems(shape.InnerText).ForEach(fun (n, t) -> condiDefs.Add(n|>TrimSpace, t|> getConditionType))
            |REALExF
            |REALExS
            |DUMMY -> ()

        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys  = copySystems
        member x.JobInfos  = jobInfos
        member x.JobCallNames  = jobInfos.Keys
        member x.RealFinished  = shape.IsUnderlined()
        
        member x.Safeties = safeties
        member x.IfName  = ifName
        member x.IfTXs   = ifTXs
        member x.IfRXs   = ifRXs
        member x.NodeType = nodeType
        member x.PageTitle    = pageTitle
        member x.CallDevName = $"{pageTitle}_{name.Split('.')[0]|>trimSpace}"
        member x.CallApiName = 
                    if name.Contains '$'
                    then failwithf $"not support '$' replace '.' {name}"
                    else 
                         let apiName = name.Split('.')[1]|>trimSpace
                         match GetSquareBrackets(apiName, false) with
                         |Some apiType ->
                            match apiType with
                            |""|"_"|"N" -> GetLastBracketRelaceName(apiName, "-")
                            | _-> apiName
                         |None -> 
                            apiName

        member val Id =  shape.GetId()
        member val Key =  Objkey(iPage, shape.GetId())
        member val Name =   name with get, set
        member val NameOrg =   shape.InnerText
        member x.IsAlias :bool   = x.Alias.IsSome
        member val Alias :pptNode  option = None with get, set
        member val AliasNumber :int = 0 with get, set

        member val ButtonDefs = btnDefs
        member val LampDefs   = lampDefs
        member val CondiDefs   = condiDefs

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
        member x.IsInterfaceEdge:bool = x.StartNode.NodeType.IsIF || x.EndNode.NodeType.IsIF
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
        let childSet =  HashSet<pptNode>()
        let nodeNames(nodes :pptNode seq) = nodes.Select(fun s->s.Name).JoinWith(", ")

        do
            let reals  = nodes.Where(fun w -> w.NodeType.IsReal)
            let calls  = nodes.Where(fun w -> w.NodeType.IsCall)

            if(reals.Count() > 1)
            then  Office.ErrorPPT(Group, ErrID._23, $"Reals:{reals|>nodeNames}", iPage, Office.ShapeID(reals.First().Shape))

            parent <-
                if(reals.Any()|>not) then None
                else Some(reals |> Seq.head)

            let children = nodes
                            |> Seq.filter (fun node ->node.NodeType = REAL |> not)

            children |> Seq.iter(fun child -> childSet.Add(child)|>ignore)

        member x.RealKey = sprintf "%d;%s"  iPage (parent.Value.Name)
        member x.PageNum = iPage

        member x.Parent:pptNode option = parent
        member x.Children =  childSet
