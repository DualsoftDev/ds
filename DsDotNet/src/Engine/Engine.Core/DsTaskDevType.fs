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

    type TaskDevParaIO(inPara: TaskDevPara option , outPara: TaskDevPara option) = 
        let mutable inPara = inPara 
        let mutable outPara = outPara 
        
        member x.InPara
            with get() = inPara
            and set(value) = inPara <- value    
            
        member x.OutPara
            with get() = outPara
            and set(value) = outPara <- value
      
        member x.IsDefaultParam = (x.InPara.IsNone || x.InPara.Value.IsDefaultParam)
                                  && (x.OutPara.IsNone || x.OutPara.Value.IsDefaultParam)

        member x.ToDsText(addrSet:Addresses) =
            match x.InPara, x.OutPara with
            | Some inp, Some outp ->
                        $"{inp.ToTextWithAddress(addrSet.In)}, {outp.ToTextWithAddress(addrSet.Out)}"
            | Some inp, None ->
                        $"{inp.ToTextWithAddress(addrSet.In)}, {addrSet.Out}"
            | None, Some outp ->
                        $"{addrSet.In}, {outp.ToTextWithAddress(addrSet.Out)}"
            | _ -> failwithlog "TaskDevParaIO is not valid"


    type TaskDevPara(devName: string option, devType: DataType option, devValue: obj option, devTime: int option) = 
        let mutable devName = devName  //symolName

        member x.DevName
            with get() = devName
            and set(value) = devName <- value

        member x.DevType = devType
        member x.DevValue= devValue
        member x.DevTime = devTime
        member x.Value = devValue |> Option.toObj
        member x.Type = 
            devType |> Option.defaultValue DuBOOL
        member x.Name = 
            devName |> Option.defaultValue ""
        member x.Time = 
            devTime

        member x.IsDefaultParam = 
            devName.IsNone 
            && devTime.IsNone
            && x.Type = DuBOOL
            && (devValue |> Option.isNone)

        member this.ToTextWithAddress(addr: string) =
            let address = addressPrint addr
            let name = devName |> Option.defaultValue ""
            let typ = devType |> Option.map (fun t -> t.ToText()) |> Option.defaultValue ""
            let value = devValue |> Option.map (fun v -> this.Type.ToStringValue(v)) |> Option.defaultValue ""
            let time = devTime |> Option.map (fun t -> $"{t}ms") |> Option.defaultValue ""

            let parts = [address; name; typ; value; time]
            let result = parts |> List.filter (fun s -> not (String.IsNullOrEmpty(s))) |> String.concat ":"
            result


    let defaultTaskDevPara() =  TaskDevPara(None, None, None, None)
    let defaultTaskDevParaIO() = TaskDevParaIO (None, None)

    let createTaskDevPara(nametype: string option) (dutype: DataType option) (v: obj option) (t: int option) =
        TaskDevPara(nametype, dutype, v, t)

    let changeSymbolTaskDevPara(x: TaskDevPara option) (symbol: string option) =
        if x.IsNone then defaultTaskDevPara()
        else
            let x = x |> Option.get
            createTaskDevPara symbol x.DevType x.DevValue x.DevTime

    let changeParam(jobName: string, paramDic: Dictionary<string, TaskDevPara>, symbol: string option) =
        let changedTaskDevPara = changeSymbolTaskDevPara(Some(paramDic.[jobName])) (symbol)
        paramDic.Remove(jobName) |> ignore
        paramDic.Add(jobName, changedTaskDevPara)


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

    let getTaskDevPara (txt: string) =
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
    
        addr, (createTaskDevPara nameOpt typeOpt valueOpt timeOpt)
