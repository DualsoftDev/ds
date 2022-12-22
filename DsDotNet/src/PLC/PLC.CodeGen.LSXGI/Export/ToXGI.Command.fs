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
            match cmdType with
            | CoilCmd (cc) -> cc :> IFunctionCommand |> fun f -> f.TerminalEndTag
            | FunctionCmd (fc) -> fc :> IFunctionCommand |> fun f -> f.TerminalEndTag
            | FunctionBlockCmd (fbc) -> fbc :> IFunctionCommand |> fun f -> f.TerminalEndTag

        member x.UsedCommandTags with get() =
            match cmdType with
            | CoilCmd (cc) -> seq{x.CoilTerminalTag}
            | FunctionCmd (fc) ->  fc.UsedCommandTags()
            | FunctionBlockCmd (fbc) -> fbc.UsedCommandTags()

        member x.HasInstance with get() = match cmdType with | FunctionBlockCmd (fbc) ->  true |_-> false
        member x.Instance with get() =
            match cmdType with
            | FunctionBlockCmd (fbc) ->
                match fbc with
                | TimerMode(tag, time) -> fbc.GetInstanceText(), VarType.TON
                | CounterMode(tag, resetTag, count) -> fbc.GetInstanceText(), VarType.CTU_INT
            |_-> failwithlog "do not make instanceTag"

        member x.LDEnum with get() =
            match cmdType with
                 | CoilCmd (cc) ->
                        match cc with
                        |CoilMode(_) -> ElementType.CoilMode
                        |ClosedCoilMode(_) -> ElementType.ClosedCoilMode
                        |SetCoilMode(_) -> ElementType.SetCoilMode
                        |ResetCoilMode(_) -> ElementType.ResetCoilMode
                        |PulseCoilMode(_) -> ElementType.PulseCoilMode
                        |NPulseCoilMode(_) -> ElementType.NPulseCoilMode
                 | FunctionCmd (_) -> ElementType.VertFBMode
                 | FunctionBlockCmd (_) -> ElementType.VertFBMode

            /// Coil의 부정 Command를 반환한다.
         member x.ReverseCmd () =
                    match cmdType with
                    | CoilCmd (cc) ->
                           match cc with
                            | CoilMode(tag) -> XgiCommand(CoilCmd(CoilOutput.ClosedCoilMode(tag)))
                            | ClosedCoilMode(tag) -> XgiCommand(CoilCmd(CoilOutput.CoilMode(tag)))
                            | _ -> failwithlogf "This ReverseCmd is not support"
                    | _ -> failwithlogf "This ReverseCmd is not support"

    let createOutputCoil(tag)    = XgiCommand(CoilCmd(CoilOutput.CoilMode(tag)))
    let createOutputCoilNot(tag) = XgiCommand(CoilCmd(CoilOutput.ClosedCoilMode(tag)))
    let createOutputSet(tag)     = XgiCommand(CoilCmd(CoilOutput.SetCoilMode(tag)))
    let createOutputRst(tag)     = XgiCommand(CoilCmd(CoilOutput.ResetCoilMode(tag)))
    let createOutputPulse(tag)   = XgiCommand(CoilCmd(CoilOutput.PulseCoilMode(tag)))
    let createOutputNPulse(tag)  = XgiCommand(CoilCmd(CoilOutput.NPulseCoilMode(tag)))

    let createOutputTime(tag, time)  = XgiCommand(FunctionBlockCmd(FunctionBlock.TimerMode(tag, time)))
    let createOutputCount(tag, resetTag, cnt)  = XgiCommand(FunctionBlockCmd(FunctionBlock.CounterMode(tag, resetTag, cnt)))
    let createOutputCopy(tag, tagA, tagB) = XgiCommand(FunctionCmd(FunctionPure.CopyMode(tag, (tagA, tagB))))
    let createOutputAdd(tag, targetTag, addValue:int) = XgiCommand(FunctionCmd(FunctionPure.Add(tag, targetTag, addValue)))

    let createOutputCompare(tag, opComp:OpComp, tagA, tagB) =
        match opComp with
        | GT -> XgiCommand(FunctionCmd(FunctionPure.CompareGT(tag, (tagA, tagB))))
        | GE -> XgiCommand(FunctionCmd(FunctionPure.CompareGE(tag, (tagA, tagB))))
        | EQ -> XgiCommand(FunctionCmd(FunctionPure.CompareEQ(tag, (tagA, tagB))))
        | LE -> XgiCommand(FunctionCmd(FunctionPure.CompareLE(tag, (tagA, tagB))))
        | LT -> XgiCommand(FunctionCmd(FunctionPure.CompareLT(tag, (tagA, tagB))))
        | NE -> XgiCommand(FunctionCmd(FunctionPure.CompareNE(tag, (tagA, tagB))))

    let drawCmdTime(coil:IExpressionTerminal, time:int, x, y) =
        let results = ResizeArray<int * string>()
        let funcSizeY = 3
        //Command 속성입력
        results.Add(createPA (sprintf "T#%dMS" time) (x-1) (y+1))
        funcSizeY-1, results

    let drawCmdCount(coil:IExpressionTerminal, reset:IExpressionTerminal, count:int, x, y) =
        let results = ResizeArray<int * string>()
        let funcSizeY = 4
        //Command 속성입력
        results.Add(createPA (reset.ToText()) (x-1) (y+1))
        results.Add(createPA (sprintf "%d" count) (x-1) (y+2))

        funcSizeY-1, results

    let drawCmdCompare(coil:IExpressionTerminal, opComp:OpComp, leftA:CommandTag, leftB:CommandTag, x, y) =
        let results = ResizeArray<int * string>()
        let funcSizeY = 4

        if(leftA.Size() <> leftB.Size())
        then failwithlog (sprintf "Tag Compare size error %s(%s),  %s(%s)" (leftA.ToText()) (leftA.SizeString) (leftB.ToText()) (leftB.SizeString))

        let opCompType = leftA.SizeString
        let func = opComp.ToText
        let funcFind =
            if(opComp = OpComp.NE)
            then sprintf "%s_%s" opComp.ToText opCompType
            else sprintf "%s2_%s" opComp.ToText opCompType

        results.Add(createFB funcFind func "" opComp.ToText x y )
        results.Add(createPA (leftA.ToText()) (x-1) (y+1))
        results.Add(createPA (leftB.ToText()) (x-1) (y+2))
        results.Add(createPA (coil.ToText())  (x+1) (y+1))

        funcSizeY-1, results

    let drawCmdAdd(tagCoil:IExpressionTerminal, targetTag:CommandTag, addValue:int, xInit, y, (pulse:bool)) =
        let results = ResizeArray<int * string>()
        let mutable x = xInit
        let funcSizeY = 4

        if(pulse)
        then
            x <- xInit + 1
            results.AddRange(drawPulseCoil (xInit, y, tagCoil, funcSizeY))
        else
            x <- xInit
            //Command 결과출력
            results.Add(createPA (tagCoil.ToText())  (x+1) (y))

        let func = "ADD"
        //test ahn : Rear UINT SINT 등등 타입 추가  필요
        //let funcFind = func + "2_" + targetTag.SizeString
        let funcFind = "ADD2_INT"

        //Pulse시 증감 처리
        //results.AddRange(drawRising(x, y))
        //함수 그리기
        results.Add(createFB funcFind func "" func x y )
        results.Add(createPA (targetTag.ToText())    (x-1) (y+1))
        results.Add(createPA (targetTag.ToText())    (x+1) (y+1))
        results.Add(createPA (addValue.ToString())   (x-1) (y+2))


        (if(pulse) then funcSizeY else funcSizeY-1), results


    let drawCmdCopy(tagCoil:IExpressionTerminal, fromTag:CommandTag, toTag:CommandTag, xInit, y, (pulse:bool)) =
        let results = ResizeArray<int * string>()
        if(fromTag.Size() <> toTag.Size())
            then failwithlog (sprintf "Tag Compare size error %s(%s),  %s(%s)" (fromTag.ToText()) (fromTag.SizeString) (toTag.ToText()) (toTag.SizeString))

        let mutable x = xInit
        let funcSizeY = 3

        if(pulse)
        then
            //Pulse Command 결과출력
            x <- xInit + 1
            results.AddRange(drawPulseCoil (xInit, y, tagCoil, funcSizeY))
        else
            //Command 결과출력
            x <- xInit
            results.Add(createPA (tagCoil.ToText())  (x+1) (y))

        let func = "MOVE"
        let funcFind = func + "_" + fromTag.SizeString

        //함수 그리기
        results.Add(createFB funcFind func "" func x y )
        results.Add(createPA (fromTag.ToText())  (x-1) (y+1))
        results.Add(createPA (toTag.ToText())  (x+1) (y+1))

        (if(pulse) then funcSizeY else funcSizeY-1), results


    let drawFunctionBlockInstance(cmd:XgiCommand, x, y) =
        let results = ResizeArray<int * string>()
        //Command instance 객체생성
        let inst, func = cmd.Instance |> fun (inst, varType) -> inst, varType.ToString()
        results.Add(createFB func func inst func x y)
        //Command 결과출력
        results.Add(createPA (cmd.CoilTerminalTag.ToText())  (x+1) (y))

        results


    let drawCommand(cmd:XgiCommand, x, y) =
        let results = ResizeArray<int * string>()

        //FunctionBlock, Function 까지 연장선 긋기
        let newX = getFBCellX x
        results.Add(((coord newX y), mutiEndLine (x + 1) (newX - 1) y))

        //FunctionBlock, Function 그리기
        let newY, result =
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
                | TimerMode(cmdCoil, time) -> drawCmdTime(cmdCoil,  time, newX, y)
                | CounterMode(cmdCoil, resetTag, count) -> drawCmdCount(cmdCoil, resetTag, count, newX, y)
            |_-> failwithlog "Unknown CommandType"

        results.AddRange(result)

        newY, results


