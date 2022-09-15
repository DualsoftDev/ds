// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Base



[<AutoOpen>]
module DsType =

    ///인과의 노드 종류
    type NodeCausal =
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
            |_-> failwithf "EdgeCausalType Error"
   
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
            | DWORD         //UDINT32
            | LWORD         //ULINT64
            | STRING        //TEXT
            | FLOAT         //Single
            | DOUBLE        //Double

    let DataToType(txt:string) =
            match txt.ToLower() with
            | "bit" | "bool" -> BOOL
            | "byte" -> BYTE
            | "word" -> WORD
            | "dword" -> DWORD
            | "lword" -> LWORD
            | "string" -> STRING
            | "float"| "single" -> FLOAT
            | "double" -> DOUBLE
            |_-> failwithf "DataToType Error"
     
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
                | Address      -> "주소"
                | Variable     -> "내부"
                | Command      -> "지시"
                | Observe      -> "관찰"
                | Button       -> "버튼"
          

    let TagToType(txt:string) =
            match txt with
            | "주소" -> Address
            | "내부" -> Variable
            | "지시" -> Command
            | "관찰" -> Observe
            | "버튼" -> Button
            |_-> failwithf "TagCase Error"
    