namespace Engine.Core

open System
open System.Linq
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Text.RegularExpressions

[<AutoOpen>]
module DsTaskDevModule =

    // Define the Addresses type with input and output addresses
    [<AllowNullLiteral>]
    type Addresses(inAddress: string, outAddress: string) =
        member x.In = inAddress
        member x.Out = outAddress

    // Helper function to print addresses, with a check for empty or null
    let addressPrint addr =
        if String.IsNullOrEmpty(addr) then TextAddrEmpty else addr

    // Define ValueParam to handle target values and ranges
    type ValueParam(valTarget: obj option, valMin: obj option, valMax: obj option, isNegativeTarget: bool, isInclusiveMin: bool, isInclusiveMax: bool) =
        let mutable datatype = DuBOOL // Default to bool type
        let valueText(v:obj option) = if v.IsSome then ToStringValue v.Value else ""
    
        // Initialization block for type checking and assignments
        do
            let types =
                [valTarget; valMin; valMax]
                |> Seq.choose id
                |> Seq.map (fun x -> x.GetType())
                |> Seq.distinct

            if Seq.length types > 1 then
                raise (ArgumentException("All values (valTarget, valMin, valMax) must be of the same type."))
            elif Seq.length types = 1 then
                datatype <- textToDataType(types.Head().Name)

            if valTarget.IsSome && (valMin.IsSome || valMax.IsSome) then 
                failWithLog $"valTarget({valTarget.Value}) cannot be used with valMin({valMin}) or valMax({valMax})"

            if datatype = DuSTRING && valTarget.IsNone then
                failWithLog $"STRING type cannot be used with valMin or valMax"

        // Properties of ValueParam
        member x.TargetValue = valTarget
        member x.TargetValueText = valueText valTarget
        member x.DataType = datatype
        member x.Min = valMin
        member x.MinText = valueText valMin
        member x.Max = valMax
        member x.MaxText = valueText valMax
        member x.IsNegativeTarget = isNegativeTarget
        member x.IsInclusiveMin = isInclusiveMin
        member x.IsInclusiveMax = isInclusiveMax
        member x.IsRangeValue = valTarget.IsNone
        member x.IsDefaultValue =
            match valTarget, valMin, valMax with
            | None, None, None -> true
            | Some v, None, None when (v :? bool) -> Convert.ToBoolean(v)
            | _ -> false

        // Convert the ValueParam to its textual representation
        member x.ToText() =
            let toStringOpt = function
                | Some value -> ToStringValue value
                | None -> ""

            let minText = toStringOpt x.Min
            let maxText = toStringOpt x.Max
            let targetText = toStringOpt x.TargetValue
            let negationPrefix = if x.IsNegativeTarget then "!" else ""

            match x.Min, x.TargetValue, x.Max with
            // Single Value Matching (IN(5))
            | None, Some _, None -> negationPrefix + targetText

            // Range Matching
            | Some _, None, Some _ ->
                let rangeText =
                    match x.IsInclusiveMin, x.IsInclusiveMax with
                    | true, true -> $"[{minText}, {maxText}]"
                    | true, false -> $"[{minText}, {maxText})"
                    | false, true -> $"({minText}, {maxText}]"
                    | false, false -> $"({minText}, {maxText})"
                negationPrefix + rangeText

            // Greater than / less than
            | Some _, None, None when x.IsInclusiveMin -> negationPrefix + $">= {minText}"
            | Some _, None, None -> negationPrefix + $"> {minText}"
            | None, None, Some _ when x.IsInclusiveMax -> negationPrefix + $"<= {maxText}"
            | None, None, Some _ -> negationPrefix + $"< {maxText}"

            // Default case
            | _ -> ""

        // Read and simulate value functions
        member x.ReadBoolValue =
            match x.TargetValue with
            | Some v when (v :? bool) -> v :?> bool
            | Some v -> failWithLog $"ReadValue {v} is not valid"
            | None -> true

        member x.ReadSimValue =
            let avg =
                match x.Min, x.Max with
                | Some mi, Some ma ->
                    match x.IsInclusiveMin, x.IsInclusiveMax with
                    | true, false -> x.Min
                    | false, true -> x.Max
                    | true, true -> x.Max
                    | false, false -> middleValue mi ma
                | Some _mi, None when x.IsInclusiveMin -> x.Min
                | Some mi, None -> middleValue mi (mi.GetType() |> typeMaxValue)
                | None, Some _ma when x.IsInclusiveMax -> x.Max
                | None, Some ma -> middleValue ma (ma.GetType() |> typeDefaultValue)
                | None, None -> x.TargetValue

            match avg with
            | Some value -> value
            | None -> failWithLog $"ReadValue {x.ToText()} is not valid"

        member x.WriteValue = 
            match x.TargetValue with
            | Some v -> v
            | None -> true

        member x.DefaultValue = 
            match x.TargetValue with
            | Some v -> Some (v.GetType() |> typeDefaultValue)
            | None -> None

    type ValueParamIO(inValueParam:ValueParam, outValueParam:ValueParam) = 
        
        member x.In = inValueParam
        member x.Out = outValueParam
        member x.InDataType = inValueParam.DataType
        member x.OutDataType = outValueParam.DataType
        member x.IsDefaultParam = inValueParam.IsDefaultValue && outValueParam.IsDefaultValue
            
        member x.ToDsText(addrSet: Addresses) =
            let inText = if inValueParam.IsDefaultValue 
                         then addressPrint addrSet.In
                         else $"{addrSet.In}:{inValueParam.ToText()}"

            let outText = if outValueParam.IsDefaultValue
                          then addressPrint addrSet.Out 
                          else $"{addrSet.Out}:{outValueParam.ToText()}"

            $"{inText}, {outText}"

    let defaultValueParam() = ValueParam(Some true, None, None, false, false, false)
    let defaultValueParamIO() = ValueParamIO(defaultValueParam(), defaultValueParam())
    
    // Function to create ValueParam based on string input
    let createValueParam (input: string) : ValueParam  =
        let xPattern = "(?i:x)(?-i)"  // Case insensitive 'x' pattern
        let singleValuePattern = $"{xPattern}\\s*=\\s*(.+)$"
        let rangePattern = @"(?<open>[\(\[])\s*(?<min>.+?)\s*,\s*(?<max>.+?)\s*(?<close>[\)\]])"
        let comparisonPattern = @"(?<operator>>=|<=|>|<)\s*(?<value>.+)"
        let defaultV = defaultValueParam() 
        let tryCompare (minValue: obj) (maxValue: obj) =
            match minValue, maxValue with
            | (:? IComparable as min), (:? IComparable as max) when min.GetType() = max.GetType() ->
                if min.CompareTo(max) > 0 then None else Some(min, max)
            | _ -> None

        match Regex.Match(input, singleValuePattern) with
        | m when m.Success && isValidValue(m.Groups.[1].Value.Trim()) ->
             ValueParam(Some(toValue(m.Groups.[1].Value.Trim())), None, None, false, false, false)
        | _ ->
            match Regex.Match(input, rangePattern) with
            | m when m.Success && isValidValue(m.Groups.["min"].Value.Trim()) && isValidValue(m.Groups.["max"].Value.Trim()) ->
                let minValue = toValue(m.Groups.["min"].Value.Trim())
                let maxValue = toValue(m.Groups.["max"].Value.Trim())
                match tryCompare minValue maxValue with
                | Some(min, max) ->
                    let inclusiveMin = m.Groups.["open"].Value = "["
                    let inclusiveMax = m.Groups.["close"].Value = "]"
                    ValueParam(None, Some(min), Some(max), false, inclusiveMin, inclusiveMax)
                | None -> defaultV
            | _ ->
                match Regex.Match(input, comparisonPattern) with
                | m when m.Success ->
                    let operatorStr = m.Groups.["operator"].Value.Trim()
                    let valueStr = m.Groups.["value"].Value.Trim()
                    match operatorStr with
                    | ">=" -> ValueParam(None, Some(toValue valueStr), None, false, true, false)
                    | ">"  -> ValueParam(None, Some(toValue valueStr), None, false, false, false)
                    | "<=" -> ValueParam(None, None, Some(toValue valueStr), false, false, true)
                    | "<"  -> ValueParam(None, None, Some(toValue valueStr), false, false, false)
                    | _ -> defaultV
                | _ ->
                    if getTextValueNType(input).IsSome then 
                        ValueParam(Some(toValue input), None, None, false, false, false)
                    else 
                        defaultV
    

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

        member x.ToDsText(addrSet: Addresses) =
            let inText = if x.InParam.IsDefault 
                         then addressPrint addrSet.In
                         else x.InParam.ToText()

            let outText = if x.OutParam.IsDefault
                          then addressPrint addrSet.Out 
                          else x.OutParam.ToText()

            $"{inText}, {outText}"

    let defaultTaskDevParam() = TaskDevParam(DuBOOL)
    let defaultTaskDevParamIO() = TaskDevParamIO(defaultTaskDevParam(), defaultTaskDevParam())
    let defaultTaskDevParamWithAddress(address: string) =
        let taskDevParam = defaultTaskDevParam()
        taskDevParam.Address <- address
        taskDevParam
   
    let changeParam(jobName: string, paramDic: Dictionary<string, TaskDevParam>, symbol: string) =
        paramDic.[jobName].Symbol <- symbol

    let parseTime(item: string) =
        let timePattern = @"^(?i:(\d+(\.\d+)?)(ms|msec|sec))$"
        let m = Regex.Match(item.ToLower(), timePattern)
        if m.Success then
            let valueStr = m.Groups.[1].Value
            let unit = m.Groups.[3].Value.ToLower()
            match unit with
            | "ms" | "msec" ->
                if valueStr.Contains(".") then
                    failwithlog $"ms and msec do not support #.# {valueStr}"
                else
                    Some(Convert.ToInt32(valueStr))
            | "sec" ->
                Some(Convert.ToInt32(float valueStr * 1000.0))
            | _ -> None
        else None

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


    let getAddressValueParam (txt: string) =
        let parts = txt.Split(':') |> Seq.toList
        let addr = parts.Head
        if parts.Tail.IsEmpty then
            addr, defaultValueParam()
        else
            let valueParam = createValueParam (String.Join(":", parts.Tail))
            addr, valueParam
