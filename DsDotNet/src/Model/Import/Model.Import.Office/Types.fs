// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module InterfaceClass =

 ///인과의 노드 종류
    type NodeType =
        | REAL          //실제 나의 시스템 1 bit
        | REALExF        //다른 Flow real
        | REALExS        //다른 System real
        | CALL          //지시관찰
        | IF            //인터페이스
        | COPY_VALUE          //시스템복사
        | COPY_REF           //시스템참조
        | DUMMY         //그룹더미
        | BUTTON        //버튼 emg,start, ...
        | CONDITION        //system Condition ready/drive 정의 블록
        | LAMP          //램프 runmode,stopmode, ...
        //| ACTIVESYS        //model ppt active  system
        with
            member x.IsReal = x = REAL || x = REALExF || x = REALExS
            member x.IsCall = x = CALL
            member x.IsRealorCall =  x.IsReal || x.IsCall

    type ViewType =
        | VFLOW
        | VREAL
        | VREALEx
        | VCALL
        | VIF
        | VCOPY_VALUE
        | VCOPY_REF
        | VDUMMY
        | VBUTTON
        | VLAMP
        | VCONDITION

    ///인터페이스 Tag 기본 형식
    type ExcelCase =
        | XlsAddress             //주소
        | XlsVariable            //변수
        | XlsAutoBTN             //자동 버튼
        | XlsManualBTN           //수동 버튼
        | XlsDriveBTN            //운전 버튼
        | XlsStopBTN             //정지 버튼
        | XlsClearBTN            //해지 버튼
        | XlsEmergencyBTN        //비상 버튼
        | XlsTestBTN             //시운전 시작 버튼
        | XlsHomeBTN             //홈(원위치) 버튼
        | XlsReadyBTN            //준비(원위치) 버튼
        | XlsAutoModeLamp        //자동 모드 램프
        | XlsManualModeLamp      //수동 모드 램프
        | XlsDriveModeLamp       //운전 모드 램프
        | XlsStopModeLamp        //정지 모드 램프
        | XlsEmergencyModeLamp   //비상 모드 램프
        | XlsReadyModeLamp       //준비 모드 램프
        | XlsTestModeLamp        //시운전 모드 램프
        | XlsConditionReady      //준비 모드  램프
        | XlsConditionDrive      //준비 모드  램프

    with
        member x.ToText() =
            match x with
            | XlsAddress           -> TextXlsAddress
            | XlsVariable          -> TextXlsVariable
            | XlsAutoBTN           -> TextXlsAutoBTN
            | XlsManualBTN         -> TextXlsManualBTN
            | XlsDriveBTN          -> TextXlsDriveBTN
            | XlsStopBTN           -> TextXlsStopBTN
            | XlsClearBTN          -> TextXlsClearBTN
            | XlsEmergencyBTN      -> TextXlsEmergencyBTN
            | XlsTestBTN           -> TextXlsTestBTN
            | XlsReadyBTN          -> TextXlsReadyBTN
            | XlsHomeBTN           -> TextXlsHomeBTN
            | XlsAutoModeLamp      -> TextXlsAutoModeLamp
            | XlsManualModeLamp    -> TextXlsManualModeLamp
            | XlsDriveModeLamp     -> TextXlsDriveModeLamp
            | XlsStopModeLamp      -> TextXlsStopModeLamp
            | XlsEmergencyModeLamp -> TextXlsEmergencyModeLamp
            | XlsTestModeLamp      -> TextXlsTestModeLamp
            | XlsReadyModeLamp     -> TextXlsReadyModeLamp
            | XlsConditionReady    -> TextXlsConditionReady
            | XlsConditionDrive    -> TextXlsConditionDrive

    let TextToXlsType(txt:string) =
        match txt.ToLower() with
        | TextXlsAddress        ->  XlsAddress
        | TextXlsVariable       ->  XlsVariable
        | TextXlsAutoBTN        ->  XlsAutoBTN
        | TextXlsManualBTN      ->  XlsManualBTN
        | TextXlsEmergencyBTN   ->  XlsEmergencyBTN
        | TextXlsStopBTN        ->  XlsStopBTN
        | TextXlsDriveBTN       ->  XlsDriveBTN
        | TextXlsTestBTN        ->  XlsTestBTN
        | TextXlsClearBTN       ->  XlsClearBTN
        | TextXlsHomeBTN        ->  XlsHomeBTN
        | TextXlsAutoModeLamp   ->  XlsAutoModeLamp
        | TextXlsManualModeLamp ->  XlsManualModeLamp
        | TextXlsDriveModeLamp  ->  XlsDriveModeLamp
        | TextXlsStopModeLamp   ->  XlsStopModeLamp
        | TextXlsReadyModeLamp  ->  XlsReadyModeLamp
        | TextXlsConditionReady ->  XlsConditionReady
        | TextXlsConditionDrive ->  XlsConditionDrive


        | _ -> failwithf $"'{txt}' TextXlsType Error check type"
