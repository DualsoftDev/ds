namespace PLC.HW

open Newtonsoft.Json
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.Runtime.CompilerServices
open System.ComponentModel

type PLCType =
    | Xgi
    | Xgk

type Platform =
    | PLC of PLCType
    | PC
    static member ofString(str:string) =
        match str with
        | "PC" -> PC
        | "XGI" -> PLC Xgi
        | "XGK" -> PLC Xgk
        | _ -> failwith "ERROR"
    member x.Stringify() =
        match x with
        | PC -> "PC"
        | PLC Xgi -> "XGI"
        | PLC Xgk -> "XGK"
    member x.TryGetPlcType() =
        match x with
        | PLC Xgi -> Some Xgi
        | PLC Xgk -> Some Xgk
        | _ -> None

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
    type IoSlot(isEmpty:bool, isInput:bool, isDigital:bool, length:int) =
        new() = IoSlot(true, true, true, 16)
        member val IsEmpty = isEmpty with get, set
        member val IsInput = isInput with get, set
        member val IsDigital = isDigital with get, set
        /// Digital 접점 수
        member val Length = length with get, set
        member x.GetCapacity(isFixedSlotAllocation:bool) =
            if isFixedSlotAllocation then
                64
            elif x.IsEmpty || not x.IsDigital then // 가변식: 비어 있거나, 아날로그이거나
                16
            else
                max 16 x.Length

    type Base(slots: IoSlot seq) =
        let slots = ResizeArray(slots)
        do
            assert(slots.Count <= MaxNumberSlots)

        new() = Base([])
        member val Slots = slots with get, set     // get, set for newtonsoft
        static member Create() =
            Base([ for i in 0..MaxNumberSlots-1 -> IoSlot() ])

        [<JsonIgnore>]
        [<DisplayName("총 슬롯수")>]
        member x.NumSlot
            with get() = x.Slots.Count
            and set(n) = x.Slots.Resize(n)
        [<JsonIgnore>]
        [<DisplayName("사용 슬롯수")>]
        member x.NumUsedSlot = x.Slots |> Seq.filter(fun s -> not s.IsEmpty) |> Seq.length
        member x.GetCapacity(isFixedSlotAllocation:bool) =
            if isFixedSlotAllocation then
                64 * 16
            else
                slots |> sumBy(_.GetCapacity(isFixedSlotAllocation))



    /// PLC HW 구성.  Slots (+ CPU + Network ...)
    type PlcHw(plcType:PLCType, isFixedSlotAllocation:bool) =
        let mutable isFixedSlotAllocation = isFixedSlotAllocation

        new() = PlcHw(Xgi, true)
        member val PLCType: PLCType = plcType with get, set
        member val Bases:Base[] = [||] with get, set
        member x.IsFixedSlotAllocation
            with get() = isFixedSlotAllocation
            and set(v) =
                if not v && x.PLCType = Xgi then
                    failwith "ERROR: XGI 에서는 가변 slot 을 사용할 수 없습니다."
                isFixedSlotAllocation <- v

        /// PLC HW 생성: Serialization 이외의 방법으로 생성하는 유일한 생성자
        static member Create(plcType, isFixedSlotAllocation) =
            PlcHw(plcType, isFixedSlotAllocation)
            |> tee(fun plc ->
                plc.Bases <- [| for i in 1..MaxNumberBases -> Base.Create() |]
            )

    /// PLC IO slot 구성에 따라서 가용한 io bit 번호를 뱉는 함수.  더 이상 가용 bit 가 없으면 None 반환
    type IOAllocatorFunction = unit -> string option

    type PlcHw with
        member x.CreateIOHaystacks(): string[] * string[] =
            let createAddress(isInput:bool, bse:int, slot:int, bitOffset:int, totalSlotOffset:int) =
                match x.PLCType, isInput with
                | Xgi, true -> $"%%IX{bse}.{slot}.{bitOffset}"
                | Xgi, false -> $"%%QX{bse}.{slot}.{bitOffset}"
                | Xgk, _ ->
                    let w = totalSlotOffset / 16
                    let b = totalSlotOffset % 16
                    sprintf "P%04d%X" w b
            let xss, yss =
                seq {

                    let mutable baseStart = 0
                    // 이번 slot 의 시작 bit 주소
                    for b, bbase in x.Bases.Indexed() do
                        let mutable slotStart = baseStart
                        for s, slot in bbase.Slots.Indexed() do
                            if not slot.IsEmpty then
                                let cap= slot.Length
                                let addresses = [| for i in 0..cap-1 -> createAddress(slot.IsInput, b, s, i, i+slotStart)|]
                                if slot.IsInput then
                                    yield addresses, [||]
                                else
                                    yield [||], addresses

                            slotStart <- slotStart + slot.GetCapacity(x.IsFixedSlotAllocation)

                        baseStart <- baseStart + bbase.GetCapacity(x.IsFixedSlotAllocation)
                } |> toArray
                |> Array.unzip
            let xs = xss |> Array.concat
            let ys = yss |> Array.concat
            xs, ys


        member x.CreateIOAllocator(forbiddenXs:string seq, forbiddenYs:string seq) =
            let xs, ys = x.CreateIOHaystacks()

            let availableXs = xs |> Seq.except forbiddenXs
            let availableYs = ys |> Seq.except forbiddenYs
            let inputAllocator:IOAllocatorFunction  = Seq.tryEnumerate availableXs
            let outputAllocator:IOAllocatorFunction = Seq.tryEnumerate availableYs
            inputAllocator, outputAllocator

type XGTDupExtensionForCSharp =
    [<Extension>] static member Duplicate(slot:IoSlot) = IoSlot(slot.IsEmpty, slot.IsInput, slot.IsDigital, slot.Length)
    [<Extension>] static member Duplicate(ioBase:Base) = Base(ioBase.Slots.Map(_.Duplicate()))
    [<Extension>]
    static member Duplicate(plcHw:PlcHw) =
        let y = PlcHw.Create(plcHw.PLCType, plcHw.IsFixedSlotAllocation)
        y.Bases <- plcHw.Bases.Map(_.Duplicate())
        y
