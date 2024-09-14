namespace Engine.Core

open System
open System.Linq
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Text.RegularExpressions

[<AutoOpen>]
module rec DsTaskDevType =

    [<AllowNullLiteral>]
    type Addresses(inAddress: string, outAddress: string) =
        member x.In = inAddress
        member x.Out = outAddress

    type SymbolAlias(name: string, dataType: DataType) =
        member x.Name = name
        member x.DataType = dataType

    let addressPrint (addr: string) =
        if String.IsNullOrEmpty(addr) then TextAddrEmpty else addr

    type ValueParam(valTarget: obj option, valMin: obj option, valMax: obj option, isInclusiveMin: bool, isInclusiveMax: bool) =
        let mutable datatype = DuBOOL //기본 bool 타입
        do 
            let types = 
                [valTarget; valMin; valMax]
                |> Seq.choose id // Filter out None values
                |> Seq.map (fun x -> x.GetType()) 
                |> Seq.distinct  // Get distinct types



            if Seq.length types > 1 then
                raise (System.ArgumentException("All values (valTarget, valMin, valMax) must be of the same type."))
            elif Seq.length types = 1 then
                datatype <- textToDataType(types.Head().Name)

            if valTarget.IsSome && (valMin.IsSome || valMax.IsSome)
            then 
                failWithLog $"valTarget({valTarget.Value}) cannot be used with valMin({valMin}) or valMax({valMax})"

            
        member x.TargetValue = valTarget
        member x.DataType = datatype
        member x.Min = valMin
        member x.Max = valMax
        member x.IsInclusiveMin = isInclusiveMin  // True if the min comparison is >=, False if >
        member x.IsInclusiveMax = isInclusiveMax  // True if the max comparison is <=, False if <
        member x.IsDefaultValue = 
                    match valTarget, valMin, valMax with
                    | None, None, None -> true
                    | Some v, None, None when (v :? bool) 
                        -> Convert.ToBoolean valTarget.Value
                    | _ ->
                        false
    
        member x.ToText() =
            match x.Min, x.TargetValue, x.Max with
            | None, Some value, None  ->
                if x.DataType <> DuBOOL then
                    $"{x.DataType.ToStringValue(value)}"
                elif not(Convert.ToBoolean(value))
                then
                    "False"
                else
                    ""
            | Some min, None, Some max when isInclusiveMin && isInclusiveMax ->
                $"{min} <= x <= {max}"
            | Some min, None, Some max when isInclusiveMin && not isInclusiveMax ->
                $"{min} <= x < {max}"
            | Some min, None, Some max when not isInclusiveMin && isInclusiveMax ->
                $"{min} < x <= {max}"
            | Some min, None, Some max ->
                $"{min} < x < {max}"
            | Some min, None, None when isInclusiveMin ->
                $"{min} <= x"
            | Some min, None, None ->
                $"{min} < x"
            | None, None, Some max when isInclusiveMax ->
                $"x <= {max}"
            | None, None, Some max ->
                $"x < {max}"
            | _ ->
                "" //공란으로 처리

        member x.ToDataTypeText() = x.DataType.ToText()


    let createValueParam(text: string) =
        let pattern = @"\s*(?<min>[\d.]+)?\s*(?<minOp><=|<)?\s*(?i:x)(?-i)\s*(?<maxOp><=|<)?\s*(?<max>[\d.]+)?\s*"
        let regex = Regex(pattern)  // No need for RegexOptions.IgnoreCase since inline case insensitivity is used
        let m = regex.Match(text)

        if m.Success then
            let min = 
                if m.Groups.["min"].Success then 
                    Some(box (toValue m.Groups.["min"].Value))
                else 
                    None
            let max = 
                if m.Groups.["max"].Success then 
                    Some(box (toValue m.Groups.["max"].Value))
                else 
                    None
            let minOp = m.Groups.["minOp"].Value
            let maxOp = m.Groups.["maxOp"].Value

            let isInclusiveMin = minOp = "<="
            let isInclusiveMax = maxOp = "<="

            match min, max with
            | None, None ->
                if text.Contains("=") then
                    let value = text.Split('=') |> Array.last |> fun v -> Some(box (toValue v))
                    ValueParam(value, None, None, false, false) |> Some
                else
                    ValueParam(Some(toValue text), None, None, false, false) |> Some
            | _ ->
                ValueParam(None, min, max, isInclusiveMin, isInclusiveMax) |> Some
        else
            match getTextValueNType text with
            | Some _ -> ValueParam(Some(toValue text), None, None, false, false) |> Some
            | None -> None

    type TaskDevParam(symbolAlias: SymbolAlias option, valueParam: ValueParam, devTime: int option) =
        let mutable symbol = symbolAlias  //symbol name
        do
            match symbolAlias, valueParam with
            | Some s,  v ->
                if s.DataType <> v.DataType && not(valueParam.IsDefaultValue)
                then
                    failWithLog $"SymbolAlias DataType({s.DataType}) and ValueParam DataType({v.DataType}) do not match"
            |_-> () 

        member x.SymbolAlias
            with get() = symbol
            and set(value) = symbol <- value

        member x.SymbolName = 
            match symbol with
            | Some sym -> sym.Name
            | None -> ""

        member x.ValueParam = valueParam
        member x.DevTime = devTime
        
        /// 기본값은 true
        member x.ReadBoolValue = 
            match valueParam.TargetValue with
            | Some v -> if(v :? bool)
                        then v:?> bool
                        else
                            failWithLog $"ReadValue {v} is not valid"
            | None -> true

        member x.ReadSimValue = 
            let vp = x.ValueParam
            let avg =
                match vp.Min, vp.Max with
                | Some mi, Some ma -> 
                        match vp.IsInclusiveMin ,vp.IsInclusiveMax with
                        | true, false -> vp.Min
                        | false, true -> vp.Max
                        | true, true ->  vp.Max //둘다 포함되면 max값으로 처리
                        | false, false -> middleValue mi ma

                | Some mi, None -> if vp.IsInclusiveMin then vp.Min
                                    else middleValue mi (mi.GetType() |> typeMaxValue)

                | None, Some ma -> if vp.IsInclusiveMax then vp.Max
                                    else middleValue ma (ma.GetType() |> typeDefaultValue)
                | None, None ->
                    match vp.TargetValue with
                    | Some v -> Some v
                    | None -> Some true

            if avg.IsSome then
                avg.Value
            else 
                failWithLog $"ReadValue {vp.ToText()}is not valid"

        ///기본값은 true
        member x.ReadRangeValue = valueParam

                

        ///기본값은 true
        member x.WriteValue = 
            match valueParam.TargetValue with
            | Some v -> v
            | _ -> true

        ///기본값은 false, 0 , "", ' ', 0.0, ...
        member x.DefaultValue = 
            match valueParam.TargetValue with
            | Some v -> v.GetType() |> typeDefaultValue
            | _ -> failWithLog $"DefaultValue {x}is not valid"

        member x.DataType = valueParam.DataType 
  
        member x.Time = devTime

        member x.IsDefaultParam =
            symbol.IsNone &&
            devTime.IsNone &&
            x.DataType = DuBOOL &&
            valueParam.IsDefaultValue

        member x.ToTextWithAddress(addr: string) =
            let address = addressPrint addr
            let symNameNType = match symbol with
                                | Some sym -> $"{sym.Name}:{sym.DataType.ToText()}"
                                | None -> ""
            let valueText = valueParam.ToText()
            let time = devTime |> Option.map (fun t -> $"{t}ms") |> Option.defaultValue ""

            [address; symNameNType; valueText; time]
            |> List.filter (fun s -> not (String.IsNullOrEmpty(s)))
            |> String.concat ":"

    type TaskDevParamIO(inParam: TaskDevParam option, outParam: TaskDevParam option) =
        let mutable inParam = inParam
        let mutable outParam = outParam

        member x.InParam
            with get() = inParam
            and set(value) = inParam <- value

        member x.OutParam
            with get() = outParam
            and set(value) = outParam <- value

        member x.IsDefaultParam =
            (x.InParam.IsNone || x.InParam.Value.IsDefaultParam) &&
            (x.OutParam.IsNone || x.OutParam.Value.IsDefaultParam)

        member x.ToDsText(addrSet: Addresses) =
            match x.InParam, x.OutParam with
            | Some inp, Some outp ->
                $"{inp.ToTextWithAddress(addrSet.In)}, {outp.ToTextWithAddress(addrSet.Out)}"
            | Some inp, None ->
                $"{inp.ToTextWithAddress(addrSet.In)}, {addressPrint addrSet.Out}"
            | None, Some outp ->
                $"{addressPrint addrSet.In}, {outp.ToTextWithAddress(addrSet.Out)}"
            | _ -> failwithlog "TaskDevParamIO is not valid"

    let defaultTaskDevParam() =
        TaskDevParam(None, defaultValueParam(Some(true)), None)

    let defaultTaskDevParamIO() = TaskDevParamIO(None, None)

    let defaultValueParam(valTarget: obj option) = ValueParam(valTarget, None, None, false, false)

    let createTaskDevParam(symbolAlias: SymbolAlias option)  (valTarget: obj option) (t: int option) =
        TaskDevParam(symbolAlias, defaultValueParam(valTarget), t)

    let createTaskDevParaIOInTrue() =
        let inParam = createTaskDevParam None (Some(true)) None
        TaskDevParamIO(Some inParam, None)

    let createTaskDevParamWithSymbol(symbolAlias: SymbolAlias) =
        createTaskDevParam (Some(symbolAlias)) (Some(true)) None

    let changeSymbolTaskDevParam(x: TaskDevParam option) (symbol: SymbolAlias option) =
        match x with
        | None -> defaultTaskDevParam()
        | Some x ->
            TaskDevParam(symbol, x.ValueParam, x.DevTime)

    let changeParam(jobName: string, paramDic: Dictionary<string, TaskDevParam>, symbol: SymbolAlias option) =
        let changedTaskDevPara = changeSymbolTaskDevParam(Some(paramDic.[jobName])) symbol
        paramDic.[jobName] <- changedTaskDevPara

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


    let isValidName(name: string) =
        Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$")

    let getTaskDevParam (txt: string) =
        let parts = txt.Split(':') |> Seq.toList
        let parseParts (acc: (string option * DataType option * ValueParam option * int option)) part =
            let nameOpt, typeOpt, valueOpt, timeOpt = acc
            match parseTime part, tryTextToDataType part, createValueParam part with
            | Some time, _, _ ->
                if timeOpt.IsSome then failwithlog $"Duplicate time part detected: {part}"
                nameOpt, typeOpt, valueOpt, Some time
            | _, Some duType, _ ->
                if typeOpt.IsSome then failwithlog $"Duplicate type part detected: {duType}"
                nameOpt, Some duType, valueOpt, timeOpt
            | _, _, Some valueParam ->
                if valueOpt.IsSome then failwithlog $"Duplicate value part detected: {part}"
                nameOpt, typeOpt, Some valueParam, timeOpt
            | _ when isValidName part ->
                if nameOpt.IsSome then failwithlog $"Duplicate name part detected: {part}"
                Some part, typeOpt, valueOpt, timeOpt
            | _ ->
                failwithlog $"Unknown format detected: text '{part}'"

        let nameOpt, typeOpt, valueOpt, timeOpt =
            parts |> List.fold parseParts (None, None, None, None)

        if nameOpt.IsSome && typeOpt.IsNone 
        then
            failwithlog $"Type is required for {nameOpt.Value}"
        else 
            let sym = nameOpt |> Option.map (fun n -> SymbolAlias(n, typeOpt.Value))
            match valueOpt with
            | Some vp -> TaskDevParam(sym, vp, timeOpt)
            | None -> 
                if sym.IsNone then
                     TaskDevParam(sym, defaultValueParam(Some(true)), timeOpt)
                else
                     TaskDevParam(sym, defaultValueParam(None), timeOpt)


    let getAddressTaskDevParam (txt: string) =
        let parts = txt.Split(':') |> Seq.toList
        let addr = parts.Head
        if parts.Tail.IsEmpty then
            addr, defaultTaskDevParam()
        else
            let taskDevParam = getTaskDevParam (parts.Tail.JoinWith(":"))
            addr, taskDevParam
