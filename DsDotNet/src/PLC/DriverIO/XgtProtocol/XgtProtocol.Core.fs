namespace XgtProtocol

open System
open Dual.PLC.Common.FS

/// XGTTag: PLC 태그 정보를 표현하는 클래스
/// - deviceAddress: "%MB00010" 같은 주소 문자열
/// - dataTypeSize: 1, 8, 16, 32, 64 (bit 단위)
/// - bitOffset: 전체 bit 기준 offset

type XGTTag(deviceAddress: string, dataTypeSize: int, bitOffset: int) =
    let mutable currentValue: obj = null
    let mutable writeValue: obj option = None
    let step = 100

    interface ITagPLC with
        member _.Address = deviceAddress
        member _.Value with get() = currentValue and set(v) = currentValue <- v
        member _.SetWriteValue(value: obj) = writeValue <- Some value
        member _.ClearWriteValue() = writeValue <- None
        member _.GetWriteValue() = writeValue

    member this.GetWriteValue() : obj option = (this :> ITagPLC).GetWriteValue()
    member this.ClearWriteValue() = (this :> ITagPLC).ClearWriteValue()
    member this.SetWriteValue(value: obj) = (this :> ITagPLC).SetWriteValue(value)
    member _.Address = deviceAddress
    member _.Value = currentValue
    member _.DataType = dataTypeSize

    member _.Device =
        let dev = deviceAddress.TrimStart('%')
        if dev.StartsWith("ZR") then dev.Substring(0, 2)
        else dev.Substring(0, 1)

    member val LWordOffset = -1 with get, set

    member _.BitOffset =
        if deviceAddress.StartsWith("S") then bitOffset / step * 16
        else bitOffset

    member x.LWordTag =
        if deviceAddress.StartsWith("%") then
            sprintf "%%%sL%d" x.Device (x.BitOffset / 64)
        else
            sprintf "%sL%d" x.Device (x.BitOffset / 64)

    member x.StartByteOffset = x.LWordOffset * 8 + x.BitOffset % 64 / 8
    member _.MemType = if dataTypeSize = 1 then 'X' else 'B'
    member x.Size = if dataTypeSize = 1 then x.BitOffset % 8 else dataTypeSize / 8

    member x.UpdateValue(buf: byte[]) : bool =
        let newValue =
            match x.DataType with
            | 1 ->
                if x.Device = "S" then
                    let lw = BitConverter.ToUInt16(buf, x.StartByteOffset) |> int
                    (lw = (bitOffset % step)) :> obj
                else
                    let lw = BitConverter.ToUInt64(buf, x.LWordOffset * 8)
                    (lw &&& (1UL <<< x.BitOffset % 64) <> 0UL) :> obj
            | 8 -> buf.[x.StartByteOffset] :> obj
            | 16 -> BitConverter.ToUInt16(buf, x.StartByteOffset) :> obj
            | 32 -> BitConverter.ToUInt32(buf, x.StartByteOffset) :> obj
            | 64 -> BitConverter.ToUInt64(buf, x.StartByteOffset) :> obj
            | _ -> failwith $"Unsupported DataTypeSize {x.DataType} for tag {x.Address}"

        if currentValue <> newValue then
            currentValue <- newValue
            true
        else
            false
