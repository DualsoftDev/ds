namespace PLC.CodeGen.LSXGI

open System.Collections.Generic
open System.Linq

open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module internal rec Command =
    /// Rung 의 Command 정의를 위한 type.
    //Command = CoilCmd or FunctionCmd or FunctionBlockCmd
    type XgiCommand(cmdType:CommandTypes) =
        member x.CommandType with get() = cmdType
        member x.CoilTerminalTag with get() =
            /// Terminal End Tag
            let tet (fc:#IFunctionCommand) = fc.TerminalEndTag
            match cmdType with
            | CoilCmd (cc)           -> tet(cc)
            | FunctionCmd (fc)       -> tet(fc)
            | FunctionBlockCmd (fbc) -> tet(fbc)

        member x.HasInstance with get() = match cmdType with | FunctionBlockCmd (fbc) ->  true |_-> false
        member x.Instance with get() =
            match cmdType with
            | FunctionBlockCmd (fbc) ->
                match fbc with
                | TimerMode ts ->
                    let varType =
                        match ts.Timer.Type with
                        | TON -> VarType.TON
                        | TOF -> VarType.TOFF
                        | RTO -> VarType.TMR
                    fbc.GetInstanceText(), varType

                | CounterMode cs ->
                    let varType =
                        match cs.Counter.Type with
                        | CTU -> VarType.CTU_INT
                        | CTD -> VarType.CTD_INT
                        | CTUD -> VarType.CTUD_INT
                        | CTR ->  VarType.CTR
                    fbc.GetInstanceText(), varType
            |_-> failwithlog "do not make instanceTag"

        member x.LDEnum with get() =
            match cmdType with
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

            /// Coil의 부정 Command를 반환한다.
         member x.ReverseCmd () =
            match cmdType with
            | CoilCmd (cc) ->
                match cc with
                | COMCoil(tag) -> XgiCommand(CoilCmd(CoilOutputMode.COMClosedCoil(tag)))
                | COMClosedCoil(tag) -> XgiCommand(CoilCmd(CoilOutputMode.COMCoil(tag)))
                | _ ->
                    failwithlogf "This ReverseCmd is not support"
            | _ ->
                failwithlogf "This ReverseCmd is not support"

    let createOutputCoil(tag)    = XgiCommand(CoilCmd(CoilOutputMode.COMCoil(tag)))
    let createOutputCoilNot(tag) = XgiCommand(CoilCmd(CoilOutputMode.COMClosedCoil(tag)))
    let createOutputSet(tag)     = XgiCommand(CoilCmd(CoilOutputMode.COMSetCoil(tag)))
    let createOutputRst(tag)     = XgiCommand(CoilCmd(CoilOutputMode.COMResetCoil(tag)))
    let createOutputPulse(tag)   = XgiCommand(CoilCmd(CoilOutputMode.COMPulseCoil(tag)))
    let createOutputNPulse(tag)  = XgiCommand(CoilCmd(CoilOutputMode.COMNPulseCoil(tag)))

    //let createOutputCopy(tag, tagA, tagB)             = XgiCommand(FunctionCmd(FunctionPure.CopyMode(tag, (tagA, tagB))))


    /// '_ON' 에 대한 flat expression
    let alwaysOnFlatExpression =
        let on = {
            new System.Object() with
                member x.Finalize() = ()
            interface IExpressionizableTerminal with
                member x.ToText() = "_ON"
        }
        FlatTerminal (on, false, false)



    type FuctionParameterShape =
        /// Input parameter connection
        | LineConnectFrom of x:int * y:int
        /// Output parameter connection
        | LineConnectTo of x:int * y:int
        /// Input/Output 라인 연결없이 직접 write
        | Value of value:IValue


    // <timer>
    let drawCmdTimer (x, y) (timerStatement:TimerStatement)  : CoordinatedRungXmlsForCommand =
        let time:int = int timerStatement.Timer.PRE.Value
        let fbSpanY = 2
        { SpanY = fbSpanY; PositionedRungXmls = [createFBParameterXml (x-1, y+1) $"T#{time}MS" ]}

    let drawCmdCounter (x, y) (counterStatement:CounterStatement) : CoordinatedRungXmlsForCommand =

        let paramDic = Dictionary<string, FuctionParameterShape>()
        let cs = counterStatement
        let pv = int cs.Counter.PRE.Value
        let typ = cs.Counter.Type
        let fbSpanY =
            match typ with
            | CTUD -> 5
            | (CTU | CTD | CTR) -> 3

        let createParam (x, y) (t:Terminal<bool> option) =
            match t with
            | Some t -> [ createFBParameterXml (x, y) t.Name  ]
            | None   -> []

        let paramXmls =
            [
                match typ with
                | CTU ->    // cu, r, pv, q, cv
                    let cu = cs.UpCondition.Value.Flatten() :?> FlatExpression
                    let xxx = rung (x, y) (Some cu) None
                    rung (x, y) (Some cu) None
                    let r = cs.ResetCondition.Value.Flatten() :?> FlatExpression
                    let xxxy = rung (x, y+1) (Some cu) None
                    rung (x, y+1) (Some r) None

                //| CTD ->
                //| CTUD ->
                //| CTR -> 3
                | _ -> ()
            ]

        //let reset = cs.Reset.Value.Name



        //Command 속성입력
        let results = [
            yield! paramXmls

            match typ with
            | (CTU | CTD ) ->
                //createFBParameterXml (x-1, y+1) reset
                createFBParameterXml (x, y+2) $"{pv}"
            | CTR ->
                createFBParameterXml (x-1, y+1) $"{pv}"
                //createFBParameterXml (x-1, y+2) reset
            | CTUD ->
                //yield! (createParam (x-1, y+1) cs.DownCondition )
                //yield! (createParam (x-1, y+2) cs.Reset     )
                //yield! (createParam (x-1, y+3) cs.Load      )
                createFBParameterXml (x-1, y+4) $"{pv}"


            let cmd = FunctionBlockCmd(CounterMode(counterStatement)) |> XgiCommand
            yield! createFunctionBlockInstanceXmls (x+1, y) cmd
        ]
        let results = results |> List.sortBy(fun x -> x.Coordinate)

        { SpanY = fbSpanY; PositionedRungXmls = results}

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

    let drawCmdCompare (x, y) (func:string) (out:INamedExpressionizableTerminal) (leftA:IExpression) (leftB:IExpression) : CoordinatedRungXmlsForCommand =
        let fbSpanY = 3
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

        { SpanY = fbSpanY; PositionedRungXmls = results}

    let drawCmdAdd (x, y) (func:string) (out:INamedExpressionizableTerminal) (in1:IExpression) (in2:IExpression): CoordinatedRungXmlsForCommand =
        let fbSpanY = 3

        let in1, in2 = toTerminalText in1, toTerminalText in2

        let results = [
            //Pulse시 증감 처리
            //yield! drawRising(x, y)
            //함수 그리기
            createFunctionXmlAt ("ADD2_INT", "ADD") "" (x, y)       // ADD2_XXX
            createFBParameterXml (x+1, y+1) (out.StorageName)
            createFBParameterXml (x-1, y+1) in1
            createFBParameterXml (x-1, y+2) in2
        ]

        { SpanY = fbSpanY; PositionedRungXmls = results}


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



    let createFunctionBlockInstanceXmls (x, y) (cmd:XgiCommand) : CoordinatedRungXml list =
        //Command instance 객체생성
        let inst, func = cmd.Instance |> fun (inst, varType) -> inst, varType.ToString()
        [
            createFunctionXmlAt (func, func) inst (x, y)

            //Command 결과출력
            //createFBParameterXml (cmd.CoilTerminalTag.PLCTagName)  (x+1) (y)
        ]


    /// (x, y) 위치에 cmd 를 생성.  cmd 가 차지하는 height 와 xml 목록을 반환
    let drawCommand (x, y) (cmd:XgiCommand) : int * (CoordinatedRungXml list) =
        let results = ResizeArray<CoordinatedRungXml>()
        let c = coord(x, y)

        let drawHLine() =
            //FunctionBlock, Function 까지 연장선 긋기
            results.Add( {Coordinate = c; Xml = hlineEmpty c})

        //FunctionBlock, Function 그리기
        let { SpanY = spanY; PositionedRungXmls = result} =
            match cmd.CommandType with
            | FunctionCmd (fc) ->
                drawHLine()
                match fc with
                //| CopyMode  (endTag, (tagA, tagB)) ->  drawCmdCopy (newX, y) endTag tagA tagB true
                | FunctionCompare (name, output, args) -> drawCmdCompare (x+1, y) name output args[0] args[1]
                | FunctionArithematic (name, output, args) -> drawCmdAdd (x+1, y) name output args[0] args[1]
            | FunctionBlockCmd (fbc) ->
                match fbc with
                | TimerMode(timerStatement) ->
                    // todo: 내부로 이동... drawCmdTimer 내에서 그려야 한다..
                    drawHLine()
                    drawCmdTimer(x+1, y) timerStatement     // <timer>
                | CounterMode(counterStatement) ->
                    // todo: 내부로 이동 작업 중...
                    drawCmdCounter(x, y) counterStatement
            | _ ->
                failwithlog "Unknown CommandType"

        results.AddRange(result)

        spanY, (results |> List.ofSeq)


    /// (x, y) 위치에 coil 생성.  height(=1) 와 xml 목록을 반환
    let drawCoil(x, y) (cmdExp:XgiCommand) : int * (CoordinatedRungXml list) =
        let lengthParam =
            let param = 3 * (coilCellX-x-2)
            $"Param={dq}{param}{dq}"
        let results = [
            let c = coord(x+1, y)
            { Coordinate = c; Xml = elementFull (int ElementType.MultiHorzLineMode) c lengthParam "" }
            let c = coord(coilCellX, y)
            { Coordinate = c; Xml = elementBody (int cmdExp.LDEnum) c (cmdExp.CoilTerminalTag.StorageName) }
        ]
        1, results


    /// Flat expression 을 논리 Cell 좌표계 x y 에서 시작하는 rung 를 작성한다.
    /// xml 및 다음 y 좌표 반환
    /// expr 이 None 이면 그리지 않는다.
    /// cmdExp 이 None 이면 command 를 그리지 않는다.
    let rung (x, y) (expr:FlatExpression option) (cmdExp:XgiCommand option) : CoordinatedRungXml =

        /// x y 위치에서 expression 표현하기 위한 정보 반환
        /// {| Xml=[|c, str|]; NextX=sx; NextY=maxY; VLineUpRightMaxY=maxY |}
        /// - Xml : 좌표 * 결과 xml 문자열
        let rec rng (x, y) (expr:FlatExpression) : RungInfosWithSpan =
            let baseRIWNP = { RungInfos = []; X=x; Y=y; SpanX=1; SpanY=1; }
            let c = coord(x, y)
            /// 좌표 * 결과 xml 문자열 보관 장소
            let rungInfos = ResizeArray<CoordinatedRungXml>()
            if enableXmlComment then
                { Coordinate = c; Xml = $"<!-- {x} {y} {expr.ToText()} -->" } |> rungInfos.Add

            match expr with
            | FlatTerminal(id, pulse, neg) ->
                let mode =
                    match pulse, neg with
                    | true, true    -> ElementType.NPulseContactMode
                    | true, false   -> ElementType.PulseContactMode
                    | false, true   -> ElementType.ClosedContactMode
                    | false, false  -> ElementType.ContactMode
                    |> int
                let str = elementBody mode c (id.ToText())
                { baseRIWNP with RungInfos = [{ Coordinate = c; Xml = str}]; }

            | FlatNary(And, exprs) ->
                let mutable sx = x
                let subRungInfos:RungInfosWithSpan list =
                    [
                        for exp in exprs do
                            let sub = rng (sx, y) exp
                            sx <- sx + sub.SpanX
                            rungInfos.AddRange(sub.RungInfos)
                            yield sub
                    ]
                let spanX = subRungInfos.Sum(fun sri-> sri.SpanX)
                let spanY = subRungInfos.Max(fun sri-> sri.SpanY)
                { baseRIWNP with RungInfos=rungInfos.Distinct().ToFSharpList(); SpanX=spanX; SpanY=spanY; }

            | FlatNary(Or, exprs) ->
                let mutable sy = y
                let subRungInfos:RungInfosWithSpan list =
                    [
                        for exp in exprs do
                            let sub = rng (x, sy) exp
                            sy <- sy + sub.SpanY
                            rungInfos.AddRange(sub.RungInfos)
                            yield sub
                    ]
                let spanX = subRungInfos.Max(fun sri-> sri.SpanX)
                let spanY = subRungInfos.Sum(fun sri-> sri.SpanY)

                [
                    for ri in subRungInfos do
                        if ri.SpanX < spanX then
                            let param =
                                let span = (spanX - ri.SpanX - 1) * 3
                                $"Param={dq}{span}{dq}"
                            let mode = int ElementType.MultiHorzLineMode
                            let c = coord (ri.X+ri.SpanX, ri.Y)
                            { Coordinate = c; Xml = elementFull mode c param "" }
                ] |> rungInfos.AddRange

                // 좌측 vertical lines
                if x >= 1 then
                    vlineDownTo (x-1, y) (spanY-1) |> rungInfos.AddRange

                // ```OR variable length 역삼각형 test```
                let lowestY =
                    subRungInfos
                        .Where(fun sri -> sri.SpanX <= spanX)
                        .Max(fun sri -> sri.Y)
                // 우측 vertical lines
                vlineDownTo (x+spanX-1, y) (lowestY-y) |> rungInfos.AddRange


                { baseRIWNP with RungInfos=rungInfos.Distinct().ToFSharpList(); SpanX=spanX; SpanY=spanY; }


            | FlatNary((OpCompare _ | OpArithematic _), exprs) ->
                failwith "ERROR : Should have been processed in early stage."    // 사전에 미리 처리 되었어야 한다.  여기 들어오면 안된다. XgiStatement

            // terminal case
            | FlatNary(OpUnit, inner::[]) ->
                inner |> rng (x, y)

            // negation 없애기
            | FlatNary(Neg, inner::[]) ->
                FlatNary(OpUnit, [inner.Negate()]) |> rng (x, y)

            | FlatZero ->
                let str = hlineEmpty c
                { baseRIWNP with RungInfos=[{ Coordinate = c; Xml = str}]; SpanX=0; SpanY=0; }

            | _ ->
                failwithlog "Unknown FlatExpression case"

        // function (block) 의 경우, 조건이 없는 경우가 대부분인데, 이때는 always on (_ON) 으로 연결한다.
        let result =
            match expr with
            | Some expr -> rng (x, y) expr
            | _ -> { RungInfos = []; X = x; Y = y; SpanX = 0; SpanY = 1 }

        let mutable commandHeight = 0
        /// 좌표 * xml 결과 문자열
        let positionedRungXmls =
            [
                yield! result.RungInfos

                match cmdExp with
                | Some cmdExp ->
                    let nx = x + result.SpanX
                    let commandSpanY, posiRungXmls =
                        match cmdExp.CommandType with
                        | CoilCmd (cc) ->
                            drawCoil (nx-1, y) cmdExp
                        | ( FunctionCmd _ | FunctionBlockCmd _ ) ->
                            drawCommand (nx, y) cmdExp

                    commandHeight <- commandSpanY
                    yield! posiRungXmls
                | None ->
                    ()
            ]


        let xml =
            positionedRungXmls
                |> Seq.sortBy (fun ri -> ri.Coordinate)   // fst
                |> Seq.map (fun ri -> ri.Xml)  //snd
                |> String.concat "\r\n"

        let c =
            let spanY = max result.SpanY commandHeight
            coord(x, spanY + y)
        { Xml = xml; Coordinate = c }




