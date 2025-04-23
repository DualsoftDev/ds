namespace MelsecProtocol

open System
open Dual.PLC.Common.FS
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open System.Globalization

/// MELSEC 명령어 구분 (Access Command)
[<RequireQualifiedAccess>]
type DeviceAccessCommand =
    | BatchRead = 0x0401
    | BatchWrite = 0x1401
    | RandomRead = 0x0403
    | RandomWrite = 0x1402
    | RemoteRun = 0x1001
    | RemoteStop = 0x1002
    | RemotePause = 0x1003
    | RemoteLatchClear = 0x1005
    | RemoteReset = 0x1006
    | ReadCpuModelName = 0x0101

/// MELSEC 디바이스 접근 타입 (비트/워드)
[<RequireQualifiedAccess>]
type DeviceAccessType =
    | Bit = 0
    | Word = 1

/// 사용할 MELSEC 프레임 형식
[<RequireQualifiedAccess>]
type McFrame =
    | MC3E = 0x0050
    | MC4E = 0x0054

/// MELSEC 디바이스 종류 (명령 코드 포함)
[<RequireQualifiedAccess>]
type MxDevice =
    | M = 0x90 | SM = 0x91 | L = 0x92 | F = 0x93 | V = 0x94 | S = 0x98 | X = 0x9C | Y = 0x9D
    | B = 0xA0 | SB = 0xA1 | DX = 0xA2 | DY = 0xA3 | D = 0xA8 | SD = 0xA9 | R = 0xAF | ZR = 0xB0
    | W = 0xB4 | SW = 0xB5 | TC = 0xC0 | TS = 0xC1 | TN = 0xC2 | CC = 0xC3 | CS = 0xC4 | CN = 0xC5
    | SC = 0xC6 | SS = 0xC7 | SN = 0xC8 | Z = 0xCC | TT = 0xCD | TM = 0xCE | CT = 0xCF
    | CM = 0xD0 | A = 0xD1 | Max = 0xFF

/// MxDevice 확장 기능
[<AutoOpen>]
module MxDeviceExtensions =

    type MxDevice with
        static member ToText(device: MxDevice) : string =
            device.ToString()

        static member IsHexa(device: MxDevice) : bool =
            match device with
            | MxDevice.X | MxDevice.Y | MxDevice.B | MxDevice.W
            | MxDevice.SW | MxDevice.SB | MxDevice.DX | MxDevice.DY -> true
            | _ -> false

        static member IsBit(device: MxDevice) : bool =
            match device with
            | MxDevice.X | MxDevice.Y | MxDevice.M | MxDevice.L | MxDevice.F
            | MxDevice.B | MxDevice.SB | MxDevice.SM | MxDevice.DX | MxDevice.DY-> true
            | _ -> false

        static member Create(head: string) : MxDevice option =
            let normalized =
                head.ToUpperInvariant()
                |> function
                    | "T" -> "TN"
                    | "C" -> "CN"
                    | x -> x

            match Enum.TryParse<MxDevice>(normalized, ignoreCase = true) with
            | true, value -> Some value
            | _ -> None

/// MELSEC 디바이스 데이터 크기 구분
[<RequireQualifiedAccess>]
type MxDeviceType =
    | MxBit
    | MxWord
    | MxDotBit
    with
        member this.ToPlcDataSizeType() =
            match this with
            | MxBit | MxDotBit -> PlcDataSizeType.Boolean
            | MxWord -> PlcDataSizeType.UInt16
