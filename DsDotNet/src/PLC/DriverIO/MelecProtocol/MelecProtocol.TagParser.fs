namespace MelsecProtocol

open System
open System.Runtime.CompilerServices
open Dual.Common.Core.FS

[<AutoOpen>]
module MxTagConst =
    let WORD, BIT = 16, 1

    let Works2Devices = "(^[X|Y|M|L|B|D|W|R|T|C|F|Z|V])([0-9A-F]+)(\.|)?([0-9A-F]*)"
    let Works2Extends = "([Z|S|D])([R|M|D|W|B|X|Y|T])([0-9A-F]+)(\.|)?([0-9A-F]*)"
    let NibbleText = @"(K)(\d+)(\D+\S+)?"

    let AllowedDevices =
        set [ "X"; "Y"; "M"; "L"; "B"; "F"; "Z"; "V"
              "D"; "W"; "R"; "ZR"; "T"; "ST"; "C"
              "SM"; "SD"; "SW"; "SB"; "DX"; "DY" ]

    let SizeMap =
        let oneBitDevices = [ "X"; "Y"; "M"; "L"; "B"; "F"; "Z"; "V"; "SM"; "SB"; "DX"; "DY" ]
        let sixteenBitDevices = [ "D"; "W"; "R"; "ZR"; "T"; "ST"; "C"; "SD"; "SW" ]
        (oneBitDevices |> List.map (fun k -> k, 1)) @ 
        (sixteenBitDevices |> List.map (fun k -> k, 16))
        |> dict

[<AutoOpen>]
type MelsecDevice =
    | X | Y | M | L | B | F | Z | V
    | D | W | R | ZR | T | ST | C
    | SM | SD | SW | SB | DX | DY
    with
        member x.ToText = DU.toString x

        static member Create(s: string) =
            match DU.fromString<MelsecDevice> s with
            | Some DX -> X
            | Some DY -> Y
            | Some dev -> dev
            | None -> failwithlog $"Invalid address [{s}]"

        static member Parsing(address: string) =
            let parse (head: MelsecDevice, d1: string, d2: string) =
                let toInt s (baseVal:int) = if String.IsNullOrWhiteSpace(s) then -1 else Convert.ToInt32(s, baseVal)
                match d2 with
                | "" | null -> if [X; Y; B; W; SW; SB] |> List.contains head
                               then head, toInt d1 16, -1
                               else head, toInt d1 10, -1
                | _ -> if [W; SW] |> List.contains head
                       then head, toInt d1 16, toInt d2 16
                       else head, toInt d1 10, toInt d2 16

            match address with
            | ActivePattern.RegexPattern (sprintf @"%s" Works2Devices) [head; d1; _; d2] ->
                parse (MelsecDevice.Create head, d1, d2)
            | ActivePattern.RegexPattern (sprintf @"%s" Works2Extends) [h1; h2; d1; _; d2] ->
                parse (MelsecDevice.Create (h1 + h2), d1, d2)
            | _ -> failwithlog $"Invalid address [{address}]"

        static member IsMelsecAddress(address: string) =
            match address with
            | ActivePattern.RegexPattern (sprintf @"%s" Works2Devices) _
            | ActivePattern.RegexPattern (sprintf @"%s" Works2Extends) _
            | ActivePattern.RegexPattern NibbleText _ -> true
            | _ -> false

[<Extension>]
type MxTagParser =

    [<Extension>]
    static member ParseAddress(device: string, bitOffset: int, bitSize: int) : string =
        match bitSize with
        | 1 -> $"{device}{bitOffset:X}"
        | 16 when bitOffset % 16 = 0 -> $"{device}{bitOffset / 16}"
        | _ -> failwithf $"Invalid size: {bitSize}"

    [<Extension>]
    static member Parse(address: string) : string option =
        if String.IsNullOrWhiteSpace(address) then failwith "주소가 없습니다"

        let addr = address.Trim().ToUpper()
        let device = addr.Substring(0, 1)

        if not (AllowedDevices.Contains device) then None
        else
            let offsetStr = addr.Substring(1)
            let offset = if offsetStr = "" then 0 else Convert.ToInt32(offsetStr, 16)
            let size = SizeMap.TryGetValue(device) |> function true, v -> v | _ -> WORD
            Some (MxTagParser.ParseAddress(device, offset, size))

    [<Extension>]
    static member ParseSegment(address: string) : string * int * int option =
        if String.IsNullOrWhiteSpace(address) then failwith "주소가 없습니다"

        let addr = address.Trim().ToUpper()
        let device = addr.Substring(0, 1)

        if not (AllowedDevices.Contains device) then
            failwith $"허용되지 않은 장치 타입입니다: {device}"

        let offsetStr = addr.Substring(1)
        let offset = if offsetStr = "" then 0 else Convert.ToInt32(offsetStr, 16)
        let sizeOpt = match SizeMap.TryGetValue(device) with true, v -> Some v | _ -> None

        device, offset, sizeOpt
