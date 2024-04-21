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
        | NoDefined
        | M
        | C
        | T
        | N
        member x.ToText() =
            match x  with
            | NoDefined -> ""
            | M -> "m"
            | C -> "c"
            | T -> "t"
            | N -> "n"

    let getFunctionType (text:string) = 
        match text.ToLower()  with
            | "m" -> M 
            | "c" -> C 
            | "t" -> T 
            | "n" -> N 
            | _ -> failwithlog $"error {text} is not functionType" 

    let getFunction (text:string) = 
        let text = text.Trim()
        if not <| text.StartsWith "$"
        then failwithlog "function text start keyword is '$' ex)$m 100 R100"
        let line = text.TrimStart('$')
        //function Name
        let name = line.Substring(0,1).ToLower()
        //function Parameters
        let parameters = line.Substring(1,line.Length-1).Split(' ').Select(fun f->f.Trim())
                          |> Seq.filter(fun f->f<>"")
                          |> Seq.toArray
        name, parameters


    type Func(name:string) =
        member x.Name = name
        member val FunctionType = FunctionTypes.NoDefined with get, set
        member val Parameters = ResizeArray<string>()
        member x.ToDsText() = 
            if x.FunctionType = FunctionTypes.NoDefined
            then  ""
            else  
                $"""${x.FunctionType.ToText()} {String.Join(" ", x.Parameters)}""".Trim()
           
    [<Extension>]
    type SystemFuncExt =
        [<Extension>] static member GetDelayTime (x:Func) =
                        let presetTime = x.Parameters.Head().ToLower()
                        let timetype = Regex.Replace(presetTime, @"\d", "");//문자 추출
                        let preset   = Regex.Replace(presetTime, @"\D", "");//숫자 추출

                        match timetype with
                        | ""  //단위 없으면 msec
                        | "ms"| "msec"-> preset|> CountUnitType.Parse
                        | "s" | "sec" -> let presetMsec = ((preset |> Convert.ToInt32) * 1000)
                                         presetMsec.ToString() |> CountUnitType.Parse

                        | _-> failwithlog "timer format Error"


        [<Extension>] static member GetRingCount (x:Func) =
                        x.Parameters |> Seq.head |> CountUnitType.Parse
