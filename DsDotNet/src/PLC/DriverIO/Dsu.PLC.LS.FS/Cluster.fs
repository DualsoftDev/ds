module Cluster

open System
open System.Collections.Generic
open Dual.Common.Core.FS
open FSharpPlus
open AddressConvert

//
// 다수의 tags 를 최소한(or sub optimal)의 횟수로 PLC 에서 읽어 오기 위한 전략 수립
//
// 연속 block read 의 경우, 1400 byte 까지 읽을 수 있다.
// random read 의 경우, 16 점 까지 읽을 수 있다.  (LWord 8 byte 단위로 읽으면 128 byte 까지 읽을 수 있다.)
// 전략
//  - 연속 block read 가 가능한/효율적인 영역을 먼저 추려낸다.
//      1. 동일 device type(M, I, X, ..) 에 속하는 모든 tag 들에 대해서
//      1. byte 로 환산한 address offset 들에 해당하는 byte 들을 marking
//            (bit/byte = 1, word = 2, dword = 4, lword = 8)
//      1. marking 된 byte 단위의 offset 에서 1400 byte window size 내에 128 byte 초과 marking 된 구간 검색
//          - marking 가능하더라도 최대한 많은 marking 을 포함하는 구간을 최우선으로 하나 추려냄
//          - 추려내고 남은 2개의 구간에 대해서 동일 과정 재귀 반복
//
// === 예제
//  * M device 에 사용된 address 가 다음과 같다고 할 때,
//      [1..5] @ [253..7..270] @ [1410..2900] @ [3000; 3005]
//      = [1; 2; 3; 4; 5; 253; 260; 267; 1410; .. 2900; 3000; 3005]
//
//  * pairwise 해서 뒤에서 앞의 것을 뺴면
//      [1; 1; 1; 1; 248; 7; 7; 1143; 1; 1; 1; 1; ... ]
//
//  * 앞에서부터 seed 0 로 쭉 더하면
//      [0; 1; 2; 3; 4; 252; 259; 266; 1409; 1410; 1411; 1412; 1413; ...
//
//  * 여기에서 1400 보다 작은 구간인 M.[0..8] 는 총 8개의 항목으로,
//      개별 쓰기 최대치인 128 보다 작으므로 연속 쓰기를 할 필요가 없다.
//      M.[0..8] 까지 구간은 추후 개별 쓰기로 사용하기 위해서 모음 : Fragment
//
//  * 나머지 구간에 대해서 반복
//
//
// === 용어
//  - Device Memory (M)
//      * Device type 이 주어졌을 때, byte 0 부터 PLC 에서 제공하는 크기만큼 순차적으로 표현한 array
//      * XGK 의 M device 의 경우, M.[0..2047] byte
//  - Device Memory Map (MM)
//      * Device memory 중에서 사용하고자 하는 영역을 marking 한 것.
//      * M 영역에서 2, 4, 5 byte 가 사용되는 영역이면, MM = [2; 4; 5]
//  - Device Memory Map Index (MMI) : 절대 byte offset
//      * MM 값의 index.  MMI.[2] = 0, MMI.[4] = 1, MMI.[5] = 2
//
//

/// MMI start * end
type IndexRange = int * int

/// MM 변경시, Tag 를 update 하기 위해서 Tag 를 찾기 위한 구조
type BackTracking = MultiMap<int, string>
type BackTrackings = Dictionary<DeviceType, BackTracking>



/// DeviceType * int list
type FragmentType = DeviceType * int list
/// DeviceType * IndexRange
type BlockType = DeviceType * IndexRange
type ClusterScan =
    | Fragment of FragmentType
    | Block of BlockType

/// tag name * value
type TagKeyValueTuple = string * uint64

/// Block read 시, start tag 주소 와 읽을 byte 수(count) 가 주어졌을 때,
/// 연속으로 읽은 byte array 를 반환한다.
type internal ByteReader = string -> int -> byte[]


/// Block write 시, start tag 주소 와 write 할 bytes 가 주어졌을 때,
type internal ByteWriter = string -> byte[] -> unit

/// Random/Block read 시, 읽을 tag 주소 목록이 주어지면, 읽은 결과를 반환하는 함수 type
type TagsReader = string[] -> TagKeyValueTuple[]


/// Random/Block write 시, write 할 [tag 명 * 값]들이 주어지면, write 한 갯수를 반환하는 함수 type
type TagsWriter = TagKeyValueTuple[] -> int

/// (이미 주어진 tag set 에 한해서) 효율적으로 읽을 수 있는 방법(function 객체)을
/// 저장해 두기 위한 type
type internal CachedTagsReader = unit -> TagKeyValueTuple[]


/// (이미 주어진 [tag * value] set 에 한해서) 효율적으로 쓸 수 있는 방법(function 객체)을
/// 저장해 두기 위한 type
type internal CachedTagsWriter = unit -> int


/// Memory scan 한 정보(ClusterScan) : Fragment or Block 정보 반환
let scanBytes cpu deviceType (mem:int list) =
    let maxBlockReadByteCount = getMaxBlockReadByteCount cpu
    /// mem 를 시작 index 에서부터 looping helper
    // Fragment or Block 의 index 들을 뽑아냄
    let rec loop startIndex =
        if startIndex >= mem.Length then
            List.empty
        else
            //tracefn "Processing from %d : %A" startIndex mem
            [
                let indexedOffsets =
                    mem.[startIndex..]
                    |> pairwise
                    |> map (fun (a, b) -> b - a)
                    |> scan ((+)) 0
                    |> indexed

                let segment =
                    indexedOffsets
                    |> takeWhile (fun (i, o) -> o < maxBlockReadByteCount)
                    |> map (fst >> ((+) startIndex))


                let s = segment |> List.head
                let e = segment |> List.last
                if segment.Length < 128 then    // 128 = 16 * 8 = maxRandomReadTagCount * LWordSize
                    //tracefn "\tYielding Framgment(%A)" segment
                    yield Fragment(deviceType, segment)
                else
                    //tracefn "\tYielding Block(%d, %d)" s e
                    yield Block(deviceType, (s, e))

                yield! loop (e+1)
            ]

    // Fragment or Block 의 index 로부터 실제 주소값 정보들을 추출
    loop 0
    |> map (
        // index 를 절대 memory offset 으로 변환
        function
            | Fragment(devTy, indices) ->
                let values = indices |> map (fun n -> mem.[n])
                Fragment(devTy, values)
            | Block (devTy, (s, e)) ->
                Block(devTy, (mem.[s], mem.[e])))


/// Tags 들을 최소 횟수 통신을 위해서 정보 추출
/// [ClusterScan] * Back tracking 정보
let clusterTags cpu (tags:string[]) =

    /// DeviceType 별 분석 결과.   (DeviceType * LsTagAnalysis []) []
    let perDevices =
        tags
        |> sort
        |> distinct
        |> map (tryParseTag >> Option.get)
        |> groupBy (fun anal -> anal.Device)


    /// MM (Memory type 별로 byte offset) 에 mapping 된 Tag 들이 무엇이 있는지 정보 기입
    let bts = BackTrackings()
    // device 별로 사용된 byte offset 을 list 로 추려냄
    let clusterScans =
        [
            for (device, anals) in perDevices do
                // 하나의 device 에 대한 byte offset 에 mapping 된 Tag names
                let bt = ResizeArray<int*string>()
                let bytes =
                    [
                        for a in anals do
                        let s = a.ByteOffset
                        let e = s - 1 + a.ByteLength
                        [s..e] |> iter (fun n -> bt.Add((n, a.Tag)))
                        yield! [s..e]
                    ] |> sort |> distinct

                yield! (scanBytes cpu device bytes)
                bts.Add(device, bt |> MultiMap.CreateFlat)
        ]
    (clusterScans, bts)




/// ULong (8byte) 에서 필요한 bit 를 읽어 내어서 ULong type 으로 반환한다.
/// start : 8byte 중에서 시작 bit
/// count : 읽을 bit 수
let getBitsFromUInt64 (number:uint64) startBit count =
    let mask =
        match count with
        | 1  -> 1UL
        | 8  -> 0xFFUL
        | 16 -> 0xFFFFUL
        | 32 -> 0xFFFF_FFFFUL
        | 64 -> 0xFFFF_FFFF_FFFFFFFFUL
        | _  -> failwith "ERROR"

    (number &&& (mask <<< startBit)) >>> startBit |> Convert.ToUInt64

// bitOffset : bytes[0] 의 0 bit 에서부터 센 offset
let getBitsFromBytes (bytes:byte[]) bitOffset bitCount =
    /// start byte offset
    let sb = bitOffset / 8
    let ulong =
        if sb < bytes.Length - 8 then
            bytes.[sb..sb+8]
        else
            bytes.[sb..bytes.Length-1] @ Array.zeroCreate<byte>(8)
        |> take 8
        |> fun bs -> BitConverter.ToUInt64(bs, 0)

    getBitsFromUInt64 ulong (bitOffset % 8) bitCount



/// ULong 8 byte 에서 주어진 bit 영역을 읽어서 ULong 으로 반환한다.
let readFromLWord ulValue (anal:LsFEnetTagInfo) =
    let bitLength = anal.BitLength
    let bitOffset = anal.BitOffset % 64
    getBitsFromUInt64 ulValue bitOffset bitLength




/// byteOffsets 의 list 를 long word(8 byte) offset 으로 변경.
/// 이때 각 byteOffset 값 마다, 해당 메모리를 참조하는 tag 들이 존재할 수 있다.
/// 가령 M[0] 메모리 영역은 %MX0, %MB0, %MW0 등이 참조할 수 있다.
/// 반환 값: [ longWordOffset * [여기를 참조하는 tag name]]
let private spitLongWordOffsets (bts:BackTrackings) (deviceType:DeviceType) (byteOffsets:int list) =
    let aa = // (int * string list) list : [offset * [tags]]
        byteOffsets
        |> map (fun b ->
            let lwOffset = b / 8
            let tags = bts.[deviceType].[b] |> toList
            (lwOffset, tags))

    let bb =
        aa
        |> groupBy fst  // [offset * [offset * [tags]]]
    let cc =
        bb
        |> List.map2nd (List.mapProject2nd id >> List.flatten >> sort >> distinct)
    cc

/// memory scan 결과를 Fragment 와 Block type 에 따라서 분류한다.
let private splitScans2FragmentsAndBlocks scans =
    let fragments = ResizeArray<FragmentType>()
    let blocks = ResizeArray<BlockType>()
    for sc in scans do
        match sc with
        | Fragment(f) -> fragments.Add(f)
        | Block(b) -> blocks.Add(b)
    (fragments, blocks)

/// device type 의 [s..e] 구간과 연관된 모든 tags 를 반환
/// s, e 는 memory 의 절대 byte offset 을 나타낸다.
let private getReferringTags (bts:BackTrackings) devType (s, e) =
    bts.[devType].Dictionary
    |> filter (fun (KeyValue(k, _)) -> s <= k && k <= e)
    |> Seq.bind (fun (KeyValue(k, v)) -> v)
    |> sort
    |> distinct
    |> toArray

let planReadTags (randomReader:TagsReader) (blockReader:ByteReader) cpu tags' =
    let txcxTags, tags =
        tags'
        |> Array.partition (fun (t:string) -> t.StartsWith("%TX") || t.StartsWith "%CX")

    /// Timer/Counter 의 Bit 만 따로 처리
    /// Timer/Counter 에서 Bit 지정은 접점값을 의미하고, Byte, word 값 지정은 현재값을 의미합니다.
    ///     * TW0 가 TX0 의 결과를 포함하지 않는다.  완전히 다른 값.
    ///         - TW0 는 Timer T0 의 word 현재값이고, TX0 는 Timer T0 의 ON/OFF 값이다.
    ///     * Scan/Clustering 시에 TX 나 CX 가 포함된 것이 있으면 완전히 따로 다루어야 한다.
    let bruteForceBitReaders =
        [|
            for chunks in txcxTags |> chunkBySize maxRandomReadTagCount do
                let reader:CachedTagsReader =
                    fun () ->
                        let result = randomReader (chunks)
                        result
                yield (chunks, reader)
        |]


    let scans, bts = clusterTags cpu tags
    let maxBlockReadByteCount = getMaxBlockReadByteCount cpu
    let (fragments, blocks) = splitScans2FragmentsAndBlocks scans

    // block type 우선 처리
    let blockReaders =
        [|
            for (devType, (s, e)) in blocks do
                let referringTags = getReferringTags bts devType (s, e)
                let referInfos =
                    [
                        for rtag in referringTags do
                            let anal = tryParseTag rtag |> Option.get
                            //  anal.BitOffset 은 device type 의 global 한 offset 이므로, 해당 block 을 반영해서 조정해야 한다.
                            let bitOffset = anal.BitOffset - (s * 8)
                            yield (rtag, bitOffset, anal.BitLength)
                    ]
                let reader:CachedTagsReader =
                    let startTag = sprintf "%%%AB%d" devType s
                    let count = e - s
                    assert(count < maxBlockReadByteCount)
                    fun () ->
                        try
                            let bytes = blockReader startTag count

                            [|
                                for (rtag, bitOffset, bitLength) in referInfos do
                                    let value = getBitsFromBytes bytes bitOffset bitLength
                                    yield (rtag, value)
                            |]
                        with exn ->
                            failwithlogf "Fatal exception on block reader"
                yield (referringTags, reader)
        |]


    // fragment type 처리
    // memory stream 을 Long type 8 바이트의 최대 16 접점씩 읽어 낼 수 있도록 처리
    // e.g fragments = [(M, [0; 3; 6;]); (P, [8;]);] (M, [1500;]); 이면
    // "%ML0"; "%P1"; "%ML187"; 을 생성해내야 한다. // 187 * 8 = 1496
    let lwTagsAndTags  =
        [
            for deviceType, byteOffsets in fragments do
            for lw, tags in spitLongWordOffsets bts deviceType byteOffsets do
                (sprintf "%%%AL%d" deviceType lw, tags)
        ]

    let lwTags = lwTagsAndTags |> map fst
    let dic = lwTagsAndTags |> Tuple.toDictionary
    let tags = lwTagsAndTags |> map snd

    // 분석 과정 제외하고 값을 얻는 부분만 funtion 화 해서 효율적 재수행
    let randomReaders =
        [|
            for channelTags in lwTags |> chunkBySize maxRandomReadTagCount do
                let a = 1
                let reader:CachedTagsReader =
                    fun () ->
                        try
                            let result = randomReader (channelTags |> toArray)
                            [|
                                for tag, ulValue in result do
                                    // referring tag 들의 value 값을 읽어서 결과 생성
                                    let referringTags = dic.[tag]
                                    for rtag in referringTags do
                                        let anal = tryParseTag rtag |> Option.get
                                        yield (rtag, readFromLWord ulValue anal)
                            |]
                        with exn ->
                            failwithlogf "Fatal exception on random reader:\n%O" exn
                yield (channelTags |> toArray, reader)
        |]

    bruteForceBitReaders @ blockReaders @ randomReaders

