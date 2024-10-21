namespace Engine.Core

open System
open System.Linq
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Text.RegularExpressions

[<AutoOpen>]
module DsTaskDevTypeModule =

    // Define the Addresses type with input and output addresses
    [<AllowNullLiteral>]
    type Addresses(inAddress: string, outAddress: string) =
        member x.In = inAddress
        member x.Out = outAddress

    // Helper function to print addresses, with a check for empty or null
    let addressPrint addr =
        if String.IsNullOrEmpty(addr) then TextAddrEmpty else addr

    type TaskDevParamFlag= 
        | Address    = 0
        | DataType   = 1
        | Symbol     = 2

    // Define the SymbolAlias type with name and data type
    type TaskDevParam(address:string, dataType: DataType, symbol:string) =
        new(dataType: DataType) = TaskDevParam(TextAddrEmpty, dataType, "")
        member val Address = address with get, set
        member val Symbol  = symbol with get, set
        member x.DataType = dataType
        member x.IsDefault = dataType = DuBOOL && x.Symbol = "" && x.Address = TextAddrEmpty
        
        member x.ToText() =
            [addressPrint x.Address; dataType.ToText(); x.Symbol]
            |> List.filter (fun s -> not (String.IsNullOrEmpty s))
            |> String.concat ";"

    type TaskDevParamIO(inParam: TaskDevParam, outParam: TaskDevParam) =
        member val InParam = inParam with get, set
        member val OutParam = outParam with get, set
        member x.IsDefaultParam = x.InParam.IsDefault && x.OutParam.IsDefault
        member x.ToDsText() =  $"{x.InParam.ToText()}, {x.OutParam.ToText()}"

    let defaultTaskDevParam() = TaskDevParam(DuBOOL)
    let defaultTaskDevParamIO() = TaskDevParamIO(defaultTaskDevParam(), defaultTaskDevParam())
    let defaultTaskDevParamWithAddress(address: string) =
        let taskDevParam = defaultTaskDevParam()
        taskDevParam.Address <- address
        taskDevParam
   
    let changeParam(jobName: string, paramDic: Dictionary<string, TaskDevParam>, symbol: string) =
        paramDic.[jobName].Symbol <- symbol


    let getTaskDevParam (txt: string) =
        let parts = txt.Split(';') |> Seq.toList
        
        let isValidName(name: string) =
            Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$")

        let validateDataType typeStr =
            match tryTextToDataType typeStr with
            | Some duType -> duType
            | None -> failwithlog $"Invalid data type: {typeStr}"

        match parts with
        | [addr; typeStr; symbol] ->
            if not (isValidName symbol) then
                failwithlog $"Invalid symbol name: {symbol}"
        
            let dataType = validateDataType typeStr
            TaskDevParam(addr, dataType, symbol)

        | [addr; typeStr] ->
            let dataType = validateDataType typeStr
            TaskDevParam(addr, dataType, "")

        | [addr; ] when addr = TextAddrEmpty ->
            defaultTaskDevParam()

        | _ ->
            failwithlog $"Unknown format detected: text '{txt}'"

