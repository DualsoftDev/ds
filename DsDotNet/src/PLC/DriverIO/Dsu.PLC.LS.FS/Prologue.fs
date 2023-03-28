[<AutoOpen>]
module PrologueModule

open Engine.Common.FS
open Dsu.PLC.Common


// 산전 PLC 주소 체계 : https://tech-e.tistory.com/10


/// CPU info.  XGK: 0xA0, XGI: 0XA4, XGR: 0xA8
// 사용설명서_XGB FEnet_국문_V1.5.pdf, 5.2.3
type CpuType =
    | Xgk    // 0xA0uy    // 160
    | Xgi    // 0xA4uy    // 164
    | Xgr    // 0xA8uy    // 168
    | XgbMk  // 0xB0uy
    | XgbIEC // 0xB4uy
    | Unknown
        member x.ToByte() =
            match x with
            | Xgk -> 0xA0uy
            | Xgi -> 0xA4uy
            | Xgr -> 0xA8uy
            | XgbMk -> 0xB0uy
            | XgbIEC -> 0xB4uy
            | _ -> failwith "ERROR"
        member x.ToText() =
            match x with
            | Xgk -> "XGK"
            | Xgi -> "XGI"
            | Xgr -> "XGR"
            | XgbMk -> "XGBMK"
            | XgbIEC -> "XGBIEC"
            | _ -> failwith "ERROR"
        member x.IsXGI() =
            match x with
            | Xgk -> false
            | Xgi -> true
            | Xgr -> false
            | XgbMk -> false
            | XgbIEC -> true
            | _ -> failwith "ERROR"
        static member FromByte (by:byte) =
            match by with
            | 0xA0uy -> CpuType.Xgk
            | 0xA4uy -> CpuType.Xgi
            | 0xA8uy -> CpuType.Xgr
            | 0xB0uy -> CpuType.XgbMk
            | 0xB4uy -> CpuType.XgbIEC
            | _ -> failwith "ERROR"


        static member FromID(cpuId:int) =
            match cpuId with
            //XGI-CPUE 106
            //XGI-CPUH 102
            //XGI-CPUS 104
            //XGI-CPUU 100
            //XGI-CPUU/D 107
            //XGI-CPUUN 111
            | 100 | 102 | 104 | 106 | 107 | 111  -> Xgi
            //XGK-CPUA 3
            //XGK-CPUE 4
            //XGK-CPUH 0
            //XGK-CPUHN 16
            //XGK-CPUS 1
            //XGK-CPUSN 17
            //XGK-CPUU 5
            //XGK-CPUUN 14
            | 0 | 1 | 3 | 4 | 14 | 16 | 17  -> Xgk
            //XGB-DR16C3 6
            //XGB-GIPAM 114
            //XGB-KL 113
            //XGB-XBCE 9
            //XGB-XBCH 7
            //XGB-XBCS 10
            //XGB-XBCU 15
            //XGB-XBCXS 22
            //XGB-XBMH 18
            //XGB-XBMH2 21
            //XGB-XBMHP 19
            //XGB-XBMS 2
            //XGB-XECE 109
            //XGB-XECH 103
            //XGB-XECS 108
            //XGB-XECU 112
            //XGB-XEMH2 116
            //XGB-XEMHP 115
            | 103 | 108 | 109 | 112 | 113 | 114 | 115 | 116 -> XgbIEC
            | 2 | 6 | 7 | 9 | 10 | 15 | 18 | 19 | 21 | 22 -> XgbMk
            | _ -> failwith "ERROR"


/// LS H/W PLC 통신을 위한 data type
type DataType =
    | Bit
    | Byte
    | Word
    | DWord
    | LWord
    | Continuous
        /// Packet 통신에 사용하기 위한 length identifier 값
        member x.ToUInt16() =
            match x with
            | Bit   -> 0us
            | Byte  -> 1us
            | Word  -> 2us
            | DWord -> 3us
            | LWord -> 4us
            | Continuous -> 0x14us
        member x.GetBitLength() =
            match x with
            | Bit   -> 1
            | Byte  -> 8
            | Word  -> 16
            | DWord -> 32
            | LWord -> 64
            | Continuous -> failwith "ERROR"
        member x.GetByteLength() =
            match x with
            | Bit   -> 1
            | _ -> x.GetBitLength() / 8
        member x.ToTagType() =
            match x with
            | Bit   -> TagType.Bit
            | Byte  -> TagType.I1
            | Word  -> TagType.I2
            | DWord -> TagType.I4
            | LWord -> TagType.I8
            | Continuous -> failwith "ERROR"
        member x.ToDataLengthType() =
            match x with
            | Bit   -> DataLengthType.Bit
            | Byte  -> DataLengthType.Byte
            | Word  -> DataLengthType.Word
            | DWord -> DataLengthType.DWord
            | LWord -> DataLengthType.LWord
            | Continuous -> failwith "ERROR"
        member x.ToMnemonic() =
            match x with
            | Bit   -> "X"
            | Byte  -> "B"
            | Word  -> "W"
            | DWord -> "D"
            | LWord -> "L"
            | Continuous -> failwith "ERROR"
        member x.TotextXGK() =
            match x with
            | Bit   -> "BIT" //for XG5000
            | Byte  -> "BYTE"
            | Word  -> "WORD"
            | DWord -> "DWORD"
            | LWord -> "LWORD"
            | Continuous -> failwith "ERROR"
        member x.TotextXGI() =
            match x with
            | Bit   -> "BOOL" //for XG5000
            | Byte  -> "BYTE"
            | Word  -> "WORD"
            | DWord -> "DWORD"
            | LWord -> "LWORD"
            | Continuous -> failwith "ERROR"

        /// uint64 를 data type 에 맞게 boxing 해서 반환
        member x.BoxUI8(v:uint64) =
            match x with
            | Bit   -> v <> 0UL |> box
            | Byte  -> byte v   |> box
            | Word  -> uint16 v |> box
            | DWord -> uint32 v |> box
            | LWord -> uint64 v |> box
            | Continuous -> failwith "ERROR"

        /// Boxing 된 값 v 를 uint64 로 unboxing 해서 반환
        member x.Unbox2UI8(v:obj) =
            match (v, x) with
            | (:? bool as b, Bit)     -> if b then 1UL else 0UL
            | (:? byte as b, Byte)    -> uint64 b
            | (:? uint16 as w, Word)  -> uint64 w
            | (:? uint32 as d, DWord) -> uint64 d
            | (:? uint64 as l, LWord) -> l
            | (:? uint64 as l, _) ->
                logWarn "Mismatched type: %A(%A)" x v
                l
            | _ -> failwith "ERROR"

        static member FromDeviceMnemonic = function
            | "X" -> Bit
            | "B" -> Byte
            | "W" -> Word
            | "D" -> DWord
            | "L" -> LWord
            | _ -> failwith "ERROR"


type DeviceType = P | M | L | K | F | D | U | N | Z | T | C | R | I | Q | A | W | ZR    //S Step제어용 디바이스 수집 불가

type LsFEnetTagInfo = {
    /// Original Tag name
    Tag:string
    Device:DeviceType
    DataType:DataType
    BitOffset:int
} with
    member x.ByteLength = (max 8 x.BitLength) / 8
    member x.BitLength  = x.DataType.GetBitLength()
    member x.ByteOffset = x.BitOffset / 8
    member x.WordOffset = x.BitOffset / 16
    static member Create(tag, device, dataType, bitOffset) = {Tag = tag; Device=device; DataType=dataType; BitOffset=bitOffset;}
    //member x.GetXgiTag():string =
    //    if x.IsIEC then
    //        x.Tag
    //    else
    //        let offset = x.BitOffset / 16
    //                    |> toString
    //                    |> (fun str -> str.PadLeft(5, '0'))

    //        match x.DataType with
    //        | DataType.Bit ->
    //            $"%%{x.Device}X{offset}"
    //        | DataType.Word ->
    //            assert(x.BitOffset%16 = 0)
    //            $"%%{x.Device}W{offset}"
    //        | _ ->
    //            failwith "ERROR"
