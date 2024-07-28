namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsJobType =

    type JobTypeAction = 
        | ActionNormal  
        | Push    
        member x.ToText() = 
            match x with
            | ActionNormal   -> ""
            | Push    -> TextJobPush
    
    type JobTypeSensing = 
        | SensingNormal  
        | SensingNegative 
        member x.ToText() = 
            match x with
            | SensingNormal   -> ""
            | SensingNegative   -> TextJobNegative

    type JobTypeTaskDevInfo = 
        {
            TaskDevCount: int
            InCount : int option
            OutCount : int option
        }
        with  
            member x.AddressInCount = if x.InCount.IsSome then x.InCount.Value else x.TaskDevCount
            member x.AddressOutCount = if x.OutCount.IsSome then x.OutCount.Value else x.TaskDevCount
            member x.ToText() = 
                if x.TaskDevCount = 1 && x.AddressInCount = 1 && x.AddressOutCount  = 1 then
                    ""
                else
                    $"{TextJobMulti}{x.TaskDevCount}({x.AddressInCount}, {x.AddressOutCount})"

    type JobParam(action: JobTypeAction, jobTypeSensing: JobTypeSensing, jobTypeTaskDevInfo: JobTypeTaskDevInfo) =
        member val JobAction = action with get
        member val JobSensing = jobTypeSensing with get
        member val JobTaskDevInfo = jobTypeTaskDevInfo with get
        
        member x.ToText() =
            let actionText = x.JobAction.ToText()
            let sensingText = x.JobSensing.ToText()
            let multiText = x.JobTaskDevInfo.ToText()
            let parts = [actionText; sensingText; multiText] |> List.filter (fun part -> not (String.IsNullOrEmpty(part)))
            String.Join("; ", parts)

        member x.TaskDevCount =
            x.JobTaskDevInfo.TaskDevCount
        member x.TaskInCount =
            x.JobTaskDevInfo.InCount
        member x.TaskOutCount =
            x.JobTaskDevInfo.OutCount

    let getJobTypeAction (name: string) =
        let endContents = GetSquareBrackets(name, false)
        match endContents with
        | Some e when e = TextJobPush -> JobTypeAction.Push
        | _ -> JobTypeAction.ActionNormal

    let getJobTypeSensing (name: string) =
        let endContents = GetSquareBrackets(name, false)
        match endContents with
        | Some e when e = TextJobNegative -> JobTypeSensing.SensingNegative     
        | _ -> JobTypeSensing.SensingNormal

    let getJobTypeTaskDevInfo (param: string) =
        let parseMultiActionString (str: string) =
            let mainPart, optionalPart =
                if str.Contains("(") then
                    let parts = str.Split([| '('; ')' |], System.StringSplitOptions.RemoveEmptyEntries)
                    (parts.[0].TrimStart(TextJobMulti.ToCharArray()), if parts.Length > 1 then Some(parts.[1]) else None)
                else
                    (str.TrimStart(TextJobMulti.ToCharArray()), None)

            let cnt = mainPart |> int
            let inCnt, outCnt =
                match optionalPart with
                | Some optPart ->
                    let values = optPart.Split(',')
                    let inCnt = if values.Length > 0 then Some(values.[0] |> int) else None
                    let outCnt = if values.Length > 1 then Some(values.[1] |> int) else None
                    (inCnt, outCnt)
                | None -> (Some cnt, Some cnt)

            cnt, inCnt, outCnt

        let cnt, inCnt, outCnt = parseMultiActionString param
        if cnt < 1 then
            failWithLog $"MultiAction Count >= 1 : {param}"

        {
            TaskDevCount = cnt
            InCount = inCnt
            OutCount = outCnt
        }

    let defaultJobTypeTaskDevInfo() =  { TaskDevCount = 1; InCount = Some 1; OutCount = Some 1 }
    let defaultJobPara() = JobParam(ActionNormal, SensingNormal, defaultJobTypeTaskDevInfo())

    let getParserJobType (param: string) =
        let param = param.TrimStart('[').TrimEnd(']')
        let items = param.Split(';')

        let jobTypeTaskDevInfo = 
            items
            |> Array.tryFind (fun item -> item.StartsWith(TextJobMulti))
            |> Option.map getJobTypeTaskDevInfo
            |> Option.defaultValue (defaultJobTypeTaskDevInfo())

        let jobTypeAction = 
            items
            |> Array.exists (fun item -> item.Trim() = TextJobPush)
            |> function
                | true -> JobTypeAction.Push
                | false -> JobTypeAction.ActionNormal

        let jobTypeSensing = 
            items
            |> Array.exists (fun item -> item.Trim() = TextJobNegative)
            |> function
                | true -> JobTypeSensing.SensingNegative
                | false -> JobTypeSensing.SensingNormal

        JobParam(jobTypeAction, jobTypeSensing, jobTypeTaskDevInfo)

