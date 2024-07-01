// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsType =

    /// Describes the segment status with default being 'Homing'
    type Status4 =
        | Ready
        | Going
        | Finish
        | Homing

    type BtnType =
    | DuAutoBTN | DuManualBTN | DuDriveBTN | DuTestBTN | DuPauseBTN
    | DuEmergencyBTN | DuClearBTN | DuHomeBTN | DuReadyBTN
        override x.ToString() =
            match x with
            | DuAutoBTN      -> "Auto"
            | DuManualBTN    -> "Manual"
            | DuDriveBTN     -> "Drive"
            | DuTestBTN      -> "Test"
            | DuPauseBTN     -> "Pause"
            | DuEmergencyBTN -> "Emergency"
            | DuClearBTN     -> "Clear"
            | DuHomeBTN      -> "Home"
            | DuReadyBTN     -> "Ready"


    /// Represents different mode types
    type LampType =
        | DuIdleModeLamp      // Idle mode lamp
        | DuAutoModeLamp      // Automatic mode  lamp
        | DuManualModeLamp    // Manual mode  lamp

        | DuDriveStateLamp     // Drive state lamp
        | DuTestDriveStateLamp // Test Drive state lamp
        | DuErrorStateLamp     // Error  state lamp
        | DuReadyStateLamp     // ready_state lamp
        | DuOriginStateLamp    // origin_state lamp
        

    /// Represents different condition types
    type ConditionType =
        | DuReadyState
        | DuDriveState

    type ExternalTag = 
        | ManualTag
        | ErrorSensorOn 
        | ErrorSensorOff 
        | ErrorOnTimeOver 
        | ErrorOnTimeShortage
        | ErrorOffTimeOver 
        | ErrorOffTimeShortage
        | ErrGoingOrigin
        | MotionStart
        | MotionEnd
        | ScriptStart
        | ScriptEnd

    type ExternalTagSet = ExternalTag * IStorage
    
    [<Flags>]    
    type ScreenType =
        | CCTV = 0
        | IMAGE  = 1

    
    let GetHeadBracketRemoveName (name: string) =
        let patternHead = "^\[[^]]*]" // 첫 대괄호 제거

        let replaceName =
            System.Text.RegularExpressions.Regex.Replace(name, patternHead, "")

        replaceName

    let GetLastBracketRelaceName (name: string, replaceName: string) =
        let patternTail = "\[[^]]*]$" // 끝 대괄호 제거

        let replaceName =
            System.Text.RegularExpressions.Regex.Replace(name, patternTail, replaceName)

        replaceName

    let GetLastParenthesesReplaceName (name: string, replaceName: string) =
        let patternTail = @"\([^)]*\)$" // 끝 소괄호 제거

        let replacedName = System.Text.RegularExpressions.Regex.Replace(name, patternTail, replaceName) // Perform the replacement
        replacedName

    let GetLastParenthesesContents (name: string) =
        let patternTail = @"\(([^)]*)\)$" // Regular expression to match the content inside the last parentheses
        let m = System.Text.RegularExpressions.Regex.Match(name, patternTail) // Find the match
        if m.Success then
            m.Groups.[1].Value // Return the captured content
        else
            "" // Return an empty string if no match is found


    // 특수 대괄호 제거 후 순수 이름 추출
    // [yy]xx[xxx]Name[1,3] => xx[xxx]Name
    // 앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsRemoveName (name: string) =
        GetLastBracketRelaceName((name |> GetHeadBracketRemoveName), "")


    /// 이 함수는 입력 문자열 name에서 대괄호로 감싸인 부분을 추출합니다.
    ///
    /// - bHead가 true이면 첫 번째 대괄호 부분을, false이면 마지막 대괄호 부분을 반환합니다.
    ///
    /// - 반환값은 Some string 또는 None일 수 있습니다.
    let GetSquareBrackets (name: string, bHead: bool): string option =
        let pattern = "(?<=\[).*?(?=\])"  // 대괄호 안에 내용은 무조건 가져온다
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if bHead then
            if name.StartsWith("[") && name.Contains("]") then Some matches.[0].Value else None
        else
            if name.EndsWith("]") && name.Contains("[") then Some matches.[matches.Count - 1].Value else None

    let isStringDigit (str: string) =
        str |> Seq.forall Char.IsDigit
