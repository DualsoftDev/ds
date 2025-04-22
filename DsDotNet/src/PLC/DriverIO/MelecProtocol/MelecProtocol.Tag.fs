namespace MelsecProtocol

open System
open Dual.PLC.Common.FS

/// MELSEC 전용 태그 표현

type MelsecTag(name: string, address: string,  dataSizeType: PlcDataSizeType, bitOffset: int, ?comment: string) =
    inherit PlcTagBase(name, address, dataSizeType, ?comment = comment)

    member val BitOffset = bitOffset with get, set

    /// 디바이스 접두어 (예: D, M, X 등)
    member this.DeviceCode = address.Chars(0).ToString()

    /// 워드 주소 문자열 (예: D100.2 → D100)
    member this.WordAddress =
        address.Split('.') |> Array.head

    /// DWord 태그 주소 그룹 식별자 (BitOffset 기준)
    member this.DWordTag =
        $"{this.DeviceCode}{this.BitOffset / 32}"

    /// DWord 단위 오프셋
    member val DWordOffset = 0 with get, set

    /// .bit 주소 포함 여부
    member this.IsDotBit = address.Contains(".")

    /// K포맷 주소 여부
    member this.IsKFormat = address.StartsWith("K", StringComparison.OrdinalIgnoreCase)

    /// 비트 디바이스 여부
    member this.IsBit =
        match dataSizeType with
        | PlcDataSizeType.Boolean -> true
        | _ -> false

    /// 워드 디바이스 여부
    member this.IsWord =
        not this.IsBit && not this.IsDotBit && not this.IsKFormat

    /// 닙블 단위 K 포맷 여부
    member this.IsNibbleK =
        this.IsKFormat && (this.BitOffset % 4 = 0)

    /// 데이터 타입 기준 실제 DWord 단위 크기 반환
    member this.RequiredDWordLength =
        match this.DataType with
        | PlcDataSizeType.Boolean
        | PlcDataSizeType.Byte
        | PlcDataSizeType.UInt16 -> 1
        | PlcDataSizeType.UInt32 -> 2
        | PlcDataSizeType.UInt64 -> 4
        | _ -> raise (NotSupportedException $"지원하지 않는 데이터 타입: {this.DataType}")

    override _.ReadWriteType = if address.StartsWith("Y") then ReadWriteType.Write else ReadWriteType.Read
    override _.IsMemory = true

    override this.UpdateValue(buffer: byte[]) =
        let offset = this.DWordOffset * 4
        let bitPos = this.BitOffset % 32
        let byteIndex = offset + bitPos / 8
        let bitIndex = bitPos % 8

        let newVal =
            match this.DataType with
            | PlcDataSizeType.Boolean ->
                let b = buffer.[byteIndex]
                (b &&& (1uy <<< bitIndex)) <> 0uy |> box
            | PlcDataSizeType.Byte ->
                buffer.[byteIndex] |> box
            | PlcDataSizeType.UInt16 ->
                BitConverter.ToUInt16(buffer, offset) |> box
            | PlcDataSizeType.UInt32 ->
                BitConverter.ToUInt32(buffer, offset) |> box
            | _ -> failwithf "지원하지 않는 데이터 타입: %A" this.DataType

        if this.Value <> newVal then
            this.Value <- newVal
            true
        else false
