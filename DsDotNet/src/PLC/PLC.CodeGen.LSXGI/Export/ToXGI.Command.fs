namespace PLC.CodeGen.LSXGI

open Engine.Common.FS
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open Engine.Core

[<AutoOpen>]
module internal Command =
    /// Rung 의 Command 정의를 위한 type.
    //Command = CoilCmd or FunctionCmd or FunctionBlockCmd
    type XgiCommand(cmdType:CommandTypes) =
        do
            noop()
        member x.CommandType with get() = cmdType
        member x.CoilTerminalTag with get() =
            /// Terminal End Tag
            let tet (fc:#IFunctionCommand) = fc.TerminalEndTag
            match cmdType with
            | CoilCmd (cc)           -> tet(cc)
            | FunctionCmd (fc)       -> tet(fc)
            | FunctionBlockCmd (fbc) -> tet(fbc)

        member x.UsedCommandTags with get() =
            match cmdType with
            | CoilCmd (cc) -> [ x.CoilTerminalTag ]
            | FunctionCmd (fc) ->  fc.UsedCommandTags()
            | FunctionBlockCmd (fbc) -> fbc.UsedCommandTags()

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

    //let createOutputTime(tag, time)                 = XgiCommand(FunctionBlockCmd(FunctionBlock.TimerMode(tag, time)))
    //let createOutputCount(tag, resetTag, cnt)         = XgiCommand(FunctionBlockCmd(FunctionBlock.CounterMode(tag, resetTag, cnt)))
    let createOutputCopy(tag, tagA, tagB)             = XgiCommand(FunctionCmd(FunctionPure.CopyMode(tag, (tagA, tagB))))
    let createOutputAdd(tag, targetTag, addValue:int) = XgiCommand(FunctionCmd(FunctionPure.Add(tag, targetTag, addValue)))

    let createOutputCompare(tag, opComp:OpComp, tagA, tagB) =
        match opComp with
        | GT -> XgiCommand(FunctionCmd(FunctionPure.CompareGT(tag, (tagA, tagB))))
        | GE -> XgiCommand(FunctionCmd(FunctionPure.CompareGE(tag, (tagA, tagB))))
        | EQ -> XgiCommand(FunctionCmd(FunctionPure.CompareEQ(tag, (tagA, tagB))))
        | LE -> XgiCommand(FunctionCmd(FunctionPure.CompareLE(tag, (tagA, tagB))))
        | LT -> XgiCommand(FunctionCmd(FunctionPure.CompareLT(tag, (tagA, tagB))))
        | NE -> XgiCommand(FunctionCmd(FunctionPure.CompareNE(tag, (tagA, tagB))))

    // <timer>
    let drawCmdTimer (x, y) (timerStatement:XgiTimerStatement)  : CoordinatedRungXmlsWithNewY =
        let time:int = int timerStatement.Timer.PRE.Value
        let fbSpanY = 2
        { SpanY = fbSpanY; PositionedRungXmls = [createFBParameterXml (x-1, y+1) $"T#{time}MS" ]}

    let drawCmdCounter (x, y) (counterStatement:XgiCounterStatement) : CoordinatedRungXmlsWithNewY =
        let count = int counterStatement.Counter.PRE.Value
        let typ = counterStatement.Counter.Type

        // 임시 :
        // todo : 산전 xgi 의 경우, cu 를 제외한 나머지는 expression 으로 받을 수 없다.
        // ResetTag 등으로 개정된 statement 구조를 만들어야 함

        //let reset = counterStatement.Counter.RES.Name

        let createParam (x, y) (t:Terminal<bool> option) =
            match t with
            | Some t -> [ createFBParameterXml (x, y) t.Name  ]
            | None   -> []

        let reset = counterStatement.Reset.Value.Name

        let fbSpanY =
            match typ with
            | CTUD -> 5
            | (CTU | CTD | CTR) -> 3

        //Command 속성입력
        let results = [
            match typ with
            | (CTU | CTD ) ->
                createFBParameterXml (x-1, y+1) reset
                createFBParameterXml (x-1, y+2) $"{count}"
            | CTR ->
                createFBParameterXml (x-1, y+1) $"{count}"
                createFBParameterXml (x-1, y+2) reset
            | CTUD ->
                yield! (createParam (x-1, y+1) counterStatement.CountDown )
                yield! (createParam (x-1, y+2) counterStatement.Reset     )
                yield! (createParam (x-1, y+3) counterStatement.Load      )
                createFBParameterXml (x-1, y+4) $"{count}"
        ]

        { SpanY = fbSpanY; PositionedRungXmls = results}

    let drawCmdCompare (x, y) (coil:IExpressionTerminal) (opComp:OpComp) (leftA:CommandTag) (leftB:CommandTag) : CoordinatedRungXmlsWithNewY =
        let fbSpanY = 3

        if(leftA.Size() <> leftB.Size())
        then failwithlog (sprintf "Tag Compare size error %s(%s),  %s(%s)" (leftA.ToText()) (leftA.SizeString) (leftB.ToText()) (leftB.SizeString))

        let opCompType = leftA.SizeString
        let func = opComp.ToText()
        let funcFind =
            if(opComp = OpComp.NE)
            then $"{opComp.ToText()}_{opCompType}"
            else $"{opComp.ToText()}2_{opCompType}"

        let results = [
            createFB funcFind func "" (opComp.ToText()) x y
            createFBParameterXml (x-1, y+1) (leftA.ToText())
            createFBParameterXml (x-1, y+2) (leftB.ToText())
            createFBParameterXml (x+1, y+1) (coil.PLCTagName)
        ]

        { SpanY = fbSpanY; PositionedRungXmls = results}

    let drawCmdAdd (x, y) (tagCoil:IExpressionTerminal) (targetTag:CommandTag) (addValue:int) (pulse:bool): CoordinatedRungXmlsWithNewY =
        let mutable xx = x
        let fbSpanY = 4

        let func = "ADD"
        //test ahn : Rear UINT SINT 등등 타입 추가  필요
        //let funcFind = func + "2_" + targetTag.SizeString
        let funcFind = "ADD2_INT"

        let results = [
            if pulse then
                xx <- x + 1
                yield! drawPulseCoil (x, y) tagCoil fbSpanY
            else
                xx <- x
                //Command 결과출력
                createFBParameterXml (xx+1, y) (tagCoil.PLCTagName)


            //Pulse시 증감 처리
            //yield! drawRising(x, y)
            //함수 그리기
            createFB funcFind func "" func xx y
            createFBParameterXml (xx-1, y+1) (targetTag.ToText())
            createFBParameterXml (xx+1, y+1) (targetTag.ToText())
            createFBParameterXml (xx-1, y+2) (addValue.ToString())
        ]

        let newY = if pulse then fbSpanY else fbSpanY-1
        { SpanY = newY; PositionedRungXmls = results}


    let drawCmdCopy (x, y) (tagCoil:IExpressionTerminal) (fromTag:CommandTag) (toTag:CommandTag) (pulse:bool) : CoordinatedRungXmlsWithNewY =
        if fromTag.Size() <> toTag.Size() then
            failwithlog $"Tag Compare size error {fromTag.ToText()}{fromTag.SizeString},  {toTag.ToText()}({toTag.SizeString})"

        let mutable xx = x
        let fbSpanY = 3
        let func = "MOVE"
        let funcFind = func + "_" + fromTag.SizeString

        let results = [
            if pulse then
                //Pulse Command 결과출력
                xx <- x + 1
                yield! drawPulseCoil (x, y) tagCoil fbSpanY
            else
                //Command 결과출력
                xx <- x
                createFBParameterXml (xx+1, y) (tagCoil.PLCTagName)


            //함수 그리기
            createFB funcFind func "" func xx y
            createFBParameterXml (xx-1, y+1) (fromTag.ToText())
            createFBParameterXml (xx+1, y+1) (toTag.ToText())
        ]

        let spanY = if pulse then fbSpanY else fbSpanY-1
        { SpanY = spanY; PositionedRungXmls = results}



    let drawFunctionBlockInstance (x, y) (cmd:XgiCommand) =
        //Command instance 객체생성
        let inst, func = cmd.Instance |> fun (inst, varType) -> inst, varType.ToString()
        [
            createFB func func inst func x y

            //Command 결과출력
            //createFBParameterXml (cmd.CoilTerminalTag.PLCTagName)  (x+1) (y)
        ]

    // <timer>
    let drawCommand (x, y) (cmd:XgiCommand) =
        let results = ResizeArray<CoordinatedRungXml>()
        let c = coord(x, y)
        results.Add( {Coordinate = c; Xml = hlineEmpty c})

        //FunctionBlock, Function 까지 연장선 긋기

        let newX = x + 1

        //FunctionBlock, Function 그리기
        let { SpanY = spanY; PositionedRungXmls = result} =
            match cmd.CommandType with
            | FunctionCmd (fc) ->
                match fc with
                | CopyMode  (endTag, (tagA, tagB)) ->  drawCmdCopy (newX, y) endTag tagA tagB true
                | CompareGT (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.GT tagA tagB
                | CompareLT (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.LT tagA tagB
                | CompareGE (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.GE tagA tagB
                | CompareLE (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.LE tagA tagB
                | CompareEQ (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.EQ tagA tagB
                | CompareNE (endTag, (tagA, tagB)) ->  drawCmdCompare (newX, y) endTag OpComp.NE tagA tagB
                | Add       (endTag, tag, value)   ->  drawCmdAdd (newX, y) endTag tag value true
            | FunctionBlockCmd (fbc) ->
                results.AddRange(drawFunctionBlockInstance (newX, y) cmd) //Command 객체생성
                match fbc with
                | TimerMode(timerStatement) -> drawCmdTimer(newX, y) timerStatement     // <timer>
                | CounterMode(counterStatement) -> drawCmdCounter(newX, y) counterStatement
            | _ ->
                failwithlog "Unknown CommandType"

        results.AddRange(result)

        spanY, (results |> List.ofSeq)


