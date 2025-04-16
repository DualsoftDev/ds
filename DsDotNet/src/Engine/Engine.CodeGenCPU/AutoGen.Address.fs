namespace Engine.CodeGenCPU

open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol

[<AutoOpen>]
module DsAddressModule =


    let mutable inDigitCnt = 0
    let mutable inAnalogCnt = 0
    let mutable outDigitByteCnt = 0
    let mutable outAnalogByteCnt = 0
    let mutable memoryCnt = InitStartMemory
    let setMemoryIndex(index:int) = memoryCnt <- index;
    let getCurrentMemoryIndex() = memoryCnt;
    let InitializeIOMemoryIndex () =
                memoryCnt <- InitStartMemory
                inDigitCnt <- 0
                inAnalogCnt <- 0
                outDigitByteCnt <- 0
                outAnalogByteCnt <- 0


    let emptyToSkipAddress address = if address = TextAddrEmpty then TextNotUsed else address.Trim().ToUpper()
    let getPCIOMTextBySize (device:string, offset: int, bitSize:int) : string =
            match bitSize with
            | 1 -> $"{device}X{offset}"
            | 8 -> $"{device}B{offset/8}.{offset % 8}"
            | 16 -> $"{device}W{offset/16}.{offset % 16}"
            | 32 -> $"{device}D{offset/32}.{offset % 32}"
            | 64 -> $"{device}L{offset/64}.{offset % 64}"
            | _ -> failwithf $"Invalid size :{bitSize}"

    let getStartPointXGK(index:int, hwSlotDataTypes:SlotDataType[]) =
        hwSlotDataTypes
            |> Seq.filter(fun (s) -> s.SlotIndex < index)
            |> Seq.map(fun (s) ->

                match s.IOType with
                | NotUsed  ->  16 //기본 더미 Size
                | In | Out ->
                    match s.DataType with
                    | DuUINT8 | DuUINT16  -> 16 //기본 더미 Size
                    | DuUINT32  -> 32
                    | DuUINT64  -> 64
                    | _   -> failwithf $"{s.DataType} not support"
                | _   -> failwithf $"{s.IOType} not support"
            ) |>  Seq.sum

    let getUsedPointXGK(index:int, usedType:IOType, hwSlotDataTypes:SlotDataType[]) =
        hwSlotDataTypes
            |> Seq.filter(fun (s) -> s.SlotIndex < index && usedType = s.IOType)
            |> Seq.map(fun (s) ->
                    match s.DataType with
                    | DuUINT8 | DuUINT16  -> 16 //기본 더미 Size
                    | DuUINT32  -> 32
                    | DuUINT64  -> 64
                    | _   -> failwithf $"{s.DataType} not support"
            ) |>  Seq.sum



    let getDuDataType (plcDataSizeType: PlcDataSizeType) : DataType =
        match plcDataSizeType with
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
        | PlcDataSizeType.DateTime -> DuUINT64 // 또는 DuSTRING 등 타임 표현 방식에 따라
        | PlcDataSizeType.UserDefined -> failwithf "PlcDataSizeType.UserDefined not exist DuDataType"



        
    let getXgiIOTextBySize (device:string, offset: int, bitSize:int, iSlot:int, sumBit:int) : string =
        if bitSize = 1
        then $"%%{device}X0.{iSlot}.{(offset-sumBit) % 64}"  //test ahn 아날로그 base 1로 일단 고정
        else
            match bitSize with  //test ahn  xgi 규격확인
            | 8 -> $"%%{device}B{offset+1024}"
            | 16 -> $"%%{device}W{offset+1024}"
            | 32 -> $"%%{device}D{offset+1024}"
            | 64 -> $"%%{device}L{offset+1024}"
            | _ -> failwithf $"Invalid size :{bitSize}"


    let getXgiMemoryTextBySize (device:string, offset: int, bitSize:int) : string =
        if bitSize = 1
        then $"%%{device}X{offset}"
        else
            if offset%8 = 0 
            then getXgiIOTextBySize (device, offset, bitSize, 0, 0)
            else failwithf $"Word Address는 8의 배수여야 합니다. {offset}"

    let matchPlcDataSizeType (plcType: PlcDataSizeType, duType: DataType) : bool =
        getDuDataType plcType = duType 


    let getValidAddress (addr: string, dataType: DsDataType.DataType, name: string, isSkip: bool, ioType:IOType, target:HwTarget) =
        let cpu, driver, hwSlotDataTypes = target.PlatformTarget, target.HwIO, target.Slots
        let addr =
            if addr.IsNullOrEmpty() then
                failwithf $"주소가 없습니다. {name} \n 인터페이스 생략시 '-' 입력필요"
            else
                if isSkip then
                    emptyToSkipAddress  addr
                else
                    addr

        let addr =  addr.Trim().ToUpper()
        let getCurrent(curr:int)(bitSize:int) =
            if curr%bitSize = 0 then
                curr
            else
                curr + bitSize - (curr%bitSize)

        let getNext (curr:int)(bitSize:int) =
            if curr%bitSize = 0 then
                curr + bitSize
            else
                curr + bitSize - (curr%bitSize) + bitSize

        let sizeBit =
            if cpu = PlatformTarget.XGI then
                dataType.ToBitSize()
            elif cpu = PlatformTarget.XGK then
                match dataType.ToBitSize() with
                | 1 -> 1
                | 8 -> 16
                | 16 -> 16
                | 32 -> 32
                | _ -> failwithf $"XGK {dataType} not support"
            else
                dataType.ToBitSize()

        let newAddr =
            if addr = TextAddrEmpty && not(isSkip) then
                let cnt =
                    match ioType with
                    | In      ->
                        if sizeBit = 1
                        then
                            let ret = getCurrent inDigitCnt sizeBit
                            inDigitCnt <- getNext inDigitCnt sizeBit ; ret
                        else
                            if XGK = cpu then
                                let ret = inAnalogCnt
                                inAnalogCnt <- inAnalogCnt+sizeBit/8 ; ret
                            else
                                let ret = inAnalogCnt
                                inAnalogCnt <- inAnalogCnt+1 ; ret

                    | Out     ->
                        if sizeBit = 1
                        then
                            let ret = getCurrent outDigitByteCnt sizeBit
                            outDigitByteCnt <- getNext outDigitByteCnt sizeBit ; ret
                        else
                            if XGK = cpu then
                                let ret = outAnalogByteCnt
                                outAnalogByteCnt <- outAnalogByteCnt+sizeBit/8 ; ret
                            else
                                let ret = outAnalogByteCnt
                                outAnalogByteCnt <- outAnalogByteCnt+1 ; ret

                    | Memory  -> let ret = getCurrent memoryCnt sizeBit
                                 memoryCnt <- getNext memoryCnt sizeBit; ret
                    | NotUsed -> failwithf $"{ioType} not support"

                let getSlotInfoNonIEC(settingType: IOType, newCnt: int) =
                    let filterSlotsByType =
                        hwSlotDataTypes
                        |> Seq.filter(fun (s) -> s.IOType = settingType)

                    let calculateAssignedUpToIndex currIndex =
                        filterSlotsByType
                        |> Seq.filter(fun (s) -> s.SlotIndex <= currIndex)
                        |> Seq.fold (fun acc (s) -> acc + fst (s.DataType.ToBlockSizeNText())) 0

                    let findAvailableSlots =
                        filterSlotsByType
                        |> Seq.tryFind(fun (s) -> s.SlotIndex < calculateAssignedUpToIndex s.SlotIndex)

                    match findAvailableSlots with
                    | Some (s) ->
                        let startPoint = getStartPointXGK (s.SlotIndex, hwSlotDataTypes)
                        if s.SlotIndex = 0 then
                            newCnt
                        else
                            let usedPoint = getUsedPointXGK (s.SlotIndex, settingType, hwSlotDataTypes)
                            startPoint + (newCnt - usedPoint)
                    | None -> failwithf "%AType 슬롯이 부족합니다." settingType


                let getSlotInfoIEC(settingType: IOType, newCnt:int) =
                    let curr =
                        match settingType with
                        | In -> newCnt
                        | Out -> newCnt
                        | Memory -> failwithf $"{settingType} not supported"
                        | NotUsed -> failwithf $"{settingType} not supported"

                    let assigned(currIndex: int) =
                        let sameTypeSlots = hwSlotDataTypes
                                            |> Seq.filter(fun (s) -> s.IOType = settingType)
                                            |> Seq.filter(fun (s) -> s.SlotIndex <= currIndex)

                        if sameTypeSlots.IsEmpty() then 0
                        else
                            sameTypeSlots
                            |> Seq.sumBy(fun (s) -> (s.DataType.ToBlockSizeNText() |> fst))

                    let slotSpares = hwSlotDataTypes
                                        |> Seq.filter(fun (s) -> s.IOType = settingType)
                                        |> Seq.filter(fun (s) -> curr < assigned s.SlotIndex)

                    match Seq.tryHead slotSpares with
                    | Some (s) -> (s.SlotIndex, assigned (s.SlotIndex - 1))
                    | None ->  failwithf $"{settingType}Type 슬롯이 부족합니다.Add I/O table 세팅필요"

                match driver with
                | OPC_IO ->
                    let getPCIOM(head:string, offsetBit) =
                        match sizeBit with
                        |  1 -> $"{head}B{offsetBit /  8}.{offsetBit % 8}"
                        |  8 -> $"{head}B{offsetBit /  8}"
                        | 16 -> $"{head}W{offsetBit / 16}"
                        | 32 -> $"{head}D{offsetBit / 32}"
                        | 64 -> $"{head}L{offsetBit / 64}"
                        | _ -> failwithf $"Invalid size :{sizeBit}"

                    match ioType with
                    | In      ->  if sizeBit = 1 then getPCIOM ("I", cnt) else getPCIOM ("I", cnt*8)
                    | Out     ->  if sizeBit = 1 then getPCIOM ("O", cnt) else getPCIOM ("O", cnt*8)
                    | Memory  ->  if sizeBit = 1 then getPCIOM ("M", cnt) else getPCIOM ("M", cnt*8)
                    | NotUsed -> failwithf $"{ioType} not support {name}"

                | LS_XGK_IO |  LS_XGI_IO ->
                    let isBool = dataType = DuBOOL
                    match ioType with
                    | (In | Out) ->
                        let iSlot, sumBit =  getSlotInfoIEC(ioType, cnt)

                        match driver with
                        | LS_XGI_IO ->
                            let io =
                                match ioType with
                                | IOType.In -> "I"
                                | IOType.Out -> "Q"
                                | _ -> failwithf $"Error {target} not support {name}"

                            getXgiIOTextBySize(io, cnt ,sizeBit, iSlot, sumBit)

                        | LS_XGK_IO ->
                            if isBool then
                                LsXgkTagParser.ParseValidText($"P{getSlotInfoNonIEC(ioType, cnt)}", isBool)
                            else
                                match ioType with
                                | IOType.In ->
                                    LsXgkTagParser.ParseValidText($"P{cnt+XGKAnalogOffsetByte}", isBool)
                                | IOType.Out ->
                                    LsXgkTagParser.ParseValidText($"P{cnt+XGKAnalogOffsetByte+XGKAnalogOutOffsetByte}", isBool)
                                | _ ->
                                    failwithf $"Error {target} not support {name}"

                        //| PlatformTarget.WINDOWS ->
                        //    let io = if ioType = IOType.In then "I" else "O"
                        //    getPCIOMTextBySize(io, cnt ,sizeBit)
                        | _ ->
                            failwithf $"Error {target} not support {name}"

                    | Memory ->
                        match driver with
                        | LS_XGI_IO ->
                            getXgiMemoryTextBySize("M", cnt ,sizeBit)
                        | LS_XGK_IO ->
                            LsXgkTagParser.ParseAddress("M", cnt, isBool)
                        //| PlatformTarget.WINDOWS ->
                        //    getPCIOMTextBySize("M", cnt ,sizeBit)
                        | _ ->
                            failwithf $"Error{name} {target} not support"

                    | NotUsed -> failwithf $"{ioType} not support {name}"

                | _ -> TextAddrEmpty

            elif addr <> TextNotUsed && isSkip then
                 failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
            elif addr = TextNotUsed then TextNotUsed

            else
                if cpu = XGK || (cpu = WINDOWS && driver = LS_XGK_IO)
                then
                
                    let addressXGK = LsXgkTagParser.ParseValidText(addr, (dataType = DuBOOL))

                    if addressXGK.IsNonNull()
                    then 
                        addressXGK
                    else 
                        failwithf $"XGK 주소가 잘못되었습니다.{name} {addr} (dataType:{dataType})"
                
                elif cpu = XGI || (cpu = WINDOWS && driver = LS_XGI_IO)
                then
                    match tryParseXgiTag (addr) with
                    | Some (_) ->  addr
                    | _ ->  failwithf $"XGI 주소가 잘못되었습니다.{name} {addr} (dataType:{dataType})"
                else 
                    addr

        newAddr


    let getValidAddressUsingPlatform (addr: string, dataType: DsDataType.DataType, name: string, isSkip: bool, ioType:IOType, platformTarget:PlatformTarget) =
        let hwTarget =
            let slot = getFullSlotHwSlotDataTypes()
            match platformTarget with
            | WINDOWS -> HwTarget(WINDOWS, OPC_IO, slot)
            | XGI -> HwTarget(XGI, LS_XGI_IO, slot)
            | XGK -> HwTarget(XGK, LS_XGK_IO, slot)

        getValidAddress(addr, dataType, name, isSkip, ioType, hwTarget)
     
    let private getValidHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) (target:HwTarget)=
        let inAddr  = getValidAddress(hwItem.InAddress,  hwItem.InDataType,  hwItem.Name, skipIn,  IOType.Memory, target)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.OutDataType, hwItem.Name, skipOut, IOType.Memory, target)
        inAddr, outAddr

    let updateHwAddress (hwItem: HwSystemDef) (inAddr, outAddr) (target:HwTarget)   =
        hwItem.InAddress <- inAddr
        hwItem.OutAddress <- outAddr

        let inA, outA =
            match hwItem with
            | :? ConditionDef as c -> getValidHwItem c  false true  target
            | :? ActionDef as a    -> getValidHwItem a  true false  target
            | :? ButtonDef as b    -> getValidHwItem b  false false target
            | :? LampDef as l      -> getValidHwItem l  true  false target
            | _ -> failWithLog $"Error {hwItem.Name} not support"

        hwItem.InAddress <- inA
        hwItem.OutAddress <- outA

    let assignAutoAddress (sys: DsSystem, startMemory:int, offsetOpModeLampBtn:int, target:HwTarget) =
        setMemoryIndex(startMemory);

        for b in sys.HWButtons do
            let inA = if b.InAddress = "" then TextAddrEmpty else b.InAddress
            let outA = TextNotUsed
            updateHwAddress b (inA, outA)  target

        for l in sys.HWLamps do
            let inA = TextNotUsed
            let outA = if l.OutAddress = "" then TextAddrEmpty else l.OutAddress
            updateHwAddress l (inA, outA)  target

        for c in sys.HWConditions do
            let inA = if c.InAddress = "" then TextAddrEmpty else c.InAddress
            let outA = TextNotUsed
            updateHwAddress c (inA, outA)  target

        for ce in sys.HWActions do
            let inA = TextNotUsed
            let outA = if ce.OutAddress = "" then TextAddrEmpty else ce.OutAddress
            updateHwAddress ce (inA, outA)  target

        let devsJob =  sys.GetTaskDevsWithoutSkipAddress()
        let mutable extCnt = 0
        for g in devsJob.GroupBy(fun (_dev, job) -> job) do
            g |> Seq.iteri(fun i (dev, job) ->

                let inSkip, outSkip = getSkipInfo(i, job)

                dev.InAddress  <- getValidAddress(dev.InAddress,  dev.InDataType,  dev.QualifiedName, inSkip,  IOType.In, target)
                dev.OutAddress <- getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target)

                if dev.IsRootOnlyDevice
                then
                    if dev.InAddress = TextAddrEmpty && not(inSkip) then
                        dev.InAddress  <-  getExternalTempMemory(target, extCnt)
                        extCnt <- extCnt+1

                    dev.OutAddress <- TextNotUsed
                    )

        setMemoryIndex(startMemory + offsetOpModeLampBtn);

