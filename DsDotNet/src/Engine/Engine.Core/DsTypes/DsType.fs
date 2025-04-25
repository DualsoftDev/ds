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

    type ActionType =
        | DuEmergencyAction
        | DuPauseAction

    type VariableType =
        | VariableType
        | ConstType

    type ExternalTag =
        | ErrorSensorOn
        | ErrorSensorOff
        | ErrorOnTimeOver
        | ErrorOnTimeUnder
        | ErrorOffTimeOver
        | ErrorOffTimeUnder
        | ErrorInterlock
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
