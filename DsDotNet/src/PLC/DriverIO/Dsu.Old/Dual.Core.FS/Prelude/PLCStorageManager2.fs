namespace Old.Dual.Core.Prelude

open Old.Dual.Common
open FSharpPlus
open System.Collections

(*
    M, R, W
    I, Q
    %IW0.1.2 : 0-th Base 의 1-th Slot 의 2-th WORD
*)
module NewIEC61131 =
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
            let a = mem, Size.X, e
            TagIndicesMemBit(mem, e) :> TagIndices

        | RegexPattern @"%([MRW])([BWDL])(\d+)$" [MemTypePattern mem; DataSizePattern size; Int32Pattern e; ] ->
            let a = mem, size, e        // 최종 = size
            TagIndicesMem(mem, size, e) :> TagIndices

        | RegexPattern @"%([MRW])([BWDL])(\d+).(\d+)$" [MemTypePattern mem; DataSizePattern size; Int32Pattern e; Int32Pattern b] ->
            let a = mem, size, e, b     // 최종 = bit
            TagIndicesMemBit(mem, size, e, b) :> TagIndices

        | _ ->
            failwith "ERROR"


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
        member x.FindEmptyBitSlot (memType:Memory, dataSize:Size) =
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
        override x.AllocateTag (memType:Memory, dataSize:Size) =
            monad {
                let! startBit = x.FindEmptyBitSlot(memType, dataSize)
                // mark allocated
                let e = startBit+dataSize.ToInteger()-1
                [startBit..e]
                |> iter(fun n -> x.BitUsage.[n] <- true)

                // 원하는 dataSize 의 형태로 tag 생성해서 반환.  M 영역은 simple
                match dataSize with
                //| Size.X -> sprintf "%%MB%d.%d" (startBit/8) (startBit%8)     // Byte 선지정 후, byte 내의 offset 으로 표기하는 방법
                | Size.X -> sprintf "%%%AX%d" memType startBit
                | Size.B -> sprintf "%%%AB%d" memType (startBit/8)
                | Size.W -> sprintf "%%%AW%d" memType (startBit/16)
                | Size.D -> sprintf "%%%AD%d" memType (startBit/32)
                | Size.L -> sprintf "%%%AL%d" memType (startBit/64)
            }

    /// IQ memory 설정.  3 level 접근 가능한 memory 영역
    type IQMemoryConfig(memType, file, element, bitLength) =
        inherit MemoryConfigBase(memType, bitLength)
        /// File. 국번.  Base
        member x.File   :int = file
        /// Element. Slot
        member x.Element:int = element
        override x.AllocateTag (memType:Memory, dataSize:Size) =
            let a = 1
            monad {
                let! startBit = x.FindEmptyBitSlot(memType, dataSize)
                // mark allocated
                let e = startBit+dataSize.ToInteger()-1
                [startBit..e]
                |> iter(fun n -> x.BitUsage.[n] <- true)

                // 원하는 dataSize 의 형태로 tag 생성해서 반환
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
                (mrw @@ ios)
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
                    let (i, cfg) = cfgs |> indexed |> List.find(fun (i, c) -> c.File = f && c.Element = e)
                    if cfg.BitLength > offset then
                        // 해당 conf 의 영역 내에 있으면 OK
                        Some (cfg, 0)
                    else
                        // 해당 conf 의 영역 바깥이면, offset 을 만족하는 최초의 다음 conf 의 index 를 찾음
                        let cfgIndex =
                            cfgs.[i+1..]
                            |> map (fun c -> c.BitLength)
                            |> scan (+) cfgs.[i].BitLength
                            |> tryFindIndex (fun n -> n > offset)

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

            | :? TagIndicesMem as mem ->
                let cfg = cfgs |> List.head
                offsets |> Seq.iter (fun o -> cfg.BitUsage.[o] <- true)
            | _ ->
                failwithlogf "Internal Error.  Failed to mark tag %s" tag
