// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
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
