namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Reflection     

[<AutoOpen>]
module DsAddressModule =
   
 
    let mutable inCnt = RuntimeDS.HwStartInBit-1
    let mutable outCnt = RuntimeDS.HwStartOutBit-1
    let mutable memoryCnt = RuntimeDS.HwStartMemoryBit-1

    let emptyToSkipAddress address = if address = TextAddrEmpty then TextSkip else address.Trim().ToUpper()
    let getValidAddress (addr: string, name: string, isSkip: bool, ioType:IOType) =

        let addr = if addr.IsNullOrEmpty()
                    then failwithf $"Empty address {name}"
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
                    |In -> inCnt <- inCnt + 1; inCnt
                    |Out -> outCnt <- outCnt + 1; outCnt
                    |Memory -> memoryCnt <- memoryCnt + 1; memoryCnt|>int
                    |NotUsed -> failwithf $"{ioType} not support"


                let getSlotInfo(settingType: IOType) =
                    let curr =
                        match settingType with 
                        | In -> inCnt
                        | Out -> outCnt
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
                    |In ->  let iSlot, sumBit  =  getSlotInfo(ioType)
                            $"%%IX0.{iSlot}.{(inCnt-sumBit) % 64}" 
                            
                    |Out -> let iSlot ,sumBit =  getSlotInfo(ioType)
                            $"%%QX0.{iSlot}.{(outCnt-sumBit) % 64}" 

                    |Memory -> $"%%MX{cnt}" 
                    |NotUsed -> failwithf $"{ioType} not support"

                else TextAddrEmpty

            elif addr <> TextSkip && isSkip then
                 failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
            else addr
        
        newAddr

  
    let private getValidBtnHwItem (hwItem:HwSystemDef) (skipIn:bool) (skipOut:bool) =
        let inAddr = getValidAddress(hwItem.InAddress, hwItem.Name, skipIn, IOType.Memory)
        let outAddr = getValidAddress(hwItem.OutAddress, hwItem.Name, skipOut, IOType.Memory)
        inAddr, outAddr

    let getValidBtnAddress (btn: ButtonDef)       = getValidBtnHwItem btn  false false
    let getValidLampAddress (lamp: LampDef)       = getValidBtnHwItem lamp true false 
    let getValidCondiAddress (cond: ConditionDef) = getValidAddress(cond.InAddress, cond.Name, false, IOType.In)

