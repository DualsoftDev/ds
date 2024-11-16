namespace PLC.HW

open Newtonsoft.Json
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.Runtime.CompilerServices
open System.ComponentModel
open Dual.Common.Base.CS
open PLC.CodeGen.Common
open Engine.Core

//type PLCType =
//    | Xgi
//    | Xgk

//type Platform =
//    | PLC of PLCType
//    | PC
//    static member ofString(str:string) =
//        match str with
//        | "PC" -> PC
//        | "XGI" -> PLC Xgi
//        | "XGK" -> PLC Xgk
//        | _ -> failwith "ERROR"
//    member x.Stringify() =
//        match x with
//        | PC -> "PC"
//        | PLC Xgi -> "XGI"
//        | PLC Xgk -> "XGK"
//    member x.TryGetPlcType() =
//        match x with
//        | PLC Xgi -> Some Xgi
//        | PLC Xgk -> Some Xgk
//        | _ -> None

/// LSE XGT 기종
[<AutoOpen>]
module rec XGT =
    let [<Literal>] MaxNumberBases = 8
    let [<Literal>] MaxNumberSlots = 12
    /// One of | 8 | 16 | 32 | 64
    type PlcIoSlotCapacity =
        | Point8
        | Point16
        | Point32
        | Point64
        member x.ToInt() =
            match x with
            | Point8  -> 8
            | Point16 -> 16
            | Point32 -> 32
            | Point64 -> 64

        member x.Stringify() = x.ToInt().ToString()
        static member ofString(str) =
            match str with
            | "8"  -> Point8
            | "16" -> Point16
            | "32" -> Point32
            | "64" -> Point64
            | _ -> failwith "ERROR"

    type internal SlotIndex = int

    /// PLC IO slot 하나.
    [<AllowNullLiteral>]
    type Slot(isEmpty:bool, isInput:bool, isDigital:bool, length:int) =
        new() = Slot(true, true, true, 16)
        member val IsEmpty = isEmpty with get, set
        member val IsInput = isInput with get, set
        member val IsDigital = isDigital with get, set
        /// Digital 접점 수
        member val Length = length with get, set

        // { UI 표출, debugging 용
        /// Slot 에 할당된 address 들.  UI 및 debugging 표시 용.   PlcHw.CreateIOHaystacks() 수행 중에 값 채움.
        [<Browsable(false)>]
        [<JsonIgnore>]
        member val Addresses: string[] = [||] with get, set
        member x.StartAddress = x.Addresses.TryHead() |? null
        member x.EndAddress = x.Addresses.TryLast() |? null
        [<JsonIgnore>]
        member val SlotNumber = -1 with get, set
        // } UI 표출, debugging 용


        member x.GetCapacity(isFixedSlotAllocation:bool) =
            if x.IsEmpty then
                0
            elif isFixedSlotAllocation then
                64
            elif not x.IsDigital then // 가변식 아날로그이거나
                16
            else
                max 16 x.Length

    [<AllowNullLiteral>]
    type Base(slots: Slot seq) =
        let slots = ResizeArray(slots)
        do
            assert(slots.Count <= MaxNumberSlots)

        new() = Base([])    // Serialize 를 위해서 default constructor 꼭 필요
        member val Slots = slots with get, set     // get, set for newtonsoft
        static member Create(isFillContents:bool) =
            Base([ for i in 0..MaxNumberSlots-1 -> if isFillContents then Slot() else null ])

        [<JsonIgnore>]
        [<DisplayName("총 슬롯수")>]
        member x.NumSlot
            with get() = x.Slots.Count
            //with get() = x.Slots.Filter(fun sl -> sl <> null && !! sl.IsEmpty) |> Seq.length
            and set(n) = x.Slots.Resize(n)
        [<JsonIgnore>]
        [<DisplayName("사용 슬롯수")>]
        member x.NumUsedSlot = x.Slots |> Seq.filter(fun sl -> sl <> null && !! sl.IsEmpty) |> Seq.length

        // { UI 표출, debugging 용
        [<JsonIgnore>]
        member val BaseNumber = -1 with get, set
        // } UI 표출, debugging 용

        member x.GetCapacity(isFixedSlotAllocation:bool) =
            if isFixedSlotAllocation then
                64 * 16
            else
                slots
                |> sumBy(fun sl ->
                    if sl = null then
                        0
                    else
                        sl.GetCapacity(isFixedSlotAllocation))

    type Base with
        member x.Compress() =
            x |> tee(fun x ->
                let slots = [| for sl in x.Slots -> (sl <> null && sl.IsEmpty) ?= (null, sl) |]
                x.Slots <- ResizeArray<Slot>(slots))



    /// PLC HW 구성.  Slots (+ CPU + Network ...)
    type PlcHw(plcType:PlatformTarget, isFixedSlotAllocation:bool) =
        let mutable isFixedSlotAllocation = isFixedSlotAllocation

        new() = PlcHw(XGI, true)    // Serialize 를 위해서 default constructor 꼭 필요
        member val PLCType: PlatformTarget = plcType with get, set
        member val Bases:Base[] = [||] with get, set

        member val StartFreeMWord = 1000 with get, set
        member val FreeMWordSize = 1000 with get, set

        /// Slot 고정 할당 여부 (I/O 슬롯 고정 점수 할당(64점))
        member x.IsFixedSlotAllocation
            with get() = isFixedSlotAllocation
            and set(v) =
                if not v && x.PLCType = XGI then
                    failwith "ERROR: XGI 나 WINDOWS 에서는 가변 slot 을 사용할 수 없습니다."
                isFixedSlotAllocation <- v

        /// PLC HW 생성: Serialization 이외의 방법으로 생성하는 유일한 생성자
        static member Create(plcType, isFixedSlotAllocation, isFillContents:bool) =
            PlcHw(plcType, isFixedSlotAllocation)
            |> tee(fun plc ->
                plc.Bases <- [| for i in 1..MaxNumberBases -> if isFillContents then Base.Create(isFillContents) else null |]
            )

        member x.NumTotalUsedSlot = x.Bases.OfNotNull() |> sumBy(_.NumUsedSlot)

    /// PLC IO slot 구성에 따라서 가용한 io bit 번호를 뱉는 함수.  더 이상 가용 bit 가 없으면 None 반환
    type IOAllocatorFunction = unit -> string option

    type PlcHw with
        member x.Compress() =
            x |> tee(fun x -> x.Bases <- [| for b in x.Bases -> (b <> null && b.NumUsedSlot = 0) ?= (null, b.Compress()) |])

        /// 현재의 HW 구성에서 사용가능한 입/출력 접점의 주소를 문자열로 각각 반환
        member x.CreateIOHaystacks(): string[] * string[] =
            let createAddress(isInput:bool, bse:int, slot:int, bitOffset:int, totalSlotOffset:int) =
                match x.PLCType, isInput with
                | XGI, true -> $"%%IX{bse}.{slot}.{bitOffset}"
                | XGI, false -> $"%%QX{bse}.{slot}.{bitOffset}"
                | XGK, _ ->
                    let w = totalSlotOffset / 16
                    let b = totalSlotOffset % 16
                    sprintf "P%04d%X" w b
                | _ -> failwith "ERROR"
            let xss, yss =
                [|
                    let mutable baseStart = 0
                    // 이번 slot 의 시작 bit 주소
                    for b, bbase in x.Bases.Indexed() do
                        if bbase <> null then
                            bbase.BaseNumber <- b
                            let mutable slotStart = baseStart
                            for s, slot in bbase.Slots.Indexed() do
                                if slot <> null then
                                    slot.SlotNumber <- s
                                    if slot.IsEmpty || not slot.IsDigital then
                                        slot.Addresses <- [||]
                                    else
                                        let cap= slot.Length
                                        let addresses = [| for i in 0..cap-1 -> createAddress(slot.IsInput, b, s, i, i+slotStart)|]
                                        slot.Addresses <- addresses

                                        if slot.IsInput then
                                            yield addresses, [||]
                                        else
                                            yield [||], addresses

                                    slotStart <- slotStart + slot.GetCapacity(x.IsFixedSlotAllocation)

                            baseStart <- baseStart + bbase.GetCapacity(x.IsFixedSlotAllocation)
                |] |> Array.unzip
            let xs = xss |> Array.concat
            let ys = yss |> Array.concat
            xs, ys


        /// 미리 할당된 주소 영역(입력: forbiddenXs, 출력:forbiddenYs) 를 제외하고,
        /// 입력 및 출력을 순차적으로 spit 하는 함수 두개를 반환
        member x.CreateIOAllocator(forbiddenXs:string seq, forbiddenYs:string seq) =
            let xs, ys = x.CreateIOHaystacks()

            let availableXs = xs |> Seq.except forbiddenXs
            let availableYs = ys |> Seq.except forbiddenYs
            let inputAllocator:IOAllocatorFunction  = Seq.tryEnumerate availableXs
            let outputAllocator:IOAllocatorFunction = Seq.tryEnumerate availableYs
            inputAllocator, outputAllocator

        [<Todo("메모리 allocator 구현")>]
        member x.CreateMAllocators(reservedBytes:int []) =
            let start, size = x.StartFreeMWord, x.FreeMWordSize
            let startByte, endByte =
                let startWord, endWord = start, start + size
                startWord * 2, endWord * 2
            let {
                BitAllocator  = x
                ByteAllocator = b
                WordAllocator = w
                DWordAllocator= d
                LWordAllocator= l
            } = MemoryAllocator.createMemoryAllocator "M" (startByte, endByte) reservedBytes x.PLCType
            x, b, w, d, l



// UI 조작의 OK, Cancel 에 대응하기 위해서 원래의 자료에 대한 사본에 대해서 작업하고 Cancel 시 사본 삭제하기 위함.
type XGTDupExtensionForCSharp =
    [<Extension>]
    static member private duplicate(slot:Slot) =
        if slot = null then
            Slot()
        else
            Slot(slot.IsEmpty, slot.IsInput, slot.IsDigital, slot.Length)

    [<Extension>] static member private duplicate(ioBase:Base) = Base(ioBase.Slots.Map(_.duplicate()))

    /// PLC HW 구성 복사본 생성.  존재하면 그 정보를 존중해서 복사하고, 없으면 fillter 로 채움
    /// 8 base * 12 slot 정보를 COM 객체에 모두 저장하기가 부담스러워, 편집시에만 모두 생성하고, 실제 저장시에는 불필요한 정보는 null 로 저장
    /// PlcHw.Compress 참고
    [<Extension>]
    static member DuplicateWithFill(plcHw:PlcHw) =
        PlcHw.Create(plcHw.PLCType, plcHw.IsFixedSlotAllocation, isFillContents=true)
        |> tee(fun y ->
            y.StartFreeMWord <- plcHw.StartFreeMWord
            y.FreeMWordSize <- plcHw.FreeMWordSize
            //y.Bases <- plcHw.Bases.Map(function | null -> null | b -> b.duplicate())
            y.Bases <- [|
                for b in plcHw.Bases do
                    match b with
                    | null -> Base.Create(isFillContents=true)
                    | b -> b.duplicate() |] )
