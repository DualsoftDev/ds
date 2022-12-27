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

    type DataType =
        | BOOL          // BIT
        | BYTE          // USINT8
        | WORD          // UINT16
        | DWORD         // UDINT32
        | LWORD         // ULINT64
        | STRING        // TEXT
        | FLOAT         // Single
        | DOUBLE        // Double

    let DataToType(txt:string) =
        match txt.ToLower() with
        | TextBit | TextBool -> BOOL
        | TextByte -> BYTE
        | TextWord -> WORD
        | TextDword -> DWORD
        | TextLword -> LWORD
        | TextString  -> STRING
        | TextSingle | TextFloat -> FLOAT
        | TextDouble -> DOUBLE
        |_ -> failwithf $"'{txt}' DataToType Error check type [
              {TextBit}, {TextBool}, {TextByte}
            , {TextWord}, {TextDword}, {TextLword}
            , {TextString}, {TextSingle}, {TextFloat}, {TextDouble}]"

    ///인터페이스 Tag 기본 형식
    type TagCase =
        | Address
        | VariableData
        | Command
        | Observe
    with
        member x.ToText() =
            match x with
            | Address      -> TextAddressDev
            | VariableData -> TextVariable
            | Command      -> TextCommand
            | Observe      -> TextObserve

    ///BtnType  종류
    type BtnType =
        | DuAutoBTN             //자동 버튼
        | DuManualBTN           //수동 버튼
        | DuEmergencyBTN        //비상 버튼
        | DuStopBTN             //정지 버튼
        | DuRunBTN              //운전 버튼
        | DuDryRunBTN           //시운전 시작 버튼
        | DuClearBTN            //해지 버튼

    ///LampType  종류
    type LampType =
        | DuRunModeLamp     //운전모드 램프
        | DuDryRunModeLamp  //시 운전모드  램프
        | DuManualModeLamp  //수동 모드 램프
        | DuStopModeLamp    //정지 모드 램프


    ///ExcelCase 입력 종류
    type ExcelCase =
        | ExcelStartBTN        
        | ExcelResetBTN        
        | ExcelAutoBTN         
        | ExcelEmergencyBTN    
        | ExcelAddress      
        | ExcelVariable     
        | ExcelCommand      
        | ExcelObserve      

    let ExcelCaseToType(txt:string) =
        match txt with
        | TextAddressDev   -> ExcelAddress
        | TextVariable     -> ExcelVariable
        | TextEmgBtn       -> ExcelEmergencyBTN
        | TextStartBtn     -> ExcelStartBTN
        | TextAutoBtn      -> ExcelResetBTN
        | TextResetBtn     -> ExcelAutoBTN
        | TextCommand      -> ExcelCommand
        | TextObserve      -> ExcelObserve
        |_-> failwithf $"'{txt}' TagCase Error check type [
                , {TextAddressDev}, {TextVariable}
                , {TextCommand}, {TextObserve}
                , {TextAutoBtn}, {TextResetBtn}
                , {TextEmgBtn}, {TextStartBtn}
                ]"
