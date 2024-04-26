namespace Engine.Core

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Dual.Common.Core.FS

[<AutoOpen>]
module CodeElements =
    (*
      [variables] = { //이름 = (타입,초기값)
        R100 = (word, 0)
      }
     *)
    /// Variable Declaration: name = (type, init)  CodeBlock 사용 ? 선택 필요
    type VariableData(name:string, varType:DataType, initValue:string) =
        member _.Name = name
        member _.Type = varType
        member _.InitValue = initValue
        member _.ToDsText() =
            let genTargetText name (varType:DataType) value =
                $"{name} = ({varType.ToText()} , {value})"

            if initValue = TextAddrEmpty then
                let forcedValue =
                    match varType with
                    | DuFLOAT32 -> "0.0f"
                    | DuFLOAT64 -> "0.0"
                    | DuINT8    -> "0y"
                    | DuUINT8   -> "0uy"
                    | DuINT16   -> "0s"
                    | DuUINT16  -> "0us"
                    | DuINT32   -> "0"
                    | DuUINT32  -> "0u"
                    | DuINT64   -> "0L"
                    | DuUINT64  -> "0UL"
                    | DuBOOL    -> "false"
                    | DuCHAR    -> "''"
                    | DuSTRING  -> "\"\""
                genTargetText name varType forcedValue
            else
                genTargetText name varType initValue

    type OperatorFunctionTypes =
        | DuOPUnDefined
        | DuOPNot
        | DuOPTimer
        | DuOPCompare
        member x.ToText() =
            match x  with
            | DuOPUnDefined -> ""
            | DuOPNot -> "not"
            | DuOPTimer -> "time"
            | DuOPCompare -> "if"
     
    let tryGetOperatorType (text:string) = 
        match text.ToLower()  with
            | "not" -> DuOPNot       |> Some   
            | "time" -> DuOPTimer     |> Some   
            | "if" -> DuOPCompare   |> Some   
            | _ -> None
            
    let getOperatorTypeNArgs (text:string) = 
        let text = text.Trim()
        let funcType, parameters =
            let parts = text.Split(" ") |> List.ofArray
            match parts with
            | head :: xs -> head, xs.ToArray()
            | [] -> failwith "Input line is empty or improperly formatted"

        funcType, parameters

    type CommandFunctionTypes =
        | DuCMDUnDefined
        | DuCMDCode

        member x.ToText() =
            match x  with
            | DuCMDUnDefined -> ""
            | DuCMDCode -> "cmd"

    let tryGetCommandType (text:string) = 
        if text <> "" && tryGetOperatorType text = None  //operator type이 아닌 경우
        then DuCMDCode  |> Some  
        else None


    [<AbstractClass>]
    type Func(name:string) =
        member x.Name = name
        member x.ToDsText() =
            match x with
            | :? OperatorFunction as op -> op.ToDsText()
            | :? CommandFunction as cmd -> cmd.ToDsText()
            | _ -> failwith "Not Supported"

    ///Comparison, Logical, ... Operators  (비교, 논리 연산자)
    and OperatorFunction(name:string) =
        inherit Func(name)
        member val OperatorType = DuOPUnDefined with get, set
        member val Parameters = ResizeArray<string>()
        member x.ToDsText() = 
            if x.OperatorType <> DuOPUnDefined   
            then $"""{x.OperatorType.ToText()} {String.Join(" ", x.Parameters)}""".Trim()
            else ""

    ///Copy, Assign, ... Commands (복사, 대입 명령)
    and CommandFunction(name:string) =
        inherit Func(name)
        member val CommandType = DuCMDUnDefined with get, set
        member val CommandCode = "" with get, set
        member x.ToDsText() = x.CommandCode

    [<Extension>]
    type SystemFuncExt =
        [<Extension>] 
        static member GetDelayTime (x:OperatorFunction) =
            let presetTime = x.Parameters.Head().ToLower()
            let timetype = Regex.Replace(presetTime, @"\d", "");//문자 추출
            let preset   = Regex.Replace(presetTime, @"\D", "");//숫자 추출

            match timetype with
            | ""  //단위 없으면 msec
            | "ms"| "msec"-> preset|> CountUnitType.Parse
            | "s" | "sec" -> 
                let presetMsec = ((preset |> Convert.ToInt32) * 1000)
                presetMsec.ToString() |> CountUnitType.Parse

            | _-> failwithlog "timer format Error"


        [<Extension>] 
        static member GetRingCount (x:OperatorFunction) =
            x.Parameters |> Seq.head |> CountUnitType.Parse
