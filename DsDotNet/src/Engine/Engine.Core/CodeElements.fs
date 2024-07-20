namespace Engine.Core

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Dual.Common.Core.FS
open System.Collections.Generic

[<AutoOpen>]
module rec CodeElements =

    type VariableType = 
        | Mutable
        | Immutable
    
           
    type VariableData(name:string, varType:DataType, variableType:VariableType)  =
        inherit FqdnObject(name, createFqdnObject([||]))
        member x.Name = name
        member x.Type = varType
        member x.VariableType = variableType
        member x.ToDsText() = 
            match variableType with
            | Mutable ->  $"{varType.ToText()} {name}"
            | Immutable -> $"const {varType.ToText()} {name} = {x.InitValue}"
        member val InitValue = getNull<string>() with get, set
         
    //action 주소를 가지는 변수
    type ActionVariable(name:string, address:string, targetName:string, varType:DataType)  =
        inherit FqdnObject(name, createFqdnObject([||]))
        member x.Name = name
        member x.Address = address
        member x.TargetName = targetName
        member x.Type = varType

    type OperatorFunctionTypes =
        | DuOPUnDefined
        | DuOPCode
        | DuOPNot
        | DuOPTimer
        member x.ToText() =
            match x  with
            | (DuOPUnDefined | DuOPCode) -> ""
            | DuOPNot -> "$not"
            | DuOPTimer -> "$time"
     


    let updateOperator (op:OperatorFunction) (funcBodyText:string) = 
        if funcBodyText <> "" then 
            op.OperatorType <- DuOPCode 
            op.OperatorCode <- funcBodyText

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

        member x.ToDsText() = if x.OperatorCode = "" then TextSkip else x.OperatorCode

    ///Copy, Assign, ... Commands (복사, 대입 명령)
    and CommandFunction(name:string) =
        inherit Func(name)
        member val CommandType = DuCMDUnDefined with get, set
        member val CommandCode = "" with get, set

        member x.ToDsText() = if x.CommandCode = "" then TextSkip else x.CommandCode
    
    let addressPrint (addr:string) = if addr.IsNullOrEmpty() then TextAddrEmpty else addr

    type DevParaIO =
     {
        InPara : DevPara option 
        OutPara: DevPara option
    } 

    type DevPara = {
        DevName : string option  //In or Out Tag 이름
        DevType: DataType option
        DevValue: obj option
        DevTime: int option  //기본 ms 단위 parsing을 위해 끝에 ms 필수
    } with
        member x.Value = x.DevValue |> Option.toObj
        member x.Type  = x.DevType  |> Option.defaultValue DuBOOL //기본 타입 bool   
        member x.Name  = x.DevName  |> Option.defaultValue ""
        member x.Time  = x.DevTime 
        member x.IsDefaultParam  = 
                    x.DevName.IsNone 
                    && x.DevTime.IsNone
                    && x.Type = DuBOOL
                    && x.Value.IsNull()
        
        member x.ToTextWithAddress(addr:string) = 
            let address  = addressPrint addr
            let name  = x.DevName  |> Option.defaultValue ""
            let typ   = x.DevType  |> Option.map (fun t -> t.ToText()) |> Option.defaultValue ""
            let value = x.DevValue |> Option.map (fun v -> x.Type.ToStringValue(v)) |> Option.defaultValue ""
            let time  = x.DevTime  |> Option.map (fun t -> $"{t}ms") |> Option.defaultValue ""

            let parts = [ address; name; typ; value; time ]
            let result = parts |> List.filter (fun s -> not(String.IsNullOrEmpty(s))) |> String.concat ":"

            result

    let defaultDevParam() = 
        {   
            DevName = None
            DevType = None
            DevValue = None
            DevTime = None
        }

    let defaultDevParaIO() = {
        InPara = None
        OutPara = None
    } 

    let createDevParam  (nametype:string option) (dutype:DataType option) (v:obj option)  (t:int option) = 
        { 
          DevName = nametype
          DevType = dutype
          DevValue =v
          DevTime =t
    }
    
    let changeSymbolDevParam (x:DevPara option)(symbol:string option) =
        if x.IsNone then defaultDevParam()
        else
            let x = x |> Option.get
            createDevParam  symbol x.DevType x.DevValue x.DevTime
    
    let addOrUpdateParam(jobName:string, paramDic:Dictionary<string, DevPara>, newParam :DevPara option) = 
        if newParam.IsSome then
            paramDic.Remove jobName |> ignore
            paramDic.Add (jobName, newParam.Value)
            
        
    let changeParam(jobName:string, paramDic:Dictionary<string, DevPara>, symbol:string option) = 
        let changedDevParam = changeSymbolDevParam (Some(paramDic[jobName]))  symbol
        paramDic.Remove(jobName) |> ignore
        paramDic.Add (jobName, changedDevParam)



    

    

    let toTextInOutDev (inp:DevPara) (outp:DevPara)  (addr:Addresses)=  
        $"{inp.ToTextWithAddress(addr.In)}, {outp.ToTextWithAddress(addr.Out)}"


        
    let parseTime (item: string) =
        let timePattern = @"^(?i:(\d+(\.\d+)?)(ms|msec|sec))$"
        let m = Regex.Match(item.ToLower(), timePattern)
        if m.Success then
            let valueStr = m.Groups.[1].Value
            let unit = m.Groups.[3].Value.ToLower()
            match unit with
            | "ms" | "msec" -> 
                if valueStr.Contains(".") then 
                    failwithlog $" ms and msec do not support #.# {valueStr}"
                else 
                    Some(Convert.ToInt32(valueStr))
            | "sec" -> 
                Some(Convert.ToInt32(float valueStr * 1000.0)) // Convert seconds to milliseconds
            | _ -> None
        else None


    let parseValueNType (item: string) =
        let trimmedTextValueNDataType = getTextValueNType item
        match trimmedTextValueNDataType with
        | Some (v,ty) -> Some(ty.ToValue(v), ty)
        | None -> None

    
    let isValidName (name:string) = Regex.IsMatch(name , @"^[a-zA-Z_][a-zA-Z0-9_]*$")

    let getDevParam (txt: string) =
        let parts = txt.Split(':') |> Seq.toList
        let addr = parts.Head
        let remainingParts = parts.Tail
    
        let mutable nameOpt = None
        let mutable typeOpt = None
        let mutable valueOpt = None
        let mutable timeOpt = None
        let checkType orgty newty = 
            if orgty <> newty then failwithlog $"Duplicate type part detected: {newty}"

        for part in remainingParts do
            match parseTime part, tryTextToDataType part, parseValueNType part  with
            | Some time, _ , _ -> 
                if timeOpt.IsSome then failwithlog $"Duplicate time part detected: {part}"
                timeOpt <- Some time

            | _, Some duType, _ ->
                if typeOpt.IsSome then checkType typeOpt.Value duType
                typeOpt <- Some duType
            | _, _, Some (value, ty) ->

                if valueOpt.IsSome then failwithlog $"Duplicate value part detected: {part}"
                valueOpt <- Some value
                if typeOpt.IsSome then checkType typeOpt.Value ty
                typeOpt <- Some ty

            | _ when isValidName(part) ->
                if nameOpt.IsSome then failwithlog $"Duplicate name part detected: {part}"
                nameOpt <- Some part
            | _ ->
                failwithlog $"Unknown format detected: text '{part}'"
                
        addr, createDevParam  nameOpt typeOpt valueOpt timeOpt

