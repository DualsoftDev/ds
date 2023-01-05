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
        interface INamedExpressionizableTerminal with
            member x.StorageName = tag
            member x.ToText() = tag
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
        | CompareGT of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "<"
        | CompareLT of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  ">"
        | CompareGE of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "<="
        | CompareLE of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  ">="
        | CompareEQ of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "=="
        | CompareNE of INamedExpressionizableTerminal *  (CommandTag * CommandTag) //endTag * (leftA, rightB)  "!="
        | Add of INamedExpressionizableTerminal *  CommandTag  * int //endTag * Tag + (-+int)
    with
        interface IFunctionCommand with
            member this.TerminalEndTag: INamedExpressionizableTerminal =
                match this with
                | CopyMode  (endTag, _) -> endTag
                | CompareGT (endTag, _) -> endTag
                | CompareLT (endTag, _) -> endTag
                | CompareGE (endTag, _) -> endTag
                | CompareLE (endTag, _) -> endTag
                | CompareEQ (endTag, _) -> endTag
                | CompareNE (endTag, _) -> endTag
                | Add       (endTag, _, _) -> endTag


        member private x.GetTerminal(tag:CommandTag) = if(tag.VarKind() =  VarKind.Variable) then [ tag :> INamedExpressionizableTerminal ] else List.empty

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



