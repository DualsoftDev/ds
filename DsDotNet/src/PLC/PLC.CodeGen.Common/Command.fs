namespace PLC.CodeGen.Common

open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module Command =

    type VarKind =
        | None     = 0
        | Variable = 1
        | Constant = 2


    /// Command 를 위한 Tag
    type CommandTag(tag:string, size:Size, kind:VarKind) =
        interface INamedExpressionizableTerminal with
            member _.StorageName = tag
            member _.ToText() = tag
        member _.Size() = size
        member _.SizeString =
            match size with
            | IEC61131.Size.Bit   -> "BOOL"
            | IEC61131.Size.Byte  -> "BYTE"
            | IEC61131.Size.Word  -> "WORD"
            | IEC61131.Size.DWord -> "DWORD"
            |_-> failwithlog "Unknown tag Size"
        member _.ToText() = tag
        member _.VarKind() = kind


    type IFunctionCommand =
        abstract member TerminalEndTag: INamedExpressionizableTerminal with get

    ///CoilOutput은 단일 출력을 내보내는 형식
    and CoilOutputMode =
        | COMCoil       of INamedExpressionizableTerminal
        | COMPulseCoil  of INamedExpressionizableTerminal
        | COMNPulseCoil of INamedExpressionizableTerminal
        | COMClosedCoil of INamedExpressionizableTerminal
        | COMSetCoil    of INamedExpressionizableTerminal
        | COMResetCoil  of INamedExpressionizableTerminal
    with
        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
                match this with
                | COMCoil      (endTag) -> endTag
                | COMPulseCoil (endTag) -> endTag
                | COMNPulseCoil(endTag) -> endTag
                | COMClosedCoil(endTag) -> endTag
                | COMSetCoil   (endTag) -> endTag
                | COMResetCoil (endTag) -> endTag

    ///FunctionPures은 Copy와 같은 Instance가 필요없는 Function에 해당
    and FunctionPure =
        | CopyMode  of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (fromA, toB)
        | FunctionCompare of name:string * output:INamedExpressionizableTerminal * arguments:IExpression list //endTag * FunctionName * Tag list
        | FunctionArithematic of name:string * output:INamedExpressionizableTerminal * arguments:IExpression list //endTag * FunctionName * Tag list
    with
        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
                match this with
                | CopyMode  (endTag, _) -> endTag
                | FunctionCompare (_, endTag, _) -> endTag
                | FunctionArithematic (_, endTag, _) -> endTag


