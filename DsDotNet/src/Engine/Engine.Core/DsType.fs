// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open Engine.Common.FS

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
        | DuAutoBTN             //자동 Select 버튼
        | DuManualBTN           //수동 Select 버튼
        | DuEmergencyBTN        //비상 Push 버튼
        | DuStopBTN             //정지 Push 버튼
        | DuRunBTN              //운전 Push 버튼
        | DuDryRunBTN           //시운전 시작 Push 버튼
        | DuClearBTN            //해지 Push 버튼
        | DuHomeBTN             //홈(원위치) Push 버튼

    ///LampType  종류
    type LampType =
        | DuEmergencyLamp   //비상모드 램프
        | DuRunModeLamp     //운전모드 램프
        | DuDryRunModeLamp  //시 운전모드  램프
        | DuManualModeLamp  //수동 모드 램프
        | DuStopModeLamp    //정지 모드 램프
