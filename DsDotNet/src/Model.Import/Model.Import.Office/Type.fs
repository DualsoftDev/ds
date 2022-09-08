// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq

[<AutoOpen>]
module Type =

    ///인과의 노드 종류
    type NodeCausal =
        | MY            //실제 나의 시스템 1 bit
        | EX            //실제 다른 시스템 1 bit
        | TR            //지시관찰 TX RX 
        | TX            //지시만
        | RX            //관찰만
        | DUMMY         //임시 그룹
        with
            member x.IsReal =   match x with
                                |MY |EX -> true
                                |_ -> false
            member x.IsCall =   match x with
                                |TR |TX |RX -> true
                                |_ -> false

            member x.IsLocation =   match x with
                                    |TR |TX |RX| EX -> true
                                    |_ -> false

                                
    ///인과의 엣지 종류
    type EdgeCausal =
        | SEdge              // A>B	        약 시작 연결
        | SPush              // A>>B	    강 시작 연결
        | SSTATE             // A->B	    시작 조건(안전) 연결
        | REdge              // A|>B	    약 리셋 연결
        | RPush              // A|>>B	    강 리셋 연결
        | RSTATE             // A|->B	    리셋 조건(안전) 연결
        | SReset             // A=>B	    약 시작 + 약 후행 리셋
        | Interlock          // A<||>B<||>C	강 상호 리셋
        with
            member x.ToText() =
                match x with
                | SEdge      -> ">"
                | SPush      -> ">>"
                | SSTATE     -> "->"
                | REdge      -> "|>"
                | RPush      -> "|>>"
                | RSTATE     -> "|->"
                | SReset     -> "=>"
                | Interlock  -> "<||>"
            member x.ToCheckText() =    match x with
                                        |SEdge |SPush |  SReset-> "Start"
                                        |REdge |RPush |  Interlock-> "Reset"
                                        |SSTATE  -> "SSTATE"
                                        |RSTATE  -> "RSTATE"

            member x.IsStart =   match x with
                                 |SEdge |SPush | SSTATE-> true
                                 |_ -> false
            member x.IsReset =   match x with
                                 |REdge |RPush | RSTATE |Interlock-> true
                                 |_ -> false

   
    ///Segment 상태 (Default 'H') reserved
    type Status =
        | H ///Homing
        | R ///Ready
        | G ///Going
        | F ///Finish

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
        with
            member x.ToText() =
                match x with
                | Address      -> "주소"
                | Variable     -> "내부"
                | Command      -> "지시"
                | Observe      -> "관찰"
          

    let TagToType(txt:string) =
            match txt with
            | "주소" -> Address
            | "내부" -> Variable
            | "지시" -> Command
            | "관찰" -> Observe
            |_-> failwithf "TagCase Error"
        
    /// 시스템 전용 문자 리스트  // '_'는 선두만 불가, '~'은 앞뒤만 가능
    let SystemChar = [
                ">"; "<"; "|"; "="; "-"; ";"; ":"; "'"; "\""; "["; "]" ; "{"; "}" 
                "!"; "@"; "#"; "^"; "&"; "*";"/"; "+"; "-"; "?" 
            ]

    let IsInvalidName(name:string) = 
        let ngName = SystemChar |> Seq.filter(fun char -> name.Contains(char))
        ngName.Any() 
        || name.StartsWith("_") 
        || (name.Length > 0 && Char.IsDigit(name.[0]))
        
