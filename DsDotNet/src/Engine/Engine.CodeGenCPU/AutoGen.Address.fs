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
                    | DuUINT8   -> 8 
                    | DuUINT16  -> 16 //기본 더미 Size
                    | DuUINT32  -> 32
                    | DuUINT64  -> 64
                    | _   -> failwithf $"{dSzie} not support"
            ) |>  Seq.sum


    let getValidAddress (addr: string, name: string, isSkip: bool, ioType:IOType, target:PlatformTarget) =
        let iec = target = PlatformTarget.XGI

        let addr = if addr.IsNullOrEmpty()
                    then failwithf $"주소가 없습니다. {name} \n 인터페이스 생략시 '-' 입력필요"  
                    else
                        if isSkip then 
                            emptyToSkipAddress  addr
                        else 
                            addr

        let addr =  addr.Trim().ToUpper()

        let newAddr =
            if addr = TextAddrEmpty && not(isSkip)
            then
                let cnt =
                    match ioType with 
                    | In      -> let ret = inCnt
                                 inCnt <- inCnt + 1 ; ret
                    | Out     -> let ret =outCnt
                                 outCnt <- outCnt + 1    ;ret
                    | Memory  -> let ret =memoryCnt
                                 memoryCnt <- memoryCnt + 1; ret
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
                            let usedPoint = getUsedPointXGK (i-1, settingType)
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


                if RuntimeDS.Package.IsPackagePC() 
                then 
                    match ioType with 
                    |In ->  $"IB{cnt / 8}.{cnt % 8}" 
                    |Out -> $"OB{cnt / 8}.{cnt % 8}" 
                    |Memory -> $"M{memoryCnt}"
                    |NotUsed -> failwithf $"{ioType} not support"

                elif RuntimeDS.Package.IsPackagePLC()
                     || RuntimeDS.Package.IsPackageSIM()    
                     || RuntimeDS.Package.IsPackageEmulation()    //시뮬레이션도 PLC 주소규격으로 일단
                then
                    
                    match ioType with 
                    |In ->  
                            if iec
                            then
                                let iSlot, sumBit = getSlotInfoIEC(ioType, cnt)
                                $"%%IX0.{iSlot}.{(cnt-sumBit) % 64}" 
                            else 
                                getXgkBitText("P", getSlotInfoNonIEC(ioType, cnt)) 
                            
                    |Out -> 
                            if iec
                            then
                                let iSlot ,sumBit = getSlotInfoIEC(ioType, cnt)
                                $"%%QX0.{iSlot}.{(cnt-sumBit) % 64}" 
                            else 
                                getXgkBitText("P", getSlotInfoNonIEC(ioType, cnt)) 

                    |Memory -> if iec
                                 then $"%%MX{cnt}" 
                                 else  getXgkBitText("M", cnt) //PLC생성 외부 변수  M , PLC생성 내부 변수는 R
                                   

                    |NotUsed -> failwithf $"{ioType} not support"

                else TextAddrEmpty

            elif addr <> TextSkip && isSkip then
                 failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
            else addr
        
        newAddr

  
    let private getValidHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) target=
        let inAddr = getValidAddress(hwItem.InAddress, hwItem.Name, skipIn, IOType.Memory, target)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.Name, skipOut, IOType.Memory, target)
        inAddr, outAddr

    let getValidCondiAddress (cond: ConditionDef)  target   = getValidHwItem cond  false true target
    let getValidBtnAddress (btn: ButtonDef)  target         = getValidHwItem btn  false false target
    let getValidLampAddress (lamp: LampDef)  target         = getValidHwItem lamp true false  target

    let assignAutoAddress (sys: DsSystem, startMemory:int, offsetOpModeLampBtn: int) target =
        
        setMemoryIndex(startMemory);

        for b in sys.HWButtons do
            b.OutAddress <- TextSkip
            if b.InAddress = "" then
                b.InAddress <- TextAddrEmpty
            b.InAddress <- getValidBtnAddress b target |> fst

        for l in sys.HWLamps do
            l.InAddress <- TextSkip
            if l.OutAddress = "" then
                l.OutAddress <- TextAddrEmpty
            l.OutAddress <- getValidLampAddress l target |> snd

        for c in sys.HWConditions do
            c.OutAddress <- TextSkip
            if c.InAddress = "" then
                c.InAddress <- TextAddrEmpty
            c.InAddress <- getValidCondiAddress c  target |> fst
            
        let devJobSet = sys.Jobs.SelectMany(fun j-> j.DeviceDefs.Select(fun dev-> dev,j))
                            |> Seq.sortBy (fun (dev,_) ->dev.ApiName)

        let vs = sys.GetVerticesOfCoins()
        for (dev, job) in devJobSet  do
            let coins = vs.GetVerticesOfJobCoins(job)
            //외부입력 전용 확인하여 출력 스킵 및 입력 선두주소 고정
            if dev.IsRootFlowDev(coins) 
            then
                dev.InAddress  <- getExternalTempMemory target
                dev.OutAddress <- TextSkip
            else 
                let inSkip, outSkip =
                    match job.ActionType with
                    |NoneRx -> true,false
                    |NoneTx -> false,true
                    |NoneTRx -> true,true
                    |_ ->  false,false
                dev.InAddress  <- getValidAddress(dev.InAddress,  dev.QualifiedName, inSkip,  IOType.In, target)
                dev.OutAddress <- getValidAddress(dev.OutAddress, dev.QualifiedName, outSkip, IOType.Out, target)

        setMemoryIndex(startMemory + offsetOpModeLampBtn);

