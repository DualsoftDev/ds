namespace Engine.Core

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Dual.Common.Core.FS

[<AutoOpen>]
module rec CodeElements =
    type VariableData(name:string, varType:DataType) =
        member _.Name = name
        member _.Type = varType
        member _.ToDsText() = $"{varType.ToText()} {name}"
        member val InitValue = getNull<string>() with get, set
           
    type OperatorFunctionTypes =
        | DuOPUnDefined
        | DuOPCode
        | DuOPNot
        | DuOPTimer
        member x.ToText() =
            match x  with
            | DuOPUnDefined|DuOPCode -> ""
            | DuOPNot -> "$not"
            | DuOPTimer -> "$time"
     
    let getOperatorType (text:string) = 
        match text.ToLower()  with
            | "$not" -> DuOPNot      
            | "$time" -> DuOPTimer   
            | _ -> 
                if text <> "" 
                then DuOPCode 
                else failWithLog "operatorType is empty error"
            
    
    let getOperatorTypeNArgs (text:string) = 
        let text = text.Trim()
        let funcType, parameters =
            let parts = text.Split(" ") |> List.ofArray
            match parts with
            | head :: xs -> head, xs.ToArray()
            | [] -> failwith "Input line is empty or improperly formatted"

        (getOperatorType funcType), parameters

    let updateOperator (op:OperatorFunction) (funcBodyText:string) = 
        let opType, parms = getOperatorTypeNArgs (funcBodyText) 
        match opType with
        |DuOPCode ->
            op.OperatorType <- DuOPCode 
            op.OperatorCode <- funcBodyText
        |_->
            op.OperatorType <- opType 
            op.Parameters <- parms.ToResizeArray()

    type CommandFunctionTypes =
        | DuCMDUnDefined
        | DuCMDCode


    [<AbstractClass>]
    type Func(name:string) =
        member x.Name = name
        member val Statements = StatementContainer()
        member x.ToDsText() =
            match x with
            | :? OperatorFunction as op -> op.ToDsText()
            | :? CommandFunction as cmd -> cmd.ToDsText()
            | _ -> failwith "Not Supported"

    ///Comparison, Logical, ... Operators  (비교, 논리 연산자)
    and OperatorFunction(name:string) =
        inherit Func(name)
        member val OperatorType = DuOPUnDefined with get, set
        member val OperatorCode = "" with get, set
        member val Parameters = ResizeArray<string>() with get, set
        member x.ToDsText() = 
            if x.OperatorType = DuOPCode   
            then x.OperatorCode
            else $"""{x.OperatorType.ToText()} {String.Join(" ", x.Parameters)}""".Trim()

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
