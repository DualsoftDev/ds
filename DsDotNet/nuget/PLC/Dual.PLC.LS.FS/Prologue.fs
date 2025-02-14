[<AutoOpen>]
module rec PrologueModule

open Dual.Common.Core.FS
open Dual.PLC.Common
open System.IO
open FSharp.Data

let path = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "DeviceSizeInfo.csv")
type DeviceSizeCsvType = CsvProvider<"DeviceSizeInfo.csv">
let DeviceSizeTable = DeviceSizeCsvType.Load(path)

// 산전 PLC 주소 지원 Max Size : XG5000설치후 C:\XG5000\l.kor\Symbol.mdb 열기 ->  device_info table -> 복사해서 DeviceSizeInfo.csv 만들기
module PLCHwModel =
 
    type ModelInfo = {
        Name:string
        Id: int
        Type: CpuType
        IsIEC: bool
    }

    let xgi    name id = { Name = name; Id = id; Type = CpuType.Xgi    ; IsIEC = true}
    let xgk    name id = { Name = name; Id = id; Type = CpuType.Xgk    ; IsIEC = false}
    let xgbmk  name id = { Name = name; Id = id; Type = CpuType.XgbMk  ; IsIEC = false}
    let xgbiec name id = { Name = name; Id = id; Type = CpuType.XgbIEC ; IsIEC = true}

    let Models = [|
            xgi "XGI-CPUU"   100
            xgi "XGI-CPUH"   102
            xgi "XGI-CPUS"   104
            xgi "XGI-CPUE"   106
            xgi "XGI-CPUU/D" 107
            xgi "XGI-CPUUN"  111


            xgk "XGK-CPUH"   0
            xgk "XGK-CPUS"   1
            xgk "XGK-CPUA"   3
            xgk "XGK-CPUE"   4
            xgk "XGK-CPUU"   5
            xgk "XGK-CPUUN"   14
            xgk "XGK-CPUHN"   16
            xgk "XGK-CPUSN"   17

            xgbmk "XGB-XBMS"  2
            xgbmk "XGB-DR16C3"  6
            xgbmk "XGB-XBCH"  7
            xgbmk "XGB-XBCE"  9
            xgbmk "XGB-XBCS"  10
            xgbmk "XGB-XBCU"  15
            xgbmk "XGB-XBMH"  18
            xgbmk "XGB-XBMHP"  19
            xgbmk "XGB-XBMH2"  21
            xgbmk "XGB-XBCXS"  22

            xgbiec "XGB-XECH"  103
            xgbiec "XGB-XECS"  108
            xgbiec "XGB-XECE"  109
            xgbiec "XGB-XECU"  112
            xgbiec "XGB-KL"  113
            xgbiec "XGB-GIPAM"  114
            xgbiec "XGB-XEMHP"  115
            xgbiec "XGB-XEMH2"  116
    |]
    let FindModel(cpuId:int) = PLCHwModel.Models |> Array.tryFind(fun (x:PLCHwModel.ModelInfo) -> x.Id = cpuId)
    let GetModelName(cpuId:int) =
        //match FindModel(cpuId) with
        //| Some model -> model.Name
        //| _ -> failwith "ERROR"
        $"CpuID: {cpuId}"


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
        member x.IsIEC() =
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
            match PLCHwModel.FindModel(cpuId) with
            | Some model -> model.Type
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

    
        

type DeviceType = P | M | L | K | F | D | U | N | Z | T | C | R | I | Q | A | W | S | ZR    //S Step제어용 디바이스 수집 불가
    with
        member x.ToText() = 
            match x with
            |P -> "P" |M -> "M" |L -> "L" |K -> "K" |F -> "F" |D -> "D" |U -> "U"
            |N -> "N" |Z -> "Z" |T -> "T" |C -> "C" |R -> "R" |I -> "I" |Q -> "Q"
            |A -> "A" |W -> "W" |S -> "S" |ZR ->"ZR"

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
    member x.GetIOM() = 
            match x.Device with
            |I -> "I" |Q -> "O"
            |_-> "M"  //XGK P 영역도 M 처리해서.. HW 설정하고 비교 필요

    static member Create(tag:string, (device: DeviceType), dataType, bitOffset:int, modelId:int option) =
        if modelId.IsSome   //None 이면 체크 생략
        then 
            let sizeWord = DeviceSizeTable.Rows 
                        |> Seq.where(fun r->r.NPLCID = modelId.Value)
                        |> Seq.find(fun r->r.StrDevice = device.ToText())
                        |> fun f-> f.NSize

            if bitOffset/16 > sizeWord
            then 
                failwithf $"size over tag:{tag}, device:{device}{bitOffset}, dataType{dataType}"


        {Tag = tag; Device=device; DataType=dataType; BitOffset=bitOffset;}
  