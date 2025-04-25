// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsText =
    let [<Literal>] TextLibrary   = "DS_Library"
    let [<Literal>] TextModelConfigJson = "modelConfig.json"
    let [<Literal>] TextOPCTagFolder = "TAG"
    let [<Literal>] TextOPCDSFolder = "Dualsoft"
    let [<Literal>] TextUserTagCase = "모니터링"


    let [<Literal>] TextEmtpyChannel = "EmtpyChannel" //channel 정보 없는 대상 Dev및 API
    let [<Literal>] TextImageChannel = "SlideImage"   //cctv url 대신에 pptx의 SlideImage 사용할 경우
    let [<Literal>] TextFuncNotUsed = "NotUsed"  //함수 사용안함
    let [<Literal>] TextNotUsed = "-"  //Not Uuse 처리 (주소 사용안함, 필드 사용안함, 등등)
    let [<Literal>] TextAddrEmpty = "_"  //주소 없음 Error 대상
    let [<Literal>] TextDeviceSplit = "-" //DS Flow, Device 자동 파싱 기준
    let [<Literal>] TextInOutSplit = ":" //in/out 구분자
    let [<Literal>] TextAllFlow = "ALL" // 모든 Flow 적용
    

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
    let [<Literal>] TextMixDataSplit = ':'

    let [<Literal>] TextMAX = "MAX" 
    let [<Literal>] TextCHK = "CHK" 
    let [<Literal>] TextAVG = "AVG" 
    let [<Literal>] TextSTD = "STD" 
    let [<Literal>] TextPPTTIME = "T" 
    let [<Literal>] TextPPTCOUNT= "C" 


[<AutoOpen>]
module DsTextExport =
    //export Excel
    let [<Literal>] TextTagIOAddress           = "외부주소"
    let [<Literal>] TextTagIOVariable          = "변수"
    let [<Literal>] TextTagIOConst             = "상수"
    let [<Literal>] TextTagIOOperator          = "연산"
    let [<Literal>] TextTagIOCommand           = "명령"
    let [<Literal>] TextTagIOAutoBTN           = "자동셀렉트"
    let [<Literal>] TextTagIOManualBTN         = "수동셀렉트"
    let [<Literal>] TextTagIODriveBTN          = "운전푸쉬버튼"
    let [<Literal>] TextTagIOPauseBTN          = "정지푸쉬버튼"
    let [<Literal>] TextTagIOClearBTN          = "해지푸쉬버튼"
    let [<Literal>] TextTagIOEmergencyBTN      = "비상푸쉬버튼"
    let [<Literal>] TextTagIOTestBTN           = "시운전푸쉬버튼"
    let [<Literal>] TextTagIOHomeBTN           = "복귀푸쉬버튼"
    let [<Literal>] TextTagIOReadyBTN          = "준비푸쉬버튼"

    let [<Literal>] TextTagIOAutoLamp          = "자동모드램프"
    let [<Literal>] TextTagIOManualLamp        = "수동모드램프"
    let [<Literal>] TextTagIODriveLamp         = "운전모드램프"
    let [<Literal>] TextTagIOErrorLamp         = "이상모드램프"
    let [<Literal>] TextTagIOEmergencyLamp     = "비상모드램프"
    let [<Literal>] TextTagIOTestLamp          = "시운전모드램프"
    let [<Literal>] TextTagIOReadyLamp         = "준비모드램프"
    let [<Literal>] TextTagIOIdleLamp          = "대기모드램프"
    let [<Literal>] TextTagIOHomingLamp        = "원위치중램프"
    let [<Literal>] TextTagIOConditionReady    = "준비조건"
    let [<Literal>] TextTagIOConditionDrive    = "운전조건"
    let [<Literal>] TextTagIOActionEmg         = "비상출력"
    let [<Literal>] TextTagIOActionPause       = "정지출력"
    
    let [<Literal>] TextTagIOAllFlow =  "ALL"


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
