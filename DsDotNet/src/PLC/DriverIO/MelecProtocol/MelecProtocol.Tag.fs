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

    /// 시작 바이트 오프셋 계산 (DWord 기준)
    member x.StartByteOffset = (x.BitOffset / 8)

    /// DWord 태그 주소 그룹 식별자 (BitOffset 기준)
    member this.DWordTag =
        let isHexa = MxDevice.IsHexa deviceInfo.Device
        match deviceInfo.DataTypeSize with
        | MxDeviceType.MxBit -> 
            if deviceInfo.NibbleK = 0 then
                deviceInfo.Address
            else
                if isHexa then
                    $"K8{deviceInfo.Device}{deviceInfo.BitOffset/32*32:X}"
                else
                    $"K8{deviceInfo.Device}"

        | MxDeviceType.MxWord
        | MxDeviceType.MxDotBit ->
            let value = deviceInfo.BitOffset/16/2*2
            if isHexa then
                $"{deviceInfo.Device}{value:X}"
            else
                $"{deviceInfo.Device}{value}"

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

    override x.UpdateValue(buffer: byte[]) =
        let dwordOffset = (x.BitOffset / 32) * 4
        let bitPos = x.BitOffset % 32

        let tryRead safeOffset size readFunc =
            if buffer.Length >= safeOffset + size then
                readFunc(buffer, safeOffset) :> obj
            else
                null

        let newVal: obj =
            match x.DataType with
            | Boolean ->
                if buffer.Length >= dwordOffset + 4 then
                    let dw = BitConverter.ToUInt32(buffer, dwordOffset)
                    ((dw >>> bitPos) &&& 0x1u <> 0u) :> obj
                else null
            | Byte    ->
                if buffer.Length > x.StartByteOffset then
                    buffer.[x.StartByteOffset] :> obj
                else null
            | UInt16  -> tryRead x.StartByteOffset 2 BitConverter.ToUInt16
            | UInt32  -> tryRead x.StartByteOffset 4 BitConverter.ToUInt32
            | UInt64  -> tryRead x.StartByteOffset 8 BitConverter.ToUInt64
            | _       -> failwith $"Unsupported data type: {x.DataType}"

        if not (obj.ReferenceEquals(newVal, null)) && x.Value <> newVal then
            x.Value <- newVal
            true
        else
            false