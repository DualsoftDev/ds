// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module InterfaceClass =


    ///인과의 노드 종류
    type NodeType =
        | REAL //실제 나의 시스템 1 bit
        | REALExF //다른 Flow real
        | REALExS //다른 System real
        | CALL //지시관찰
        | IF_DEVICE //인터페이스
        | IF_LINK //인터페이스
        | COPY_DEV //시스템복사 deivce
        | OPEN_EXSYS_LINK //시스템참조 Passive sytem(초기 로딩과 같은 경로 ExSystem 이면 Acive)
        | OPEN_EXSYS_CALL //시스템참조 Active sytem (초기 로딩과 다른 경로 ExSystem 이면 Passive)
        | DUMMY //그룹더미
        | BUTTON //버튼 emg,start, ...
        | LAYOUT //위치 디바이스 기준
        | LAMP //램프 runmode,stopmode, ...

        member x.IsReal = x = REAL || x = REALExF || x = REALExS
        member x.IsCall = x = CALL
        member x.IsLoadSys = x = COPY_DEV || x = OPEN_EXSYS_LINK || x = OPEN_EXSYS_CALL
        member x.IsRealorCall = x.IsReal || x.IsCall
        member x.IsIF = x = IF_DEVICE || x = IF_LINK

        member x.GetLoadingType() =
            match x with
            | OPEN_EXSYS_LINK
            | OPEN_EXSYS_CALL -> DuExternal
            | COPY_DEV -> DuDevice
            | _ -> failwithlog "error"


    type ViewType =
        | VFLOW
        | VREAL
        | VREALEx
        | VCALL
        | VIF
        | VCOPY_DEV
        | VOPEN_EXSYS_LINK
        | VDUMMY
        | VBUTTON
        | VLAMP
        | VCONDITION

    ///인터페이스 Tag 기본 형식
    type ExcelCase =
        | XlsAddress //주소
        | XlsVariable //변수
        | XlsAutoBTN //자동 버튼
        | XlsManualBTN //수동 버튼
        | XlsDriveBTN //운전 버튼
        | XlsStopBTN //정지 버튼
        | XlsClearBTN //해지 버튼
        | XlsEmergencyBTN //비상 버튼
        | XlsTestBTN //시운전 시작 버튼
        | XlsHomeBTN //홈(원위치) 버튼
        | XlsReadyBTN //준비(원위치) 버튼
        | XlsAutoLamp //자동 램프
        | XlsManualLamp //수동 램프
        | XlsDriveLamp //운전 램프
        | XlsStopLamp //정지 램프
        | XlsEmergencyLamp //비상 램프
        | XlsReadyLamp //준비 램프
        | XlsIdleLamp //대기 램프
        | XlsTestLamp //시운전 램프
        | XlsConditionReady //준비 램프
        | XlsConditionDrive //운전 램프

        member x.ToText() =
            match x with
            | XlsAddress -> TextXlsAddress
            | XlsVariable -> TextXlsVariable
            | XlsAutoBTN -> TextXlsAutoBTN
            | XlsManualBTN -> TextXlsManualBTN
            | XlsDriveBTN -> TextXlsDriveBTN
            | XlsStopBTN -> TextXlsStopBTN
            | XlsClearBTN -> TextXlsClearBTN
            | XlsEmergencyBTN -> TextXlsEmergencyBTN
            | XlsTestBTN -> TextXlsTestBTN
            | XlsReadyBTN -> TextXlsReadyBTN
            | XlsHomeBTN -> TextXlsHomeBTN
            | XlsAutoLamp -> TextXlsAutoLamp
            | XlsManualLamp -> TextXlsManualLamp
            | XlsDriveLamp -> TextXlsDriveLamp
            | XlsStopLamp -> TextXlsStopLamp
            | XlsEmergencyLamp -> TextXlsEmergencyLamp
            | XlsTestLamp -> TextXlsTestLamp
            | XlsReadyLamp -> TextXlsReadyLamp
            | XlsIdleLamp -> TextXlsIdleLamp
            | XlsConditionReady -> TextXlsConditionReady
            | XlsConditionDrive -> TextXlsConditionDrive

    let TextToXlsType (txt: string) =
        match txt.ToLower() with
        | TextXlsAddress -> XlsAddress
        | TextXlsVariable -> XlsVariable
        | TextXlsAutoBTN -> XlsAutoBTN
        | TextXlsManualBTN -> XlsManualBTN
        | TextXlsEmergencyBTN -> XlsEmergencyBTN
        | TextXlsStopBTN -> XlsStopBTN
        | TextXlsDriveBTN -> XlsDriveBTN
        | TextXlsTestBTN -> XlsTestBTN
        | TextXlsClearBTN -> XlsClearBTN
        | TextXlsHomeBTN -> XlsHomeBTN
        | TextXlsReadyBTN -> XlsReadyBTN
        | TextXlsAutoLamp -> XlsAutoLamp
        | TextXlsManualLamp -> XlsManualLamp
        | TextXlsDriveLamp -> XlsDriveLamp
        | TextXlsTestLamp -> XlsTestLamp
        | TextXlsStopLamp -> XlsStopLamp
        | TextXlsEmergencyLamp -> XlsEmergencyLamp
        | TextXlsReadyLamp -> XlsReadyLamp
        | TextXlsIdleLamp -> XlsIdleLamp
        | TextXlsConditionReady -> XlsConditionReady
        | TextXlsConditionDrive -> XlsConditionDrive


        | _ -> failwithf $"'{txt}' TextXlsType Error check type"
