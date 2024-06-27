namespace Engine.CodeGenCPU

open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open PLC.CodeGen.Common


[<AutoOpen>]
module DsAddressModule =
   
 
    let mutable inCnt = 0
    let mutable outCnt = 0
    let mutable memoryCnt = InitStartMemory
    let setMemoryIndex(index:int) = memoryCnt <- index;
    let getCurrentMemoryIndex() = memoryCnt;
    let InitializeIOMemoryIndex () =
                memoryCnt <- InitStartMemory
                inCnt <- 0
                outCnt <- 0

    let emptyToSkipAddress address = if address = TextAddrEmpty then TextSkip else address.Trim().ToUpper()

   
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


    let getValidAddress (addr: string, dataType: DataType, name: string, isSkip: bool, ioType:IOType, target:PlatformTarget) =

        

        let addr = if addr.IsNullOrEmpty()
                    then failwithf $"주소가 없습니다. {name} \n 인터페이스 생략시 '-' 입력필요"  
                    else
                        if isSkip then 
                            emptyToSkipAddress  addr
                        else 
                            addr

        let addr =  addr.Trim().ToUpper()
        let getCurrent(curr:int)(bitSize:int) = 
            if curr%bitSize = 0 then curr
            else
                curr + bitSize - (curr%bitSize)

        let getNext (curr:int)(bitSize:int) = 
            if curr%bitSize = 0 then  curr + bitSize
            else
                curr + bitSize - (curr%bitSize) + bitSize


        let sizeBit = 
                if target = PlatformTarget.XGI then dataType.ToPLCBitSize()
                else
                    match dataType.ToPLCBitSize() with
                    | 1 -> 1
                    | 8 -> 16
                    | 16 -> 16
                    | 32 -> 32
                    | _ -> failwithf $"XGK {dataType} not support"
        let newAddr =
            if addr = TextAddrEmpty && not(isSkip)
            then
                let cnt =
                    

                    match ioType with 
                    | In      -> let ret = getCurrent inCnt sizeBit
                                 inCnt <- getNext inCnt sizeBit ; ret
                    | Out     -> let ret = getCurrent outCnt sizeBit 
                                 outCnt <- getNext outCnt sizeBit   ;ret
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
                        if i = 0 then newCnt
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


                if RuntimeDS.Package.IsPCorPCSIM() 
                then 
                    match ioType with 
                    |In ->  $"IB{cnt / 8}.{cnt % 8}" 
                    |Out -> $"OB{cnt / 8}.{cnt % 8}" 
                    |Memory -> $"M{memoryCnt}"
                    |NotUsed -> failwithf $"{ioType} not support"

                elif RuntimeDS.Package.IsPLCorPLCSIM()
                then
                    match ioType with 
                    |In |Out -> 
                        let iSlot, sumBit =  getSlotInfoIEC(ioType, cnt)

                        if target = PlatformTarget.XGI && ioType = IOType.In
                        then
                            getXgiIOTextBySize("I", cnt ,sizeBit, iSlot, sumBit)
                        elif target = PlatformTarget.XGI  && ioType = IOType.Out 
                        then
                            getXgiIOTextBySize("Q", cnt ,sizeBit, iSlot, sumBit)
                        elif target = PlatformTarget.XGK
                        then
                            getXgkTextByType("P", getSlotInfoNonIEC(ioType, cnt), dataType = DuBOOL)
                        else failwithf $"Error {target} not support"

                    |Memory -> if target = PlatformTarget.XGI
                               then 
                                    getXgiMemoryTextBySize("M", cnt ,sizeBit)
                               elif target = PlatformTarget.XGK
                               then
                                    getXgkTextByType("M", cnt, dataType = DuBOOL)
                               else failwithf $"Error {target} not support"


                    |NotUsed -> failwithf $"{ioType} not support"

                else TextAddrEmpty

            elif addr <> TextSkip && isSkip then
                 failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
            elif addr = TextSkip then TextSkip 
                
            else
                if target = PlatformTarget.XGK
                then
                    match tryParseXGKTagByBitType addr (dataType = DuBOOL) with
                    | Some (t) -> t |> getXgKTextByTag
                                  
                    | _ -> failwithf $"주소가 잘못되었습니다. {addr} (dataType:{dataType})"
                else addr
     
        newAddr

  
    let private getValidHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) target=
        let inAddr = getValidAddress(hwItem.InAddress, hwItem.InParam.Type, hwItem.Name, skipIn, IOType.Memory, target)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.OutParam.Type , hwItem.Name, skipOut, IOType.Memory, target)
        inAddr, outAddr

    let updateHwAddress (hwItem: HwSystemDef) (inAddr, outAddr) target   =
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

    let assignAutoAddress (sys: DsSystem, startMemory:int, offsetOpModeLampBtn: int) target =
        
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
            
        let vs = sys.GetVerticesOfCoins()
        let mutable extCnt = 0
        for job in sys.Jobs do
            job.DeviceDefs |> Seq.iteri(fun i dev ->
                let inSkip = job.JobParam.JobMulti.AddressInCount > i |>not
                let outSkip = job.JobParam.JobMulti.AddressOutCount > i |>not
                dev.InAddress  <- getValidAddress(dev.InAddress, dev.InDataType, dev.QualifiedName, inSkip,  IOType.In, target)
                dev.OutAddress <- getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target)
                    
                let coins = vs.GetVerticesOfJobCoins(job)
                if dev.IsRootFlowDev(coins) 
                then
                    if dev.InAddress = TextAddrEmpty && not(inSkip)
                    then
                        dev.InAddress  <-  getExternalTempMemory(target, extCnt)
                        extCnt <- extCnt+1

                    dev.OutAddress <- TextSkip
            )

        setMemoryIndex(startMemory + offsetOpModeLampBtn);

