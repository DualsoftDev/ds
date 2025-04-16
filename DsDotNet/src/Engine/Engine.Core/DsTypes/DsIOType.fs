// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open Dual.Common.Core.FS
open System


[<AutoOpen>]
module IOTypeModule =
    
    [<Flags>]
    type IOType = 
    | In        //실제 센서 
    | Out       //실제 액츄에이터 
    | Memory    //모니터링 변수
    | NotUsed

    type SlotDataType(slotIndex:int, ioType:IOType, dataType:DataType) =
        member x.SlotIndex = slotIndex
        member x.IOType = ioType
        member x.DataType = dataType

        member x.ToText() = sprintf "%d %A %A" x.SlotIndex x.IOType (x.DataType.ToType().FullName)
        /// 문자열로부터 SlotDataType 생성
        static member Create(slotIndex: string, ioType: string, dataTypeText: string) =
            try
                let slotIndex = int slotIndex
                let ioType = 
                    match ioType with
                    | "In" -> IOType.In
                    | "Out" -> IOType.Out
                    | "NotUsed" -> IOType.NotUsed
                    | _ -> failwithf "Invalid IOType: %s" ioType

                let dataType = getDataTypeFromName(dataTypeText.Trim('"'))
                SlotDataType(slotIndex, ioType, dataType)
            with
            | _ as ex ->
                failwithf "Failed to create SlotDataType: %s" ex.Message
