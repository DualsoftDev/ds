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

    let Objkey (iPage, Id) = $"{iPage}page{Id}"
    let TrimSpace (name: string) = name.TrimStart(' ').TrimEnd(' ')

    let CopyName (name: string, cnt) =
        sprintf "Copy%d_%s" cnt (name.Replace(".", "_"))

    let GetSysNApi (flowName: string, name: string) =

        if name.Contains '$' then
            failwithf $"not support '$' replace '.' {name}"

        $"{flowName}_{TrimSpace(name.Split('.').[0])}",   GetBracketsRemoveName(TrimSpace(name.Split('.').[1]))

    let GetSysNFlow (fileName: string, name: string, pageNum: int) =
        if (name.StartsWith("$")) then
            if name.Contains(".") then
                (TrimSpace(name.Split('.').[0]).TrimStart('$')), TrimSpace(name.Split('.').[1])
            else
                (TrimSpace(name.TrimStart('$'))), "_"

        elif (name = "") then
            fileName, sprintf "Page%d" pageNum
        else
            fileName, TrimSpace(name)

    ///전체 사용된 화살표 반환 (앞뒤연결 필수)
    let Connections (doc: PresentationDocument) =
        Office.SlidesSkipHide(doc)
        |> Seq.map (fun (slide,_) -> slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<ConnectionShape>())
        |> Seq.map (fun (slide, connects) ->
            slide,
            connects
            |> Seq.map (fun conn ->
                let Id = conn.Descendants<NonVisualDrawingProperties>().First().Id

                let startNode =
                    conn
                        .Descendants<NonVisualConnectionShapeProperties>()
                        .First()
                        .Descendants<StartConnection>()
                        .FirstOrDefault()

                let endNode =
                    conn
                        .Descendants<NonVisualConnectionShapeProperties>()
                        .First()
                        .Descendants<EndConnection>()
                        .FirstOrDefault()

                let connStartId = if (startNode = null) then 0u else startNode.Id.Value
                let connEndId = if (endNode = null) then 0u else endNode.Id.Value

                conn, Id, connStartId, connEndId))

    ///전체 사용된 도형간 그룹지정 정보
    let Groups (doc: PresentationDocument) =
        Office.SlidesSkipHide(doc)
        |> Seq.filter (fun (slide,_) -> slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>().Any())
        |> Seq.map (fun (slide,_) -> slide, slide.Slide.CommonSlideData.ShapeTree.Descendants<GroupShape>() |> Seq.toList)

    let GetCausal (conn: ConnectionShape, iPage, startName, endName) =
        let shapeProperties = conn.Descendants<ShapeProperties>().FirstOrDefault()
        let outline = shapeProperties.Descendants<Outline>().FirstOrDefault()
        let tempHead, tempTail = outline.getConnectionHeadTail()

        let isChangeHead =
            (tempHead = LineEndValues.None |> not) && (tempTail = LineEndValues.None)

        let headShape = if (isChangeHead) then tempTail else tempHead
        let tailShape = if (isChangeHead) then tempHead else tempTail

        let existHead = headShape = LineEndValues.None |> not
        let existTail = tailShape = LineEndValues.None |> not

        let headArrow =
            headShape = LineEndValues.Triangle
            || headShape = LineEndValues.Arrow
            || headShape = LineEndValues.Stealth

        let tailArrow =
            tailShape = LineEndValues.Triangle
            || tailShape = LineEndValues.Arrow
            || tailShape = LineEndValues.Stealth

        let dashLine = Office.IsDashLine(conn)

        let single =
            outline.CompoundLineType = null
            || outline.CompoundLineType.Value = CompoundLineValues.Single

        let edgeProperties =
            conn.Descendants<NonVisualConnectionShapeProperties>().FirstOrDefault()

        let connStart = edgeProperties.Descendants<StartConnection>().FirstOrDefault()
        let connEnd = edgeProperties.Descendants<EndConnection>().FirstOrDefault()


        //연결오류 찾아서 예외처리
        if (connStart = null && connEnd = null) then
            conn.ErrorConnect(ErrID._4, startName, endName, iPage)

        if (connStart = null) then
            conn.ErrorConnect(ErrID._5, startName, endName, iPage)

        if (connEnd = null) then
            conn.ErrorConnect(ErrID._6, startName, endName, iPage)

        if (existHead && existTail) then
            if (not dashLine) then
                if (headArrow && tailArrow) then
                    conn.ErrorConnect(ErrID._8, startName, endName, iPage)

                if ((headArrow || tailArrow) |> not) then
                    conn.ErrorConnect(ErrID._9, startName, endName, iPage)

                if (not headArrow && not tailArrow) then
                    conn.ErrorConnect(ErrID._10, startName, endName, iPage)


        //인과 타입과 <START, END> 역전여부
        match existHead, existTail, dashLine with
        | true, true, true -> InterlockWeak, false
        | true, true, false ->
            if (not headArrow && tailArrow) then
                StartReset, false
            else
                StartReset, true //반대로 뒤집기 필요
        // dashLine 점선라인, single 한줄라인
        | _ ->
            match single, tailArrow, dashLine with
            | true, true, false -> StartEdge, isChangeHead
            //| false, true, false -> StartPush, isChangeHead //강연결 사용안함 24/03.08
            | false, true, false -> StartEdge, isChangeHead 
            | true, true, true -> ResetEdge, isChangeHead
            //| false, true, true -> ResetPush, isChangeHead   //강연결 사용안함 24/03.08
            | false, true, true -> ResetEdge, isChangeHead
            | _ -> conn.ErrorConnect(ErrID._3, startName, endName, iPage)

    let GetAliasNumber (names: string seq) =
        let usedNames = HashSet<string>()

        seq {

            let Number (testName) =
                if names |> Seq.filter (fun name -> name = testName) |> Seq.length = 1 then
                    usedNames.Add(testName) |> ignore
                    0
                else
                    let mutable cnt = 0
                    let mutable copy = testName

                    while usedNames.Contains(copy) do
                        if (cnt > 0) then
                            copy <- CopyName(testName, cnt)

                        cnt <- cnt + 1

                    usedNames.Add(copy) |> ignore
                    cnt

            for name in names do
                yield name, Number(name) - 1
        }

    let nameCheck (shape: Shape, nodeType: NodeType, iPage: int) =
        let name = GetBracketsRemoveName(shape.InnerText) |> trimSpace

        if name.Contains(";") then
            shape.ErrorName(ErrID._18, iPage)

        //REAL other flow 아니면 이름에 '.' 불가
        let checkDotErr () =
            if nodeType <> REALExF && name.Contains(".") then
                            shape.ErrorName(ErrID._19, iPage)
        let checkSafetyErr() =
            match GetSquareBrackets(shape.InnerText, true) with
                | Some text -> //safety 체크
                        text.Split(';').Iter(fun f->
                             if not(f.Contains("."))
                             then 
                                shape.ErrorName(ErrID._68, iPage)
                        )
                        
                | None -> ()

        match nodeType with
        | REAL -> checkSafetyErr();checkDotErr();
        | REALExF ->
            if name.Contains(".") |> not then
                shape.ErrorName(ErrID._54, iPage)
            checkSafetyErr();
        | CALL ->
            if not (name.Contains(".")) then
                shape.ErrorName(ErrID._56, iPage)
            checkSafetyErr();

        | OPEN_EXSYS_CALL
        | OPEN_EXSYS_LINK
        | COPY_DEV ->
            let name, number = GetTailNumber(shape.InnerText)

            if GetSquareBrackets(name, false).IsNone then
                shape.ErrorName(ErrID._7, iPage)

            try
                GetBracketsRemoveName(name) + ".pptx" |> PathManager.getValidFile |> ignore
            with ex ->
                shape.ErrorName(ex.Message, iPage)

        |  CALLCMDFunc -> 
            let nameNFunc = GetBracketsRemoveName(shape.InnerText) 
            let namePure = GetLastParenthesesReplaceName(nameNFunc, "")
            if not(namePure.Contains(".")) &&  namePure <> nameNFunc  // ok :  dev.api(10,403)[XX]  err : dev(10,403)[XX] 순수CMD 호출은 속성입력 금지
            then
                shape.ErrorName(ErrID._70, iPage)
                
        | CALLOPFunc -> ()
        | IF_DEVICE
        | IF_LINK
        | DUMMY
        | BUTTON
        | CONDITION
        | LAYOUT
        | LAMP -> checkDotErr()




    let IsDummyShape (shape: Shape) =
        shape.IsDashShape() && (shape.CheckRectangle() || shape.CheckEllipse())

    type pptPage(slidePart: SlidePart, iPage: int, bShow: bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape: Presentation.Shape, iPage: int, pageTitle: string, slieSize: int * int,  isHeadPage:bool) =
        let copySystems = Dictionary<string, string>() //copyName, orgiName
        let safeties = HashSet<string>()
        let jobInfos = Dictionary<string, HashSet<string>>() // jobBase, api SystemNames
        let btnHeadPageDefs = Dictionary<string, BtnType>()
        let btnDefs = Dictionary<string, BtnType>()
        let lampHeadPageDefs = Dictionary<string, LampType>()
        let lampDefs = Dictionary<string, LampType>()
        let condiHeadPageDefs = Dictionary<string, ConditionType>()
        let condiDefs = Dictionary<string, ConditionType>()

        let mutable name = ""
        let mutable ifName = ""
        let mutable opDevParam:DevParam option = None
        let mutable cmdDevParam:(DevParam*DevParam) option = None
        let mutable ifTXs = HashSet<string>()
        let mutable ifRXs = HashSet<string>()
        let mutable nodeType: NodeType = NodeType.REAL

        let trimNewLine (text: string) = text.Replace("\n", "")
        let trimSpace (text: string) = text.TrimStart(' ').TrimEnd(' ')
        let trimSpaceNewLine (text: string) = text |> trimSpace |> trimNewLine

        let trimStartEndSeq (texts: string seq) = texts |> Seq.map trimSpace

        let updateSafety (barckets: string) =
            barckets.Split(';')
                |> Seq.iter (fun f ->
                        safeties.Add(f) |> ignore
                    )

        let updateCopySys (barckets: string, orgiSysName: string, groupJob: int) =
            if (groupJob > 0) then
                shape.ErrorName(ErrID._19, iPage)
            else
                let copyRows = barckets.Split(';').Select(fun s -> s.Trim())
                let copys = copyRows.Select(fun sys -> $"{pageTitle}_{sys}")

                if copys.Distinct().length () <> copys.length () then
                    Office.ErrorName(shape, ErrID._33, iPage)

                copys
                |> Seq.iter (fun copy ->
                    copySystems.Add(copy, orgiSysName)
                    jobInfos.Add(copy, [ copy ] |> HashSet))

        let updateDeviceIF (text: string) =
            ifName <- GetBracketsRemoveName(text) |> trimSpace |> trimNewLine

            match GetSquareBrackets(shape.InnerText, false) with
            | Some txrx ->
                if (txrx.Contains('~')) then
                    let txs = (txrx.Split('~')[0])
                    let rxs = (txrx.Split('~')[1])

                    ifTXs <-
                        txs.Split(';').Where(fun f -> f = "" |> not)
                        |> trimStartEndSeq
                        |> Seq.filter (fun f -> f = "_" |> not)
                        |> HashSet

                    ifRXs <-
                        rxs.Split(';').Where(fun f -> f = "" |> not)
                        |> trimStartEndSeq
                        |> Seq.filter (fun f -> f = "_" |> not)
                        |> HashSet
                else
                    shape.ErrorName(ErrID._43, iPage)
            | None -> shape.ErrorName(ErrID._53, iPage)

        let updateLinkIF (text: string) =
            ifName <- GetBracketsRemoveName(text) |> trimSpace |> trimNewLine
            let txrx = GetSquareBrackets(shape.InnerText, false)

            if (txrx.IsSome) then
                ifTXs <-
                    txrx.Value.Split(';').Where(fun f -> f = "" |> not)
                    |> trimStartEndSeq
                    |> Seq.filter (fun f -> f = "_" |> not)
                    |> HashSet
            else
                shape.ErrorName(ErrID._53, iPage)

        let getBracketItems (name: string) =
            name.Split('[').Where(fun w -> w <> "")
            |> Seq.map (fun f ->
                match GetSquareBrackets("[" + f, true) with
                | Some item -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), item
                | None -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), "")

        let getNodeType() =
            let nameNfunc = GetBracketsRemoveName(shape.InnerText)
            let name = GetLastParenthesesReplaceName (nameNfunc, "")
            if (shape.CheckFlowChartPreparation()) then
                CALLOPFunc
            elif (shape.CheckRectangle()) then
                if name.Contains(".")
                then REALExF
                else REAL
            elif (shape.CheckHomePlate()) then
                match GetSquareBrackets(shape.InnerText, false) with
                | Some text -> if text.Contains("~") then IF_DEVICE else IF_LINK
                | None -> IF_LINK

            elif (shape.CheckFoldedCornerPlate()) then
                OPEN_EXSYS_CALL
            elif (shape.CheckFoldedCornerRound()) then
                COPY_DEV
            elif (shape.CheckEllipse()) then
                if name.Contains(".") && nameNfunc = name
                then CALL
                else CALLCMDFunc
            elif (shape.CheckBevelShapePlate()) then
                LAMP
            elif (shape.CheckBevelShapeRound()) then
                BUTTON     
            elif (shape.CheckBevelShapeMaxRound()) then
                CONDITION
            elif (shape.CheckLayout()) then
                shape.ErrorName(ErrID._62, iPage)
            else
                shape.ErrorName(ErrID._1, iPage)

        let getOperatorParam (name:string) = 
            try
                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",")
                then 
                    failwithf $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력) 규격 입니다"

                $"{'-'}:{func}"|> getDevParam
            with _->
                shape.ErrorName(ErrID._70, iPage)


        let getCommadParam (name:string) = 
            let error()  = $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if not(func.Contains(","))
                then 
                    failwithlog (error())

                let inFunc, outFunc =
           
                    if func.Contains(",")
                    then 
                        func.Split(",").Head() |> trimSpaceNewLine
                        , func.Split(",").Last() |> trimSpaceNewLine
                    else 
                        TextSkip, func 

                let getParam x = 
                    if x = TextSkip
                    then 
                        $"{'-'}" |> getDevParam
                    else 
                        $"{'-'}:{x}"|> getDevParam

                getParam inFunc, getParam outFunc


            with _->
                shape.ErrorName((error()), iPage)

        let getPostParam(x:DevParam) =
            match x.DevValue, x.DevTime with 
            |Some (v), None->   $"{v}"
            |Some (v), Some(t)->   $"{v}_{t}"
            |None, Some(t)->   $"{t}"
            |None, None->   $""
        do

            nodeType <- getNodeType() 
            let nameNFunc =  shape.InnerText.Replace("”", "\"").Replace("“", "\"")  |> GetHeadBracketRemoveName |> trimSpaceNewLine //ppt “ ” 입력 호환
            let namePure =  GetLastParenthesesReplaceName(nameNFunc, "") |> trimSpaceNewLine
            let nameTrim  =  String.Join('.', namePure.Split('.').Select(trimSpace)) |> trimSpaceNewLine
            match GetSquareBrackets(shape.InnerText, false) with
                | Some text ->name <- $"{nameTrim |> GetBracketsRemoveName|> trimSpaceNewLine}[{text}]"  
                | None -> name <- nameTrim
            

            match nodeType with
            | CALLOPFunc ->
                if nameTrim.Contains(".") then
                    if GetLastParenthesesReplaceName(nameNFunc, "") =  nameNFunc
                    then
                        opDevParam <-  Some (createDevParam "" None  (Some(DuBOOL)) (Some(true)) None)
                    else 
                        opDevParam <-  Some (getOperatorParam nameNFunc)

            | CALLCMDFunc ->
                if namePure.Contains(".") then
                    cmdDevParam <-  Some(getCommadParam nameNFunc)
            | _ -> ()

            nameCheck (shape, nodeType, iPage)

            match nodeType with
  
            | CALL
            | REAL ->
                match GetSquareBrackets(shape.InnerText, true) with
                | Some text -> updateSafety text
                | None -> ()
            | IF_DEVICE -> updateDeviceIF shape.InnerText
            | IF_LINK -> updateLinkIF shape.InnerText
            | OPEN_EXSYS_CALL
            | OPEN_EXSYS_LINK
            | COPY_DEV ->
                let name, number = GetTailNumber(shape.InnerText)

                match GetSquareBrackets(name, false) with
                | Some text -> updateCopySys (text, (GetBracketsRemoveName(name) |> trimSpace), number)
                | None -> ()


            | BUTTON ->
                let addDic = if isHeadPage then btnHeadPageDefs else btnDefs
                getBracketItems(shape.InnerText)
                    .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getBtnType))

            | LAMP ->
                let addDic = if isHeadPage then lampHeadPageDefs else lampDefs
                getBracketItems(shape.InnerText)
                    .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getLampType))

            | CONDITION ->
                let addDic = if isHeadPage then condiHeadPageDefs else condiDefs
                getBracketItems(shape.InnerText)
                    .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getConditionType))

            | REALExF
            | LAYOUT
            | CALLOPFunc 
            | CALLCMDFunc 
            | DUMMY -> ()

        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys = copySystems
        member x.JobInfos = jobInfos
        member x.JobCallNames = jobInfos.Keys
        member x.RealFinished = shape.IsUnderlined()

        member x.Safeties = safeties

        member x.IfName = ifName
        member x.IfTXs = ifTXs
        member x.IfRXs = ifRXs
        member x.NodeType = nodeType
        member x.PageTitle = pageTitle

        member x.Position = shape.GetPosition(slieSize)

                              
                              
        member x.OperatorName = pageTitle+"_"+name.Replace(".", "_")
        member x.CommandName  = pageTitle+"_"+name.Replace(".", "_")
        member x.IsDevOperator  = opDevParam.IsSome && nodeType = CALLOPFunc 
        member x.IsDevCommand  = cmdDevParam.IsSome && nodeType = CALLCMDFunc 
        member x.IsPureOperator  = opDevParam.IsNone && nodeType = CALLOPFunc 
        member x.IsPureCommand  = cmdDevParam.IsNone && nodeType = CALLCMDFunc 
        member x.IsFunction = nodeType = CALLOPFunc || nodeType = CALLCMDFunc 
        member x.OpDevParam  = opDevParam
        member x.CmdDevParam  = cmdDevParam
        member x.IsAliasFunction = x.Alias.IsSome && (x.IsDevOperator || x.IsDevCommand)
        
        member x.JobName =
            let pureJob = pageTitle+"_"+name.Replace(".", "_")
            if x.IsFunction then
                if x.IsDevOperator then
                    let post = getPostParam (x.OpDevParam.Value)
                    if post = "" then $"{pureJob}" else $"{pureJob}_IN{post}"
                elif  x.IsDevCommand then
                    let postIn = getPostParam (x.CmdDevParam.Value|>fst)
                    let postOut = getPostParam (x.CmdDevParam.Value|>snd)
                    if postIn = "" && postOut = "" 
                    then $"{pureJob}"
                    else $"{pureJob}_IN{postIn}_OUT{postOut}" 
                else failwithlog "error"
            else 
                pureJob
                              

        member x.CallName = $"{pageTitle}_{name.Split('.')[0] |> trimSpace}"

        member x.CallApiName =
            if (nodeType = CALLOPFunc && x.IsPureOperator) || (nodeType = CALLCMDFunc && x.IsPureCommand) then
                failwithf $"not support {nodeType}({name}) type"

            let apiName = name.Split('.')[1] |> trimSpace

            match GetSquareBrackets(apiName, false) with
            | Some apiType ->
                match apiType with
                | ""
                | "_"
                | "N" -> GetLastBracketRelaceName(apiName, "-")
                | _ -> apiName
            | None -> apiName

        member val Id = shape.GetId()
        member val Key = Objkey(iPage, shape.GetId())
        member val Name = name with get, set
        member val NameOrg = shape.InnerText
        member x.IsAlias: bool = x.Alias.IsSome
        member val Alias: pptNode option = None with get, set
        member val AliasNumber: int = 0 with get, set

        member val ButtonHeadPageDefs = btnHeadPageDefs
        member val ButtonDefs = btnDefs
        member val LampHeadPageDefs = lampHeadPageDefs
        member val LampDefs = lampDefs
        member val CondiHeadPageDefs = condiHeadPageDefs
        member val CondiDefs = condiDefs
        

        member x.GetRectangle(slideSize: int * int) = shape.GetPosition(slideSize)

    and pptEdge(conn: Presentation.ConnectionShape, iEdge: UInt32Value, iPage: int, sNode: pptNode, eNode: pptNode) =
        let mutable reverse = false
        let mutable causal: ModelingEdgeType = ModelingEdgeType.StartEdge

        do

            GetCausal(conn, iPage, sNode.Name, eNode.Name)
            |> fun (c, r) ->
                causal <- c
                reverse <- r

        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.IsInterfaceEdge: bool = x.StartNode.NodeType.IsIF || x.EndNode.NodeType.IsIF
        member x.StartNode: pptNode = if (reverse) then eNode else sNode
        member x.EndNode: pptNode = if (reverse) then sNode else eNode
        member x.ParentId = 0 //reserve


        member val Name = conn.EdgeName()
        member val Key = Objkey(iPage, iEdge)

        member x.Text =
            let sName =
                if sNode.Alias.IsSome then
                    sNode.Alias.Value.Name
                else
                    sNode.Name

            let eName =
                if eNode.Alias.IsSome then
                    eNode.Alias.Value.Name
                else
                    eNode.Name

            if (reverse) then
                $"{iPage};{eName}{causal.ToText()}{sName}"
            else
                $"{iPage};{sName}{causal.ToText()}{eName}"

        member val Causal: ModelingEdgeType = causal

    and pptRealGroup(iPage: int, nodes: pptNode seq) =
        let mutable parent: pptNode option = None
        let childSet = HashSet<pptNode>()

        let nodeNames (nodes: pptNode seq) =
            nodes.Select(fun s -> s.Name).JoinWith(", ")

        do
            let reals = nodes.Where(fun w -> w.NodeType.IsReal)
            let calls = nodes.Where(fun w -> w.NodeType.IsCall)

            if (reals.Count() > 1) then
                Office.ErrorPPT(
                    Group,
                    ErrID._23,
                    $"Reals:{reals |> nodeNames}",
                    iPage,
                    Office.ShapeID(reals.First().Shape)
                )

            parent <-
                if (reals.Any() |> not) then
                    None
                else
                    Some(reals |> Seq.head)

            let children = nodes |> Seq.filter (fun node -> node.NodeType = REAL |> not)

            children |> Seq.iter (fun child -> childSet.Add(child) |> ignore)

        member x.RealKey = sprintf "%d;%s" iPage (parent.Value.Name)
        member x.PageNum = iPage

        member x.Parent: pptNode option = parent
        member x.Children = childSet
