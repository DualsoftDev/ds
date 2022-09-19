// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core



[<AutoOpen>]
module DsType =

    ///인과의 노드 종류
    type NodeType =
        | MY            //실제 나의 시스템 1 bit
        | EX            //실제 다른 시스템 1 bit
        | TR            //지시관찰 TX RX 
        | TX            //지시만
        | RX            //관찰만
        with
            member x.IsReal =   match x with
                                |MY |EX -> true
                                |_ -> false
            member x.IsCall =   match x with
                                |TR |TX |RX -> true
                                |_ -> false

    ///인과의 엣지 종류
    type EdgeCausal =
        | SEdge              // A>B	        약 시작 연결
        | SPush              // A>>B	    강 시작 연결
        | REdge              // A|>B	    약 리셋 연결
        | RPush              // A|>>B	    강 리셋 연결
        | SReset             // A=>B	    약 시작 + 약 후행 리셋
        | Interlock          // A<||>B<||>C	강 상호 리셋
        with
            member x.ToText() =
                match x with
                | SEdge      -> TextSEdge
                | SPush      -> TextSPush     
                | REdge      -> TextREdge     
                | RPush      -> TextRPush     
                | SReset     -> TextSReset    
                | Interlock  -> TextInterlock 

            member x.IsStart =   match x with
                                 |SEdge |SPush  -> true
                                 |_ -> false
            member x.IsReset =   match x with
                                 |REdge |RPush |Interlock-> true
                                 |_ -> false

    let EdgeCausalType(txt:string) =
            match txt with
            | TextSEdge      -> SEdge
            | TextSPush      -> SPush     
            | TextREdge      -> REdge     
            | TextRPush      -> RPush     
            | TextSReset     -> SReset    
            | TextInterlock  -> Interlock 
            |_-> failwithf $"'{txt}' EdgeCausalType Error check type [
                            , {TextSEdge}, {TextSPush}
                            , {TextREdge}, {TextRPush}
                            , {TextSReset}, {TextInterlock}]"
            
   
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
            |_-> failwithf $"'{txt}' DataToType Error check type [
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
                | Address      -> TextAddress  
                | Variable     -> TextVariable 
                | Command      -> TextCommand  
                | Observe      -> TextObserve  
                | Button       -> TextButton   
          

    let TagToType(txt:string) =
            match txt with
            | TextAddress   -> Address
            | TextVariable  -> Variable
            | TextCommand   -> Command
            | TextObserve   -> Observe
            | TextButton    -> Button
            |_-> failwithf $"'{txt}' TagCase Error check type [
                            , {TextAddress}, {TextVariable}
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

    