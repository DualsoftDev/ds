module Address

let (|LsTagPatternXgk|_|) tag =
    let getBitTag device wordOffset bitOffset = {
        Tag       = tag
        Device    = device
        DataType  = DataType.Bit
        BitOffset = wordOffset * 16 + bitOffset}
    let getWordTag device wordOffset = {
        Tag       = tag
        Device    = device
        DataType  = DataType.Word
        BitOffset = wordOffset * 16}
    match tag with
    (* { Old code *)
    //word + bit 타입은 word 가 4자리 고정
    | RegexPattern @"([PMKF])(\d\d\d\d)([\da-fA-F])"
        [DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
        Some (getBitTag device wordOffset bitOffset)
    | RegexPattern @"([PMKF])(\d\d\d\d)"
        [DevicePattern device; Int32Pattern wordOffset;] ->
        Some (getWordTag device wordOffset)

    //L타입은 word + bit 타입은 word 가 4자리 고정
    | RegexPattern @"(L)(\d\d\d\d\d)([\da-fA-F])"
        [DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
        Some (getBitTag device wordOffset bitOffset)
    | RegexPattern @"(L)(\d\d\d\d\d)"
        [DevicePattern device; Int32Pattern wordOffset;] ->
        Some (getWordTag device wordOffset)

    //ZR 타입은 word  타입
    | RegexPattern @"(ZR)(\d+)$"
        [DevicePattern device;   Int32Pattern wordOffset;] ->
        Some (getWordTag DeviceType.R wordOffset)
    //R or D 타입은 word 타입
    | RegexPattern @"([RDTCZN])(\d+)$"
        [DevicePattern device;  Int32Pattern wordOffset;] ->
        Some (getWordTag device wordOffset)
    // word.bit 타입
    | RegexPattern @"([RD])(\d+)\.([\da-fA-F])"
        [DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
        Some (getBitTag device wordOffset bitOffset)

    //S타입 word.bit
    | RegexPattern @"(S)(\d+)\.(\d+)$"  //마지막 비트 단위가 100인 특수 디바이스
        [DevicePattern device;  Int32Pattern wordOffset; Int32Pattern bitOffset] ->
        Some (getBitTag device 0 (wordOffset*100+bitOffset))
    //U타입 word
    | RegexPattern @"(U)(\d+)\.(\d+)$"
        [DevicePattern device;  Int32Pattern wordOffsetA; Int32Pattern wordOffsetB;] ->
        Some (getWordTag device (wordOffsetA*32+wordOffsetB))
    //U타입 word.bit 타입
    | RegexPattern @"(U)(\d+)\.(\d+)\.([\da-fA-F])$"
        [DevicePattern device; Int32Pattern wordOffsetA;Int32Pattern wordOffsetB; HexPattern bitOffset] ->
        Some (getBitTag device 0 (wordOffsetA*16*32 + wordOffsetB*16 + bitOffset))

    // 수집 전용 타입
    | RegexPattern @"%([PMLKFWURDTCZN])([BWDL])(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern offset;] ->
            let byteOffset = offset * dataType.GetByteLength()
            Some {
                Tag       = tag
                Device    = device
                DataType  = dataType
                BitOffset = byteOffset * 8}
    //ZR은 R영역으로 수집한다.
    | RegexPattern @"%(ZR)([BWDL])(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern offset;] ->
            let byteOffset = offset * dataType.GetByteLength()
            Some {
                Tag       = tag
                Device    = DeviceType.R
                DataType  = dataType
                BitOffset = byteOffset * 8}
    (* } Old code *)


    (* { New code *)
    // bit devices
    | RegexPattern @"^%([PMLKFTCDUZSN])X(\d+)([\da-fA-F])$"     // CDFKLMNPSTUZ
        [DevicePattern device; Int32Pattern wordOffset; HexPattern bitOffset] ->
        Some {
            Tag       = tag
            Device    = device
            DataType  = DataType.Bit
            BitOffset = (wordOffset * 16) + bitOffset}
    // byte / word / dword / lword
    | RegexPattern @"^%([PMLKFTCDUZSN])([BWDL])(\d+)$"
        [DevicePattern device; DataTypePattern dataType; Int32Pattern offset;] ->
        let byteOffset = offset * dataType.GetByteLength()
        Some {
            Tag       = tag
            Device    = device
            DataType  = dataType
            BitOffset = byteOffset * 8}
    (* } New code *)
    | _ ->
        None
