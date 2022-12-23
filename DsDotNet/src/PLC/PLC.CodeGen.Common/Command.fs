namespace PLC.CodeGen.Common

open System.Diagnostics
open System.Runtime.CompilerServices
open Engine.Common.FS
open System
open Engine.Core

//open Dual.Core.Prelude
//open Dual.Core.Prelude.IEC61131

[<AutoOpen>]
module Command =

    type VarKind =
        | None     = 0
        | Variable = 1
        | Constant = 2


    /// Command 를 위한 Tag
    type CommandTag(tag:string, size:Size, kind:VarKind) =
        interface IExpressionTerminal with
            member x.PLCTagName = tag
        member x.Size() = size
        member x.SizeString =
            match size with
            | IEC61131.Size.Bit   -> "BOOL"
            | IEC61131.Size.Byte  -> "BYTE"
            | IEC61131.Size.Word  -> "WORD"
            | IEC61131.Size.DWord -> "DWORD"
            |_-> failwithlog "Unknown tag Size"
        member x.ToText() = tag
        member x.VarKind() = kind


    type IFunctionCommand =
        abstract member TerminalEndTag: IExpressionTerminal with get

    ///CoilOutput은 단일 출력을 내보내는 형식
    and CoilOutput =
        | CoilMode       of IExpressionTerminal
        | PulseCoilMode  of IExpressionTerminal
        | NPulseCoilMode of IExpressionTerminal
        | ClosedCoilMode of IExpressionTerminal
        | SetCoilMode    of IExpressionTerminal
        | ResetCoilMode  of IExpressionTerminal
    with
        interface IFunctionCommand with
            member this.TerminalEndTag: IExpressionTerminal =
                match this with
                | CoilMode      (endTag) -> endTag
                | PulseCoilMode (endTag) -> endTag
                | NPulseCoilMode(endTag) -> endTag
                | ClosedCoilMode(endTag) -> endTag
                | SetCoilMode   (endTag) -> endTag
                | ResetCoilMode (endTag) -> endTag

    ///FunctionPures은 Copy와 같은 Instance가 필요없는 Function에 해당
    and FunctionPure =
        | CopyMode  of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (fromA, toB)
        | CompareGT of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "<"
        | CompareLT of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  ">"
        | CompareGE of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "<="
        | CompareLE of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  ">="
        | CompareEQ of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "=="
        | CompareNE of IExpressionTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "!="
        | Add of IExpressionTerminal *  CommandTag  * int //endTag * Tag + (-+int)
    with
        interface IFunctionCommand with
            member this.TerminalEndTag: IExpressionTerminal =
                match this with
                | CopyMode  (endTag, _) -> endTag
                | CompareGT (endTag, _) -> endTag
                | CompareLT (endTag, _) -> endTag
                | CompareGE (endTag, _) -> endTag
                | CompareLE (endTag, _) -> endTag
                | CompareEQ (endTag, _) -> endTag
                | CompareNE (endTag, _) -> endTag
                | Add       (endTag, _, _) -> endTag


        member private x.GetTerminal(tag:CommandTag) = if(tag.VarKind() =  VarKind.Variable) then [ tag :> IExpressionTerminal ] else List.empty

        member x.UsedCommandTags() =
            match x with
            | CopyMode  (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareGT (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareLT (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareGE (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareLE (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareEQ (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | CompareNE (endTag, (a, b)) -> [ endTag ]  @ x.GetTerminal(a) @ x.GetTerminal(b)
            | Add       (endTag, a, b)   -> [ endTag ]  @ x.GetTerminal(a)


    ///FunctionBlocks은 Timer와 같은 현재 측정 시간을 저장하는 Instance가 필요있는 Command 해당
    and FunctionBlock =
        | TimerMode of TimerStatement //endTag, time
        | CounterMode of IExpressionTerminal *  CommandTag  * int  //endTag, countResetTag, count
    with
        member x.GetInstanceText() =
            match x with
            | TimerMode(timerStruct) -> $"T_{timerStruct.Timer.Name}"
            | CounterMode(tag, resetTag, count) ->  $"C_{tag.PLCTagName}"
        member x.UsedCommandTags() =
            match x with
            //| TimerMode(tag, time) -> [ tag ]
            | TimerMode(timerStruct) -> failwith "ERROR"
            | CounterMode(tag, resetTag, count) -> [ tag; resetTag ]

        interface IFunctionCommand with
            member this.TerminalEndTag: IExpressionTerminal =
                match this with
                //| TimerMode(tag, time) -> tag
                | TimerMode(timerStruct) -> timerStruct.Timer.DN
                | CounterMode(tag, resetTag, count) -> tag


    /// 실행을 가지는 type
    type CommandTypes =
        | CoilCmd          of CoilOutput
        | FunctionCmd      of FunctionPure
        /// Timer, Counter 등
        | FunctionBlockCmd of FunctionBlock

    let createPLCCommandCopy(endTag, from, toTag) = FunctionPure.CopyMode(endTag, (from, toTag))
    let createPLCCommandCompare(endTag, op, left, right) =
        match op with
        | GT ->FunctionPure.CompareGT(endTag, (left, right))
        | GE ->FunctionPure.CompareGE(endTag, (left, right))
        | EQ ->FunctionPure.CompareEQ(endTag, (left, right))
        | LE ->FunctionPure.CompareLE(endTag, (left, right))
        | LT ->FunctionPure.CompareLT(endTag, (left, right))
        | NE ->FunctionPure.CompareNE(endTag, (left, right))

    let createPLCCommandAdd(endTag, tag, value)          = FunctionPure.Add(endTag, tag, value)
    //let createPLCCommandTimer(endTag, time)              = FunctionBlock.TimerMode(endTag, time)
    let createPLCCommandCounter(endTag, resetTag, count) = FunctionBlock.CounterMode(endTag, resetTag , count)

