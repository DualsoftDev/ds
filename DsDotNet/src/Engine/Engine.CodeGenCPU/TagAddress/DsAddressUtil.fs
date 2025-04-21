namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol
open Engine.Core

module DsAddressUtil =

    let emptyToSkipAddress address =
        if address = TextAddrEmpty then TextNotUsed else address.Trim().ToUpper()

    let getDuDataType = function
        | PlcDataSizeType.Boolean -> DuBOOL
        | PlcDataSizeType.Byte    -> DuUINT8
        | PlcDataSizeType.SByte   -> DuINT8
        | PlcDataSizeType.Int16   -> DuINT16
        | PlcDataSizeType.UInt16  -> DuUINT16
        | PlcDataSizeType.Int32   -> DuINT32
        | PlcDataSizeType.UInt32  -> DuUINT32
        | PlcDataSizeType.Int64   -> DuINT64
        | PlcDataSizeType.UInt64  -> DuUINT64
        | PlcDataSizeType.Float   -> DuFLOAT32
        | PlcDataSizeType.Double  -> DuFLOAT64
        | PlcDataSizeType.String  -> DuSTRING
        | PlcDataSizeType.DateTime -> DuUINT64
        | PlcDataSizeType.UserDefined -> failwith "UserDefined는 지원되지 않습니다."

    
    let matchPlcDataSizeType (plcType, duType) =
        getDuDataType plcType = duType

    let getPCIOMTextBySize (device, offset, bitSize) =
        match bitSize with
        | 1   -> $"{device}X{offset}"
        | 8   -> $"{device}B{offset / 8}.{offset % 8}"
        | 16  -> $"{device}W{offset / 16}.{offset % 16}"
        | 32  -> $"{device}D{offset / 32}.{offset % 32}"
        | 64  -> $"{device}L{offset / 64}.{offset % 64}"
        | _   -> failwith $"잘못된 비트 크기: {bitSize}"


    let getStartPointXGK (index, hwSlotDataTypes:SlotDataType seq) =
        hwSlotDataTypes
        |> Seq.filter (fun s -> s.SlotIndex < index)
        |> Seq.sumBy (fun s ->
            match s.IOType, s.DataType with
            | NotUsed, _ -> 16
            | (In | Out), DuUINT32 -> 32
            | (In | Out), DuUINT64 -> 64
            | (In | Out), _         -> 16
            | _ -> failwith $"{s.IOType} 또는 {s.DataType} 지원 안됨")



    let getUsedPointXGK (index, usedType, hwSlotDataTypes:SlotDataType seq) =
        hwSlotDataTypes
        |> Seq.filter (fun s -> s.SlotIndex < index && s.IOType = usedType)
        |> Seq.sumBy (fun s ->
            match s.DataType with
            | DuUINT32 -> 32
            | DuUINT64 -> 64
            | _        -> 16)

    let getSlotInfoNonIEC (settingType, newCnt, hwSlotDataTypes:SlotDataType seq) =
        let filterByType = hwSlotDataTypes |> Seq.filter (fun s -> s.IOType = settingType)
        let assigned i = filterByType |> Seq.filter (fun s -> s.SlotIndex <= i) |> Seq.sumBy (fun s -> fst (s.DataType.ToBlockSizeNText()))
        filterByType
        |> Seq.tryFind (fun s -> s.SlotIndex < assigned s.SlotIndex)
        |> function
        | Some s ->
            let startPoint = getStartPointXGK (s.SlotIndex, hwSlotDataTypes)
            let used = getUsedPointXGK (s.SlotIndex, settingType, hwSlotDataTypes)
            startPoint + (newCnt - used)
        | None -> failwith $"{settingType}Type 슬롯이 부족합니다."

    let getSlotInfoIEC (settingType, newCnt, hwSlotDataTypes:SlotDataType seq) =
        let assigned i =
            hwSlotDataTypes
            |> Seq.filter (fun s -> s.IOType = settingType && s.SlotIndex <= i)
            |> Seq.sumBy (fun s -> fst (s.DataType.ToBlockSizeNText()))

        hwSlotDataTypes
        |> Seq.filter (fun s -> s.IOType = settingType && newCnt < assigned s.SlotIndex)
        |> Seq.tryHead
        |> function
        | Some s -> s.SlotIndex, assigned (s.SlotIndex - 1)
        | None -> failwith $"{settingType}Type 슬롯이 부족합니다. 주소 할당 필요."
