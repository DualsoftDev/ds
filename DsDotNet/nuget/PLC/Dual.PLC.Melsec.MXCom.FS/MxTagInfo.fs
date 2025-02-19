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

    interface ITagPLC with
        member x.Address with get() = address 
        member x.Value with get() = currentValue and set(v) = currentValue <- v
        member x.SetWriteValue(value: obj) = writeValue <- Some value
        member x.ClearWriteValue() = writeValue <- None
        member x.GetWriteValue() = writeValue

    member x.Value = currentValue
    member x.Address = address
    member x.DataType = mxTagInfo.DataTypeSize
    member x.Device = mxTagInfo.Device

    member x.WordTag =
        let offset = 
            if x.Device.IsHexa 
            then $"{(mxTagInfo.BitOffset / 16):X}" 
            else $"{(mxTagInfo.BitOffset / 16)}" 

        if x.DataType = MxBit then
            $"K4{x.Device}{offset}"
        else
            $"{x.Device}{offset}"

    member x.UpdateValue(data: int16) : bool =
        let newValue =
            match x.DataType with
            | MxBit -> (data &&& (1s <<< mxTagInfo.BitOffset % 16) <> 0s) :> obj
            | MxWord -> data :> obj

        if currentValue <> newValue then
            currentValue <- newValue
            true
        else false
