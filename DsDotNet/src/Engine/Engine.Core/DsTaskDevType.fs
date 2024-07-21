namespace Engine.Core

open System
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

    let addressPrint (addr: string) =
        if addr.IsNullOrEmpty() then TextAddrEmpty else addr

    type DevParaIO =
        {
            InPara: DevPara option 
            OutPara: DevPara option
        }

    type DevPara = {
        DevName: string option
        DevType: DataType option
        DevValue: obj option
        DevTime: int option
    } with
        member x.Value = x.DevValue |> Option.toObj
        member x.Type = x.DevType |> Option.defaultValue DuBOOL
        member x.Name = x.DevName |> Option.defaultValue ""
        member x.Time = x.DevTime 
        member x.IsDefaultParam =
            x.DevName.IsNone 
            && x.DevTime.IsNone
            && x.Type = DuBOOL
            && x.Value.IsNull()

        member x.ToTextWithAddress(addr: string) =
            let address = addressPrint addr
            let name = x.DevName |> Option.defaultValue ""
            let typ = x.DevType |> Option.map (fun t -> t.ToText()) |> Option.defaultValue ""
            let value = x.DevValue |> Option.map (fun v -> x.Type.ToStringValue(v)) |> Option.defaultValue ""
            let time = x.DevTime |> Option.map (fun t -> $"{t}ms") |> Option.defaultValue ""

            let parts = [address; name; typ; value; time]
            let result = parts |> List.filter (fun s -> not (String.IsNullOrEmpty(s))) |> String.concat ":"
            result

    let defaultDevParam() = 
        {
            DevName = None
            DevType = None
            DevValue = None
            DevTime = None
        }

    let defaultDevParaIO() =
        {
            InPara = None
            OutPara = None
        }

    let createDevParam(nametype: string option) (dutype: DataType option) (v: obj option) (t: int option) =
        {
            DevName = nametype
            DevType = dutype
            DevValue = v
            DevTime = t
        }

    let changeSymbolDevParam(x: DevPara option) (symbol: string option) =
        if x.IsNone then defaultDevParam()
        else
            let x = x |> Option.get
            createDevParam symbol x.DevType x.DevValue x.DevTime

    let addParam(jobName: string, paramDic: Dictionary<string, DevPara>, newParam: DevPara option) =
        if not(paramDic.ContainsKey jobName) then
            let param = if newParam.IsSome then newParam.Value else defaultDevParam()
            paramDic.Add(jobName, param)

    let changeParam(jobName: string, paramDic: Dictionary<string, DevPara>, symbol: string option) =
        let changedDevParam = changeSymbolDevParam(Some(paramDic.[jobName])) (symbol)
        paramDic.Remove(jobName) |> ignore
        paramDic.Add(jobName, changedDevParam)

    let toTextInOutDev(inp: DevPara, outp: DevPara, addr: Addresses) =
        $"{inp.ToTextWithAddress(addr.In)}, {outp.ToTextWithAddress(addr.Out)}"

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

    let parseValueNType(item: string) =
        let trimmedTextValueNDataType = getTextValueNType item
        match trimmedTextValueNDataType with
        | Some(v, ty) -> Some(ty.ToValue(v), ty)
        | None -> None

    let isValidName(name: string) =
        Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$")

    let getDevParam (txt: string) =
        let parts = txt.Split(':') |> Seq.toList
        let addr = parts.Head
        let remainingParts = parts.Tail

        let parseParts ((nameOpt: string option), (typeOpt: DataType option), (valueOpt: obj option), (timeOpt: int option)) part =
            match parseTime part, tryTextToDataType part, parseValueNType part with
            | Some time, _, _ ->
                if timeOpt.IsSome then failwithlog $"Duplicate time part detected: {part}"
                nameOpt, typeOpt, valueOpt, Some time
            | _, Some duType, _ ->
                if typeOpt.IsSome then failwithlog $"Duplicate type part detected: {duType}"
                nameOpt, Some duType, valueOpt, timeOpt
            | _, _, Some (value, ty) ->
                if valueOpt.IsSome then failwithlog $"Duplicate value part detected: {part}"
                if typeOpt.IsSome && typeOpt.Value <> ty then failwithlog $"Duplicate type part detected: {ty}"
                nameOpt, Some ty, Some value, timeOpt
            | _ when isValidName part ->
                if nameOpt.IsSome then failwithlog $"Duplicate name part detected: {part}"
                Some part, typeOpt, valueOpt, timeOpt
            | _ ->
                failwithlog $"Unknown format detected: text '{part}'"
    
        let nameOpt, typeOpt, valueOpt, timeOpt =
            remainingParts |> List.fold parseParts (None, None, None, None)
    
        addr, (createDevParam nameOpt typeOpt valueOpt timeOpt)
