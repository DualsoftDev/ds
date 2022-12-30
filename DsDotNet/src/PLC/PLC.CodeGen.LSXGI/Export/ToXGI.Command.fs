namespace PLC.CodeGen.LSXGI

open Engine.Common.FS
open PLC.CodeGen.Common
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine
open Config.POU.Program.LDRoutine
open Engine.Core

[<AutoOpen>]
module internal Command =
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
                | TimerMode(_) -> fbc.GetInstanceText(), VarType.TON
                | CounterMode(tag, resetTag, count) -> fbc.GetInstanceText(), VarType.CTU_INT
            |_-> failwithlog "do not make instanceTag"

        member x.LDEnum with get() =
            match cmdType with
                | CoilCmd (cc) ->
                    match cc with
                    | CoilMode(_)       -> ElementType.CoilMode
                    | ClosedCoilMode(_) -> ElementType.ClosedCoilMode
                    | SetCoilMode(_)    -> ElementType.SetCoilMode
                    | ResetCoilMode(_)  -> ElementType.ResetCoilMode
                    | PulseCoilMode(_)  -> ElementType.PulseCoilMode
                    | NPulseCoilMode(_) -> ElementType.NPulseCoilMode
                | (FunctionCmd (_) | FunctionBlockCmd (_))
                    -> ElementType.VertFBMode

            /// Coil의 부정 Command를 반환한다.
         member x.ReverseCmd () =
            match cmdType with
            | CoilCmd (cc) ->
                match cc with
                | CoilMode(tag) -> XgiCommand(CoilCmd(CoilOutput.ClosedCoilMode(tag)))
                | ClosedCoilMode(tag) -> XgiCommand(CoilCmd(CoilOutput.CoilMode(tag)))
                | _ ->
                    failwithlogf "This ReverseCmd is not support"
            | _ ->
                failwithlogf "This ReverseCmd is not support"

    let createOutputCoil(tag)    = XgiCommand(CoilCmd(CoilOutput.CoilMode(tag)))
    let createOutputCoilNot(tag) = XgiCommand(CoilCmd(CoilOutput.ClosedCoilMode(tag)))
    let createOutputSet(tag)     = XgiCommand(CoilCmd(CoilOutput.SetCoilMode(tag)))
    let createOutputRst(tag)     = XgiCommand(CoilCmd(CoilOutput.ResetCoilMode(tag)))
    let createOutputPulse(tag)   = XgiCommand(CoilCmd(CoilOutput.PulseCoilMode(tag)))
    let createOutputNPulse(tag)  = XgiCommand(CoilCmd(CoilOutput.NPulseCoilMode(tag)))

    //let createOutputTime(tag, time)                 = XgiCommand(FunctionBlockCmd(FunctionBlock.TimerMode(tag, time)))
    let createOutputCount(tag, resetTag, cnt)         = XgiCommand(FunctionBlockCmd(FunctionBlock.CounterMode(tag, resetTag, cnt)))
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
    let drawCmdTimer(timerStatement:TimerStatement, x, y) : CoordinatedRungXmlsWithNewY =
        let time:int = int timerStatement.Timer.PRE.Value
        //let coil:IExpressionTerminal =
        let funcSizeY = 3
        //Command 속성입력
        { NewY = funcSizeY-1; PositionedRungXmls = [createFBParameterXml $"T#{time}MS" (x-1) (y+1)]}
        //{ NewY = funcSizeY-1; PositionedRungXmls = [createFBParameterXml $"T#{time}MS" x y]}

    let drawCmdCounter(coil:IExpressionTerminal, reset:IExpressionTerminal, count:int, x, y) : CoordinatedRungXmlsWithNewY =
        let funcSizeY = 4
        //Command 속성입력
        let results = [
            createFBParameterXml reset.PLCTagName (x-1) (y+1)
            createFBParameterXml $"{count}" (x-1) (y+2)
        ]

        { NewY = funcSizeY-1; PositionedRungXmls = results}

    let drawCmdCompare(coil:IExpressionTerminal, opComp:OpComp, leftA:CommandTag, leftB:CommandTag, x, y) : CoordinatedRungXmlsWithNewY =
        let funcSizeY = 4

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

        { NewY = funcSizeY - 1; PositionedRungXmls = results}

    let drawCmdAdd(tagCoil:IExpressionTerminal, targetTag:CommandTag, addValue:int, xInit, y, (pulse:bool)): CoordinatedRungXmlsWithNewY =
        let mutable x = xInit
        let funcSizeY = 4

        let func = "ADD"
        //test ahn : Rear UINT SINT 등등 타입 추가  필요
        //let funcFind = func + "2_" + targetTag.SizeString
        let funcFind = "ADD2_INT"

        let results = [
            if pulse then
                x <- xInit + 1
                yield! drawPulseCoil (xInit, y, tagCoil, funcSizeY)
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

        let newY = if pulse then funcSizeY else funcSizeY-1
        { NewY = newY; PositionedRungXmls = results}


    let drawCmdCopy(tagCoil:IExpressionTerminal, fromTag:CommandTag, toTag:CommandTag, xInit, y, (pulse:bool)) : CoordinatedRungXmlsWithNewY =
        if fromTag.Size() <> toTag.Size() then
            failwithlog $"Tag Compare size error {fromTag.ToText()}{fromTag.SizeString},  {toTag.ToText()}({toTag.SizeString})"

        let mutable x = xInit
        let funcSizeY = 3
        let func = "MOVE"
        let funcFind = func + "_" + fromTag.SizeString

        let results = [
            if pulse then
                //Pulse Command 결과출력
                x <- xInit + 1
                yield! drawPulseCoil (xInit, y, tagCoil, funcSizeY)
            else
                //Command 결과출력
                x <- xInit
                createFBParameterXml (tagCoil.PLCTagName)  (x+1) (y)


            //함수 그리기
            createFB funcFind func "" func x y
            createFBParameterXml (fromTag.ToText())  (x-1) (y+1)
            createFBParameterXml (toTag.ToText())  (x+1) (y+1)
        ]

        let newY = if pulse then funcSizeY else funcSizeY-1
        { NewY = newY; PositionedRungXmls = results}



    let drawFunctionBlockInstance(cmd:XgiCommand, x, y) =
        //Command instance 객체생성
        let inst, func = cmd.Instance |> fun (inst, varType) -> inst, varType.ToString()
        [
            createFB func func inst func x y

            //Command 결과출력
            //createFBParameterXml (cmd.CoilTerminalTag.PLCTagName)  (x+1) (y)
        ]

    // <timer>
    let drawCommand(cmd:XgiCommand, x, y, lineConnectionStartX) =       // lineConnectionStartX : 일반적으로 x+1
        let results = ResizeArray<CoordinatedRungXml>()

        //FunctionBlock, Function 까지 연장선 긋기
        let needNewLineFeed = (x % minFBCellX) >= 6
        let numLineSpan = x / minFBCellX
        let mutable newX = max (x + 1) ((1 + numLineSpan) * minFBCellX  - 3)
        if x  < newX - 1 then
            //newX <- getFBCellX x
            results.Add( {Coordinate = coord newX y; Xml = mutiEndLine lineConnectionStartX  (newX - 1) y})
        //else
        //    results.Add( {Position = coord newX y; Xml=mutiEndLine (x + 1) (minFBCellX + newX - 1) y})

        //FunctionBlock, Function 그리기
        let { NewY = newY; PositionedRungXmls = result} =
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
                | CounterMode(cmdCoil, resetTag, count) -> drawCmdCounter(cmdCoil, resetTag, count, newX, y)
            | _ ->
                failwithlog "Unknown CommandType"

        results.AddRange(result)

        newY, (results |> List.ofSeq)


