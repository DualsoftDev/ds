[<AutoOpen>]
module rec PLCHwModelImpl

open Dual.Common.Core.FS
open System.IO
open FSharp.Data


// 산전 PLC 주소 지원 Max Size : XG5000설치후 C:\XG5000\l.kor\Symbol.mdb 열기 ->  device_info table -> 복사해서 DeviceSizeInfo.csv 만들기
module PLCHwModel =

    type TagType =
        | Bit
        | I1
        | I2
        | I4
        | I8
        | F4

    type DataLengthType =
        | Undefined
        | Bit // 1 Bit
        | Byte // 1 Byte
        | Word // 2 Byte
        | DWord // 4 Byte
        | LWord // 8 Byte

    // 산전 PLC 주소 체계 : https://tech-e.tistory.com/10
    /// CPU info.  XGK: 0xA0, XGI: 0XA4, XGR: 0xA8
    // 사용설명서_XGB FEnet_국문_V1.5.pdf, 5.2.3
    type CpuType =
        | Xgk // 0xA0uy    // 160
        | Xgi // 0xA4uy    // 164
        | Xgr // 0xA8uy    // 168
        | XgbMk // 0xB0uy
        | XgbIEC // 0xB4uy
        | Unknown

       
        member x.ToText() =
            match x with
            | Xgk -> "XGK"
            | Xgi -> "XGI"
            | Xgr -> "XGR"
            | XgbMk -> "XGBMK"
            | XgbIEC -> "XGBIEC"
            | _ -> failwithlog "ERROR"

        member x.IsIEC() =
            match x with
            | Xgk -> false
            | Xgi -> true
            | Xgr -> false
            | XgbMk -> false
            | XgbIEC -> true
            | _ -> failwithlog "ERROR"


    /// LS H/W PLC 통신을 위한 data type
    type DataType =
        | Bit
        | Byte
        | Word
        | DWord
        | LWord
        | Continuous


        member x.GetBitLength() =
            match x with
            | Bit -> 1
            | Byte -> 8
            | Word -> 16
            | DWord -> 32
            | LWord -> 64
            | Continuous -> failwithlog "ERROR"

        member x.GetByteLength() =
            match x with
            | Bit -> 1
            | _ -> x.GetBitLength() / 8

      
        member x.ToDataLengthType() =
            match x with
            | Bit -> DataLengthType.Bit
            | Byte -> DataLengthType.Byte
            | Word -> DataLengthType.Word
            | DWord -> DataLengthType.DWord
            | LWord -> DataLengthType.LWord
            | Continuous -> failwithlog "ERROR"

        member x.ToMnemonic() =
            match x with
            | Bit -> "X"
            | Byte -> "B"
            | Word -> "W"
            | DWord -> "D"
            | LWord -> "L"
            | Continuous -> failwithlog "ERROR"

        member x.TotextXGK() =
            match x with
            | Bit -> "BIT" //for XG5000
            | Byte -> "BYTE"
            | Word -> "WORD"
            | DWord -> "DWORD"
            | LWord -> "LWORD"
            | Continuous -> failwithlog "ERROR"

      

        static member FromDeviceMnemonic =
            function
            | "X" -> Bit
            | "B" -> Byte
            | "W" -> Word
            | "D" -> DWord
            | "L" -> LWord
            | _ -> failwithlog "ERROR"




    type DeviceType =
        | P
        | M
        | L
        | K
        | F
        | D
        | U
        | N
        | Z
        | T
        | C
        | R
        | I
        | Q
        | A
        | W
        | S
        | ZR //S Step제어용 디바이스 수집 불가

        member x.ToText() =
            match x with
            | P -> "P"
            | M -> "M"
            | L -> "L"
            | K -> "K"
            | F -> "F"
            | D -> "D"
            | U -> "U"
            | N -> "N"
            | Z -> "Z"
            | T -> "T"
            | C -> "C"
            | R -> "R"
            | I -> "I"
            | Q -> "Q"
            | A -> "A"
            | W -> "W"
            | S -> "S"
            | ZR -> "ZR"
