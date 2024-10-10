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
            if datatype = DuSTRING && valTarget.IsNone // STRING 은 TARGET만 지원 RANGE 지원 안함 
            then
                failWithLog $"STRING type cannot be used with valMin or valMax"
            
        member x.TargetValue = valTarget
        member x.DataType = datatype
        member x.Min = valMin
        member x.Max = valMax
        member x.IsInclusiveMin = isInclusiveMin  // True if the min comparison is >=, False if >
        member x.IsInclusiveMax = isInclusiveMax  // True if the max comparison is <=, False if <
        member x.IsRangeValue = valTarget.IsNone
        member x.IsDefaultValue = 
                    match valTarget, valMin, valMax with
                    | None, None, None -> true
                    | Some v, None, None when (v :? bool) 
                        -> Convert.ToBoolean valTarget.Value
                    | _ ->
                        false
    
        member x.ToText() =
            let minText = if x.Min.IsSome then ToStringValue x.Min.Value else ""
            let maxText = if x.Max.IsSome then ToStringValue x.Max.Value else ""
            let targetText = if x.TargetValue.IsSome then ToStringValue x.TargetValue.Value else ""
           
            match x.Min, x.TargetValue, x.Max with
            | None, Some value, None  ->
                if x.DataType <> DuBOOL then
                    targetText
                elif not(Convert.ToBoolean(value))
                then
                    "False"
                else
                    ""
            | Some _min, None, Some _max when isInclusiveMin && isInclusiveMax ->
                $"{minText} <= x <= {maxText}"
            | Some _min, None, Some _max when isInclusiveMin && not isInclusiveMax ->
                $"{minText} <= x < {maxText}"
            | Some _min, None, Some _max when not isInclusiveMin && isInclusiveMax ->
                $"{minText} < x <= {maxText}"
            | Some _min, None, Some _max ->
                $"{minText} < x < {maxText}"
            | Some _min, None, None when isInclusiveMin ->
                $"{minText} <= x"
            | Some _min, None, None ->
                $"{minText} < x"
            | None, None, Some _max when isInclusiveMax ->
                $"x <= {maxText}"
            | None, None, Some _max ->
                $"x < {maxText}"
            | _ ->
                "" //공란으로 처리

        member x.ToDataTypeText() = x.DataType.ToText()

    let createValueParam (input: string) : ValueParam option =
        let xPattern = "(?i:x)(?-i)"  //x, X 대소문자 구분안함
        //string 타입도 대응 x="AAAA" or x=1234 or x=1
        let singleValuePattern = $"{xPattern}\s*=(.+)$" 
        //int 외 타입도 대응 123.0f<x<= 123.1f
        let rangePattern = $"(?:(.+)(<=|<)\s*)?{xPattern}\s*(?:(<=|<)(.+))?$"

        match Regex.Match(input, singleValuePattern) with
        | m when m.Success ->
            Some (ValueParam(Some(toValue(m.Groups.[1].Value.Trim())), None, None, false, false))
        | _ ->
            match Regex.Match(input, rangePattern) with
            | m when m.Success ->
                let minValue = if m.Groups.[1].Success then Some(toValue (m.Groups.[1].Value.Trim())) else None
                let maxValue = if m.Groups.[4].Success then Some(toValue (m.Groups.[4].Value.Trim())) else None
                let inclusiveMin = m.Groups.[2].Value.Trim() = "<="
                let inclusiveMax = m.Groups.[3].Value.Trim() = "<="
                Some (ValueParam(None, minValue, maxValue, inclusiveMin, inclusiveMax))
            | _ -> 
                if getTextValueNType(input).IsSome
                then 
                    Some (ValueParam(Some(toValue input), None, None, false, false))
                else 
                    None

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
