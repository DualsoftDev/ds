namespace MelsecProtocol

open System
open Dual.PLC.Common.FS

/// MELSEC 전용 태그 표현

type MelsecTag(name: string, deviceInfo: MxDeviceInfo, ?comment: string) =
    inherit PlcTagBase(
        name,
        deviceInfo.Address,
        deviceInfo.DataTypeSize.ToPlcDataSizeType(),
        defaultArg comment ""
    )

    member x.BitOffset = deviceInfo.BitOffset 

    /// 디바이스 접두어 (예: D, M, X 등)
    member this.DeviceCode = deviceInfo.Device.ToString()

    /// DWord 태그 주소 그룹 식별자 (BitOffset 기준)
    member this.DWordTag = $"{this.DeviceCode}{this.BitOffset / 32}"

    /// DWord 단위 오프셋
    member val DWordOffset = 0 with get, set

    /// 비트 디바이스 여부
    member this.IsBit =
        match this.DataType with
        | PlcDataSizeType.Boolean -> true
        | _ -> false

    override x.ReadWriteType =
        if x.Address.StartsWith("Y", StringComparison.OrdinalIgnoreCase)
        then ReadWriteType.Write
        else ReadWriteType.Read

    override x.IsMemory = 
        match deviceInfo.Device with
        | MxDevice.X | MxDevice.Y | MxDevice.DX | MxDevice.DX -> false
        | _ -> true

    override this.UpdateValue(buffer: byte[]) =
        let offset = this.DWordOffset * 4
        let bitPos = this.BitOffset % 32
        let byteIndex = offset + bitPos / 8
        let bitIndex = bitPos % 8

        let newVal =
            match this.DataType with
            | PlcDataSizeType.Boolean ->
                let b = buffer.[byteIndex]
                ((b &&& (1uy <<< bitIndex)) <> 0uy) :> obj
            | PlcDataSizeType.Byte -> buffer.[byteIndex] :> obj
            | PlcDataSizeType.UInt16 -> BitConverter.ToUInt16(buffer, offset) :> obj
            | PlcDataSizeType.UInt32 -> BitConverter.ToUInt32(buffer, offset) :> obj
            | _ -> failwithf "지원하지 않는 데이터 타입: %A" this.DataType

        if this.Value <> newVal then
            this.Value <- newVal
            true
        else
            false