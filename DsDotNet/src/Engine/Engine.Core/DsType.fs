// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open Dual.Common.Core.FS
open System

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

    
    type JobActionType = 
        | Normal  ///RXs(ActionIn) 인터페이스가 관찰될때까지 ON
        | NoneRx  //인터페이스 관찰 없는 타입
        | NoneTx  //인터페이스 지시 없는 타입
        | NoneTRx //인터페이스 지시관찰 없는 타입
        | Inverse ///구현대기 : 항시ON RXs(ActionIn) 인터페이스가 관찰될때까지 OFF
        | Push    // reset 인터페이스(Plan Out) 관찰될때까지 ON 
        | MultiAction  of string*int // 동시동작 개수 받기

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

    // 특수 대괄호 제거 후 순수 이름 추출
    // [yy]xx[xxx]Name[1,3] => xx[xxx]Name
    // 앞뒤가 아닌 대괄호는 사용자 이름 뒷단에서 "xx[xxx]Name" 처리
    let GetBracketsRemoveName (name: string) =
        GetLastBracketRelaceName((name |> GetHeadBracketRemoveName), "")


    let GetSquareBrackets (name: string, bHead: bool): string option =
        let pattern = "(?<=\[).*?(?=\])"  // 대괄호 안에 내용은 무조건 가져온다
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if bHead then
            if name.StartsWith("[") && name.Contains("]") then Some matches.[0].Value else None
        else
            if name.EndsWith("]") && name.Contains("[") then Some matches.[matches.Count - 1].Value else None


    let getJobActionType (name: string) =
        let nameContents = GetBracketsRemoveName(name)
        let endContents = GetSquareBrackets(name, false)
        let isStringDigit (str: string) = str |> Seq.forall System.Char.IsDigit

        match endContents with
        | Some "-" -> JobActionType.Normal
        | Some "XX" -> JobActionType.NoneTRx
        | Some "XT" -> JobActionType.NoneTx
        | Some "XR" -> JobActionType.NoneRx
        | Some "I" -> JobActionType.Inverse
        | Some "P" -> JobActionType.Push
        | Some s when isStringDigit s -> JobActionType.MultiAction (nameContents, (int s)) // 숫자일 경우 MultiAction으로 변환
        | Some t -> failwithf "Unknown ApiActionType: %s" t
        | None -> JobActionType.Normal

