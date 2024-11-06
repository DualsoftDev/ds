namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Text.RegularExpressions

[<AutoOpen>]
module DsCallTypeModule =

    /// 인터페이스 에러체크용 시간(사용자 입력 or Api.Tx~Rx AVG, STD 이용하여 CPK로 계산)
    type CallTime() = 
        // 기본값 상수 msec
        static let DefaultMax = 15000u
        static let DefaultChk = 0u

        member val TimeOut: uint option = None with get, set //ON 동작시간 에러초과 msec
        member val DelayCheck: uint option = None with get, set // 센서고장체크 딜레이 msec

        member x.IsDefault = x.TimeOut.IsNone  && x.DelayCheck.IsNone 

        member x.TimeOutMaxMSec  = x.TimeOut |> Option.defaultValue DefaultMax
        member x.TimeDelayCheckMSec  = x.DelayCheck |> Option.defaultValue DefaultChk
        
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

    let parseUIntMSec (txt: string) (findKey: string) =
        let parsePattern = @$"{findKey.ToLower()}\((\d+(\.\d+)?(ms)?)\)"
        let matchResult = Regex.Match(txt.ToLower(), parsePattern)
        
        if matchResult.Success then
            let parsedValue = matchResult.Groups.[1].Value.Trim()
            if parsedValue.Contains(".") then failwith $"Invalid time format: {parsedValue} must be an integer"
            if findKey.ToLower() <> TextPPTCOUNT.ToLower() && not(parsedValue.EndsWith("ms")) then failwith $"Invalid time format: {parsedValue} must end with 'ms'"
            let numericValue = parsedValue.Replace("ms", "")
            UInt32.TryParse(numericValue) |> function | true, value -> Some value | _ -> None
        else None

    type CallActionType =
        | ActionNormal = 0
        | Push = 1

    let getCallActionTypeToText x =
        match x with
        | CallActionType.Push -> TextCallPush
        | CallActionType.ActionNormal -> ""
        | _ -> failWithLog $"{x} Unknown CallActionType"

    let getCallTypeAction name =
        match GetSquareBrackets(name, false) with
        | Some e when e = TextCallPush -> CallActionType.Push
        | _ -> CallActionType.ActionNormal
