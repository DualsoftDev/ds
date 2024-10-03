namespace PLC.HW

open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices


/// 상위 어디엔가로 옮겨야 할 module
[<AutoOpen>]
module rec DsCommonApiModule =
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

    /// PLC IO slot 하나.  UI 표출 시에는 ToEnumString() 를 통해 문자열로 변환하고, C# 에서 문자열을 Enum.Parse 를 통해 Ppt.AddIn.DevXUI.UiPlcIoSlotTypeE 로 변환
    type PlcIoSlot =
        | Input of PlcIoSlotCapacity
        | Output of PlcIoSlotCapacity
        member x.BitCapacity =
            match x with
            | Input cap -> cap.ToInt()
            | Output cap -> cap.ToInt()

    /// 문자열로 encoding된 PlcIoSlot 정보를 decoding
    /// - 반대: PlcIoSlot.ToEnumString()
    let tryStr2PlcIoSlot (str:string) : PlcIoSlot option =
        match str with  // e.g Unused, {IN,OUT}{8,16,32,64}
        | "Unused" -> None
        | RegexPattern "^(IN|OUT)(\d+)?$" [io; Int32Pattern n] ->
            match io, n with
            | "IN",  8  -> Some <| Input  Point8
            | "IN",  16 -> Some <| Input  Point16
            | "IN",  32 -> Some <| Input  Point32
            | "IN",  64 -> Some <| Input  Point64
            | "OUT", 8  -> Some <| Output Point8
            | "OUT", 16 -> Some <| Output Point16
            | "OUT", 32 -> Some <| Output Point32
            | "OUT", 64 -> Some <| Output Point64
            | unused, _ ->
                assert(unused = "Unused")
                None
        | _ -> failwith "ERROR"

    /// PLC HW 구성.  Slots (+ CPU + Network ...)
    type PlcHw(plcType:PLCType, ioSlots:PlcIoSlot option []) =
        do
            assert(ioSlots.Length = MaxNumberSlots) // 해당 slot 이 비었으면 None 으로라도 채워져서 전체 갯수가 맞아야 한다.

        new(plcType) = PlcHw(plcType, repeat Option<PlcIoSlot>.None |> take MaxNumberSlots |> toArray)
        new() = PlcHw(Xgi)
        member val PLCType: PLCType = plcType with get, set
        member val OptIoSlots = ioSlots with get, set

    /// PLC IO slot 구성에 따라서 가용한 io bit 번호를 뱉는 함수.  더 이상 가용 bit 가 없으면 None 반환
    type IOAllocatorFunction = unit -> int option

    type PlcHw with
        [<JsonIgnore>]
        member x.FilledIoSlots:(SlotIndex * PlcIoSlot) [] =
            x.OptIoSlots                            // (PlcIoSlot option) []
            |> indexed
            |> filter (snd >> Option.isSome)        // (SlotIndex * PlcIoSlot option) []
            |> Array.map2nd Option.get              // (SlotIndex * PlcIoSlot) []

        member x.CreateIOHaystacks(): int[] * int[] =
            let xss, yss =
                chooseSeq {
                    // 이번 slot 의 시작 bit 주소
                    let mutable start = 0
                    for (i, optSlot) in x.OptIoSlots.Indexed() do
                        let! slot = optSlot

                        let cap= slot.BitCapacity

                        match slot with
                        | Input _ ->
                            [| start .. start+cap|], [||]
                        | Output _ ->
                            [||], [| start .. start+cap |]
                        match x.PLCType with
                        | Xgi -> start <- start + 64
                        | Xgk -> start <- start + max 16 (cap + 1)
                } |> toArray
                |> Array.unzip
            let xs = xss |> Array.concat
            let ys = yss |> Array.concat
            xs, ys


        member x.CreateIOAllocator(forbiddenXs:int seq, forbiddenYs:int seq) =
            let xs, ys = x.CreateIOHaystacks()

            let availableXs = xs |> Seq.except forbiddenXs
            let availableYs = ys |> Seq.except forbiddenYs
            let inputAllocator:IOAllocatorFunction  = Seq.tryEnumerate availableXs
            let outputAllocator:IOAllocatorFunction = Seq.tryEnumerate availableYs
            inputAllocator, outputAllocator


type PlcIoSlotExtension =
    /// C# 의 UiPlcIoSlotTypeE 와 match 되게 print 해야 함.
    [<Extension>]
    static member ToEnumString(optSlot:PlcIoSlot option) =
        match optSlot with
        | None -> "Unused"
        | Some (Input c) -> $"IN{c.Stringify()}"
        | Some (Output c) -> $"OUT{c.Stringify()}"




