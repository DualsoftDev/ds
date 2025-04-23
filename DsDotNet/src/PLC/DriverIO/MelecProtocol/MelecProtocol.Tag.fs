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
    member this.DeviceCode = deviceInfo.Device


    /// DWord 태그 주소 그룹 식별자 (BitOffset 기준)
    member val DWordOffset = -1 with get, set
    member this.DWordTag =
        let isHexa = MxDevice.IsHexa deviceInfo.Device
        match deviceInfo.DataTypeSize with
        | MxDeviceType.MxBit -> 
            if deviceInfo.NibbleK > 0 then
                deviceInfo.Address
            else
                if isHexa then
                    $"K8{deviceInfo.Device}{deviceInfo.BitOffset/32*32:X}"
                else
                    $"K8{deviceInfo.Device}{deviceInfo.BitOffset/32*32}"

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


        
    /// 버퍼 값을 읽어 현재 값으로 설정, 변경 여부 반환
    override x.UpdateValue(buffer: byte[]) : bool =
        
        let startByte = x.DWordOffset * 4
        let startByteOffset = startByte + (x.BitOffset % 32) / 8

        let newValue : obj =
            match x.DataType with
            | Boolean ->
                let lw = BitConverter.ToUInt32(buffer, startByte)
                (lw &&& (1u <<< (x.BitOffset % 32)) <> 0u) :> obj
            | Byte  -> buffer.[startByteOffset] :> obj
            | UInt16  -> BitConverter.ToUInt16(buffer, startByteOffset) :> obj
            | UInt32  -> BitConverter.ToUInt32(buffer, startByteOffset) :> obj
            | UInt64  -> BitConverter.ToUInt64(buffer, startByteOffset) :> obj
            | _-> failwith $"Unsupported data type: {x.DataType}"

        if base.Value <> newValue then
            base.Value <- newValue
            true
        else
            false
