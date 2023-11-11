// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open Dual.Common.Core.FS

[<AutoOpen>]
module DsType =

    /// Describes the segment status with default being 'Homing'
    type Status4 =
        | Ready
        | Going
        | Finish
        | Homing

    /// Represents different button types
    type BtnType =
        | DuAutoBTN      // Automatic Select button
        | DuManualBTN    // Manual Select button
        | DuDriveBTN     // Drive Push button
        | DuTestBTN      // Test Drive Start Push button
        | DuStopBTN      // Stop Push button
        | DuEmergencyBTN // Emergency Push button
        | DuClearBTN     // Clear Push button
        | DuHomeBTN      // Home (Original position) Push button
        | DuReadyBTN     // Drive Ready Push button

    /// Represents different lamp types
    type LampType =
        | DuAutoLamp      // Automatic button lamp
        | DuManualLamp    // Manual button lamp
        | DuDriveLamp     // Drive lamp
        | DuStopLamp      // Stop lamp
        | DuEmergencyLamp // Emergency lamp
        | DuTestDriveLamp // Test Drive lamp
        | DuReadyLamp     // Ready lamp
        | DuIdleLamp      // Idle lamp

    /// Represents different condition types
    type ConditionType =
        | DuReadyState
        | DuDriveState

    
    type ApiActionType = 
        | Normal  ///RXs(ActionIn) 인터페이스가 관찰될때까지 ON
        | Inverse ///구현대기 : 항시ON RXs(ActionIn) 인터페이스가 관찰될때까지 OFF
        | Push    // reset 인터페이스(Plan Out) 관찰될때까지 ON 
        | Rising  ///구현대기 : TXs(ActionOut) Rising Pulse


    let GetSquareBrackets (name: string, bHead: bool): string option =
        let pattern = "(?<=\[).*?(?=\])"  // 대괄호 안에 내용은 무조건 가져온다
        let matches = System.Text.RegularExpressions.Regex.Matches(name, pattern)
        if bHead then
            if name.StartsWith("[") && name.Contains("]") then Some matches.[0].Value else None
        else
            if name.EndsWith("]") && name.Contains("[") then Some matches.[matches.Count - 1].Value else None

    
   

    let getApiActionType(name :string) =
        let endContents = GetSquareBrackets(name, false)
        if endContents.IsSome
        then 
            match endContents.Value with
            |"-"-> ApiActionType.Normal
            |"I"-> ApiActionType.Inverse
            |"P"-> ApiActionType.Push
            |"R"-> ApiActionType.Rising  
            |_ as t -> failwithf "Unknown ApiActionType: %s" t
        else ApiActionType.Normal