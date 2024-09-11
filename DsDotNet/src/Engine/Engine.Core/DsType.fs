// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open System.Collections

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
        | DuEmergencyState

    type ExternalTag =
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

    /// 공통 함수: 문자열에서 마지막으로 닫히는 기호를 찾고, 그에 대응하는 여는 기호를 찾음
    let FindEnclosedGroup (name: string, openSymbol: char, closeSymbol: char, searchFromStart: bool) =
        let mutable startIdx = -1
        let mutable endIdx = -1
        let stack = new Stack()

        let findIndices direction startPos =
            let rec loop i =
                if i < 0 || i >= name.Length then ()
                else
                    if name.[i] = openSymbol then
                        if stack.Count = 0 then startIdx <- i
                        stack.Push(i)
                    elif name.[i] = closeSymbol then
                        if stack.Count > 0 then stack.Pop() |> ignore
                        if stack.Count = 0 then endIdx <- i

                    if startIdx = -1 || endIdx = -1 then loop (i + direction)
            loop startPos

        if searchFromStart then
            if name.StartsWith(openSymbol.ToString()) then findIndices 1 0
        else
            if name.EndsWith(closeSymbol.ToString()) then findIndices -1 (name.Length - 1)

        startIdx, endIdx


    let getFindText (name:string)  startIdx endIdx=
        if startIdx <> -1 && endIdx <> -1 then
            name.Substring(startIdx + 1, endIdx - startIdx - 1)
        else
            ""

    let getRemoveText (name:string)  startIdx endIdx=
        if startIdx <> -1 && endIdx <> -1 then
            name.Substring(0, startIdx) + name.Substring(endIdx + 1)
        else
            name

    /// 첫 번째 대괄호 그룹 제거
    let GetHeadBracketRemoveName (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', true)
        getRemoveText name startIdx endIdx

    /// 마지막 대괄호 그룹 제거
    let GetLastBracketRelaceName (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', false)
        getRemoveText name startIdx endIdx

    /// 마지막 괄호 그룹을 주어진 문자열로 교체
    let GetLastParenthesesReplaceName (name: string, replaceName: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '(', ')', false)
        if startIdx <> -1 && endIdx <> -1 then
            name.Substring(0, startIdx) + replaceName + name.Substring(endIdx + 1)
        else
            name

    /// 마지막 괄호 그룹 내용 반환
    let GetLastParenthesesContents (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '(', ')', false)
        getFindText name startIdx endIdx

    /// 마지막 대괄호 그룹 내용 반환
    let GetLastBracketContents (name: string) =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', false)
        getFindText name startIdx endIdx

    /// 첫 번째 또는 마지막 대괄호 그룹을 반환
    let GetSquareBrackets (name: string, bHead: bool): string option =
        let startIdx, endIdx = FindEnclosedGroup(name, '[', ']', bHead)
        let text = getFindText name startIdx endIdx
        match text with
        | "" -> None
        | _ -> Some text


    /// 특수 대괄호 제거 후 순수 이름 추출
    /// [yy]xx[xxx]Name[1,3] => xx[xxx]Name
    /// 앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsRemoveName (name: string) =
        name |> GetLastBracketRelaceName  |> GetHeadBracketRemoveName

    let GetLastParenthesesRemoveName (name: string) =
        GetLastParenthesesReplaceName (name ,  "" )

    let isStringDigit (str: string) =
        str |> Seq.forall Char.IsDigit
