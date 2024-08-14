namespace Engine.CodeGenCPU

open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.Common


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


    let emptyToSkipAddress address = if address = TextAddrEmpty then TextSkip else address.Trim().ToUpper()
    let getPCIOMTextBySize (device:string, offset: int, bitSize:int) : string =
            match bitSize with  
            | 1 -> $"{device}X{offset}" 
            | 8 -> $"{device}B{offset/8}.{offset % 8}" 
            | 16 -> $"{device}W{offset/16}.{offset % 16}" 
            | 32 -> $"{device}D{offset/32}.{offset % 32}" 
            | 64 -> $"{device}L{offset/64}.{offset % 64}" 
            | _ -> failwithf $"Invalid size :{bitSize}"

    let getStartPointXGK(index:int) =
        RuntimeDS.HwSlotDataTypes
            |> Seq.filter(fun (i, _, _) -> i < index)
            |> Seq.map(fun (_, dtype, dSzie) ->
                
                match dtype with
                | NotUsed  ->  16 //기본 더미 Size
                | In | Out ->  
                    match dSzie with
                    | DuUINT8 | DuUINT16  -> 16 //기본 더미 Size
                    | DuUINT32  -> 32
                    | DuUINT64  -> 64
                    | _   -> failwithf $"{dSzie} not support"
                | _   -> failwithf $"{dtype} not support"
            ) |>  Seq.sum

    let getUsedPointXGK(index:int, usedType:IOType) =
        RuntimeDS.HwSlotDataTypes
            |> Seq.filter(fun (i, dtype, _) -> i < index && usedType = dtype)
            |> Seq.map(fun (_, _, dSzie) ->
                    match dSzie with
                    | DuUINT8 | DuUINT16  -> 16 //기본 더미 Size
                    | DuUINT32  -> 32
                    | DuUINT64  -> 64
                    | _   -> failwithf $"{dSzie} not support"
            ) |>  Seq.sum




    let getValidAddress (addr: string, dataType: DataType, name: string, isSkip: bool, ioType:IOType, target:HwTarget) =

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

        let cpu, driver = target
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
                        RuntimeDS.HwSlotDataTypes 
                        |> Seq.filter(fun (_, ioType, _) -> ioType = settingType)

                    let calculateAssignedUpToIndex currIndex =
                        filterSlotsByType 
                        |> Seq.filter(fun (i, _, _) -> i <= currIndex)
                        |> Seq.fold (fun acc (_, _, data) -> acc + fst (data.ToBlockSizeNText())) 0

                    let findAvailableSlots =
                        filterSlotsByType 
                        |> Seq.tryFind(fun (i, _, _) -> newCnt < calculateAssignedUpToIndex i)

                    match findAvailableSlots with
                    | Some (i, _, _) -> 
                        let startPoint = getStartPointXGK (i)
                        if i = 0 then
                            newCnt
                        else 
                            let usedPoint = getUsedPointXGK (i, settingType)
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
                        let sameTypeSlots = RuntimeDS.HwSlotDataTypes 
                                            |> Seq.filter(fun (_, ioType, _) -> ioType = settingType)
                                            |> Seq.filter(fun (i, _, _) -> i <= currIndex)

                        if sameTypeSlots.IsEmpty() then 0
                        else 
                            sameTypeSlots
                            |> Seq.sumBy(fun (_, _, data) -> (data.ToBlockSizeNText() |> fst))

                    let slotSpares = RuntimeDS.HwSlotDataTypes 
                                        |> Seq.filter(fun (_, ioType, _) -> ioType = settingType)
                                        |> Seq.filter(fun (i, _, _) -> curr < assigned i)

                    match Seq.tryHead slotSpares with
                    | Some (i, _, _) -> (i, assigned (i - 1))
                    | None ->  failwithf $"{settingType}Type 슬롯이 부족합니다."

                match target with
                | WINDOWS, PAIX_IO ->
                    let getPCIOM(head:string, offsetBit) =
                        match sizeBit with
                        | 1 -> $"{head}B{offsetBit / 8}.{offsetBit % 8}" 
                        | 8 -> $"{head}B{offsetBit  / 8}"
                        | 16 -> $"{head}W{offsetBit / 16}"
                        | 32 -> $"{head}D{offsetBit / 32}"
                        | 64 -> $"{head}L{offsetBit / 64}"
                        | _ -> failwithf $"Invalid size :{sizeBit}"

                    match ioType with 
                    | In      ->  if sizeBit = 1 then getPCIOM ("I", cnt) else getPCIOM ("I", cnt*8)
                    | Out     ->  if sizeBit = 1 then getPCIOM ("O", cnt) else getPCIOM ("O", cnt*8)
                    | Memory  ->  if sizeBit = 1 then getPCIOM ("M", cnt) else getPCIOM ("M", cnt*8)
                    | NotUsed -> failwithf $"{ioType} not support {name}"

                | (WINDOWS, LS_XGK_IO | WINDOWS, LS_XGI_IO)
                | (XGK, _ | XGI, _) ->
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
                            let isBool = dataType = DuBOOL
                            if sizeBit = 1 then
                                getXgkTextByType("P", getSlotInfoNonIEC(ioType, cnt), isBool)
                            else 
                                match ioType with
                                | IOType.In ->
                                    getXgkTextByType("P", cnt+XGKAnalogOffsetByte, isBool)
                                | IOType.Out ->
                                    getXgkTextByType("P", cnt+XGKAnalogOffsetByte+XGKAnalogOutOffsetByte, isBool)  //test ahn 임시 Q 는  시프트 ??
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
                            getXgkTextByType("M", cnt, dataType = DuBOOL)
                        //| PlatformTarget.WINDOWS ->
                        //    getPCIOMTextBySize("M", cnt ,sizeBit)
                        | _ ->
                            failwithf $"Error{name} {target} not support"

                    | NotUsed -> failwithf $"{ioType} not support {name}"

                | _ -> TextAddrEmpty

            elif addr <> TextSkip && isSkip then
                 failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
            elif addr = TextSkip then TextSkip 
                
            else
                if cpu = XGK || (cpu = WINDOWS && driver = LS_XGK_IO)
                then
                    let xgkAddress (addr: string) =
                        if List.contains addr.[0] ['P'; 'M'; 'K'; 'F'] then
                            let padCnt = if dataType = DuBOOL then 5 else 4
                            addr.[0].ToString() + addr.Substring(1).PadLeft(padCnt, '0')
                        else
                            addr

                    match tryParseXGKTagByBitType (xgkAddress addr) with
                    | Some (t) -> t |> getXgKTextByTag
                                  
                    | _ ->  failwithf $"XGK 주소가 잘못되었습니다.{name} {addr} (dataType:{dataType})"
                else addr
     
        newAddr



  
    let private getValidHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) (target:HwTarget)=
        let inAddr = getValidAddress(hwItem.InAddress, hwItem.InDataType, hwItem.Name, skipIn, IOType.Memory, target)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.OutDataType , hwItem.Name, skipOut, IOType.Memory, target)
        inAddr, outAddr

    let updateHwAddress (hwItem: HwSystemDef) (inAddr, outAddr) (target:HwTarget)   =
        hwItem.InAddress <- inAddr
        hwItem.OutAddress <- outAddr

        let inA, outA = 
            match hwItem with
            | :? ConditionDef as c -> getValidHwItem c  false true target
            | :? ButtonDef as b -> getValidHwItem b  false false target
            | :? LampDef as l -> getValidHwItem l  true false target
            | _ -> failWithLog $"Error {hwItem.Name} not support"
            
        hwItem.InAddress <- inA
        hwItem.OutAddress <- outA

    let assignAutoAddress (sys: DsSystem, startMemory:int, offsetOpModeLampBtn: int) (target:HwTarget)=
        
        setMemoryIndex(startMemory);

        for b in sys.HWButtons do
            let inA = if b.InAddress = "" then TextAddrEmpty else b.InAddress 
            let outA = TextSkip
            updateHwAddress b (inA, outA)  target

        for l in sys.HWLamps do
            let inA = TextSkip
            let outA = if l.OutAddress = "" then TextAddrEmpty else l.OutAddress 
            updateHwAddress l (inA, outA)  target

        for c in sys.HWConditions do
            let inA = if c.InAddress = "" then TextAddrEmpty else c.InAddress 
            let outA = TextSkip
            updateHwAddress c (inA, outA)  target
            
        let devsJob =  sys.GetTaskDevsSkipEmptyAddress()
        let mutable extCnt = 0
        for dev, job in devsJob do
            let inSkip, outSkip = getSkipInfo(dev, job)

            dev.InAddress  <- getValidAddress(dev.InAddress,  dev.InDataType,  dev.QualifiedName, inSkip,  IOType.In, target)
            dev.OutAddress <- getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target)
           
            if dev.IsRootOnlyDevice 
            then
                if dev.InAddress = TextAddrEmpty && not(inSkip) then
                    dev.InAddress  <-  getExternalTempMemory(target, extCnt)
                    extCnt <- extCnt+1

                dev.OutAddress <- TextSkip
        
        setMemoryIndex(startMemory + offsetOpModeLampBtn);

