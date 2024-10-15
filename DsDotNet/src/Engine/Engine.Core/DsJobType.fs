namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Text.RegularExpressions

[<AutoOpen>]
module DsJobType =

    /// 인터페이스 에러체크용 시간(사용자 입력 or Api.Tx~Rx AVG, STD 이용하여 CPK로 계산)
    type JobTime() = 
        // 기본값 상수 msec
        static let DefaultMax = 15000u
        static let DefaultChk = 0u

        member val Max: uint option = None with get, set //ON 동작시간 에러초과 msec
        member val Check: uint option = None with get, set // 센서고장체크 딜레이 msec

        member x.IsDefault = x.Max.IsNone  && x.Check.IsNone 

        member x.TimeOutMaxMSec  = x.Max |> Option.defaultValue DefaultMax
        member x.TimeDelayCheckMSec  = x.Check |> Option.defaultValue DefaultChk



    let parseUIntMSec (txt: string) (findKey: string) =
    // findKey와 txt의 대소문자를 구분하지 않도록 소문자로 변환
        let lowerFindKey = findKey.ToLower()
        let lowerTxt = txt.ToLower()
        let parsePattern = @$"{lowerFindKey}\((\d+(\.\d+)?(ms)?)\)"
    
        match Regex.Match(lowerTxt, parsePattern) with
        | m when m.Success -> 
            let parsedValue = m.Groups.[1].Value.Trim()
        
            // 소수점 포함 시 예외 처리
            if parsedValue.Contains(".") then 
                failwith $"Invalid time format: {parsedValue} must be an integer"
        
            // 특정 조건에서 'ms'가 반드시 포함되어야 함
            if lowerFindKey <> TextCOUNT.ToLower() && not(parsedValue.EndsWith("ms", StringComparison.OrdinalIgnoreCase)) then
                failwith $"Invalid time format: {parsedValue} must end with 'ms'"
        
            // 'ms'가 있는 경우 제거하고 숫자 파싱 시도
            let numericValue = parsedValue.ToLower().Replace("ms", "")

            match UInt32.TryParse(numericValue) with
            | true, value -> Some value
            | _ -> None
        
        | _ -> None

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

