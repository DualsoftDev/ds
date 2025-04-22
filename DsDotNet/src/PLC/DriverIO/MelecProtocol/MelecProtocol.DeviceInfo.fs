namespace MelsecProtocol

open System
open Dual.PLC.Common.FS
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System.Globalization
open Dual.Common.Core.FS

/// 파싱된 디바이스 정보
[<Struct>]
type MxDeviceInfo =
    {
        Device: MxDevice
        DataTypeSize: MxDeviceType
        BitOffset: int
        NibbleK: int
    }
    with
        member x.Address =
            let dev, bitOffset, wordOffset = x.Device, x.BitOffset, x.BitOffset / 16
            let isHexa = MxDevice.IsHexa dev
            match x.DataTypeSize with
            | MxDeviceType.MxWord ->
                if isHexa then $"{dev}{wordOffset:X}" 
                          else $"{dev}{wordOffset}"
            | MxDeviceType.MxBit ->
                if x.NibbleK > 0 then
                    if isHexa 
                    then $"K{x.NibbleK}{dev}{bitOffset:X}"
                    else $"K{x.NibbleK}{dev}{bitOffset}"
                else
                    if isHexa then $"{dev}{bitOffset:X}" 
                              else $"{dev}{bitOffset}"
            | MxDeviceType.MxDotBit ->
                if isHexa then  $"{dev}{wordOffset:X}.{bitOffset % 16:X}" 
                          else  $"{dev}{wordOffset}.{bitOffset % 16:X}" 
                

        static member private parseKFormat (m: Match) : MxDeviceInfo option =
            let kSize = int m.Groups[1].Value
            let head = m.Groups[2].Value
            let offsetStr = m.Groups[3].Value
            match MxDevice.Create(head) with
            | Some dev when dev = MxDevice.TS ||  dev = MxDevice.CS -> None
            | Some dev when MxDevice.IsBit dev ->
                let isHexa = MxDevice.IsHexa dev
                let style = if isHexa then NumberStyles.HexNumber else NumberStyles.Integer
                match Int32.TryParse(offsetStr, style, CultureInfo.InvariantCulture) with
                | true, offset ->
                    Some {
                        Device = dev
                        DataTypeSize = MxDeviceType.MxBit
                        BitOffset = offset
                        NibbleK = kSize
                    }
                | _ -> None
            | _ -> None

        static member private parseStandardFormat (address: string) : MxDeviceInfo option =
            let getRecord (device: string, d1: string, d2: string option) =
                match MxDevice.Create(device) with
                | Some dev ->
                    let isHexa = MxDevice.IsHexa dev
                    let style = if isHexa then NumberStyles.HexNumber else NumberStyles.Integer
                    let baseOffset =
                        match Int32.TryParse(d1, style, CultureInfo.InvariantCulture) with
                        | true, v -> Some v
                        | _ -> None
                    match baseOffset with
                    | Some offset ->
                        let dataType, bitOffset =
                            match d2 with
                            | Some bitStr when Regex.IsMatch(bitStr, "^[0-9A-F]$") ->
                                let dot = Convert.ToInt32(bitStr, 16)
                                MxDeviceType.MxDotBit, offset * 16 + dot
                            | None ->
                                if MxDevice.IsBit dev then MxDeviceType.MxBit, offset
                                else MxDeviceType.MxWord, offset * 16
                            | _ -> MxDeviceType.MxWord, -1
                        if bitOffset >= 0 then
                            Some {
                                Device = dev
                                DataTypeSize = dataType
                                BitOffset = bitOffset
                                NibbleK = 0
                            }
                        else None
                    | None -> None
                | None -> None

            match address.ToUpperInvariant() with
            | RegexPattern @"^(Z|D|R)(\d+)(?:\.([0-9A-F]))$" [device; d1; d2]
            | RegexPattern @"^(W)([0-9A-F]+)(?:\.([0-9A-F]))$" [device; d1; d2]
            | RegexPattern @"^(ZR|SD)(\d+)(?:\.([0-9A-F]))$" [device; d1; d2]
            | RegexPattern @"^(SW)([0-9A-F]+)(?:\.([0-9A-F]))$" [device; d1; d2] ->
                getRecord(device, d1, Some d2)

            | RegexPattern @"^(X|Y|B|W)([0-9A-F]+)$" [device; d1]
            | RegexPattern @"^(M|L|F|Z|V|D|R|T|C)(\d+)$" [device; d1]
            | RegexPattern @"^(ZR|ST|SM|SD)(\d+)$" [device; d1]
            | RegexPattern @"^(SW|SB|DX|DY)([0-9A-F]+)$" [device; d1] ->
                getRecord(device, d1, None)
            | _ -> None

        static member Create(address: string) : MxDeviceInfo option =
            let mK = Regex.Match(address, "^K([1-8])([A-Z]{1,2})([0-9A-F]+)$")
            if mK.Success then MxDeviceInfo.parseKFormat mK
            else MxDeviceInfo.parseStandardFormat address
