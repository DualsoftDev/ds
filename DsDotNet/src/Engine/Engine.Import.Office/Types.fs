// Copyright (c) Dualsoft  All Rights Reserved.
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
        | CALL //지시관찰
        | AUTOPRE //전제조건 Node
        //| CALLOPFunc  //Operator 함수전용
        //| CALLCMDFunc //Command  함수전용
        | IF_DEVICE //인터페이스
        | COPY_DEV //시스템복사 deivce
        | OPEN_EXSYS_LINK //시스템참조 Passive sytem(초기 로딩과 같은 경로 ExSystem 이면 Acive)
        | OPEN_EXSYS_CALL //시스템참조 Active sytem (초기 로딩과 다른 경로 ExSystem 이면 Passive)
        | DUMMY //그룹더미
        | BUTTON //버튼 emg,start, ...
        | LAYOUT //위치 디바이스 기준
        | LAMP //램프 runmode,stopmode, ...
        | CONDITIONorAction //READY조건, Drive 조건, EmergencyAction, ...

        member x.IsReal = x = REAL || x = REALExF
        member x.IsCall = x = CALL 
        member x.IsLoadSys = x = COPY_DEV || x = OPEN_EXSYS_LINK || x = OPEN_EXSYS_CALL
        member x.IsRealorCall = x.IsReal || x.IsCall
        member x.IsIF = x = IF_DEVICE

        member x.GetLoadingType() =
            match x with
            | OPEN_EXSYS_LINK
            | OPEN_EXSYS_CALL -> DuExternal
            | COPY_DEV -> DuDevice
            | _ -> failwithlog "error"


    type ViewType =
        | VFLOW
        | VREAL
        | VCALL
        | VIF
        | VCOPY_DEV
        | VOPEN_EXSYS_LINK
        | VDUMMY
        | VBUTTON
        | VLAMP
        | VCONDITION
        | VACTION

    ///인터페이스 Tag 기본 형식
    type ExcelCase =
        | XlsAddress //주소
        | XlsVariable //변수
        | XlsConst //상수
        | XlsCommand  //명령
        | XlsOperator //연산
        | XlsAutoBTN //자동 버튼
        | XlsManualBTN //수동 버튼
        | XlsDriveBTN //운전 버튼
        | XlsPauseBTN //일시정지 버튼
        | XlsClearBTN //해지 버튼
        | XlsEmergencyBTN //비상 버튼
        | XlsTestBTN //시운전 시작 버튼
        | XlsHomeBTN //홈(원위치) 버튼
        | XlsReadyBTN //준비(원위치) 버튼
        | XlsAutoLamp //자동 램프
        | XlsManualLamp //수동 램프
        | XlsDriveLamp //운전 램프
        | XlsErrorLamp //이상 램프
        | XlsReadyLamp //준비 램프
        | XlsIdleLamp //대기 램프
        | XlsHomingLamp //원위치중 램프
        | XlsTestLamp //시운전 램프
        | XlsConditionReady //준비조건 상태
        | XlsConditionDrive //운전조건 상태
        | XlsActionEmg  //비상 상태시 출력
        | XlsActionPause //정지 상태시 출력

        member x.ToText() =
            match x with
            | XlsAddress -> TextXlsAddress
            | XlsVariable -> TextXlsVariable
            | XlsConst   -> TextXlsConst
            | XlsCommand -> TextXlsCommand
            | XlsOperator -> TextXlsOperator
            | XlsAutoBTN -> TextXlsAutoBTN
            | XlsManualBTN -> TextXlsManualBTN
            | XlsDriveBTN -> TextXlsDriveBTN
            | XlsPauseBTN -> TextXlsPauseBTN
            | XlsClearBTN -> TextXlsClearBTN
            | XlsEmergencyBTN -> TextXlsEmergencyBTN
            | XlsTestBTN -> TextXlsTestBTN
            | XlsReadyBTN -> TextXlsReadyBTN
            | XlsHomeBTN -> TextXlsHomeBTN
            | XlsAutoLamp -> TextXlsAutoLamp
            | XlsManualLamp -> TextXlsManualLamp
            | XlsDriveLamp -> TextXlsDriveLamp
            | XlsErrorLamp -> TextXlsErrorLamp
            | XlsTestLamp -> TextXlsTestLamp
            | XlsReadyLamp -> TextXlsReadyLamp
            | XlsIdleLamp -> TextXlsIdleLamp
            | XlsHomingLamp -> TextXlsHomingLamp
            | XlsConditionReady -> TextXlsConditionReady
            | XlsConditionDrive -> TextXlsConditionDrive
            | XlsActionEmg -> TextXlsActionEmg
            | XlsActionPause -> TextXlsActionPause
            
    let TextToXlsType (txt: string) =
        match txt.ToLower() with
        | TextXlsAddress -> XlsAddress
        | TextXlsVariable -> XlsVariable
        | TextXlsConst -> XlsConst
        | TextXlsCommand -> XlsCommand
        | TextXlsOperator -> XlsOperator
        | TextXlsAutoBTN -> XlsAutoBTN
        | TextXlsManualBTN -> XlsManualBTN
        | TextXlsEmergencyBTN -> XlsEmergencyBTN
        | TextXlsPauseBTN -> XlsPauseBTN
        | TextXlsDriveBTN -> XlsDriveBTN
        | TextXlsTestBTN -> XlsTestBTN
        | TextXlsClearBTN -> XlsClearBTN
        | TextXlsHomeBTN -> XlsHomeBTN
        | TextXlsReadyBTN -> XlsReadyBTN
        | TextXlsAutoLamp -> XlsAutoLamp
        | TextXlsManualLamp -> XlsManualLamp
        | TextXlsDriveLamp -> XlsDriveLamp
        | TextXlsTestLamp -> XlsTestLamp
        | TextXlsErrorLamp -> XlsErrorLamp
        | TextXlsReadyLamp -> XlsReadyLamp
        | TextXlsIdleLamp -> XlsIdleLamp
        | TextXlsHomingLamp -> XlsHomingLamp
        
        | TextXlsConditionReady -> XlsConditionReady
        | TextXlsConditionDrive -> XlsConditionDrive
        | TextXlsActionEmg -> XlsActionEmg
        | TextXlsActionPause -> XlsActionPause
        

        | _ -> failwithf $"'{txt}' TextXlsType Error check type"
