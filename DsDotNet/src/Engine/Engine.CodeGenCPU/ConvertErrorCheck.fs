namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic
open System

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

        if RuntimeDS.Package.IsPackagePLC() then
            for btn in sys.HWButtons do
                if btn.InAddress = TextAddrEmpty ||btn.InAddress = TextSkip
                    then failwithf $"HW Button : {btn.Name} InAddress 값이 없습니다."


        if RuntimeDS.Package.IsPackagePLC() then
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
                             


    let checkErrExternalStartRealExist (sys:DsSystem) = 
        let flowEdges = (sys.Flows |> Seq.collect(fun f-> f.Graph.Edges))
        
        
        let exEdges = flowEdges
                        .Where(fun e -> e.Source.GetPureCall().IsSome
                                       || e.Target.GetPureCall().IsSome)  

        if not(exEdges.any()) then
            failwithf $"PLC 시스템은 외부시작 신호가 없으면 시작 불가 입니다. HelloDS 모델을 참고하세요"

    let checkErrRealResetExist (sys:DsSystem) =
        let errors = checkRealEdgeErrExist sys false
        if errors.Count > 0 then
            let fullErrorMessage = String.Join("\n", errors.Select(fun e-> $"{e.Flow.Name}.{e.Name}"))
            failwithf $"Work는 Reset 연결이 반드시 필요합니다. \n\n{fullErrorMessage}"

    let checkDuplicatesNNullAddress (sys: DsSystem) = 
        // Check for null addresses in jobs
        let nullTagJobs = 
            sys.Jobs
            |> Seq.filter (fun j -> 
                j.DeviceDefs
                |> Seq.exists (fun f -> f.InAddress = TextAddrEmpty && f.OutAddress = TextAddrEmpty))
            |> Seq.toList

        if nullTagJobs |> List.isEmpty |> not then 
            let errJobs = nullTagJobs |> List.map (fun j -> j.Name) |> String.concat "\n"
            failwithf $"Device 주소가 없습니다. \n{errJobs} \n\nAdd I/O Table을 수행하세요"

        // Check for null buttons
        let nullBtns = 
            sys.HWButtons
            |> Seq.filter (fun b -> b.InTag.IsNull() || (b.OutTag.IsNull() && b.OutAddress <> TextSkip))
            |> Seq.toList

        if nullBtns |> List.isEmpty |> not then 
            let errBtns = nullBtns |> List.map (fun b -> 
                let inout = if b.InTag.IsNull() then "입력" else "출력"
                $"{b.ButtonType} 해당 {inout} 주소가 없습니다. [{b.Name}]") |> String.concat "\n"
            
            failwithf $"버튼 주소가 없습니다. \n{errBtns}"

        // Check for null lamps
        let nullLamps = 
            sys.HWLamps
            |> Seq.filter (fun l -> l.OutTag.IsNull())
            |> Seq.toList

        if nullLamps |> List.isEmpty |> not then 
            let errLamps = nullLamps |> List.map (fun l -> l.Name) |> String.concat "\n"
            failwithf $"램프 주소가 없습니다. \n{errLamps}"

              // Aggregate all addresses to check for duplicates along with their API names
        let allAddresses = 
            [
                yield! sys.Jobs |> Seq.collect (fun j -> 
                    j.DeviceDefs |> Seq.collect (fun d -> [($"{d.ApiName}_IN", d.InAddress); ($"{d.ApiName}_OUT", d.OutAddress)]))
                yield! sys.HWButtons |> Seq.collect (fun b -> [(b.Name, b.InAddress); (b.Name, b.OutAddress)])
                yield! sys.HWLamps |> Seq.collect (fun l -> [(l.Name, l.OutAddress)])
            ] |> Seq.filter (fun (_, addr) -> addr <> TextAddrEmpty && addr <> TextSkip) |> Seq.toList

        // Helper to find duplicate elements and group them by API names
        let findDuplicates list =
            list 
            |> Seq.groupBy snd
            |> Seq.filter (fun (_, items) -> Seq.length items > 1)
            |> Seq.map (fun (addr, items) -> addr, items |> Seq.map fst |> Seq.distinct |> Seq.toList)

          // Find and handle duplicates
        let duplicates = findDuplicates allAddresses

        if not (Seq.isEmpty duplicates) then
            let dupMsg = 
                duplicates 
                |> Seq.map (fun (addr, apis) -> 
                    let apiList = apis |> String.concat "\n"
                    $"\n해당주소:{addr}\n{apiList}")
                |> String.concat "\n "
             
            failwithf $"중복 주소 발견: {dupMsg}"

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
