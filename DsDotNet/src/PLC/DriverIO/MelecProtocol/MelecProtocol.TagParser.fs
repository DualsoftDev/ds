namespace MelsecProtocol

open System
open System.Runtime.CompilerServices
open Dual.PLC.Common.FS

[<AutoOpen>]
[<Extension>]
type MxTagParser =

    [<Extension>]
    static member TryParseToMxTag(tag: TagInfo): MelsecTag option =
        match MxDeviceInfo.Create(tag.Address) with
        | Some info -> Some(MelsecTag(tag.Name, info, comment = tag.Comment))
        | None -> None


    [<Extension>]
    static member TryParseToMxTag(tag: string): MelsecTag option =
        match MxDeviceInfo.Create(tag) with
        | Some info -> Some(MelsecTag("", info))
        | None -> None


   
    [<Extension>]
    static member Parse(address: string) : string option =
        match MxDeviceInfo.Create(address) with
        | Some info -> Some info.Address
        | None -> None

    [<Extension>]
    static member ParseToSegment(address: string) : string * int * int option =
        match MxDeviceInfo.Create(address) with
        | Some info ->
            let bitSize =
                match info.DataTypeSize with
                | MxDeviceType.MxBit | MxDeviceType.MxDotBit -> Some 1
                | MxDeviceType.MxWord -> Some 16
            info.Device.ToString(), info.BitOffset, bitSize
        | None -> raise (ArgumentException $"주소 파싱 실패: {address}")


    [<Extension>]
    static member ParseFromSegment(device: string, bitOffset: int, bitDataSize: int) : string =
        match MxDevice.Create(device) with
        | Some dev when bitDataSize = 1 ->
            if MxDevice.IsHexa dev then $"{dev}{bitOffset:X}" else $"{dev}{bitOffset}"
        | Some dev when bitDataSize = 16 ->
            let wordOffset = bitOffset / 16
            if MxDevice.IsHexa dev then $"{dev}{wordOffset:X}" else $"{dev}{wordOffset}"
        | _ -> raise (ArgumentException("Invalid device or data size"))
