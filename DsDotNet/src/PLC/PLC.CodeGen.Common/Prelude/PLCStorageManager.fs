namespace PLC.CodeGen.Common

open Dual.Common.Core.FS
open Engine.Core
open System.Collections
open System

(* IEC-61131-3.pdf, pp37.  Table 15 - Location and size prefix features for directly represented variable
----------------------------------------------------------------------------------------------
No. | Prefix  | Meaning                         | Default data type
----------------------------------------------------------------------------------------------
1   | I       | Input location                  |
2   | Q       | Output location                 |
3   | M       | Memory location                 |
4   | X       | Single bit size                 | BOOL
5   | None    | Single bit size                 | BOOL
6   | B       | Byte (8 bits) size              | BYTE
7   | W       | Word (16 bits) size             | WORD
8   | D       | Double word (32 bits) size      | DWORD
9   | L       | Long (quad) word (64 bits) size | LWORD
----------------------------------------------------------------------------------------------
10 Use of an asterisk (*) to indicate a not yet specified location (NOTE 2)
----------------------------------------------------------------------------------------------
NOTE 1 National standards organizations can publish tables of translations of these prefixes.
NOTE 2 Use of feature 10 in this table requires feature 11 of table 49 and vice ver
----------------------------------------------------------------------------------------------


EXAMPLES
%QX75 and %Q75  | Output bit 75
%IW215          | Input word location 215
%QB7            | Output byte location 7
%MD48           | Double word at memory location 48
%IW2.5.7.1      | See explanation below
%Q*             | Output at a not yet specified locat




----------------------------------------------------------------------------------------------
Keyword         | Variable usage
----------------+-----------------------------------------------------------------------------
VAR             | Internal to organization unit
VAR_INPUT       | Externally supplied, not modifiable within organization unit
VAR_OUTPUT      | Supplied by organization unit to external entities
VAR_IN_OUT      | Supplied by external entities - can be modified within organization unit
VAR_EXTERNAL    | Supplied by configuration via VAR_GLOBAL (2.7.1)
                | Can be modified within organization unit
VAR_GLOBAL      | Global variable declaration (2.7.1)
VAR_ACCESS      | Access path declaration (2.7.1)
VAR_TEMP        | Temporary storage for variables in function blocks and programs (2.4.3)
VAR_CONFIG      | Instance-specific initialization and location assignment.
RETAIN          | Retentive variables (see preceding text)
NON_RETAIN      | Non-retentive variables (see preceding text)
CONSTANT        | Constant (variable cannot be modified)
AT              | Location assignment (2.4.3.1)
----------------+-----------------------------------------------------------------------------
NOTE 1  The usage of these keywords is a feature of the program organization unit or
        configuration element in which they are used. Normative requirements for the use
        of these keywords are given in 2.4.3.1, 2.4.3.2, 2.5 and 2.7.
NOTE 2 Examples of the use of VAR_IN_OUT variables are given in figures 11b and 1



*)


(*
    M, R, W
    I, Q
    %IW0.1.2 : 0-th Base 의 1-th Slot 의 2-th WORD
*)
module NewIEC61131 =    // from Dual.Core.FS/Prelude/PLCStorageManager2.fs
    type Size =
        | X
        | B
        | W
        | D
        | L
        with
            member x.ToInteger() =
                match x with
                | X -> 1
                | B -> 8
                | W -> 16
                | D -> 32
                | L -> 64

            static member TryParse str =
                match str with
                | "X" -> Some Size.X
                | "B" -> Some Size.B
                | "W" -> Some Size.W
                | "D" -> Some Size.D
                | "L" -> Some Size.L
                | _ -> None
    type Memory =
        | I
        | Q
        | M
        | R
        | W
        with
            static member TryParse str =
                match str with
                | "I" -> Some Memory.I
                | "Q" -> Some Memory.Q
                | "M" -> Some Memory.M
                | "R" -> Some Memory.R
                | "W" -> Some Memory.W
                | _ -> None

    [<AbstractClass>]
    type TagIndices(memType, dataSize, e) =
        member x.MemoryType = memType
        /// one of X, B, W, D, L
        member x.Size:Size = dataSize

        member x.Element: int = e
        //member x.Slot = x.Element

        abstract member GetBitOffsets:unit -> int seq
        override x.GetHashCode() = hash (x.MemoryType, x.Size, x.Element)
        override x.Equals(y) =
            match y with
            | :? TagIndices as z -> x.MemoryType = z.MemoryType && x.Size = z.Size && x.Element = z.Element
            | _ -> false


    /// %IX0.0.1, %IW0.1.2
    type TagIndicesIQ(memType, dataSize, f, e, nth) =
        inherit TagIndices(memType, dataSize, e)
        member x.File:int = f
        member x.Nth:int = nth

        //member x.Base = x.File

        /// 주어진 slot 내에서의 offset
        override x.GetBitOffsets() =
            let sz = dataSize.ToInteger()
            let start = nth * sz
            {start..start+sz-1}


        override x.GetHashCode() = hash (x.File, x.Nth, base.GetHashCode())
        override x.Equals(y) =
            match y with
            | :? TagIndicesIQ as z -> x.File = z.File && x.Nth = z.Nth && base.Equals(y)
            | _ -> false

    /// M, R, W.  %MW10  %MW10.1
    /// MX 는 제외
    type TagIndicesMem(memType, dataSize, e) =
        inherit TagIndices(memType, dataSize, e)
        override x.GetBitOffsets() =
            let sz = dataSize.ToInteger()
            let start = e * sz
            {start..start+sz-1}


    type TagIndicesMemBit(memType, b) =
        inherit TagIndicesMem(memType, Size.X, 0)
        member x.Bit:int = b
        override x.GetBitOffsets() = seq {b}

        new (memType, dataSize:Size, e, b) =
            let sz = dataSize.ToInteger()
            let bit = sz * e + b
            TagIndicesMemBit(memType, bit)

        override x.GetHashCode() = hash (x.Bit, base.GetHashCode())
        override x.Equals(y) =
            match y with
            | :? TagIndicesMemBit as z -> x.Bit = z.Bit && base.Equals(y)
            | _ -> false




    let (|Int32Pattern|_|) (str: string) =
        match System.Int32.TryParse(str) with
        | true, v -> Some(v)
        | _ -> None

    let (|DataSizePattern|_|) (str: string) = Size.TryParse str
    let (|MemTypePattern|_|) (str: string) = Memory.TryParse str

    [<Obsolete("성능 개선 필요")>]
    /// Tag t 에 대한 index 정보를 추출한다.
    let getTagIndices t =
        printfn "Parsing %s" t
        match t with
        // I/Q 에 대한 full spec 조건: %IB0.1.2
        | RegexPattern @"%([IQ])([XBWDL]?)(\d+).(\d+).(\d+)$" [MemTypePattern mem; strSize; Int32Pattern f; Int32Pattern e; Int32Pattern nth] ->
            let sz = Size.TryParse strSize |? Size.X
            TagIndicesIQ(mem, sz, f, e, nth) :> TagIndices

        // I/Q 에 대한 생략 조건 : %IW2.3 ==> 무조건 bit 지정 address 임
        | RegexPattern @"%([IQ])([BWDL])(\d+).(\d+)$" [MemTypePattern mem; DataSizePattern size; Int32Pattern e; Int32Pattern nth] ->
            let offset = size.ToInteger() * e + nth
            TagIndicesIQ(mem, Size.X, 0, 0, offset) :> TagIndices

        | RegexPattern @"%([IQ])([XBWDL]?)(\d+)$" [MemTypePattern mem; strSize; Int32Pattern nth] ->
            let sz = Size.TryParse strSize |? Size.X
            let f, e = 0, 0
            TagIndicesIQ(mem, sz, f, e, nth) :> TagIndices

        | RegexPattern @"%([MRW])X(\d+)$" [MemTypePattern mem; Int32Pattern e; ] ->
            TagIndicesMemBit(mem, e) :> TagIndices

        | RegexPattern @"%([MRW])([BWDL])(\d+)$" [MemTypePattern mem; DataSizePattern size; Int32Pattern e; ] ->
            TagIndicesMem(mem, size, e) :> TagIndices

        | RegexPattern @"%([MRW])([BWDL])(\d+).(\d+)$" [MemTypePattern mem; DataSizePattern size; Int32Pattern e; Int32Pattern b] ->
            TagIndicesMemBit(mem, size, e, b) :> TagIndices

        | _ ->
            failwithlog "ERROR"


    /// PLC memory 설정 base class
    [<AbstractClass>]
    type MemoryConfigBase(memType, bitLength:int) =
        let ba = BitArray(bitLength)
        /// 현재 설정의 memory type
        member x.MemoryType:Memory = memType
        /// 현재 설정의 bit 환산 길이
        member x.BitLength:int = bitLength

        /// 할당된 bit 만 1의 값을 가짐
        member x.BitUsage = ba

        abstract member AllocateTag: Memory * Size -> string option
        member x.AllocateTag(dataSize) = x.AllocateTag(x.MemoryType, dataSize)

        /// 현재의 memory 구성에서 dataSize 를 고려해서 free 영역을 할당할 수 있으면, 해당 영역의 첫 bit 의 offset 을 반환
        member x.FindEmptyBitSlot (dataSize:Size) =
            let sz = dataSize.ToInteger()

            let rec loop s e =
                if s > e then
                    None
                else
                    /// 시작 위치 s 에서 data size sz 만큼 사용되지 않은 영역을 검색
                    let found = [s..s+sz-1] |> List.forall(fun c -> not x.BitUsage.[c])
                    if found then Some s
                    else
                        let newS = s+sz
                        loop newS e

            let e = x.BitUsage.Length-sz
            loop 0 e

    /// M 영역 memory 설정
    type MMemoryConfig(memType, bitLength) =
        inherit MemoryConfigBase(memType, bitLength)
        override x.AllocateTag (_memType:Memory, dataSize:Size) =
            option {
                let! startBit = x.FindEmptyBitSlot(dataSize)
                // mark allocated
                let e = startBit+dataSize.ToInteger()-1
                [startBit..e]
                |> iter(fun n -> x.BitUsage.[n] <- true)

                // 원하는 dataSize 의 형태로 tag 생성해서 반환.  M 영역은 simple
                return
                    match dataSize with
                    //| Size.X -> sprintf "%%MB%d.%d" (startBit/8) (startBit%8)     // Byte 선지정 후, byte 내의 offset 으로 표기하는 방법
                    | Size.X -> sprintf "%%RX%d" startBit
                    | Size.B -> sprintf "%%MB%d" (startBit/8)
                    | Size.W -> sprintf "%%MW%d" (startBit/16)
                    | Size.D -> sprintf "%%MD%d" (startBit/32)
                    | Size.L -> sprintf "%%ML%d" (startBit/64)
            }

    /// IQ memory 설정.  3 level 접근 가능한 memory 영역
    type IQMemoryConfig(memType, file, element, bitLength) =
        inherit MemoryConfigBase(memType, bitLength)
        /// File. 국번.  Base
        member x.File   :int = file
        /// Element. Slot
        member x.Element:int = element
        override x.AllocateTag (_memType:Memory, dataSize:Size) =
            option {
                let! startBit = x.FindEmptyBitSlot(dataSize)
                // mark allocated
                let e = startBit+dataSize.ToInteger()-1
                [startBit..e]
                |> iter(fun n -> x.BitUsage.[n] <- true)

                // 원하는 dataSize 의 형태로 tag 생성해서 반환
                return
                    match dataSize with
                    | Size.X -> sprintf "%%%AX%d.%d.%d" x.MemoryType x.File x.Element startBit
                    | Size.B -> sprintf "%%%AB%d.%d.%d" x.MemoryType x.File x.Element (startBit/8)
                    | Size.W -> sprintf "%%%AW%d.%d.%d" x.MemoryType x.File x.Element (startBit/16)
                    | Size.D -> sprintf "%%%AD%d.%d.%d" x.MemoryType x.File x.Element (startBit/32)
                    | Size.L -> sprintf "%%%AL%d.%d.%d" x.MemoryType x.File x.Element (startBit/64)
            }


    /// PLC memory 관리자.  I/Q/M/R/W 영역의 메모리를 관리한다.
    type MemoryManager(memConfigs:MemoryConfigBase seq) =
        /// PLC H/W memory 구성
        let memoryConfigs =
            // M, R, W memory.  증설 없음
            let mrw =
                memConfigs
                |> Seq.ofType<MMemoryConfig>
                |> Seq.groupBy(fun c -> c.MemoryType)
                |> Seq.map2nd(fun cfgs -> cfgs |> Seq.cast<MemoryConfigBase> |> List.ofSeq)

            // I, O memories (base, slot).  동일 memory type 에 대해서 복수개의 base/slot 이 존재할 수 있다.  (증설)
            let ios =
                memConfigs
                |> Seq.ofType<IQMemoryConfig>
                |> Seq.groupBy(fun c -> c.MemoryType)
                |> Seq.map2nd(fun cfgs -> cfgs |> Seq.sortBy(fun c -> c.File, c.Element) |> Seq.cast<MemoryConfigBase> |> List.ofSeq)

            let cfgs =
                (mrw @ ios)
                |> Tuple.toDictionary
            cfgs

        member x.MemoryConfigs = memoryConfigs
        member x.MemoryIQConfigs =
            memoryConfigs.[I] @ memoryConfigs.[Q]

        /// 선할당된 영역을 피해서 주어진 크기의 memory type tag 주소를 반환한다.
        /// 새로 할당된 영역도 marking 으로 표시한다.
        member x.AllocateTag(memType:Memory, size:Size) =
            let cfgs = x.MemoryConfigs.[memType]
            cfgs
            |> Seq.choose(fun c ->
                c.AllocateTag(memType, size))
            |> Seq.tryHead

        /// 선정의된 tag 가 사용하는 memory 영역을 marking 한다.
        member x.MarkAllocated(tag) =
            let ti = getTagIndices tag
            let cfgs = memoryConfigs.[ti.MemoryType]
            let offsets = ti.GetBitOffsets()

            match ti with
            | :? TagIndicesIQ as io ->
                // I/Q memory 의 경우, 해당 slot 을 넘어가는 범위를 지정하게 되면, 자동으로 다음 slot 으로 할당해야 한다.
                let findConfig (cfgs:IQMemoryConfig list) f e offset  =
                    // 주어진 base,slot 에 해당하는 메모리 conf 를 먼저 찾음
                    let (i, cfg) = cfgs |> indexed |> List.find(fun (_i, c) -> c.File = f && c.Element = e)
                    if cfg.BitLength > offset then
                        // 해당 conf 의 영역 내에 있으면 OK
                        Some (cfg, 0)
                    else
                        // 해당 conf 의 영역 바깥이면, offset 을 만족하는 최초의 다음 conf 의 index 를 찾음
                        let cfgIndex =
                            cfgs.[i+1..]
                            |> map (fun c -> c.BitLength)
                            |> List.scan (+) cfgs.[i].BitLength
                            |> List.tryFindIndex (fun n -> n > offset)

                        match cfgIndex with
                        | Some ci ->
                            /// 최종 conf 이전의 총 bit length
                            let presum = cfgs.[0..ci-1] |> List.sumBy (fun c -> c.BitLength)
                            Some (cfgs.[ci], presum)
                        | None ->
                            None


                let cfgs = cfgs |> List.ofType<IQMemoryConfig>
                offsets
                |> iter (fun o ->
                    match findConfig cfgs io.File io.Element o with
                    | Some(cfg, skips) ->
                        // (주어진 slot, base 를 넘어가는 경우, 다음 번) matching 되는 conf 의 해당 bit 에 이미 할당으로 marking
                        cfg.BitUsage.[o-skips] <- true
                    | None ->
                        failwithlogf "Failed to mark tag %s" tag )

            | :? TagIndicesMem ->
                let cfg = cfgs |> List.head
                offsets |> Seq.iter (fun o -> cfg.BitUsage.[o] <- true)
            | _ ->
                failwithlogf "Internal Error.  Failed to mark tag %s" tag


/// PLC 주소 영역 중복 사용 관리를 위한 module
// IEC-61131 Addressing
// http://www.microshadow.com/ladderdip/html/basic_iec_addressing.htm
// https://deltamotion.com/support/webhelp/rmctools/Registers/Address_Formats/Addressing_IEC.htm
// https://d1.amobbs.com/bbs_upload782111/files_31/ourdev_569653.pdf
(*
% [Q | I ] [X] [file] . [element] . [bit]
% [Q | I ] [B|W|D] [file] . [element]
% MX [file] . [element] . [bit]
% M [B|W|D] [file] . [element]

pp.37 of https://d1.amobbs.com/bbs_upload782111/files_31/ourdev_569653.pdf
%QX75 and %Q75      Output bit 75
%IW215              Input word location 215
%QB7                Output byte location 7
%MD48               Double word at memory location 48
%IW2.5.7.1          See explanation below
%Q*                 Output at a not yet specified location

*)
[<AutoOpen>]
module IEC61131 =   // from Dual.Core.FS/Prelude/PLCStorageManager.fs
    /// signed byte type enumerations
    type MemoryAllocationStatus =
        /// 초기화로 인해서 아직 의미가 결정안된 영역
        | Undefined = -2y
        /// 사용할 수 없는 영역 지정.  H/W 적으로 장착안된 slot 등이 Forbidden 값을 가짐
        | Forbidden = -1y
        /// 이미 메모리가 할당된 영역
        | Allocated = 1y
        /// 사용가능한 영역
        | Free = 0y
    /// Shortcut name for MemoryAllocationStatus
    and internal MAS = MemoryAllocationStatus

    type Size =
        | Bit = 1
        | Byte = 8
        | Word = 16
        | DWord = 32
        | LWord = 64
    /// Shortcut name for Size
    and private SS = Size

    module StorageSizeM =
        let toString (x:Size) =
            match x with
            | Size.Bit   -> K.X
            | Size.Byte  -> K.B
            | Size.Word  -> K.W
            | Size.DWord -> K.D
            | Size.LWord -> K.L
            | _ -> failwithlog "Unknown case of Size"

    /// PLC 메모리 종류
    type StorageType =
        /// Input
        | I
        /// Output
        | Q
        /// Memory
        | M

    module StorageTypeM =
        let toString x =
            match x with
            | I -> K.I
            | Q -> K.Q
            | M -> K.M

    type StorageConstants =
        | MaxMBits = 262144
        | MaxIQLevel2 = 32
        | MaxIQLevel1 = 16
        | MaxIQBits = 64

    /// 주소 영역의 범위를 표현.
    /// e.g %IX{d2}.{d1}.{d0} 표현에서 하나의 d 를 표현.
    /// - max : d 가 가질 수 있는 최대값.
    /// - availableSlots : [0..max - 1] 까지의 slot 중에서 H/W 적으로 사용가능한 영역
    type StorageRange(max, availableSlots:int seq) =
        member val Max = max with get, set
        member val AvailableSlots = availableSlots |> Array.ofSeq
    and private SR = StorageRange

    /// PLC 의 storage type (I/Q/M) 이 정해 졌을때 size 단위(X/B/W/D)로 접근하기 위한 interface.
    /// e.g Input(I) IPLCStorage 가 주어지면, 이에 대해 IX/IB/IW/ID 의 단위별 접근 하기 위한 개개의 interface 를 표현
    type IPLCSizedStorageSection =
        /// SubStorage 를 flat array 로 표현한 것
        abstract member Array: MAS array with get
        abstract member StorageType: StorageType with get
        abstract member Size: Size with get
        /// 메모리 영역(e.g "IX")을 Undefined 로 marking
        abstract member Initialize: unit -> unit
        /// 메모리 타입에 따른 주소 prefix 반환.  e.g "IX" (or, "IB", ... "QX", "QB", "QW", ...)
        abstract member AddressPrefix: string with get
        /// Parent(size agnostic 메모리 영역) 반환.  e.g IX --> I 관리자
        abstract member Parent:IPLCStorageSection with get
        abstract member GetAddressFromFlatIndex: int -> string
        abstract member GetCrossIndex: Size*int -> int option

    /// PLC 의 storage type.  I/Q/M 에 대해서 하나가 생긴다.
    and IPLCStorageSection =
        /// 메모리 size 를 고려한 children 집합.  e.g this 가 I 라면 IX/IB/IW/ID 가 children 집합이 된다.
        abstract member Children: IPLCSizedStorageSection seq with get
        /// 메모리 size 를 고려하지 않은 check.  e.g "IX" 와 "IW" 가 공통으로 사용된 영역이 존재하는지 check
        abstract member SizeAgnosticCrossCheck: unit -> (string * string) seq // TODO // (ITag*ITag) seq

    //let sizeAgnosticCrossCheck = ForwardDecl.declare<IPLCSizedStorageSection -> IPLCSizedStorageSection -> (string * string) seq>  // TODO // (ITag*ITag) seq
    let sizeAgnosticCrossCheck (ss1:IPLCSizedStorageSection) (ss2:IPLCSizedStorageSection) =
        // M은 M끼리, I는 I끼리, Q는 Q끼리 비교해야 하므로 ss type 까지 서로 같아야 함
        assert(ss1.GetType() = ss2.GetType())
        assert(ss1.StorageType = ss2.StorageType)
        assert(ss1.Size <> ss2.Size)

        let min, max =
            if ss1.Size > ss2.Size
            then ss2, ss1
            else ss1, ss2
        assert(max.Array |> Seq.forall (fun v -> v = MAS.Forbidden || v = MAS.Free || v = MAS.Allocated) )


        /// min 쪽에서 사용된 index 들
        let minUsedIndices =
            min.Array
            |> Seq.indexed
            |> Seq.filter (snd >> ((=) MAS.Allocated))
            |> Seq.map fst

        let ratio = (int max.Size) / (int min.Size)
        /// min 쪽에서 사용된 index 를 max 쪽으로 환산한 index 들
        let convertedMaxUsedIndices =
            minUsedIndices
            |> Seq.map (fun i -> i / ratio)
            |> Seq.distinct
            |> Array.ofSeq

        /// 두 군데 모두에서 모두 사용되는 index
        let conflictedMaxIndices =
            max.Array
                |> Seq.indexed
                |> Seq.filter (fun (i, v) ->
                    convertedMaxUsedIndices |> Array.contains(i) && v = MAS.Allocated)
                |> Seq.map fst
                |> Array.ofSeq

        seq {
            for i in conflictedMaxIndices do
                let maxAddr = max.GetAddressFromFlatIndex(i)
                let minAddrs =
                    minUsedIndices
                    |> Seq.filter(fun i -> conflictedMaxIndices |> Array.contains(i / ratio))
                let minAddr = min.GetAddressFromFlatIndex(minAddrs |> Seq.head)
                yield minAddr, maxAddr
        }
        |> Seq.eagerOnDebug

    let private setValueHelper (manager:IPLCSizedStorageSection) v (s, e) =
        let arr = manager.Array
        for i in s..e do
            if v <> MAS.Undefined then
                let addr = lazy (manager.GetAddressFromFlatIndex(i))
                match arr.[i] with
                /// 초기화가 아니면서 금지된 영역을 접근하는 경우
                | MAS.Forbidden ->
                    failwithlogf $"Invalid operation: 금지된 PLC 메모리 영역({addr.Force()})을 사용하려 했습니다."
                | MAS.Allocated ->
                    failwithlogf $"Invalid operation: 이미 할당된 PLC 메모리 영역({addr.Force()})을 재사용하려 했습니다."
                | _ ->
                    ()
            arr.[i] <- v


    /// 3 Level PLC address 관리자.  e.g {I/Q}{X/B/W/D}
    /// %IX{lv2}.{lv1}.{lv0}
    type PLCSubStorage3(parent:PLCStorage3, typ, size, range2:SR, range1:SR, lv0) as this =
        /// 최상위 level(slot) 의 최대값
        let lv2 = range2.Max
        /// 차상위 level 의 최대값
        let lv1 = range1.Max
        /// Level2 와 leve1 이 주어졌을때, array 의 영역(시작과 끝) 반환
        let getLevelRange1 n2 n1 =
            let s = (n2 * lv1 * lv0) + (n1 * lv0);
            let e = s + lv0 - 1
            s, e
        /// Level2 만 주어졌을때, array 의 영역(시작과 끝) 반환
        let getLevelRange2 n2 =
            let s = n2 * lv1 * lv0
            let e = s + lv1 * lv0 - 1
            s, e

        /// 3개의 level 이 주어졌을때, array 의 index 반환
        let getIndex(n2, n1, n0) = (n2 * lv1 * lv0) + (n1 * lv0) + n0

        /// 메모리 사용 여부를 표시하기 위한 실제의 array
        let arr = Array.create (lv2 * lv1 * lv0) MAS.Undefined

        /// array 의 start 와 end 영역을 v 값으로 marking
        let setValue v (s, e) = setValueHelper this v (s, e)
        /// array 의 start 와 end 영역의 값을 반환
        let getValue (s, e) = arr.[s..e]


        do
            // 3 level 은 I 나 Q 에 국한됨.  memory (M) 타입은 1 level
            assert(typ = I || typ = Q)
            // 가용한 slot 영역을 free 로 marking
            for r2 in range2.AvailableSlots do
                for r1 in range1.AvailableSlots do
                    getLevelRange1 r2 r1 |> setValue MAS.Free
            // 가용 영역이 아닌 곳을 금지 영역으로 marking
            for i in 0..arr.Length-1 do
                if arr.[i] = MAS.Undefined then
                    arr.[i] <- MAS.Forbidden

        /// SubStorage 를 flat array 로 표현한 것
        member x.Array = arr
        member x.StorageType = typ
        member x.Size = size
        /// 메모리 영역(e.g "IX")을 Undefined 로 marking
        member x.Initialize() = setValue MAS.Undefined (0, arr.Length)
        /// 메모리 타입에 따른 주소 prefix 반환.  e.g "IX" (or, "IB", ... "QX", "QB", "QW", ...)
        member x.AddressPrefix with get() = StorageTypeM.toString typ + StorageSizeM.toString size
        member x.Item
            with get(n2, n1, n0)    = arr.[getIndex(n2, n1, n0)]
            and set(n2, n1, n0) v   =
                let i = getIndex(n2, n1, n0)
                setValue v (i,i)

        member x.Item
            with get(n2, n1)    = getLevelRange1 n2 n1 |> getValue
            and set(n2, n1) v   = getLevelRange1 n2 n1 |> setValue v

        member x.Item
            with get(n2)    = getLevelRange2 n2 |> getValue
            and set(n2) v   = getLevelRange2 n2 |> setValue v

        /// component n2, n1, n0 의 index 를 flat index 로 환산해서 반환
        member x.GetFlatIndex(n2, n1, n0) = getIndex(n2, n1, n0)
        /// flat index 를 component n2, n1, n0 의 index 로 환산해서 반환
        member x.GetComponentIndices(flatIndex) =
            let n = flatIndex
            let n0 = n % lv0
            let n1 = (n / lv0) % lv1
            let n2 = (n / (lv0 * lv1))

            n2, n1, n0

        /// this type 을 target type 의 메모리 타입으로 바꾸었을 때의 index 반환.
        member x.GetCrossIndex(targetSize, sourceFlatIndex) = parent.GetCrossIndex(size, targetSize, sourceFlatIndex)

        member x.GetAddressFromFlatIndex(n) =
            let n2, n1, n0 = x.GetComponentIndices(n)
            match size with
            | Size.Bit -> sprintf "%%%s%d.%d.%d" x.AddressPrefix n2 n1 n0
            | _ -> sprintf "%%%s%d.%d" x.AddressPrefix n2 n1

        interface IPLCSizedStorageSection with
            member x.StorageType with get() = x.StorageType
            member x.Size with get() = x.Size
            member x.Array with get() = x.Array
            member x.Initialize() = x.Initialize()
            member x.AddressPrefix with get() = x.AddressPrefix
            member x.Parent with get() = parent :> IPLCStorageSection
            member x.GetAddressFromFlatIndex(n) = x.GetAddressFromFlatIndex(n)
            member x.GetCrossIndex(targetSize, sourceFlatIndex) = x.GetCrossIndex(targetSize, sourceFlatIndex)

    /// %IX{lv2}.{lv1}.{lv0} 과 같이 3개의 level 로 나뉘어지는 PLC storage 정보
    and PLCStorage3(typ, range2:StorageRange, range1:StorageRange, ?lvl0) as this =
        let lvl0 = lvl0 |? 64
        let xx =  PLCSubStorage3(this, typ, SS.Bit,   range2, range1, lvl0)
        let xb =  PLCSubStorage3(this, typ, SS.Byte,  range2, range1, lvl0/8)
        let xw =  PLCSubStorage3(this, typ, SS.Word,  range2, range1, lvl0/16)
        let xdw = PLCSubStorage3(this, typ, SS.DWord, range2, range1, lvl0/32)
        let xlw = PLCSubStorage3(this, typ, SS.LWord, range2, range1, lvl0/64)
        let children = [| xx; xb; xw; xdw; xlw |]
        let dic = children |> Seq.map (fun sp -> sp.Size, sp) |> dict
        member x.Item with get(sz) = dic.[sz]

        /// 메모리 타입을 바꾸었을 때의 index 반환.
        /// e.g IX 로 볼때의 sourceFlatIndex 값을 IW 로 볼 때 index 값은 얼마인가?
        /// - size 가 작은 type 에서 큰 type (e.g IX -> IB) 으로 index 환산은 의미가 있어서 Some 값을 반환한다.
        ///     작은 type 이 포함된 큰 type 에서의 시작 index
        /// - size 가 큰 type 을 작은 type 으로 반환은 의미가 없으므로 None 값을 반환한다.
        member x.GetCrossIndex(sourceSize, targetSize, sourceFlatIndex) =
            if sourceSize = targetSize then
                Some sourceFlatIndex
            else
                let src = dic.[sourceSize]
                let tgt = dic.[targetSize]
                let n2, n1, n0 = src.GetComponentIndices(sourceFlatIndex)
                match sourceSize, targetSize with
                | SS.Bit, _ ->
                    Some <| tgt.GetFlatIndex(n2, n1, n0 / (int targetSize))
                | SS.Byte, SS.Bit
                | SS.Word, SS.Bit
                | SS.Word, SS.Byte
                | SS.DWord, _ ->
                    None
                | SS.Byte, _-> Some <| tgt.GetFlatIndex(n2, n1, n0 / (int targetSize / 8))
                | SS.Word, SS.DWord -> Some <| tgt.GetFlatIndex(n2, n1, n0 / 2)
                | _ ->
                    failwithlog "Unexpected case."
        member x.Children with get() = children |> Seq.cast<IPLCSizedStorageSection>

        /// 메모리 size 를 고려하지 않은 check.  e.g "IX" 와 "IW" 가 공통으로 사용된 영역이 존재하는지 check
        member x.SizeAgnosticCrossCheck() =
            seq {
                for combis in x.Children |> combinations 2 do
                    assert(combis.Length = 2)
                    let combis = combis |> List.sortBy (fun m -> m.Size)
                    let m0, m1 = combis.[0], combis.[1]
                    yield! sizeAgnosticCrossCheck m0 m1
            }

        interface IPLCStorageSection with
            member x.Children with get() = x.Children
            member x.SizeAgnosticCrossCheck() = x.SizeAgnosticCrossCheck()

    /// 1 Level PLC address format : M
    /// %MX{lv0}
    type PLCSubStorage1(parent:PLCStorage1, typ, size, lv0) as this =
        /// 메모리 사용 여부를 표시하기 위한 실제의 array
        let arr = Array.create lv0 MAS.Undefined
        /// array 의 start 와 end 영역을 v 값으로 marking
        let setValue v (s, e) = setValueHelper this v (s, e)

        member x.Item
            with get(n) = arr.[n]
            and set(n) v = setValue v (n,n)

        /// SubStorage 를 flat array 로 표현한 것
        member x.Array = arr
        member x.StorageType = typ
        member x.Size = size
        /// 메모리 영역(e.g "IX")을 Undefined 로 marking
        member x.Initialize() = setValue MAS.Undefined (0, arr.Length)
        /// 메모리 타입에 따른 주소 prefix 반환.  e.g "MX" (or, "MB", ...)
        member x.AddressPrefix with get() = StorageTypeM.toString typ + StorageSizeM.toString size
        member x.GetAddressFromFlatIndex(n) = sprintf "%%%s%d" x.AddressPrefix n
        /// this type 을 target type 의 메모리 타입으로 바꾸었을 때의 index 반환.
        member x.GetCrossIndex(targetSize, sourceFlatIndex) = parent.GetCrossIndex(size, targetSize, sourceFlatIndex)

        interface IPLCSizedStorageSection with
            member x.StorageType with get() = x.StorageType
            member x.Size with get() = x.Size
            member x.Array with get() = x.Array
            member x.Initialize() = x.Initialize()
            member x.AddressPrefix with get() = x.AddressPrefix
            member x.Parent with get() = parent :> IPLCStorageSection
            member x.GetAddressFromFlatIndex(n) = x.GetAddressFromFlatIndex(n)
            member x.GetCrossIndex(targetSize, sourceFlatIndex) = x.GetCrossIndex(targetSize, sourceFlatIndex)

    /// 1 level PLC 주소 영역 관리.  현재는 M type 만 해당됨.  // TODO :  추후 timer, ... 등이 추가될 걸로 예상됨
    and PLCStorage1(m, bitSize) as this =
        let mx =   PLCSubStorage1(this, m, SS.Bit, bitSize)
        let mb =   PLCSubStorage1(this, m, SS.Byte, bitSize/8)
        let mw =   PLCSubStorage1(this, m, SS.Word, bitSize/16)
        let mdw =  PLCSubStorage1(this, m, SS.DWord, bitSize/32)
        let mlw =  PLCSubStorage1(this, m, SS.DWord, bitSize/64)
        let children = [| mx; mb; mw; mdw; mlw |]

        member x.Children with get() = children |> Seq.cast<IPLCSizedStorageSection>

        /// 메모리 size 를 고려하지 않은 check.  e.g "MX" 와 "MW" 가 공통으로 사용된 영역이 존재하는지 check
        member x.SizeAgnosticCrossCheck() =
            seq {
                for combis in x.Children |> combinations 2 do
                    assert(combis.Length = 2)
                    yield! sizeAgnosticCrossCheck combis.[0] combis.[1]
            }

        /// 메모리 타입을 바꾸었을 때의 index 반환.
        /// e.g MX 로 볼 때의 sourceFlatIndex 값을 MW 로 볼 때 index 값은 얼마인가?
        /// - size 가 작은 type 에서 큰 type (e.g MX -> MB) 으로 index 환산은 의미가 있어서 Some 값을 반환한다.
        ///     작은 type 이 포함된 큰 type 에서의 시작 index
        /// - size 가 큰 type 을 작은 type 으로 반환은 의미가 없으므로 None 값을 반환한다.
        member x.GetCrossIndex(sourceSize, targetSize, sourceFlatIndex) =
            if sourceSize > targetSize then
                None
            elif sourceSize = targetSize then
                Some sourceFlatIndex
            else
                let ratio = (int targetSize) / (int sourceSize)
                Some <| sourceFlatIndex / ratio

        interface IPLCStorageSection with
            member x.Children with get() = x.Children
            member x.SizeAgnosticCrossCheck() = x.SizeAgnosticCrossCheck()

    /// PLC 주소 영역의 중복 사용을 관리하기 위한 매니저
    [<AllowNullLiteral>]
    type PLCStorageManager(specs:IPLCStorageSection seq) =
        /// 모든 type/size 별로 관리하는 sub managers
        let descendants = specs |> Seq.collect (fun sp -> sp.Children)

        /// (type, size) 별로 sub manager 를 검색하기위한 사전
        let dic =
            descendants
            |> Seq.map (fun sp -> (sp.StorageType, sp.Size), sp) |> dict

        /// AddressPrefix (type+size 의 문자열. e.g "IX") 별로 sub manager 를 검색하기위한 사전
        let dicStr =
            descendants
            |> Seq.map (fun sp -> sp.AddressPrefix, sp) |> dict

        member x.Sections = specs
        /// (type, size) 로 sub manager 검색
        member x.Item with get(tp, sz) = dic.[(tp, sz)]
        /// AddressPrefix (type+size 의 문자열. e.g "IX") 로 sub manager 검색
        member x.Item with get(prefix) = dicStr.[prefix]

        /// 주어진 tag 가 사용된 영역을 marking
        member x.RegisterTags (tags: ITag seq) = x.RegisterTags(tags |> map address)

        /// 주어진 tag 가 사용된 영역을 marking
        /// marking 도중 이미 사용된 영역을 marking 하려고 할 때, Exception 발생.    // TODO : Exception 말고, 일반화된 처리 필요
        [<Obsolete("성능 개선 필요")>]
        member x.RegisterTags (tags: string seq) =
            let allocated = MAS.Allocated
            for t in tags do
                match t with
                // I/Q 영역 주소
                | RegexPattern @"%([IQ])([XBWDL])" [iq; size;] ->
                    let storage3 = dicStr.[iq + size] :?> PLCSubStorage3
                    match t with
                    | RegexPattern @"%[IQ][XBWDL](\d+).(\d+).(\d+)$" [d2; d1; d0] ->
                        storage3.[int d2, int d1, int d0] <- allocated
                    | RegexPattern @"%[IQ][XBWDL](\d+).(\d+)$" [d1; d0] ->
                        storage3.[0, int d1, int d0] <- allocated
                    | RegexPattern @"%[IQ][XBWDL](\d+)$" [d0] ->
                        storage3.[0, 0, int d0] <- allocated
                    | _ ->
                        failwithlogf $"Unknown tag format : {t}"
                // M 영역 주소
                | RegexPattern @"%M([XBWDL])(\d+)$" [size; d0] ->
                    let storage1 = dicStr.["M" + size] :?> PLCSubStorage1
                    storage1.[int d0] <- allocated
                /// Tag 의 실제 주소 없으면 무시
                | "" -> ()
                | _ ->
                    failwithlogf "Unknown tag format : %s" t

        /// 모든 메모리 영역별로 가능한 최대치를 수용할 수 있는 manager 생성
        static member CreateFullBlown() =
            let lv2 = int StorageConstants.MaxIQLevel2  // 32
            let lv1 = int StorageConstants.MaxIQLevel1  // 16
            let mxMax = int StorageConstants.MaxMBits   // 262144

            let input  = PLCStorage3(I, SR(lv2, [0..lv2-1]), SR(lv1, [0..lv1-1])) :> IPLCStorageSection
            let output = PLCStorage3(Q, SR(lv2, [0..lv2-1]), SR(lv1, [0..lv1-1])) :> IPLCStorageSection
            let memory = PLCStorage1(M, mxMax) :> IPLCStorageSection
            let storages = [input; output; memory]
            PLCStorageManager(storages)



