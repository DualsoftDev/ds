module AddressConvert

open Engine.Common.FS
open Dsu.PLC.Common
open System.Text.RegularExpressions


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




//let inline xToBytes (x:'a) = x |> uint16 |> fun x -> x.ToBytes()


let (|DevicePattern|_|) (str:string) = DU.fromString<DeviceType> str

let (|DataTypePattern|_|) str =
    try
        Some <| DataType.FromDeviceMnemonic str
    with exn -> None

let isXgiTag tag =
    Regex(@"^%([MLKFNRAWIQU])X([\d]+)$").IsMatch(tag)
    || Regex(@"^%([IQU])X(\d+)\.(\d+)\.(\d+)$").IsMatch(tag)
    || Regex(@"^%([MLKFNRAWIQU])([BWDL])(\d+)\.(\d+)$").IsMatch(tag)
    || Regex(@"^%([MLKFNRAWIQU])([BWDL])(\d+)$").IsMatch(tag)
    || Regex(@"^%([IQU])([BWDL])(\d+)\.(\d+)\.(\d+)$").IsMatch(tag)

let isXgkTag tag =
    Regex(@"^([DPMNLKFTCSZ])(\d{4})([\da-fA-F])$").IsMatch(tag)
    || Regex(@"^([DRUPMNLKFTCSZ])(\d{4})$").IsMatch(tag)


/// e.g XGK cpu 의 tag "P0000A" 를 FEnet 통신을 위한 tag 인 "%PX10" 으로 변환해서 반환
/// e.g "P0011" -> "%PW11"
/// e.g "P0011F" -> "%PX191" (11*16 + 15 = 191)
let (|ToFEnetTag|_|) (fromCpu:CpuType) tag =
    match fromCpu with
    | CpuType.XgbMk ->
        /// Word 와 bit type 만 존재
        match tag with
        // bit devices : Full blown 만 허용.  'P1001A'.  마지막 hex digit 만 bit 로 인식
        | RegexPattern @"^([DPMNLKFTCSZ])(\d{4})([\da-fA-F])$" [ DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
            let bitOffset = wordOffset * 16 + bitOffset
            Some $"%%{device}X{bitOffset}"

        // {word device} or {bit device 의 word 표현} : 'P0000'
        | RegexPattern @"^([DRPMNLKFTCSZ])(\d{4})$" [ DevicePattern device; Int32Pattern wordOffset; ] ->
            Some $"%%{device}W{wordOffset}"
        | RegexPattern @"^U(\d+)\.(\d+)$" [Int32Pattern element; Int32Pattern bit] ->
            Some $"%%UW{element * 32 + bit}"
        //| RegexPattern @"^U(\d+)\.(\d+).(\d+)$" [Int32Pattern file; Int32Pattern element; Int32Pattern bit] ->
        //    Some $"%%UX{file * 32 * 16 + element * 16 + bit}"
        | _ ->
            None
    | CpuType.Xgi ->
        match tag with
        | RegexPattern @"^%?([IQ])X(\d+)\.(\d+)\.(\d+)$" [DevicePattern device; Int32Pattern file; Int32Pattern element; Int32Pattern bit] ->
            let totalBitOffset = file * 16*64 + element*64  + bit
            Some $"%%{device}X{totalBitOffset}"
        | RegexPattern @"^%?(U)X(\d+)\.(\d+)\.(\d+)$" [DevicePattern device; Int32Pattern file; Int32Pattern element; Int32Pattern bit] ->
            let totalBitOffset = file * 16*512 + element*512  + bit
            Some $"%%{device}X{totalBitOffset}"
        | RegexPattern @"^%?([IQ])([BWDL])(\d+)\.(\d+)\.(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern d1; Int32Pattern d2; Int32Pattern d3] ->
            let step = 64/dataType.GetBitLength()
            let offset = d1 * 16 * step + d2 * step + d3
            Some $"%%{device}{dataType.ToMnemonic()}{offset}"
        | RegexPattern @"^%?(U)([BWDL])(\d+)\.(\d+)\.(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern d1; Int32Pattern d2; Int32Pattern d3] ->
            let step = 64/dataType.GetBitLength() * 8
            let offset = d1 * 16 * step + d2 * step + d3
            Some $"%%{device}{dataType.ToMnemonic()}{offset}"
        | RegexPattern @"^%?([IQ])([BWDLX])(\d+)\.(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern element; Int32Pattern bit] ->
            let totalBitOffset = element * dataType.GetBitLength() + bit
            Some $"%%{device}X{totalBitOffset}"
        | RegexPattern @"^%?([IQ])([BWDLX])(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern bit] ->
            Some $"%%{device}{dataType.ToMnemonic()}{bit}"
        | RegexPattern @"^%?([MLKFNRAWIQ])(X)(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern bit;] ->
            Some $"%%{device}{dataType.ToMnemonic()}{bit}"
        | RegexPattern @"^%?([MLKFNRAWIQ])([BWDL])(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern offset;] ->
            Some $"%%{device}{dataType.ToMnemonic()}{offset}"
        | RegexPattern @"^%?([MLKFNRAWIQ])([BWDL])(\d+)\.(\d+)$" [DevicePattern device; DataTypePattern dataType;  Int32Pattern element; Int32Pattern bit;] ->
            let totalBitOffset = element * dataType.GetBitLength() + bit
            Some $"%%{device}X{totalBitOffset}"

        | _ ->
            None
    | _ ->
        None


let tryToFEnetTag (fromCpu:CpuType) tag = (|ToFEnetTag|_|) fromCpu tag

let createTagInfo = LsFEnetTagInfo.Create >> Some
let (|LsTagPatternFEnet|_|) tag =
    match tag with
    // XGI IEC 61131 : bit
    // XGK "Z" "D"
    | RegexPattern @"^%([PMLKFNRAWIQUZD])X([\da-fA-F]+)$" [ DevicePattern device; HexPattern bitOffset ] ->
        createTagInfo(tag, device, DataType.Bit, bitOffset)

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
        createTagInfo(tag, device, DataType.Bit, totalBitOffset)

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


    | RegexPattern @"^%([PMLKFNRAWIQU])([BWDL])([\da-fA-F]+)\.(\d+)$"
       [DevicePattern device; DataTypePattern dataType;  Int32Pattern offset; Int32Pattern bit;] ->
        let totalBitOffset = offset * dataType.GetBitLength() + bit
        createTagInfo(tag, device, DataType.Bit, totalBitOffset)


    //  XGI IEC 61131 : byte / word / dword / lword  
    //  XGK "S" "T" "C" "Z" "D"
    | RegexPattern @"^%([PMLKFNRAWIQUSTCZD])([BWDL])(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern offset;] ->
        let byteOffset = offset * dataType.GetByteLength()
        let totalBitOffset = byteOffset * 8
        createTagInfo(tag, device, dataType, totalBitOffset)
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
        createTagInfo(tag, device, dataType, offset)
    | _ ->
        logWarn "Failed to parse tag : %s" tag
        None

/// LS PLC 통신 규약인 FEnet 규격을 따르는 tag 정보를 parsing.   규격 미충족 tag 는 Option.None 반환
let tryParseTag tag =  (|LsTagPatternFEnet|_|) tag


/// LS PLC 의 tag 명을 기준으로 data 의 bit 수를 반환
let getBitSize tag =
    tryParseTag tag |> Option.get |> fun x -> x.BitLength

let getBitOffset tag =
    tryParseTag tag |> Option.get |> fun x -> x.BitOffset

let getByteSize tag =
    tryParseTag tag |> Option.get |> fun x -> x.ByteLength

let getDataType tag =
    tryParseTag tag |> Option.get |> fun x -> x.DataType

