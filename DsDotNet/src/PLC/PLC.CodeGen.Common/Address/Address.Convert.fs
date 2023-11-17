
[<AutoOpen>]
module AddressPattern

open System.Text.RegularExpressions
open Dual.Common.Core.FS

let subBitPattern (size: int) (str: string) =
    match System.Int32.TryParse(str) with
    | true, v when v < size -> Some(v)
    | _ -> None

let (|ByteSubBitPattern|_|) = subBitPattern 8
let (|WordSubBitPattern|_|) = subBitPattern 16
let (|DWordSubBitPattern|_|) = subBitPattern 32
let (|LWordSubBitPattern|_|) = subBitPattern 64
let (|DevicePattern|_|) (str: string) = DU.fromString<DeviceType> str
let (|DataTypePattern|_|) (str:string)=
    try
        Some <|  str.FromDeviceMnemonic()
    with exn ->
        None

let createTagInfo = LsTagInfo.Create >> Some
let (|LsTagPatternFEnet|_|) ((modelId: int option), (tag: string)) =
    match tag with
    | RegexPattern @"^%([IQU])X(\d+)\.(\d+)\.(\d+)$" [ DevicePattern device; Int32Pattern file; WordSubBitPattern element; LWordSubBitPattern bit ] ->
        let baseStep, slotStep = if device.Equals(DeviceType.U) then 512 * 16, 512 else 64 * 16, 64
        let totalBitOffset = file * baseStep + element * slotStep + bit
        createTagInfo (tag, device, DataType.Bit, totalBitOffset, modelId)
    | RegexPattern @"^%([PMLKFNRAWIQUSTCZD])([BWDL])(\d+)$" [ DevicePattern device; DataTypePattern dataType; Int32Pattern offset ] ->
        let byteOffset = offset * dataType.GetByteLength()
        let totalBitOffset = byteOffset * 8
        createTagInfo (tag, device, dataType, totalBitOffset, modelId)
    | RegexPattern @"^%([IQU])([BWDL])(\d+)\.(\d+)\.(\d+)$" [ DevicePattern device; DataTypePattern dataType; Int32Pattern file; Int32Pattern element; Int32Pattern bit ] ->
        let uMemStep = if device.Equals(DeviceType.U) then 8 else 1
        let bitStandard = 8 * uMemStep / dataType.GetByteLength()

        let bitSet = (bit % bitStandard) * dataType.GetByteLength() * 8
        let elementSet = (element % 16 + bit / bitStandard) * 8 * 8 * uMemStep
        let fileSet = (file + element / 16) * 8 * 8 * 16 * uMemStep

        let offset = bitSet + elementSet + fileSet
        createTagInfo (tag, device, dataType, offset, modelId)
    | _ -> logWarn $"Failed to parse tag : {tag}"; None

let tryParseTag tag = (|LsTagPatternFEnet|_|) (None, tag)
let tryParseTagByCpu (tag: string) (modelId: int) = (|LsTagPatternFEnet|_|) (modelId |> Some, tag)
