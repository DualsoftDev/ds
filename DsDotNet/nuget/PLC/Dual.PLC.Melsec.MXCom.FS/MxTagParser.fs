namespace DsMxComm

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open Dual.PLC.Common.FS

[<AutoOpen>]
module MxTagParserModule =


    /// Mitsubishi PLC의 다양한 장치 유형을 정의하는 타입
    [<AutoOpen>]
    type MxDevice =
        | X | Y | M | L | B | F | Z | V
        | D | W | R | ZR | T | C    
        | SM | SD | SW | SB | DX | DY
        with
            member x.ToText = x.ToString()
            member x.DevType =
                match x with
                | X | Y | DX | DY | M | L | B | F | SB | SM -> Boolean
                | _ -> UInt16

            static member Create s =
                match s with
                | "X" -> Some X | "Y" -> Some Y | "M" -> Some M | "L" -> Some L
                | "B" -> Some B | "F" -> Some F | "Z" -> Some Z | "V" -> Some V
                | "D" -> Some D | "W" -> Some W | "R" -> Some R | "C" -> Some C
                | "T" -> Some T | "ZR" -> Some ZR  | "SM" -> Some SM
                | "SD" -> Some SD | "SW" -> Some SW | "SB" -> Some SB | "DX" -> Some DX
                | "DY" -> Some DY
                | _ -> None

            member x.IsHexa = 
                match x with
                | X | Y | B | W | SW | SB | DX | DY -> true
                | _ -> false

    type MxTagInfo = 
        {
            Device: MxDevice
            DataTypeSize: PlcDataSizeType
            BitOffset: int
        }

    /// 안전한 숫자 변환 함수
    let tryParseInt (value: string) (isHex: bool) =
        try
            if isHex then Convert.ToInt32(value, 16) else Convert.ToInt32(value)
        with
        | :? FormatException -> -1
        | :? OverflowException -> -1

    /// 주소에서 MxDevice와 인덱스를 추출하는 함수
    let tryParseMxTag (address: string) : MxTagInfo option =
        let getBitOffset (parsedDevice: MxDevice) (d1: string) (d2: string option) =
            let baseOffset = tryParseInt d1 parsedDevice.IsHexa
            if baseOffset = -1 then None
            else
                match d2 with
                | Some bit when parsedDevice.DevType = UInt16 -> 
                    let bitOffset = tryParseInt bit true
                    if bitOffset = -1 then None else Some (baseOffset * 16 + bitOffset)
                | None -> if parsedDevice.DevType = Boolean
                            then Some baseOffset
                            else Some (baseOffset*16)
                | _ -> None

        let getRecord (device: string, d1: string, d2: string option) =
            match MxDevice.Create device with
            | Some parsedDevice ->
                getBitOffset parsedDevice d1 d2
                |> Option.map (fun bitOffset -> 
                    { Device = parsedDevice
                      DataTypeSize = if d2.IsSome then Boolean else  parsedDevice.DevType
                      BitOffset = bitOffset })
            | None -> None


//X, Y, B, W, SW, SB는 16진수 기반으로 변환
        match address.ToUpper() with
            // ✅ 1글자 장치 (Z, D, R) - 비트 포함 
            | RegexPattern @"^(Z|D|R)(\d+)(?:\.([0-9A-F]))$" [device; d1; d2] ->
                getRecord(device, d1, Some d2)

            // ✅ 16진수 기반 주소 (W) - 비트 포함 
            | RegexPattern @"^(W)([0-9A-F]+)(?:\.([0-9A-F]))$" [device; d1; d2] ->
                getRecord(device, d1, Some d2)

            // ✅ 2글자 장치 (ZR,  SD) - 비트 포함 
            | RegexPattern @"^(ZR|SD)(\d+)(?:\.([0-9A-F]))$" [device; d1; d2] ->
                getRecord(device, d1, Some d2)

            // ✅ 16진수 기반 주소 (SW) - 비트 포함 
            | RegexPattern @"^(SW)([0-9A-F]+)(?:\.([0-9A-F]))$" [device; d1; d2] ->
                getRecord(device, d1, Some d2)

            // ✅ 비트 정보 없는 경우 (Hexa 타입 장치: X, Y, B, W)
            | RegexPattern @"^(X|Y|B|W)([0-9A-F]+)$" [device; d1] ->
                getRecord(device, d1, None)

            // ✅ 비트 정보 없는 경우 (일반 장치: M, L, F, Z, V, D, R, T, C, S)
            | RegexPattern @"^(M|L|F|Z|V|D|R|T|C)(\d+)$" [device; d1] ->
                getRecord(device, d1, None)

            // ✅ 비트 정보 없는 경우 (ZR, ST, SM, SD, DX, DY)
            | RegexPattern @"^(ZR|ST|SM|SD)(\d+)$" [device; d1] ->
                getRecord(device, d1, None)

            // ✅ 비트 정보 없는 경우 (Hexa 타입 장치: SW, SB, DX, DY)
            | RegexPattern @"^(SW|SB|DX|DY)([0-9A-F]+)$" [device; d1] ->
                getRecord(device, d1, None)

            // ❌ 매칭되지 않으면 None 반환
            | _ -> None



[<Extension>]   // For C#
type MxTagParser =
    [<Extension>]
    static member Parse(tag: string): MxTagInfo =
        tryParseMxTag tag |? (getNull<MxTagInfo>())
