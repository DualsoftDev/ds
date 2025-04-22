namespace MelsecProtocol

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Dual.PLC.Common.FS

[<AutoOpen>]
module MxTagParserModule =

    type MxDeviceType =
        | MxBit 
        | MxWord 
        | MxNibbleK
        | MxDotBit
        with
            member x.ToPlcDataSizeType() =
                match x with
                | MxDotBit 
                | MxBit -> PlcDataSizeType.Boolean
                | MxWord 
                | MxNibbleK -> PlcDataSizeType.UInt16

    type MxDevice =
        | X | Y | M | L | B | F | Z | V
        | D | W | R | ZR | T | C | ST    
        | SM | SD | SW | SB | DX | DY
        with
            member x.ToText = x.ToString()

            member x.DevType =
                match x with
                | X | Y | DX | DY | M | L | B | F | SB | SM -> MxBit
                | _ -> MxWord

            member x.IsHexa = 
                match x with
                | X | Y | B | W | SW | SB | DX | DY -> true
                | _ -> false

            static member Create s =
                match s with
                | "X" -> Some X | "Y" -> Some Y | "M" -> Some M | "L" -> Some L
                | "B" -> Some B | "F" -> Some F | "Z" -> Some Z | "V" -> Some V
                | "D" -> Some D | "W" -> Some W | "R" -> Some R | "C" -> Some C
                | "T" -> Some T | "ST" -> Some ST | "ZR" -> Some ZR  | "SM" -> Some SM
                | "SD" -> Some SD | "SW" -> Some SW | "SB" -> Some SB | "DX" -> Some DX
                | "DY" -> Some DY
                | _ -> None

    type MxTagInfo = 
        {
            Device: MxDevice
            DataTypeSize: MxDeviceType
            BitOffset: int
        }

    let tryParseInt (value: string) (isHex: bool) =
        try
            if isHex then Convert.ToInt32(value, 16)
            else Convert.ToInt32(value)
        with _ -> -1

    let tryParseKAddress (address: string) =
        let m = Regex.Match(address, @"^K([1-8])([A-Z]{1,2})([0-9A-F]+)$")
        if m.Success then
            let kSize = Convert.ToInt32(m.Groups[1].Value)
            let deviceStr = m.Groups[2].Value
            let indexStr = m.Groups[3].Value
            match MxDevice.Create(deviceStr) with
            | Some dev when (tryParseInt  indexStr dev.IsHexa) >= 0 ->
                Some {
                    Device = dev
                    DataTypeSize = MxNibbleK
                    BitOffset = (tryParseInt indexStr dev.IsHexa) * kSize
                }
            | _ -> None
        else None

    let tryParseMxTag (address: string) : MxTagInfo option =
        let upper = address.ToUpper()

        let parseBitOffset (dev: MxDevice) (main: string) (sub: string option) =
            let base1 = tryParseInt main dev.IsHexa
            if base1 < 0 then None else
            match sub with
            | Some bit when dev.DevType = MxWord ->
                let bitOffset = tryParseInt bit true
                if bitOffset < 0 then None else Some (base1 * 16 + bitOffset)
            | _ -> if dev.DevType = MxBit then Some base1 else Some (base1 * 16)

        let tryMakeTag device main sub =
            match MxDevice.Create device with
            | Some dev ->
                parseBitOffset dev main sub |> Option.map (fun offset ->
                    {
                        Device = dev
                        DataTypeSize =
                            match sub with
                            | Some _ -> MxDotBit
                            | None -> dev.DevType
                        BitOffset = offset
                    })
            | None -> None

        match tryParseKAddress upper with
        | Some tag -> Some tag
        | None ->
            match upper with
            | RegexPattern @"^(Z|D|R)(\d+)(?:\.([0-9A-F]))$" [d; a; b]
            | RegexPattern @"^(W|SW)([0-9A-F]+)(?:\.([0-9A-F]))$" [d; a; b]
            | RegexPattern @"^(ZR|SD)(\d+)(?:\.([0-9A-F]))$" [d; a; b] -> tryMakeTag d a (Some b)
            | RegexPattern @"^(X|Y|B|W)([0-9A-F]+)$" [d; a]
            | RegexPattern @"^(M|L|F|Z|V|D|R|T|C)(\d+)$" [d; a]
            | RegexPattern @"^(ZR|ST|SM|SD)(\d+)$" [d; a]
            | RegexPattern @"^(SW|SB|DX|DY)([0-9A-F]+)$" [d; a] -> tryMakeTag d a None
            | _ -> None
    
    let tryParse (device: string) (bitOffset: int) (bitDataSize: int) : string option =
        match MxDevice.Create(device) with
        | Some dev ->
            match bitDataSize with
            | 1 ->
                if dev.IsHexa then Some($"{device}{bitOffset:X}") else Some($"{device}{bitOffset}")
            | 16 ->
                let addr = bitOffset / 16
                if dev.IsHexa then Some($"{device}{addr:X}") else Some($"{device}{addr}")
            | _ -> None
        | None -> None

[<AutoOpen>]
[<Extension>]
type MxTagParser =

    [<Extension>]
    static member TryParseToMxTag(tag: TagInfo): MelsecTag option =
        match tryParseMxTag tag.Address with
        | Some v -> Some (MelsecTag(tag.Name, tag.Address,  v.DataTypeSize.ToPlcDataSizeType(), v.BitOffset, tag.Comment))
        | None -> None

    [<Extension>]
    static member ParseAddress(device: string, bitOffset: int, plcDataType: PlcDataSizeType) : string =
        match tryParse device bitOffset (PlcDataSizeType.TypeBitSize(plcDataType)) with
        | Some v  -> v
        | None -> failwithf $"Invalid size: {plcDataType}"

    [<Extension>]
    static member ParseFromSegment(device: string, bitOffset: int, bitDataSize: int) : string =
        match tryParse device bitOffset bitDataSize with
        | Some v  -> v
        | None -> failwithf $"Invalid size: {bitDataSize}"

    [<Extension>]
    static member Parse(address: string) : string option =
        tryParseMxTag address
        |> Option.map (fun tag -> MxTagParser.ParseAddress(tag.Device.ToText, tag.BitOffset, tag.DataTypeSize.ToPlcDataSizeType()))

    [<Extension>]
    static member ParseToSegment(address: string) : string * int * int option =
        match tryParseMxTag address with
        | Some tag ->
            let size =
                match tag.DataTypeSize with
                | MxBit | MxDotBit -> Some 1
                | MxWord | MxNibbleK -> Some 16
            tag.Device.ToText, tag.BitOffset, size
        | None -> failwith $"주소를 파싱할 수 없습니다: {address}"
