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


        for btn in sys.HWButtons do
            if btn.InAddress = TextAddrEmpty ||btn.InAddress = TextSkip
                then failwithf $"HW Button : {btn.Name} InAddress 값이 없습니다."


        for lamp in sys.HWLamps do
            if lamp.OutAddress = TextAddrEmpty ||lamp.OutAddress = TextSkip
                then failwithf $"HW Lamp : {lamp.Name} OutAddress 값이 없습니다."


    let checkErrApi(sys:DsSystem) = 

          for coin in sys.GetVerticesOfCoinCalls() do
                for td in coin.TaskDevs do
                    let api = td.ApiItem
                    if api.RXs.IsEmpty() then
                        failwithf $"interface 정의시 관찰 Work가 없습니다. \n(error: {api.Name})"
                    if api.TXs.IsEmpty() then
                        failwithf $"interface 정의시 지시 Work가 없습니다. \n(error: {api.Name})"

                    if td.OutAddress <> TextSkip && coin.TargetJob.ActionType = JobActionType.Push 
                    then 
                        if coin.MutualResetCalls.Select(fun c->c.VC.MM).isEmpty()
                        then 
                            failwithf $"Push type must be an interlock device \n(error: {coin.Name})"
                             

    let checkErrRealResetExist(sys:DsSystem) = 

          for real in sys.GetVertices().OfType<Real>() do
            if CodeConvertUtilExt.GetResetRootEdges(real.V).isEmpty()
            then 
                failwithf $"Work는 Reset 모델링이 반드시 필요합니다.\n(flow:{real.Flow.Name}, name:{real.Name})"
                             



    let checkErrNullAddress(sys:DsSystem) = 
        let nullTagJobs = sys.Jobs
                             .Where(fun j-> j.DeviceDefs.Where(fun f-> 
                                               f.InAddress = TextAddrEmpty
                                            && f.OutAddress = TextAddrEmpty
                                            ).any())
                      
        if nullTagJobs.any()
        then 
            let errJobs = StringExt.JoinWith(nullTagJobs.Select(fun j -> j.Name), "\n")
            failwithlogf $"Device 주소가 없습니다. \n{errJobs} \n\nAdd I/O Table을 수행하세요"
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
