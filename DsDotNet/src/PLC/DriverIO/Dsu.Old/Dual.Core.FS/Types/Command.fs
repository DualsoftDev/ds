namespace Old.Dual.Core.Types

open System.Diagnostics
open System.Runtime.CompilerServices
open Old.Dual.Common
open System
open Old.Dual.Core.Prelude
open Old.Dual.Core.Prelude.IEC61131

[<AutoOpen>]
module Command =
    
    type VarKind = 
        | None              = 0
        | Variable          = 1
        | Constant          = 2


    /// Command 를 위한 Tag
    type CommandTag(tag:string, size:Size, kind:VarKind) = 
        interface IExpressionTerminal with
            member x.ToText() = tag
            member x.Equals t = 
                t :? CommandTag 
                && x.ToText() = t.ToText() 
                && x.Size() = (t :?> CommandTag).Size()
                && x.VarKind() = (t :?> CommandTag).VarKind()
        member x.Size() = size
        member x.SizeString = 
            match size with
            | IEC61131.Size.Bit -> "BOOL"
            | IEC61131.Size.Byte -> "BYTE"
            | IEC61131.Size.Word -> "WORD"
            | IEC61131.Size.DWord -> "DWORD"
            |_-> failwithlog "Unknown tag Size"
        member x.ToText() = tag
        member x.VarKind() = kind
    
      
    type IFunctionCommand =
        abstract member TerminalEndTag: IExpressionTerminal with get
    ///CoilOutput은 단일 출력을 내보내는 형식
    and CoilOutput =
        | CoilMode of IExpressionTerminal 
        | PulseCoilMode of IExpressionTerminal 
        | NPulseCoilMode of IExpressionTerminal 
        | ClosedCoilMode of IExpressionTerminal 
        | SetCoilMode of IExpressionTerminal 
        | ResetCoilMode of IExpressionTerminal 
        interface IFunctionCommand with
            member this.TerminalEndTag =
                match this with
                | CoilMode(endTag) -> endTag
                | PulseCoilMode(endTag) -> endTag
                | NPulseCoilMode(endTag) -> endTag
                | ClosedCoilMode(endTag) -> endTag
                | SetCoilMode(endTag) -> endTag
                | ResetCoilMode(endTag) -> endTag


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
                | CopyMode  (endTag, (a, b)) -> endTag
                | CompareGT (endTag, (a, b)) -> endTag
                | CompareLT (endTag, (a, b)) -> endTag
                | CompareGE (endTag, (a, b)) -> endTag
                | CompareLE (endTag, (a, b)) -> endTag
                | CompareEQ (endTag, (a, b)) -> endTag
                | CompareNE (endTag, (a, b)) -> endTag
                | Add       (endTag, a, b)   -> endTag
        

        member private x.GetTerminal(tag:CommandTag) = if(tag.VarKind() =  VarKind.Variable) then seq {tag :> IExpressionTerminal} else Seq.empty

        member x.UsedCommandTags() =
            match x with
            | CopyMode  (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareGT (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareLT (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareGE (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareLE (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareEQ (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | CompareNE (endTag, (a, b)) -> seq{endTag}  @@ x.GetTerminal(a) @@ x.GetTerminal(b)
            | Add       (endTag, a, b)   -> seq{endTag}  @@ x.GetTerminal(a) 



    ///FunctionBlocks은 Timer와 같은 현재 측정 시간을 저장하는 Instance가 필요있는 Command 해당
    and FunctionBlock =
        | TimerMode of IExpressionTerminal * int    //endTag, time
        | CounterMode of IExpressionTerminal *  CommandTag  * int  //endTag, countResetTag, count 
        member x.GetInstanceText() =
            match x with
            | TimerMode(tag, time) -> sprintf "T_%s" (tag.ToText())
            | CounterMode(tag, resetTag, count) ->  sprintf "C_%s" (tag.ToText())
        member x.UsedCommandTags() =
            match x with
            | TimerMode(tag, time) -> seq{tag}
            | CounterMode(tag, resetTag, count) -> seq{tag;resetTag}

        interface IFunctionCommand with
            member this.TerminalEndTag: IExpressionTerminal =  
                match this with
                | TimerMode(tag, time) -> tag
                | CounterMode(tag, resetTag, count) -> tag


    /// 실행을 가지는 type
    type CommandTypes =
        | CoilCmd of CoilOutput
        | FunctionCmd of FunctionPure
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
    let createPLCCommandAdd(endTag, tag, value) = FunctionPure.Add(endTag, tag, value)
    let createPLCCommandTimer(endTag, time) = FunctionBlock.TimerMode(endTag, time)
    let createPLCCommandCounter(endTag, resetTag, count) = FunctionBlock.CounterMode(endTag, resetTag , count)
   
[<AutoOpen>]
module CommandTypesM =
    let replaceEndTag tag (func:IFunctionCommand) =
        match func with
        | :? CoilOutput as cc ->
            match cc with
            | CoilMode(_) -> CoilMode(tag)
            | PulseCoilMode(_) -> PulseCoilMode(tag)
            | NPulseCoilMode(_) -> NPulseCoilMode(tag)
            | ClosedCoilMode(_) -> ClosedCoilMode(tag)
            | SetCoilMode(_) -> SetCoilMode(tag)
            | ResetCoilMode(_) -> ResetCoilMode(tag)
            :> IFunctionCommand
        | :? FunctionPure as fc ->
            match fc with
            | CopyMode  (_, (a, b)) -> CopyMode  (tag, (a, b))
            | CompareGT (_, (a, b)) -> CompareGT (tag, (a, b))
            | CompareLT (_, (a, b)) -> CompareLT (tag, (a, b))
            | CompareGE (_, (a, b)) -> CompareGE (tag, (a, b))
            | CompareLE (_, (a, b)) -> CompareLE (tag, (a, b))
            | CompareEQ (_, (a, b)) -> CompareEQ (tag, (a, b))
            | CompareNE (_, (a, b)) -> CompareNE (tag, (a, b))
            | Add       (_, a, b)   -> Add       (tag, a, b)  
            :> IFunctionCommand
        | :? FunctionBlock as fb ->
            match fb with
            | TimerMode(_, time)              -> TimerMode(tag, time)             
            | CounterMode(_, resetTag, count) -> CounterMode(tag, resetTag, count)
            :> IFunctionCommand
        | _ -> func

            