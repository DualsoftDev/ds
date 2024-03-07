namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Reflection     

[<AutoOpen>]
module DsAddressModule =
   
    let blockInSize, blockInText = RuntimeDS.HwBlockSizeIn.ToBlockSizeNText()
    let blockOutSize, blockOutText = RuntimeDS.HwBlockSizeOut.ToBlockSizeNText()
    let blockInHead = blockInText.Substring(0, 1);
    let blockOutHead = blockOutText.Substring(0, 1);
    let mutable inCnt = RuntimeDS.HwStartInDINT * blockInSize - 1
    let mutable outCnt = RuntimeDS.HwStartOutDINT * blockOutSize - 1
    let mutable memoryCnt = RuntimeDS.HwStartMemoryDINT-1u
    type IOType = | In | Out | Memory 

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
                    |Memory -> memoryCnt <- memoryCnt + 1u; memoryCnt|>int

                if RuntimeDS.Package.IsPackagePC() 
                then
                    match ioType with 
                    |In ->  $"I{blockInHead}{cnt / blockInSize}.{cnt % blockInSize}" 
                    |Out -> $"O{blockOutHead}{cnt / blockOutSize}.{cnt % blockOutSize}" 
                    |Memory -> $"M{memoryCnt}"

                elif RuntimeDS.Package.IsPackagePLC()  || RuntimeDS.Package.IsPackageSIM()    //시뮬레이션도 PLC 주소규격으로 일단
                then
                    match ioType with 
                    |In ->  if RuntimeDS.HwBlockSizeIn = DuUINT64
                            then $"%%IX0.{cnt / 64}.{cnt % 64}" 
                            else $"%%I{blockInHead}{cnt / blockInSize}.{cnt % blockInSize}" 

                    |Out -> if RuntimeDS.HwBlockSizeIn = DuUINT64
                            then $"%%QX0.{cnt / 64}.{cnt % 64}" 
                            else $"%%Q{blockOutHead}{cnt / blockOutSize}.{cnt % blockOutSize}" 
                    |Memory -> $"%%MX{cnt}" 

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
    let getValidCondiAddress (cond: ConditionDef) = getValidBtnHwItem cond false true 
