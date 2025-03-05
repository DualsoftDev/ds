// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsText =
    let [<Literal>] TextLibrary   = "DS_Library"
    let [<Literal>] TextDSJson    = "dualsoft.json"
    let [<Literal>] TextEmtpyChannel = "EmtpyChannel" //channel 정보 없는 대상 Dev및 API
    let [<Literal>] TextImageChannel = "SlideImage"   //cctv url 대신에 pptx의 SlideImage 사용할 경우
    let [<Literal>] TextFuncNotUsed = "NotUsed"  //함수 사용안함
    let [<Literal>] TextNotUsed = "-"  //Not Uuse 처리 (주소 사용안함, 필드 사용안함, 등등)
    let [<Literal>] TextAddrEmpty = "_"  //주소 없음 Error 대상
    let [<Literal>] TextDeviceSplit = "_" //DS Flow, Device 자동 파싱 기준
    let [<Literal>] TextInOutSplit = ":" //in/out 구분자

    //edge
    let [<Literal>] TextStartEdge         = ">"
    let [<Literal>] TextResetEdge         = "|>"
    let [<Literal>] TextStartReset        = "=>"
    let [<Literal>] TextSelfReset         = "=|>"
    let [<Literal>] TextInterlock         = "<|>"
    let [<Literal>] TextStartEdgeRev      = "<"
    let [<Literal>] TextResetEdgeRev      = "<|"
    let [<Literal>] TextStartResetRev     = "<="
    let [<Literal>] TextSelfResetRev      = "<|="


    let [<Literal>] TextCallPush = "PUSH"
    let [<Literal>] TextJobMulti = "N"
    let [<Literal>] TextCallNegative= '!'

    let [<Literal>] TextMAX = "MAX" 
    let [<Literal>] TextCHK = "CHK" 
    let [<Literal>] TextAVG = "AVG" 
    let [<Literal>] TextSTD = "STD" 
    let [<Literal>] TextPPTTIME = "T" 
    let [<Literal>] TextPPTCOUNT= "C" 


[<AutoOpen>]
module DsTextExport =
    //export Excel
    let [<Literal>] TextXlsAddress           = "외부주소"
    let [<Literal>] TextXlsVariable          = "변수"
    let [<Literal>] TextXlsConst             = "상수"
    let [<Literal>] TextXlsOperator          = "연산"
    let [<Literal>] TextXlsCommand           = "명령"
    let [<Literal>] TextXlsAutoBTN           = "자동셀렉트"
    let [<Literal>] TextXlsManualBTN         = "수동셀렉트"
    let [<Literal>] TextXlsDriveBTN          = "운전푸쉬버튼"
    let [<Literal>] TextXlsPauseBTN          = "정지푸쉬버튼"
    let [<Literal>] TextXlsClearBTN          = "해지푸쉬버튼"
    let [<Literal>] TextXlsEmergencyBTN      = "비상푸쉬버튼"
    let [<Literal>] TextXlsTestBTN           = "시운전푸쉬버튼"
    let [<Literal>] TextXlsHomeBTN           = "복귀푸쉬버튼"
    let [<Literal>] TextXlsReadyBTN          = "준비푸쉬버튼"

    let [<Literal>] TextXlsAutoLamp          = "자동모드램프"
    let [<Literal>] TextXlsManualLamp        = "수동모드램프"
    let [<Literal>] TextXlsDriveLamp         = "운전모드램프"
    let [<Literal>] TextXlsErrorLamp         = "이상모드램프"
    let [<Literal>] TextXlsEmergencyLamp     = "비상모드램프"
    let [<Literal>] TextXlsTestLamp          = "시운전모드램프"
    let [<Literal>] TextXlsReadyLamp         = "준비모드램프"
    let [<Literal>] TextXlsIdleLamp          = "대기모드램프"
    let [<Literal>] TextXlsHomingLamp        = "원위치중램프"
    let [<Literal>] TextXlsConditionReady    = "준비조건"
    let [<Literal>] TextXlsConditionDrive    = "운전조건"
    let [<Literal>] TextXlsActionEmg         = "비상출력"
    let [<Literal>] TextXlsActionPause       = "정지출력"
    
    let [<Literal>] TextXlsAllFlow =  "ALL"
[<AutoOpen>]
module DsTextProperty =

    let [<Literal>] TextFlow    = "flow"
    let [<Literal>] TextSystem  = "sys"
    let [<Literal>] TextAddress = "addresses"
    let [<Literal>] TextSafety  = "safety"
    let [<Literal>] TextAlias   = "alias"
    let [<Literal>] TextLayout  = "layouts"
    let [<Literal>] TextJobs    = "jobs"
    let [<Literal>] TextDevice  = "device"



[<AutoOpen>]
module DsTextFunction =

    let [<Literal>] TextMove    = "m"
    let [<Literal>] TextOnDelayTimer = "t"
    let [<Literal>] TextRingCounter = "c"
    let [<Literal>] TextNot = "n"
    let [<Literal>] TextEQ = "=="
    let [<Literal>] TextNotEQ = "!="
    let [<Literal>] TextGT = ">"
    let [<Literal>] TextGTE = ">="
    let [<Literal>] TextLT = "<"
    let [<Literal>] TextLTE = "<="
