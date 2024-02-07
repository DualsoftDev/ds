namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module ConvertErrorCheck =
    
    let checkErrHWItem(sys:DsSystem) = 
        let hwManuFlows = sys.ManualHWButtons |>Seq.collect(fun f->f.SettingFlows)
        let hwAutoFlows = sys.AutoHWButtons |>Seq.collect(fun f->f.SettingFlows)
        for btn in sys.AutoHWButtons do
            for flow in btn.SettingFlows do
                if not(hwManuFlows.Contains flow)
                then failwithf $"{flow.Name} manual btn not exist"

        for btn in sys.ManualHWButtons do
            for flow in btn.SettingFlows do
                if not(hwAutoFlows.Contains flow)
                then failwithf $"{flow.Name} auto btn not exist"

    let checkErrLightPLC(sys:DsSystem) = 
        let hwManuFlows = sys.ManualHWButtons |>Seq.collect(fun f->f.SettingFlows)
        let hwAutoFlows = sys.AutoHWButtons |>Seq.collect(fun f->f.SettingFlows)
        if hwAutoFlows.any() ||hwManuFlows.any()
        then failwithf $"Cannot create auto/manual button \nwhen Light PLC mode"

    let checkErrNullAddress(sys:DsSystem) = 
        let nullTagJobs = sys.Jobs
                             .Where(fun j-> j.DeviceDefs.Where(fun f-> 
                                            f.InTag.IsNull() && f.ApiItem.RXs.any()
                                            ||f.OutTag.IsNull() && f.ApiItem.TXs.any()
                                            ).any())
                      
        if nullTagJobs.any()
        then 
            let errJobs = StringExt.JoinWith(nullTagJobs.Select(fun j -> j.Name), "\n")
            failwithlogf $"Device 주소가 없습니다. \n{errJobs}"
        let nullBtns = sys.HWButtons.Where(fun b-> 
                                    b.InTag.IsNull() 
                                    ||b.OutTag.IsNull() && b.OutAddress <> TextSkip)
        if nullBtns.any()
        then 
            let errBtns = StringExt.JoinWith(
                            nullBtns.Select(fun b -> 
                                let inout = if b.InTag.IsNull() then "입력" else "출력"
                                $"{b.ButtonType} 해당 {inout} 주소가 없습니다. [{b.Name}]"), "\n")

            failwithlogf $"버튼 주소가 없습니다. \n{errBtns}"
                                      
        let nullLamps = sys.HWLamps.Where(fun b-> b.OutTag.IsNull())
        if nullLamps.any()
        then 
            let errLamps= StringExt.JoinWith(nullLamps.Select(fun j -> j.Name), "\n")
            failwithlogf $"램프 주소가 없습니다. \n{errLamps}"

    let setSimulationAddress(sys:DsSystem) = 
        sys.Jobs.ForEach(fun j->
            j.DeviceDefs.ForEach(fun d-> 
                        if d.InAddress.IsNullOrEmpty() then  d.InAddress <- TextAddrEmpty
                        if d.OutAddress.IsNullOrEmpty() then d.OutAddress <- TextAddrEmpty)
            )
        sys.HWLamps.ForEach(fun l -> 
                        if l.OutAddress.IsNullOrEmpty() then  l.OutAddress <- TextAddrEmpty)
        sys.HWButtons.ForEach(fun b->                                         
                         if b.InAddress.IsNullOrEmpty() then   b.InAddress <- TextAddrEmpty
                         if b.OutAddress.IsNullOrEmpty() then  b.OutAddress <-TextAddrEmpty
                        )
    