namespace Engine.Core

open Dual.Common.Core.FS

[<AutoOpen>]
module DsJobType =

    /// 인터페이스 에러체크용 시간(사용자 입력 or Api.Tx~Rx AVG, STD 이용하여 CPK로 계산)
    type JobTime() = 
        // 기본값 상수 sec
        static let DefaultMax = 15.0
        static let DefaultMin = 0.0
        static let DefaultChk = 0.0

        member val MaxOn: float option = None with get, set //ON 동작시간 에러초과 sec
        member val MinOn: float option = None with get, set //ON 동작시간 에러미달 sec
        member val MaxOff: float option = None with get, set //ON 동작시간 에러초과 sec
        member val MinOff: float option = None with get, set //ON 동작시간 에러미달 sec
        member val Check: float option = None with get, set // 센서고장체크 딜레이 sec

        // 초 단위를 밀리초로 변환하는 내부 함수
        member private x.toMilliseconds (value: float option) defaultVal = 
            (value |> Option.defaultValue defaultVal) * 1000.0 |> uint32

        member x.IsDefault = x.MaxOn.IsNone  && x.MinOn.IsNone 
                          && x.MaxOff.IsNone && x.MinOff.IsNone 
                          && x.Check.IsNone

        member x.TimeMaxOnMSec  = x.toMilliseconds x.MaxOn DefaultMax
        member x.TimeMinOnMSec  = x.toMilliseconds x.MinOn DefaultMin
        member x.TimeMaxOffMSec = x.toMilliseconds x.MaxOff DefaultMax
        member x.TimeMinOffMSec = x.toMilliseconds x.MinOff DefaultMin
        member x.TimeCheckMSec = x.toMilliseconds x.Check DefaultChk

    type JobTypeAction =
        | ActionNormal
        | Push
        member x.ToText() =
            match x with
            | ActionNormal -> ""
            | Push -> TextJobPush

    type JobTypeSensing =
        | SensingNormal
        | SensingNegative
        member x.ToText() =
            match x with
            | SensingNormal -> ""
            | SensingNegative -> TextJobNegative

    type JobTypeTaskDevInfo =
        {
            TaskDevCount: int
            InCount : int option
            OutCount : int option
        }
        with
            member x.AddressInCount  = x.InCount  |? x.TaskDevCount
            member x.AddressOutCount = x.OutCount |? x.TaskDevCount
            member x.ToText() =
                if x.TaskDevCount = 1 && x.AddressInCount = 1 && x.AddressOutCount = 1 then
                    ""
                else
                    // e.g "N3(1, 2)"
                    $"{TextJobMulti}{x.TaskDevCount}({x.AddressInCount}, {x.AddressOutCount})"

   
    type JobDevParam(action: JobTypeAction, jobTypeSensing: JobTypeSensing, jobTypeTaskDevInfo: JobTypeTaskDevInfo) =
        member _.JobAction = action
        member _.JobSensing = jobTypeSensing
        member _.JobTaskDevInfo = jobTypeTaskDevInfo

        member x.ToText() =
            [|
                x.JobAction.ToText()
                x.JobSensing.ToText()
                x.JobTaskDevInfo.ToText()
            |] |> filter (String.any) |> String.concat("; ")

        member x.TaskDevCount = x.JobTaskDevInfo.TaskDevCount
        member x.TaskInCount  = x.JobTaskDevInfo.InCount
        member x.TaskOutCount = x.JobTaskDevInfo.OutCount

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
                    let inCnt  = values.TryItem(0).Map(int)
                    let outCnt = values.TryItem(1).Map(int)
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
    let defaultJobParam() = JobDevParam(ActionNormal, SensingNormal, defaultJobTypeTaskDevInfo())

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

        JobDevParam(jobTypeAction, jobTypeSensing, jobTypeTaskDevInfo)

