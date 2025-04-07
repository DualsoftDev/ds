namespace MelsecProtocol

open System
open Dual.PLC.Common.FS

/// MelsecTag: Melsec(Mitsubishi Electric) 전용 태그 표현
type MelsecTag(name: string, address: string, dataSizeType: PlcDataSizeType, bitOffset: int, ?comment: string) =
    inherit PlcTagBase(name, address, dataSizeType, ?comment = comment)

    /// 비트 오프셋 (예: %M0.3에서 .3 부분)
    member val BitOffset = bitOffset with get, set

    /// MELSEC 디바이스 코드 추출 (예: "M", "X", "D" 등)
    member this.DeviceCode =
        let start = address |> Seq.takeWhile Char.IsLetter |> Seq.toArray
        new string(start)

    /// Word Address로 변환 (예: D100.0 -> D100)
    member this.WordAddress =
        address.Split('.') |> Array.head

    /// 기본 LWordTag 생성 (블록 읽기용 주소 라벨링)
    member this.LWordTag =
        $"{this.DeviceCode}{this.BitOffset / 64}"

    /// 기본 LWord 오프셋
    member val LWordOffset = 0 with get, set

    /// 읽기/쓰기 타입 (기본 Read)
    override _.ReadWriteType = Read

    /// MELSEC 응답 버퍼로부터 값 업데이트
    override this.UpdateValue(buffer: byte[]) =
        let offset = this.LWordOffset * 8
        let bitPos = this.BitOffset % 64
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
            | PlcDataSizeType.UInt64 ->
                BitConverter.ToUInt64(buffer, offset) |> box
            | _ -> failwithf "지원하지 않는 데이터 타입: %A" this.DataType

        if this.Value <> newVal then
            this.Value <- newVal
            true
        else false
