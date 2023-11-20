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

        let addr = if addr = null 
                    then "" 
                    else addr.Trim().ToUpper()

        let newAddr =
            match addr.IsNullOrEmpty(), isSkip with
            | true, true -> TextSkip
            | true, false -> 
                let cnt = if bInput
                            then inCnt <- inCnt + 1; inCnt
                            else outCnt <- outCnt + 1; outCnt

                let prefix = if RuntimeDS.Package.IsPackagePC() 
                             then if bInput then "I" else "O"
                             elif bInput then "%IX" else "%QX"

                $"{prefix}{cnt / 64}.{cnt % 64}"

            | false, _ -> 
                match addr, isSkip with
                | _, true   when addr <> TextSkip -> failwithf $"{name} 인터페이스 대상이 없으면 대쉬('-') 기입 필요."
                | _, false  when addr =  TextSkip -> failwithf $"{name} 인터페이스 대상이 있으면 대쉬('-') 대신 실주소 기입 필요."
                | _ -> addr
        
            //parsing을 위헤서 '-' -> '_' 변경 
        if newAddr = TextSkip then TextEmpty else newAddr
      

    let getValidDevAddress (taskDev: TaskDev, bInput: bool) =
        let isSkip = if bInput then taskDev.ApiItem.RXs.Count = 0 else taskDev.ApiItem.TXs.Count = 0
        let address =  if bInput then taskDev.InAddress else taskDev.OutAddress
        getValidAddress(address, taskDev.QualifiedName, isSkip, bInput)

    let getValidBtnAddress (btn: ButtonDef, addr:string,  bInput) =
        let isSkip  = (addr = TextSkip || addr = "")
      //  let inout   = if bInput then "입력" else "출력"
        //if addr = "" 
        //then failwithf $"{inout} 부분 {btn.Name} 물리배선 없는 경우 대쉬('-') 기입 필요."
        getValidAddress(addr, btn.Name, isSkip, bInput)

    let getValidLampAddress (lamp: LampDef) =
        getValidAddress(lamp.OutAddress, lamp.Name, false, false)

    let getValidCondiAddress (cond: ConditionDef) =
        getValidAddress(cond.InAddress, cond.Name, false, true)
