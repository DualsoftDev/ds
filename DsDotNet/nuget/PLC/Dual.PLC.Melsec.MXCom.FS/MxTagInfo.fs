namespace DsMxComm

open System
open Dual.Common.Core.FS
open Dual.PLC.Common.FS

[<AutoOpen>]
// MxTag 클래스
type MxTag(deviceAddress: string, dataTypeSize: MxDeviceType, bitOffset: int) =
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
    member x.DataType = dataTypeSize
    member x.Device = 
        match tryParseMxTag deviceAddress with
        | Some (dev, _, _) -> dev
        | None -> failwith "Invalid device address"

    member x.BitOffset = bitOffset
    
    member x.WordTag =
        if x.DataType = MxBit then
            $"K4{x.Device}{(x.BitOffset / 16):X}"
        else
            $"{x.Device}{x.BitOffset / 16}"

    member val WordOffset = -1 with get, set

    member x.MemType = if dataTypeSize = MxBit then 'X' else 'B'
    member x.Size = if dataTypeSize = MxBit then x.BitOffset % 8 else dataTypeSize.Size / 8

    member x.UpdateValue(data: int16) : bool =
        let newValue =
            match x.DataType with
            | MxBit -> (data &&& (1s <<< x.BitOffset % 16) <> 0s) :> obj
            | MxWord -> data :> obj

        if currentValue <> newValue then
            currentValue <- newValue
            true
        else false
