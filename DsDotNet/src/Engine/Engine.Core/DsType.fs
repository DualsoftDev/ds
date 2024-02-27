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
        | DuAutoLamp      // Automatic button lamp
        | DuManualLamp    // Manual button lamp
        | DuDriveLamp     // Drive lamp
        | DuErrorLamp     // Error lamp
        | DuTestDriveLamp // Test Drive lamp
        | DuReadyLamp     // Ready lamp
        | DuIdleLamp      // Idle lamp
        | DuOriginLamp    // Homing lamp

    /// Represents different condition types
    type ConditionType =
        | DuReadyState
        | DuDriveState

    
    type JobActionType = 
        | Normal  ///RXs(ActionIn) 인터페이스가 관찰될때까지 ON
        | Inverse ///구현대기 : 항시ON RXs(ActionIn) 인터페이스가 관찰될때까지 OFF
        | Push    // reset 인터페이스(Plan Out) 관찰될때까지 ON 
        | Rising  ///구현대기 : TXs(ActionOut) Rising Pulse
        | MultiAction  of  int // 동시동작 개수 받기

    [<Flags>]    
    type ScreenType =
        | CCTV = 0
        | IMAGE  = 1

    let GetSquareBrackets (name: string, bHead: bool): string option =
        let pattern = "(?<=\[).*?(?=\])"  // 대괄호 안에 내용은 무조건 가져온다
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if bHead then
            if name.StartsWith("[") && name.Contains("]") then Some matches.[0].Value else None
        else
            if name.EndsWith("]") && name.Contains("[") then Some matches.[matches.Count - 1].Value else None


    let getJobActionType (name: string) =
        let endContents = GetSquareBrackets(name, false)
        let isStringDigit (str: string) = str |> Seq.forall System.Char.IsDigit

        match endContents with
        | Some "-" -> JobActionType.Normal
        | Some "I" -> JobActionType.Inverse
        | Some "P" -> JobActionType.Push
        | Some "R" -> JobActionType.Rising
        | Some s when isStringDigit s -> JobActionType.MultiAction (int s)  // 숫자일 경우 MultiAction으로 변환
        | Some t -> failwithf "Unknown ApiActionType: %s" t
        | None -> JobActionType.Normal

