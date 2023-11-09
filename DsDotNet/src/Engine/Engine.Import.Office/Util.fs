namespace Engine.Import.Office

open System.Collections.Concurrent
open System
open Engine.Core

[<AutoOpen>]
module Util =

    /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item: 'T) = x.TryAdd(item, item)

    let trimSpace (text: string) = text.Trim()
    let trimNewLine (text: string) = text.Trim('\n').Trim('\r')

    let mutable activeSysDir = ""
    let mutable currentFileName = ""
    

    let GetTailNumber (name: string) =
        let pattern = "\d+$"  // 글자 마지막 숫자를 찾음
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if matches.Count > 0 then
            let name = System.Text.RegularExpressions.Regex.Replace(name, pattern, "")
            let number = matches.[matches.Count - 1].Value |> trimSpace |> Convert.ToInt32
            name, number
        else name, 0

    
    let GetHeadBracketRemoveName (name: string) =
        let patternHead = "^\[[^]]*]" // 첫 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(name, patternHead, "")
        replaceName

    let GetLastBracketRelaceName (name: string, replaceName:string) =
        let patternTail = "\[[^]]*]$" // 끝 대괄호 제거
        let replaceName = System.Text.RegularExpressions.Regex.Replace(name, patternTail, replaceName)
        replaceName

    

    // 특수 대괄호 제거 후 순수 이름 추출
    // [yy]xx[xxx]Name[1,3] => xx[xxx]Name
    // 앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsRemoveName (name: string) =
        GetLastBracketRelaceName((name |> GetHeadBracketRemoveName),  "")


    let getBtnType(key:string) =
        match key.Trim().ToUpper() with
        | "A"   -> BtnType.DuAutoBTN
        | "M"   -> BtnType.DuManualBTN
        | "D"   -> BtnType.DuDriveBTN
        | "S"   -> BtnType.DuStopBTN
        | "E"   -> BtnType.DuEmergencyBTN
        | "T"   -> BtnType.DuTestBTN
        | "R"   -> BtnType.DuReadyBTN
        | "H"   -> BtnType.DuHomeBTN
        | "C"   -> BtnType.DuClearBTN
        | _     ->  failwith $"{key} is Error Type"

    let getLampType(key:string) =
        match key.Trim().ToUpper() with
        | "A"   -> LampType.DuAutoLamp
        | "M"   -> LampType.DuManualLamp
        | "D"   -> LampType.DuDriveLamp
        | "S"   -> LampType.DuStopLamp
        | "E"   -> LampType.DuEmergencyLamp
        | "T"   -> LampType.DuTestDriveLamp
        | "R"   -> LampType.DuReadyLamp
        | "I"   -> LampType.DuIdleLamp
        | _     ->  failwith $"{key} is Error Type"

    let getConditionType(key:string) =
        match key.Trim().ToUpper() with
        | "R"   -> ConditionType.DuReadyState
        | "D"   -> ConditionType.DuDriveState
        | _     ->  failwith $"{key} is Error Type"