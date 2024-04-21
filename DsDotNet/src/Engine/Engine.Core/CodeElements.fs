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

    type FunctionTypes =
        | DuFuncUnDefined
        | DuFuncAdd
        | DuFuncSub
        | DuFuncMove
        | DuFuncCompare
        | DuFuncTimer
        | DuFuncNot
        member x.ToText() =
            match x  with
            | DuFuncUnDefined -> ""
            | DuFuncAdd -> "$add"
            | DuFuncSub -> "$sub"
            | DuFuncMove -> "$mov"
            | DuFuncCompare -> "$c"
            | DuFuncTimer -> "$t"
            | DuFuncNot -> "$n"

        member x.IsCommand() =
            match x  with
            | DuFuncAdd
            | DuFuncSub
            | DuFuncMove -> true
            | DuFuncCompare 
            | DuFuncTimer 
            | DuFuncNot -> false
            | _ -> failwithlog $"error Function UnDefined" 

    let getFunctionType (text:string) = 
        match text.ToLower()  with
            | "$add" -> DuFuncAdd 
            | "$sub" -> DuFuncSub 
            | "$mov" -> DuFuncMove  
            | "$c" -> DuFuncCompare 
            | "$t" -> DuFuncTimer 
            | "$n" -> DuFuncNot 
            | _ -> failwithlog $"error {text} is not functionType" 

    let getFunction (text:string) = 
        let text = text.Trim()
        if not <| text.StartsWith "$"
        then failwithlog "function text start keyword is '$' ex)$m 100 R100"
        //function type parameters
        let funcType, parameters =
            let parts = text.Split(" ") |> List.ofArray
            match parts with
            | head :: xs -> head, xs.ToArray()
            | [] -> failwith "Input line is empty or improperly formatted"

        funcType, parameters


    type Func(name:string) =
        member x.Name = name
        member val FunctionType = DuFuncUnDefined with get, set
        member val Parameters = ResizeArray<string>()
        member x.ToDsText() = 
            if x.FunctionType = DuFuncUnDefined
            then  ""
            else  
                $"""{x.FunctionType.ToText()} {String.Join(" ", x.Parameters)}""".Trim()
           
    [<Extension>]
    type SystemFuncExt =
        [<Extension>] 
        static member GetDelayTime (x:Func) =
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
        static member GetRingCount (x:Func) =
            x.Parameters |> Seq.head |> CountUnitType.Parse
