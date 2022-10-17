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
        | Variable
        | Command
        | Observe
        | Button
    with
        member x.ToText() =
            match x with
            | Address      -> TextAddressDev  
            | Variable     -> TextVariable 
            | Command      -> TextCommand  
            | Observe      -> TextObserve  
            | Button       -> TextButton   
          

    let TagToType(txt:string) =
        match txt with
        | TextAddressDev   -> Address
        | TextVariable     -> Variable
        | TextCommand      -> Command
        | TextObserve      -> Observe
        | TextButton       -> Button
        |_-> failwithf $"'{txt}' TagCase Error check type [
                , {TextAddressDev}, {TextVariable}
                , {TextCommand}, {TextObserve}
                , {TextButton}]"


    ///BtnType 인과의 노드 종류
    type BtnType =
        | StartBTN            //시작 버튼
        | ResetBTN            //리셋 버튼
        | AutoBTN             //자동 버튼
        | EmergencyBTN        //비상 버튼
       

    let BtnToType(txt:string) =
        match txt with
        | TextStartBtn -> StartBTN
        | TextResetBtn -> ResetBTN
        | TextAutoBtn -> AutoBTN
        | TextEmgBtn -> EmergencyBTN
        |_-> failwithf $"'{txt}' BtnToType Error check type [
                , {TextStartBtn}, {TextResetBtn}
                , {TextAutoBtn}, {TextEmgBtn}]"



