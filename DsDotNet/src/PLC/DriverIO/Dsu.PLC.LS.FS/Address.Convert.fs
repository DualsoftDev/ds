module AddressConvert

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

/// 16.  개별 읽기 모드에서의 최대 접점 수
let maxRandomReadTagCount = 16


/// 연속 읽기 모드에서의 최대 byte 수.  XGB 일때는 512, 나머지는 1400
let getMaxBlockReadByteCount = function
    | XgbMk -> 512
    | _ -> 1400


// "%MX1A" : word = 1, bit offset = 10  ==> 절대 bit offset = 1 * 16 + 10 = 26
let wordAndBit2AbsBit (word, bit) = word * 16 + bit

// 절대 bit offset 26 ==> word = 1, bit = A
let bit2WordAndBit nthBit =
    let word = nthBit / 16
    let bit = nthBit % 16
    (word, bit)



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

//let inline xToBytes (x:'a) = x |> uint16 |> fun x -> x.ToBytes()

type DeviceType = P | M | L | K | F | D | U | N | Z | T | C | R | I | Q | A | W | ZR    //S Step제어용 디바이스 수집 불가

let (|DevicePattern|_|) (str:string) = DU.fromString<DeviceType> str

let (|DataTypePattern|_|) str =
    try
        Some <| DataType.FromDeviceMnemonic str
    with exn -> None

type LsTagAnalysis = {
    /// Original Tag name
    Tag:string
    Device:DeviceType
    DataType:DataType
    BitOffset:int
    IsIEC:bool
} with
    member x.ByteLength = (max 8 x.BitLength) / 8
    member x.BitLength  = x.DataType.GetBitLength()
    member x.ByteOffset = x.BitOffset / 8
    member x.WordOffset = x.BitOffset / 16
    static member Create(tag, device, dataType, bitOffset, isIEC) = {Tag = tag; Device=device; DataType=dataType; BitOffset=bitOffset; IsIEC=isIEC}
    member x.GetXgiTag():string =
        if x.IsIEC then
            x.Tag
        else
            let offset = x.BitOffset / 16
                        |> toString
                        |> (fun str -> str.PadLeft(4, '0'))

            let offsetBit = x.BitOffset % 16
                            |> (fun str -> sprintf "%X" str)
                            
            match x.DataType with
            | DataType.Bit ->
                
                $"%%{x.Device}X{offset}{offsetBit}"
            | DataType.Word ->
                assert(x.BitOffset%16 = 0)
                $"%%{x.Device}W{offset}"
            | _ ->
                failwith "ERROR"

let createTagInfo = LsTagAnalysis.Create >> Some
let (|LsTagPatternXgi|_|) tag =
    let isIEC = true
    match tag with
    // XGI IEC 61131 : bit
    | RegexPattern @"^%([MLKFNRAWIQU])X([\da-fA-F]+)$" [ DevicePattern device; HexPattern bitOffset ] ->
        createTagInfo(tag, device, DataType.Bit, bitOffset, isIEC)

    | RegexPattern @"^%([IQU])X(\d+)\.(\d+)\.(\d+)$"
        [DevicePattern device; Int32Pattern file; Int32Pattern element; Int32Pattern bit] ->
        let baseStep : int =
            match device with
                | x when x.Equals(DeviceType.U) -> 512 * 16
                | _ -> 64 * 16
        let slotStep : int =
            match device with
                | x when x.Equals(DeviceType.U) -> 512
                | _ -> 64

        //logInfo "test : %O %d %d %d" device file element bit;
        let totalBitOffset = file * baseStep + element * slotStep + bit
        createTagInfo(tag, device, DataType.Bit, totalBitOffset, isIEC)

    //// U 영역은 특수 처리 (서보 및 드라이버)
    //| RegexPattern @"^%(U)([XBWDL])(\d+)\.(\d+)\.(\d+)$"
    //    [DevicePattern device; DataTypePattern dataType; Int32Pattern file; Int32Pattern element; Int32Pattern bit] ->
    //    let byteOffset = element * dataType.GetByteLength()
    //    let fileOffset = file * 16 * 512  //max %U file.element(16).bit(512)
    //    Some {
    //        Tag       = tag
    //        Device    = device
    //        DataType  = dataType
    //        BitOffset = fileOffset + byteOffset + bit }


    | RegexPattern @"^%([MLKFNRAWIQU])([BWDL])([\da-fA-F]+)\.(\d+)$"
       [DevicePattern device; DataTypePattern dataType;  Int32Pattern offset; Int32Pattern bit;] ->
        let totalBitOffset = offset * dataType.GetBitLength() + bit
        createTagInfo(tag, device, DataType.Bit, totalBitOffset, isIEC)


    // XGI IEC 61131 : byte / word / dword / lword
    | RegexPattern @"^%([MLKFNRAWIQU])([BWDL])(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern offset;] ->
        let byteOffset = offset * dataType.GetByteLength()
        let totalBitOffset = byteOffset * 8
        createTagInfo(tag, device, dataType, totalBitOffset, isIEC)
    | RegexPattern @"^%([IQU])([BWDL])(\d+)\.(\d+)\.(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern file; Int32Pattern element; Int32Pattern bit;]->
        let uMemStep : int =
            match device with
                | x when x.Equals(DeviceType.U) -> 8
                | _ -> 1

        let bitStandard = 8 * uMemStep / dataType.GetByteLength();

        let bitSet = (bit % bitStandard) * dataType.GetByteLength() * 8;
        let elementSet = (element % 16 + bit / bitStandard) * 8 * 8 * uMemStep;
        let fileSet = (file + element / 16) * 8 * 8 * 16 * uMemStep;

        let offset = bitSet + elementSet + fileSet;

        //logInfo "bitSet = %d  elementSet = %d fileSet = %d offset = %d" bitSet elementSet fileSet offset;
        createTagInfo(tag, device, dataType, offset, isIEC)
    | _ ->
        logWarn "Failed to parse tag : %s" tag
        None

let (|LsTagPatternXgk|_|) tag =
    let isIEC = false
    match tag with
    // bit devices : Full blown 만 허용.  'P1001A'.  마지막 hex digit 만 bit 로 인식
    | RegexPattern @"^%?([PMLKFTCS])X?(\d{4})([\da-fA-F])$" [ DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
        let totalBitOffset = (wordOffset * 16) + bitOffset
        createTagInfo(tag, device, DataType.Bit, totalBitOffset, isIEC)

    // {word device} or {bit device 의 word 표현} : 'P0000'
    | RegexPattern @"^%?([DRUPMLKFTCS])W?(\d{4})$" [ DevicePattern device; Int32Pattern wordOffset; ] ->
        let totalBitOffset = wordOffset * 16
        createTagInfo(tag, device, DataType.Word, totalBitOffset, isIEC)
    | _ ->
        None

let getXgkTagInfo tag = (|LsTagPatternXgk|_|) tag
let getXgiTagInfo tag = (|LsTagPatternXgi|_|) tag

let tryParseTag (cpu:CpuType) tag =
    match (cpu, tag) with
    | CpuType.Xgk, LsTagPatternXgk x -> Some x
    | CpuType.XgbMk, LsTagPatternXgk x -> Some x
    | CpuType.Xgi, LsTagPatternXgi x -> Some x
    | _ ->
        logWarn "Failed to parse tag : %s" tag
        None
//let tryParseIECTag (tag) =
//    match tag with
//    | RegexPattern @"([PMLKF])(\d\d\d\d)([\da-fA-F])"
//        [DevicePattern device; Int32Pattern offset; HexPattern bitOffset] ->
//        Some (sprintf "%%%sX%d" (device.ToString()) (offset*16 + bitOffset)), Some(DataType.Bit)
//    | RegexPattern @"([PMLKF])(\d+)$"
//        [DevicePattern device; Int32Pattern offset;] ->
//        if(offset > 9999)
//        then  Some (sprintf "%%%sX%d" (device.ToString()) ((offset/10*16)+(offset%16))), Some(DataType.Bit)
//        else  Some (sprintf "%%%sW%d" (device.ToString()) offset), Some(DataType.Word)

//    | RegexPattern @"([RD])(\d+)$"
//        [DevicePattern device;  Int32Pattern offset] ->
//        Some (sprintf "%%%sW%d" (device.ToString()) offset), Some(DataType.Word)
//    | RegexPattern @"([RD])(\d+)\.([\da-fA-F])"
//        [DevicePattern device;  Int32Pattern offset; HexPattern bitOffset] ->
//        Some (sprintf "%%%sW%d.%d" (device.ToString()) offset bitOffset), Some(DataType.Bit)
//    | _ ->
//        //logWarn "Failed to parse tag : %s" tag
//        None, None


/// LS PLC 의 tag 명을 기준으로 data 의 bit 수를 반환
let getBitSize cpu tag =
    tryParseTag cpu tag |> Option.get |> fun x -> x.BitLength

let getBitOffset cpu tag =
    tryParseTag cpu tag |> Option.get |> fun x -> x.BitOffset

let getByteSize cpu tag =
    tryParseTag cpu tag |> Option.get |> fun x -> x.ByteLength

let getDataType cpu tag =
    tryParseTag cpu tag |> Option.get |> fun x -> x.DataType

