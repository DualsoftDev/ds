namespace XgtProtocol

open System
open Dual.PLC.Common.FS

/// XGTTag: LS(XGT) 전용 태그 표현
type XGTTag(address: string, dataTypeSize: int, bitOffset: int) =
    inherit PlcTagBase(address, dataTypeSize)

    let step = 100

    member val LWordOffset = -1 with get, set

    /// 디바이스 문자열 (e.g., "MB", "MW", "ZR")
    member _.Device =
        let dev = address.TrimStart('%')
        if dev.StartsWith("ZR") then dev.Substring(0, 2)
        else dev.Substring(0, 1)

    /// 비트 단위 오프셋 계산
    member _.BitOffset =
        if address.StartsWith("S") then bitOffset / step * 16
        else bitOffset

    /// LWord 태그 이름 (e.g., %ML0)
    member x.LWordTag =
        if address.StartsWith("%") then
            sprintf "%%%sL%d" x.Device (x.BitOffset / 64)
        else
            sprintf "%sL%d" x.Device (x.BitOffset / 64)

    /// 시작 바이트 위치 (LWordOffset * 8 + 내부 오프셋)
    member x.StartByteOffset = x.LWordOffset * 8 + (x.BitOffset % 64) / 8

    /// 메모리 타입 문자 (Bit → 'X', 그 외 → 'B')
    member _.MemType = if dataTypeSize = 1 then 'X' else 'B'

    /// 데이터 크기 (Bit → 내부 비트 인덱스, 그 외 → 바이트 크기)
    member x.Size =
        if dataTypeSize = 1 then x.BitOffset % 8
        else dataTypeSize / 8

    /// 버퍼 값을 읽어 현재 값으로 설정, 변경 여부 반환
    override x.UpdateValue(buffer: byte[]) : bool =
        let newValue : obj =
            match x.DataType with
            | Bit ->
                if x.Device = "S" then
                    let lw = BitConverter.ToUInt16(buffer, x.StartByteOffset) |> int
                    (lw = (bitOffset % step)) :> obj
                else
                    let lw = BitConverter.ToUInt64(buffer, x.LWordOffset * 8)
                    (lw &&& (1UL <<< (x.BitOffset % 64)) <> 0UL) :> obj
            | Byte  -> buffer.[x.StartByteOffset] :> obj
            | Word  -> BitConverter.ToUInt16(buffer, x.StartByteOffset) :> obj
            | DWord -> BitConverter.ToUInt32(buffer, x.StartByteOffset) :> obj
            | LWord -> BitConverter.ToUInt64(buffer, x.StartByteOffset) :> obj

        if base.Value <> newValue then
            base.Value <- newValue
            true
        else
            false
