// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open Dual.Common.Core.FS

[<AutoOpen>]
module DsType =
    ///Seg 상태 (Default 'Homing')
    type Status4 =
        | Ready
        | Going
        | Finish
        | Homing

    ///BtnType  종류
    type BtnType =
        | DuAutoBTN      //자동 Select 버튼
        | DuManualBTN    //수동 Select 버튼
        | DuDriveBTN     //운전 Push 버튼
        | DuTestBTN      //시운전 시작 Push 버튼
        | DuStopBTN      //정지 Push 버튼
        | DuEmergencyBTN //비상 Push 버튼
        | DuClearBTN     //해지 Push 버튼
        | DuHomeBTN      //홈(원위치) Push 버튼
        | DuReadyBTN     //운전 준비 Push 버튼

   
    ///LampType  종류 
    type LampType =
        | DuAutoLamp      //자동  버튼
        | DuManualLamp    //수동  버튼
        | DuDriveLamp     //운전  램프
        | DuStopLamp      //정지  램프
        | DuEmergencyLamp //비상  램프
        | DuTestDriveLamp //시운전  램프
        | DuReadyLamp     //준비  램프
        | DuIdleLamp      //대기  램프

    type ConditionType =
        | DuReadyState
        | DuDriveState
