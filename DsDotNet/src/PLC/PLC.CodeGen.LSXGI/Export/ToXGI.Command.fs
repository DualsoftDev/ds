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
    let drawCmdTimer(timerStatement:XgiTimerStatement, x, y) : CoordinatedRungXmlsWithNewY =
        let time:int = int timerStatement.Timer.PRE.Value
        let fbSpanY = 2
        { SpanY = fbSpanY; PositionedRungXmls = [createFBParameterXml $"T#{time}MS" (x-1) (y+1)]}

    let drawCmdCounter(counterStatement:XgiCounterStatement, x, y) : CoordinatedRungXmlsWithNewY =
        let count = int counterStatement.Counter.PRE.Value
        let typ = counterStatement.Counter.Type

        // 임시 :
        // todo : 산전 xgi 의 경우, cu 를 제외한 나머지는 expression 으로 받을 수 없다.
        // ResetTag 등으로 개정된 statement 구조를 만들어야 함

        //let reset = counterStatement.Counter.RES.Name

        let createParam (t:Terminal<bool> option) x y =
            match t with
            | Some t ->
                let name =
                    match t with
                    | DuTag t -> t.Name
                    | _ -> failwith "ERROR: need check"
                [ createFBParameterXml name x y ]
            | None -> []

        let reset =
            match counterStatement.Reset with
            | Some(DuTag t) -> t.Name
            | _ -> failwith "ERROR: need check"
            //| DuLiteral of 'T
            //| DuVariable of VariableBase<'T>

        let fbSpanY =
            match typ with
            | CTUD -> 5
            | (CTU | CTD | CTR) -> 3

        //Command 속성입력
        let results = [
            match typ with
            | (CTU | CTD ) ->
                createFBParameterXml reset      (x-1) (y+1)
                createFBParameterXml $"{count}" (x-1) (y+2)
            | CTR ->
                createFBParameterXml $"{count}" (x-1) (y+1)
                createFBParameterXml reset      (x-1) (y+2)
            | CTUD ->
                yield! (createParam counterStatement.CountDown (x-1) (y+1))
                yield! (createParam counterStatement.Reset     (x-1) (y+2))
                yield! (createParam counterStatement.Load      (x-1) (y+3))
                createFBParameterXml $"{count}" (x-1) (y+4)
        ]

        { SpanY = fbSpanY; PositionedRungXmls = results}

    let drawCmdCompare(coil:IExpressionTerminal, opComp:OpComp, leftA:CommandTag, leftB:CommandTag, x, y) : CoordinatedRungXmlsWithNewY =
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
            createFBParameterXml (leftA.ToText()) (x-1) (y+1)
            createFBParameterXml (leftB.ToText()) (x-1) (y+2)
            createFBParameterXml (coil.PLCTagName)  (x+1) (y+1)
        ]

        { SpanY = fbSpanY; PositionedRungXmls = results}

    let drawCmdAdd(tagCoil:IExpressionTerminal, targetTag:CommandTag, addValue:int, xInit, y, (pulse:bool)): CoordinatedRungXmlsWithNewY =
        let mutable x = xInit
        let fbSpanY = 4

        let func = "ADD"
        //test ahn : Rear UINT SINT 등등 타입 추가  필요
        //let funcFind = func + "2_" + targetTag.SizeString
        let funcFind = "ADD2_INT"

        let results = [
            if pulse then
                x <- xInit + 1
                yield! drawPulseCoil (xInit, y, tagCoil, fbSpanY)
            else
                x <- xInit
                //Command 결과출력
                createFBParameterXml (tagCoil.PLCTagName)  (x+1) (y)


            //Pulse시 증감 처리
            //yield! drawRising(x, y)
            //함수 그리기
            createFB funcFind func "" func x y
            createFBParameterXml (targetTag.ToText())    (x-1) (y+1)
            createFBParameterXml (targetTag.ToText())    (x+1) (y+1)
            createFBParameterXml (addValue.ToString())   (x-1) (y+2)
        ]

        let newY = if pulse then fbSpanY else fbSpanY-1
        { SpanY = newY; PositionedRungXmls = results}


    let drawCmdCopy(tagCoil:IExpressionTerminal, fromTag:CommandTag, toTag:CommandTag, xInit, y, (pulse:bool)) : CoordinatedRungXmlsWithNewY =
        if fromTag.Size() <> toTag.Size() then
            failwithlog $"Tag Compare size error {fromTag.ToText()}{fromTag.SizeString},  {toTag.ToText()}({toTag.SizeString})"

        let mutable x = xInit
        let fbSpanY = 3
        let func = "MOVE"
        let funcFind = func + "_" + fromTag.SizeString

        let results = [
            if pulse then
                //Pulse Command 결과출력
                x <- xInit + 1
                yield! drawPulseCoil (xInit, y, tagCoil, fbSpanY)
            else
                //Command 결과출력
                x <- xInit
                createFBParameterXml (tagCoil.PLCTagName)  (x+1) (y)


            //함수 그리기
            createFB funcFind func "" func x y
            createFBParameterXml (fromTag.ToText())  (x-1) (y+1)
            createFBParameterXml (toTag.ToText())  (x+1) (y+1)
        ]

        let spanY = if pulse then fbSpanY else fbSpanY-1
        { SpanY = spanY; PositionedRungXmls = results}



    let drawFunctionBlockInstance(cmd:XgiCommand, x, y) =
        //Command instance 객체생성
        let inst, func = cmd.Instance |> fun (inst, varType) -> inst, varType.ToString()
        [
            createFB func func inst func x y

            //Command 결과출력
            //createFBParameterXml (cmd.CoilTerminalTag.PLCTagName)  (x+1) (y)
        ]

    // <timer>
    let drawCommand(cmd:XgiCommand, x, y) =
        let results = ResizeArray<CoordinatedRungXml>()
        let c = coord x y
        results.Add( {Coordinate = c; Xml = hlineEmpty c})

        //FunctionBlock, Function 까지 연장선 긋기

        let newX = x + 1

        //FunctionBlock, Function 그리기
        let { SpanY = spanY; PositionedRungXmls = result} =
            match cmd.CommandType with
            | FunctionCmd (fc) ->
                match fc with
                | CopyMode  (endTag, (tagA, tagB)) ->  drawCmdCopy(endTag, tagA, tagB, newX, y, true)
                | CompareGT (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.GT, tagA, tagB, newX, y)
                | CompareLT (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.LT, tagA, tagB, newX, y)
                | CompareGE (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.GE, tagA, tagB, newX, y)
                | CompareLE (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.LE, tagA, tagB, newX, y)
                | CompareEQ (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.EQ, tagA, tagB, newX, y)
                | CompareNE (endTag, (tagA, tagB)) ->  drawCmdCompare(endTag, OpComp.NE, tagA, tagB, newX, y)
                | Add       (endTag, tag, value)   ->  drawCmdAdd(endTag, tag, value, newX, y, true)
            | FunctionBlockCmd (fbc) ->
                results.AddRange(drawFunctionBlockInstance(cmd, newX, y)) //Command 객체생성
                match fbc with
                | TimerMode(timerStatement) -> drawCmdTimer(timerStatement, newX, y)     // <timer>
                | CounterMode(counterStatement) -> drawCmdCounter(counterStatement, newX, y)
            | _ ->
                failwithlog "Unknown CommandType"

        results.AddRange(result)

        spanY, (results |> List.ofSeq)


