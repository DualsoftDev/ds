namespace DsMxComm

open System
open Dual.Common.Core.FS
open Dual.PLC.Common.FS

[<AutoOpen>]
// MxTag 클래스

type MxTag(deviceAddress: string, mxTagInfo: MxTagInfo) =
    let mutable currentValue: obj = null
    let mutable address: string = deviceAddress
    let mutable writeValue: obj option = None

    interface IPlcTag with
        member x.Name with get() = address 
        member x.DataType = mxTagInfo.DataTypeSize
        member x.Comment with get() = "" 
    interface IPlcTagReadWrite with
        member x.Address with get() = address 
        member x.Value with get() = currentValue and set(v) = currentValue <- v
        member x.SetWriteValue(value: obj) = writeValue <- Some value
        member x.ClearWriteValue() = writeValue <- None
        member x.GetWriteValue() = writeValue

    member x.Value = currentValue
    member x.Address = address
    member x.DataType = mxTagInfo.DataTypeSize
    member x.Device = mxTagInfo.Device 
    member x.DeviceText = 
                if mxTagInfo.Device = DX then "X"
                else if mxTagInfo.Device = DY then "Y"
                else
                    $"{mxTagInfo.Device}"

    member x.IsDotBit = address.Contains(".")

    member x.WordTag =
        let offset = mxTagInfo.BitOffset / 16
           
        if x.DataType = Bit && not(x.IsDotBit) then
            if x.Device.IsHexa
            then $"K4{x.DeviceText}{(offset*16):X}" 
            else $"K4{x.DeviceText}{offset*16}"
        else
             if x.Device.IsHexa 
             then $"{x.DeviceText}{(offset):X}" 
             else $"{x.DeviceText}{(offset)}" 

    member x.UpdateValue(data: int16) : bool =
        let newValue =
            match x.DataType with
            | Bit -> (data &&& (1s <<< mxTagInfo.BitOffset % 16) <> 0s) :> obj
            | Word -> data :> obj 
            | _ -> failwith "Unsupported data type"

        if currentValue = null || currentValue <> newValue then
            currentValue <- newValue
            true
        else false
