namespace PLC.CodeGen.LSXGI

open System.Linq

open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open Engine.Common.FS
open Engine.Core
open FB

[<AutoOpen>]
module internal rec Command =
    /// Rung 의 Command 정의를 위한 type.
    //Command = CoilCmd or FunctionCmd or FunctionBlockCmd
    type CommandTypes with
        member x.CoilTerminalTag =
            /// Terminal End Tag
            let tet (fc:#IFunctionCommand) = fc.TerminalEndTag
            match x with
            | CoilCmd (cc)           -> tet(cc)
            | FunctionCmd (fc)       -> tet(fc)
            | FunctionBlockCmd (fbc) -> tet(fbc)

        member x.InstanceName =
            match x with
            | FunctionBlockCmd (fbc) -> fbc.GetInstanceText()
            | _-> failwithlog "do not make instanceTag"

        member x.VarType =
            match x with
            | FunctionBlockCmd (fbc) ->
                match fbc with
                | TimerMode ts ->
                    match ts.Timer.Type with
                    | TON -> VarType.TON
                    | TOF -> VarType.TOFF
                    | RTO -> VarType.TMR

                | CounterMode cs ->
                    match cs.Counter.Type with
                    | CTU -> VarType.CTU_INT
                    | CTD -> VarType.CTD_INT
                    | CTUD -> VarType.CTUD_INT
                    | CTR ->  VarType.CTR
            |_-> failwithlog "do not make instanceTag"

        member x.LDEnum =
            match x with
            | CoilCmd (cc) ->
                match cc with
                | COMCoil _       -> ElementType.CoilMode
                | COMClosedCoil _ -> ElementType.ClosedCoilMode
                | COMSetCoil _    -> ElementType.SetCoilMode
                | COMResetCoil _  -> ElementType.ResetCoilMode
                | COMPulseCoil _  -> ElementType.PulseCoilMode
                | COMNPulseCoil _ -> ElementType.NPulseCoilMode
            | (FunctionCmd  _ | FunctionBlockCmd  _)
                -> ElementType.VertFBMode

         //   /// Coil의 부정 Command를 반환한다.
         //member x.``ReverseCmd 사용안함`` () =
         //   match cmdType with
         //   | CoilCmd (cc) ->
         //       match cc with
         //       | COMCoil(tag) -> CoilCmd(CoilOutputMode.COMClosedCoil(tag))
         //       | COMClosedCoil(tag) -> CoilCmd(CoilOutputMode.COMCoil(tag))
         //       | _ ->
         //           failwithlogf "This ReverseCmd is not support"
         //   | _ ->
         //       failwithlogf "This ReverseCmd is not support"

    //let createOutputCoil(tag)    = CoilCmd(CoilOutputMode.COMCoil(tag))
    //let createOutputCoilNot(tag) = CoilCmd(CoilOutputMode.COMClosedCoil(tag))
    //let createOutputSet(tag)     = CoilCmd(CoilOutputMode.COMSetCoil(tag))
    //let createOutputRst(tag)     = CoilCmd(CoilOutputMode.COMResetCoil(tag))
    //let createOutputPulse(tag)   = CoilCmd(CoilOutputMode.COMPulseCoil(tag))
    //let createOutputNPulse(tag)  = CoilCmd(CoilOutputMode.COMNPulseCoil(tag))

    //let createOutputCopy(tag, tagA, tagB)             = FunctionCmd(FunctionPure.CopyMode(tag, (tagA, tagB)))


    /// '_ON' 에 대한 flat expression
    let alwaysOnFlatExpression =
        let on = {
            new System.Object() with
                member x.Finalize() = ()
            interface IExpressionizableTerminal with
                member x.ToText() = "_ON"
        }
        FlatTerminal (on, false, false)



    //type FuctionParameterShape =
    //    /// Input parameter connection
    //    | LineConnectFrom of x:int * y:int
    //    /// Output parameter connection
    //    | LineConnectTo of x:int * y:int
    //    /// Input/Output 라인 연결없이 직접 write
    //    | Value of value:IValue

    /// Option<IExpression<bool>> to IExpression
    let private obe2e (obe:IExpression<bool> option): IExpression = obe.Value :> IExpression
    let private flatten (exp:IExpression) = exp.Flatten() :?> FlatExpression

    // <timer>
    let drawCmdTimer (x, y) (timerStatement:TimerStatement)  : CoordinatedXmlElement list =
        let ts = timerStatement
        let typ = ts.Timer.Type
        let time:int = int ts.Timer.PRE.Value
        let parameters =
            [
                "PT", (literal2expr $"T#{time}MS") :> IExpression
                "IN", obe2e ts.RungInCondition
                match typ with
                | RTO ->
                    "RST", obe2e ts.ResetCondition
                | _ ->
                    ()
            ]

        //Command 속성입력
        let results = [
            let cmd = FunctionBlockCmd(TimerMode(ts))
            yield! createFunctionBlockInstanceXmls (x, y) cmd parameters
        ]
        results

    let drawCmdCounter (x, y) (counterStatement:CounterStatement) : CoordinatedXmlElement list =

        //let paramDic = Dictionary<string, FuctionParameterShape>()
        let cs = counterStatement
        let pv = int16 cs.Counter.PRE.Value
        let typ = cs.Counter.Type

        let parameters =
            [
                "PV", (literal2expr pv) :> IExpression
                match typ with
                | CTU ->    // cu, r, pv,       q, cv
                    "CU", obe2e cs.UpCondition
                    "R" , obe2e cs.ResetCondition
                | CTD ->    // cd, ld, pv,       q, cv
                    "CD", obe2e cs.DownCondition
                    "LD", obe2e cs.LoadCondition
                | CTUD ->   // cu, cd, r, ld, pv,       qu, qd, cv
                    "CU", obe2e cs.UpCondition
                    "CD", obe2e cs.DownCondition
                    "R",  obe2e cs.ResetCondition
                    "LD", obe2e cs.LoadCondition
                | CTR -> // cd, pv, rst,       q, cv
                    "CD", obe2e cs.DownCondition
                    "RST", obe2e cs.ResetCondition
            ]

        //Command 속성입력
        let results = [
            let cmd = FunctionBlockCmd(CounterMode(cs))
            yield! createFunctionBlockInstanceXmls (x, y) cmd parameters
        ]

        results

    type System.Type with
        member x.SizeString = systemTypeNameToXgiTypeName x.Name


    let private toTerminalText (exp:IExpression) =
        match exp.Terminal with
        | Some t ->
            match t.Variable, t.Literal with
            | Some storage, None -> storage.Name
            | None, Some (:? ILiteralHolder as literal) -> literal.ToTextWithoutTypeSuffix()
            | None, Some literal -> literal.ToText()
            | _ -> failwith "ERROR"
        | _ -> failwith "ERROR"

    let drawCmdCompare (x, y) (func:string) (out:INamedExpressionizableTerminal) (leftA:IExpression) (leftB:IExpression) : CoordinatedXmlElement list =
        let a, b = toTerminalText leftA, toTerminalText leftB

        if(leftA.DataType <> leftB.DataType) then
            failwithlog $"Type mismatch: {a}({leftA.DataType}) <> {b}({leftB.DataType})"

        let opCompType = leftA.DataType.SizeString
        let detailedFunctionName =
            //if opComp = OpComp.NE then
            //    $"{func}_{opCompType}"
            //else
                $"{func}2_{opCompType}"

        let results = [
            createFunctionXmlAt (detailedFunctionName, func) "" (x, y)
            createFBParameterXml (x-1, y+1) a
            createFBParameterXml (x-1, y+2) b
            createFBParameterXml (x+1, y+1) (out.StorageName)
        ]

        results

    let drawCmdAdd (x, y) (func:string) (out:INamedExpressionizableTerminal) (in1:IExpression) (in2:IExpression): CoordinatedXmlElement list =

        let in1, in2 = toTerminalText in1, toTerminalText in2

        [
            //Pulse시 증감 처리
            //yield! drawRising(x, y)
            //함수 그리기
            createFunctionXmlAt ("ADD2_INT", "ADD") "" (x, y)       // ADD2_XXX
            createFBParameterXml (x+1, y+1) (out.StorageName)
            createFBParameterXml (x-1, y+1) in1
            createFBParameterXml (x-1, y+2) in2
        ]



    //let drawCmdCopy (x, y) (tagCoil:INamedExpressionizableTerminal) (fromTag:CommandTag) (toTag:CommandTag) (pulse:bool) : CoordinatedRungXmlsForCommand =
    //    if fromTag.Size() <> toTag.Size() then
    //        failwithlog $"Tag Compare size error {fromTag.ToText()}{fromTag.SizeString},  {toTag.ToText()}({toTag.SizeString})"

    //    let mutable xx = x
    //    let fbSpanY = 3
    //    let func = "MOVE"
    //    let funcFind = func + "_" + fromTag.SizeString

    //    let results = [
    //        if pulse then
    //            //Pulse Command 결과출력
    //            xx <- x + 1
    //            yield! drawPulseCoil (x, y) tagCoil fbSpanY
    //        else
    //            //Command 결과출력
    //            xx <- x
    //            createFBParameterXml (xx+1, y) (tagCoil.StorageName)


    //        //함수 그리기
    //        createFunctionXmlAt (funcFind, func) "" (xx, y)
    //        createFBParameterXml (xx-1, y+1) (fromTag.ToText())
    //        createFBParameterXml (xx+1, y+1) (toTag.ToText())
    //    ]

    //    let spanY = if pulse then fbSpanY else fbSpanY-1
    //    { SpanY = spanY; PositionedRungXmls = results}

    type CheckType with
        member t.IsRoughlyEqual(typ:System.Type) =
            match typ.Name with
            | "Single" -> t.HasFlag CheckType.REAL
            | "Double" -> t.HasFlag CheckType.LREAL
            | "SByte"  -> t.HasFlag CheckType.BYTE
            | "Byte"   -> t.HasFlag CheckType.BYTE
            | "Int16"  -> t.HasFlag CheckType.INT
            | "UInt16" -> t.HasFlag CheckType.UINT
            | "Int32"  -> t.HasFlag CheckType.DINT
            | "UInt32" -> t.HasFlag CheckType.UDINT
            | "Int64"  -> t.HasFlag CheckType.LINT
            | "UInt64" -> t.HasFlag CheckType.ULINT
            | "Boolean"-> t.HasFlag CheckType.BOOL
            | "String" -> t.HasFlag CheckType.STRING || t.HasFlag CheckType.TIME
            //| "Char"   , CheckType.
            | _ ->
                failwith "ERROR"

(*
    | BOOL          = 0x00000001
    | BYTE          = 0x00000002
    | WORD          = 0x00000004
    | DWORD         = 0x00000008
    | LWORD         = 0x00000010
    | SINT          = 0x00000020
    | INT           = 0x00000040
    | DINT          = 0x00000080
    | LINT          = 0x00000100
    | USINT         = 0x00000200
    | UINT          = 0x00000400
    | UDINT         = 0x00000800
    | ULINT         = 0x00001000
    | REAL          = 0x00002000
    | LREAL         = 0x00004000
    | TIME          = 0x00008000
    | DATE          = 0x00010000
    | TOD           = 0x00020000
    | DT            = 0x00040000
    | STRING        = 0x00080000
*)



    let createFunctionBlockInstanceXmls (rungStartX, rungStartY) (cmd:CommandTypes) (namedParameters:(string*IExpression) list) : CoordinatedXmlElement list =
        //Command instance 객체생성
        let inst = cmd.InstanceName
        let func = cmd.VarType.ToString()
        let inputSpecs = getFunctionInputSpecs func |> Array.ofSeq      // e.g ["CD, 0x00200001, , 0"; "LD, 0x00200001, , 0"; "PV, 0x00200040, , 0"]

        namedParameters.Length = inputSpecs.Length |> verifyM "ERROR: Function input parameter mismatch."

        let dic = namedParameters |> dict
        let alignedParameters =
            [|
                for s in inputSpecs do
                    let exp = dic[s.Name]
                    s.CheckType.IsRoughlyEqual exp.DataType |> verify
                    s.Name, exp, s.CheckType
            |]

        let (x, y) = (rungStartX, rungStartY)

        // y 위치에 literal parameter 쓸 공간 확보 (x 좌표는 아직 미정)
        let reservedLiteralInputParam = ResizeArray<int*IExpression>()
        let mutable sy = 0
        let blockXmls =
            [
                for (i, (name, exp, checkType)) in alignedParameters.Indexed() do
                    if checkType.HasFlag CheckType.BOOL then
                        let blockXml = drawFunctionInputLadderBlock (x, y + sy) (flatten exp)
                        blockXml
                        sy <- sy + blockXml.TotalSpanY
                    else
                        (y + i, exp) |> reservedLiteralInputParam.Add
                        sy <- sy + 1
            ]

        let sx = blockXmls.Max(fun x -> x.TotalSpanX)

        let cxmls =
            [
                (* Timer 의 PT, Counter 의 PV 등의 상수 값을 입력 모선에서 연결하지 않고, function cell 에 바로 입력 하기 위함*)
                for (ry, rexp) in reservedLiteralInputParam do
                    let literal =
                        match rexp.Terminal with
                        | Some terminal ->
                            match terminal.Literal, terminal.Variable with
                            | Some (:? ILiteralHolder as literal), None -> literal.ToTextWithoutTypeSuffix()
                            | Some literal, None -> literal.ToText()
                            | None, Some variable -> variable.ToText()
                            | _ -> failwith "ERROR"
                        | _ ->
                            failwith "ERROR"
                    createFBParameterXml (x + sx - 1, ry) literal

                yield! blockXmls |> bind(fun bx -> bx.XmlElements)
                let x, y = rungStartX, rungStartY // tmp

                //Command 결과출력
                createFunctionXmlAt (func, func) inst (x+sx, y)
            ]
        cxmls |> List.sortBy(fun cxml -> cxml.Coordinate)


    /// (x, y) 위치에 cmd 를 생성.  cmd 가 차지하는 height 와 xml 목록을 반환
    let drawCommand (x, y) (cmd:CommandTypes) : CoordinatedXmlElement list =
        let c = coord(x, y)

        let drawHLine() =
            //FunctionBlock, Function 까지 연장선 긋기
            {Coordinate = c; Xml = hlineEmpty c; SpanX = 1; SpanY = 1}

        //FunctionBlock, Function 그리기
        let results =
            [
                match cmd with
                | FunctionCmd (fc) ->
                    // todo: 내부로 이동... drawCmdXXX 내에서 그려야 한다..
                    drawHLine()
                    match fc with
                    //| CopyMode  (endTag, (tagA, tagB)) ->  drawCmdCopy (newX, y) endTag tagA tagB true
                    | FunctionCompare (name, output, args) -> yield! drawCmdCompare (x+1, y) name output args[0] args[1]
                    | FunctionArithematic (name, output, args) -> yield! drawCmdAdd (x+1, y) name output args[0] args[1]
                | FunctionBlockCmd (fbc) ->
                    match fbc with
                    | TimerMode(timerStatement) ->
                        yield! drawCmdTimer(x, y) timerStatement
                    | CounterMode(counterStatement) ->
                        yield! drawCmdCounter(x, y) counterStatement
                | _ ->
                    failwithlog "Unknown CommandType"
            ]

        results

    /// (x, y) 위치에 coil 생성.  height(=1) 와 xml 목록을 반환
    let drawCoil(x, y) (cmdExp:CommandTypes) : CoordinatedXmlElement list =
        let spanX = (coilCellX-x-2)
        let lengthParam = $"Param={dq}{3 * spanX}{dq}"
        let results = [
            let c = coord(x+1, y)
            let xml = elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""
            { Coordinate = c; Xml = xml; SpanX = spanX; SpanY = 1 }
            let c = coord(coilCellX, y)
            let xml = elementBody (int cmdExp.LDEnum) c (cmdExp.CoilTerminalTag.StorageName)
            { Coordinate = c; Xml = xml; SpanX = 1; SpanY = 1 }
        ]
        results

    /// function input 에 해당하는 expr 을 그리되, 맨 마지막을 multi horizontal line 연결 가능한 상태로 만든다.
    let drawFunctionInputLadderBlock (x, y) (expr:FlatExpression) : BlockSummarizedXmlElements =
        let blockXml = drawLadderBlock (x, y) expr
        if isFunctionBlockConnectable expr then
            blockXml
        else
            let b = blockXml
            let x = b.X + b.TotalSpanX + 1
            let c = coord(x, b.Y)
            let lineXml =
                let xml = elementFull (int ElementType.HorzLineMode) c "" ""
                { Coordinate = c; Xml = xml; SpanX = 1; SpanY = 1 }
            { blockXml with TotalSpanX = b.TotalSpanX + 1; XmlElements = b.XmlElements +++ lineXml }

    /// x y 위치에서 expression 표현하기 위한 정보 반환
    /// {| Xml=[|c, str|]; NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}
    /// - Xml : 좌표 * 결과 xml 문자열
    let rec private drawLadderBlock (x, y) (expr:FlatExpression) : BlockSummarizedXmlElements =
        let baseRIWNP = { RungInfos = []; X=x; Y=y; SpanX=1; SpanY=1; }
        let c = coord(x, y)

        // todo : uncomment

        //if enableXmlComment then
        //    let xml = $"<!-- {x} {y} {expr.ToText()} -->"
        //    { Coordinate = c; Xml = xml; SpanX = 1; SpanY = 1 }

        match expr with
        | FlatTerminal(terminal, pulse, neg) ->
            let mode =
                match pulse, neg with
                | true, true    -> ElementType.NPulseContactMode
                | true, false   -> ElementType.PulseContactMode
                | false, true   -> ElementType.ClosedContactMode
                | false, false  -> ElementType.ContactMode
                |> int
            let terminalText =
                match terminal with
                | :? IStorage as storage -> storage.Name
                | _ -> terminal.ToText()
            let str = elementBody mode c terminalText
            let xml = { Coordinate = c; Xml = str; SpanX = 1; SpanY = 1;}
            { XmlElements = [xml]; X=x; Y=y; TotalSpanX = 1; TotalSpanY = 1}

        | FlatNary(And, exprs) ->
            let mutable sx = x
            let blockedExprXmls:BlockSummarizedXmlElements list =
                [
                    for exp in exprs do
                        let sub = drawLadderBlock (sx, y) exp
                        sx <- sx + sub.TotalSpanX
                        sub
                ]

            let spanX = blockedExprXmls.Sum(fun x -> x.TotalSpanX)
            let spanY = blockedExprXmls.Max(fun x -> x.TotalSpanY)
            let exprXmls = blockedExprXmls |> List.collect(fun x -> x.XmlElements)
            { XmlElements = exprXmls; X=x; Y=y; TotalSpanX = spanX; TotalSpanY = spanY}


        | FlatNary(Or, exprs) ->
            let mutable sy = y
            let blockedExprXmls:BlockSummarizedXmlElements list =
                [
                    for exp in exprs do
                        let sub = drawLadderBlock (x, sy) exp
                        sy <- sy + sub.TotalSpanY
                        sub
                ]
            let spanX = blockedExprXmls.Max(fun x -> x.TotalSpanX)
            let spanY = blockedExprXmls.Sum(fun x -> x.TotalSpanY)
            let exprXmls = blockedExprXmls |> List.collect(fun x -> x.XmlElements)

            let xmls = [
                yield! exprXmls

                let auxLineXmls =
                    [
                        for ri in blockedExprXmls do
                            if ri.TotalSpanX < spanX then
                                let span = (spanX - ri.TotalSpanX - 1)
                                let param = $"Param={dq}{span*3}{dq}"
                                let mode = int ElementType.MultiHorzLineMode
                                let c = coord (x + ri.TotalSpanX, ri.Y)
                                let xml = elementFull mode c param ""
                                { Coordinate = c; Xml = xml; SpanX = span; SpanY = 1 }
                    ]
                yield! auxLineXmls


                // 좌측 vertical lines
                if x >= 1 then
                    yield! vlineDownTo (x-1, y) (spanY-1)

                // ```OR variable length 역삼각형 test```
                let lowestY =
                    blockedExprXmls
                        .Where(fun sri -> sri.TotalSpanX <= spanX)
                        .Max(fun sri -> sri.Y)
                // 우측 vertical lines
                yield! vlineDownTo (x+spanX-1, y) (lowestY-y)
            ]
            { XmlElements = xmls; X=x; Y=y; TotalSpanX = spanX; TotalSpanY = spanY}

        | FlatNary((OpCompare _ | OpArithematic _), exprs) ->
            failwith "ERROR : Should have been processed in early stage."    // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement

        // terminal case
        | FlatNary(OpUnit, inner::[]) ->
            drawLadderBlock (x, y) inner

        // negation 없애기
        | FlatNary(Neg, inner::[]) ->
            FlatNary(OpUnit, [inner.Negate()]) |> drawLadderBlock (x, y)

        //| FlatZero ->
        //    let str = hlineEmpty c
        //    { baseRIWNP with RungInfos=[{ Coordinate = c; Xml = str; SpanX=0; SpanY=0;}]; SpanX=0; SpanY=0; }

        | _ ->
            failwithlog "Unknown FlatExpression case"


    /// Flat expression 을 논리 Cell 좌표계 x y 에서 시작하는 rung 를 작성한다.
    /// xml 및 다음 y 좌표 반환
    /// expr 이 None 이면 그리지 않는다.
    /// cmdExp 이 None 이면 command 를 그리지 않는다.
    let rung (x, y) (expr:FlatExpression option) (cmdExp:CommandTypes option) : CoordinatedXmlElement =

        let exprSpanX, exprSpanY, exprXmls =
            match expr with
            | Some expr ->
                let exprBlockXmlElement = drawLadderBlock (x, y) expr
                let ex = exprBlockXmlElement
                ex.TotalSpanX, ex.TotalSpanY, ex.XmlElements |> List.distinct
            | _ ->
                0, 0, []

        let cmdSpanX, cmdSpanY, cmdXmls =
            match cmdExp with
            | Some cmdExp ->
                let nx = x + exprSpanX
                let cmdXmls =
                    match cmdExp with
                    | CoilCmd (cc) ->
                        drawCoil (nx-1, y) cmdExp
                    | ( FunctionCmd _ | FunctionBlockCmd _ ) ->
                        drawCommand (nx, y) cmdExp
                let spanX = exprSpanX + cmdXmls.Max(fun x -> x.SpanX)
                let spanY = max exprSpanY (cmdXmls.Max(fun x -> x.SpanY))
                spanX, spanY, cmdXmls
            | None ->
                0, 0, []




        let xml =
            exprXmls @ cmdXmls
                |> Seq.sortBy (fun ri -> ri.Coordinate)   // fst
                |> Seq.map (fun ri -> ri.Xml)  //snd
                |> String.concat "\r\n"

        let spanX = exprSpanX + cmdSpanX
        let spanY = max exprSpanY cmdSpanY
        let c = coord(x, spanY + y)
        { Xml = xml; Coordinate = c; SpanX = spanX; SpanY=spanY }




