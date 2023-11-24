// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module ImportIOUtil =

    let mutable inCnt = -1
    let mutable outCnt = 63

    let getValidAddress (addr: string, name: string, isSkip: bool, bInput: bool) =

        let addr = if addr.IsNullOrEmpty()
                    then failwithf $"Empty address {name}"
                    else addr.Trim().ToUpper()

        let newAddr =
            match addr = TextAddrEmpty, isSkip with
            | true, true -> TextSkip
            | true, false -> 
                let cnt = if bInput
                            then inCnt <- inCnt + 1; inCnt
                            else outCnt <- outCnt + 1; outCnt

                let prefix = if RuntimeDS.Package.IsPackagePC() 
                             then if bInput then "I" else "O"
                             elif bInput then "%IX0." else "%QX0."

                $"{prefix}{cnt / 64}.{cnt % 64}"

            | false, _ -> 
                match addr, isSkip with
                | _, true   when addr <> TextSkip -> failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
                //| _, false  when addr =  TextSkip -> failwithf $"{name} 인터페이스 대상이 있으면 대쉬('-') 대신 실주소 기입 필요."
                | _ -> addr
        
            //parsing을 위헤서 '-' -> '_' 변경 
        if newAddr = TextSkip then TextAddrEmpty else newAddr
      

    let getValidDevAddress (taskDev: TaskDev, bInput: bool) =
        let isSkip = if bInput then taskDev.ApiItem.RXs.Count = 0 else taskDev.ApiItem.TXs.Count = 0
        let address =  if bInput then taskDev.InAddress else taskDev.OutAddress
        getValidAddress(address, taskDev.QualifiedName, isSkip, bInput)

    let getValidBtnAddress (btn: ButtonDef, bInput:bool) =
        if bInput then
            getValidAddress(btn.InAddress, btn.Name, false, bInput)
        else 
            getValidAddress(btn.OutAddress, btn.Name, btn.OutAddress = TextSkip, bInput)

    let getValidLampAddress (lamp: LampDef) =
        getValidAddress(lamp.OutAddress, lamp.Name, false, false)

    let getValidCondiAddress (cond: ConditionDef) =
        getValidAddress(cond.InAddress, cond.Name, false, true)
