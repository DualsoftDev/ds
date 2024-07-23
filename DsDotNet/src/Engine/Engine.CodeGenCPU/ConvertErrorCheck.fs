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

        if RuntimeDS.Package.IsPLCorPLCSIM() then
            for btn in sys.HWButtons do
                if btn.InAddress = TextAddrEmpty ||btn.InAddress = TextSkip
                    then failwithf $"HW Button : {btn.Name} InAddress 값이 없습니다."


        if RuntimeDS.Package.IsPLCorPLCSIM() then
            for lamp in sys.HWLamps do
                if lamp.OutAddress = TextAddrEmpty ||lamp.OutAddress = TextSkip
                    then failwithf $"HW Lamp : {lamp.Name} OutAddress 값이 없습니다."


    let checkErrApi(sys:DsSystem) = 

          for coin in sys.GetVerticesOfJobCalls() do
                for td in coin.TargetJob.TaskDefs do
                    for api in td.ApiItems do
                        if api.RX.IsNull() then
                            failwithf $"interface 정의시 관찰 Work가 없습니다. \n(error: {api.Name})"
                        if api.TX.IsNull() then
                            failwithf $"interface 정의시 지시 Work가 없습니다. \n(error: {api.Name})"

                        if td.OutAddress <> TextSkip && coin.TargetJob.JobParam.JobAction = Push 
                        then 
                            if coin.MutualResetCoins.isEmpty()
                            then 
                                failwithf $"Push type must be an interlock device \n(error: {coin.Name})"
                             


    let checkErrExternalStartRealExist (sys:DsSystem) = 
        let flowEdges = (sys.Flows |> Seq.collect(fun f-> f.Graph.Edges))
        
        
        let exEdges = flowEdges
                        .Where(fun e -> e.Source.GetPureCall().IsSome
                                       || e.Target.GetPureCall().IsSome)  

        if not(exEdges.any()) then
            failwithf $"PLC 시스템은 외부시작 신호가 없으면 시작 불가 입니다. HelloDS 모델을 참고하세요"


    let checkMultiDevPair(sys: DsSystem) = 
        let devicesCalls = 
            sys.GetTaskDevsCall().DistinctBy(fun (td, c) -> (td, c.TargetJob))
               .Where(fun (_, call) -> call.TargetJob.JobTaskDevInfo.TaskDevCount > 1)


        let groupDev =
               devicesCalls  |> Seq.groupBy (fun (dev, _) -> dev.DeviceName)
    
        for (_, calls) in groupDev  do
            let jobMultis = calls |> Seq.map (fun (_, call) -> call.TargetJob.JobTaskDevInfo.TaskDevCount ) |> Seq.distinct
            if Seq.length jobMultis > 1 then
                let callTexts = String.Join("\r\n", calls.Select(fun (_, call) -> call.Name))
                failwithf $"동일 다비이스의 multi 수량은 같아야 합니다. \r\n{callTexts}"


    let checkErrRealResetExist (sys:DsSystem) =
        let errors = checkRealEdgeErrExist sys false
        if errors.Count > 0 then
            let fullErrorMessage = String.Join("\n", errors.Select(fun e-> $"{e.Parent.GetFlow().Name}.{e.Name}"))
            failwithf $"Work는 Reset 연결이 반드시 필요합니다. \n\n{fullErrorMessage}"

    let checkNullAddress (sys: DsSystem) = 
        // Check for null addresses in jobs
        let nullTagJobs = 
            sys.Jobs.SelectMany(fun j->j.GetNullAddressDevTask())

        if nullTagJobs.any() then 
            let errJobs = String.Join ("\n", nullTagJobs.Select(fun s->s.QualifiedName))
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
       
    let updateDuplicateAddress (sys: DsSystem) = 
              // Aggregate all addresses to check for duplicates along with their API names
        let allAddresses = 
            [
                yield! sys.GetTaskDevsSkipEmptyAddress().Select(fst).Distinct()
                          |> Seq.collect(fun d -> [($"{d.ApiPureName}_IN", d.InTag); ($"{d.ApiPureName}_OUT", d.InTag)])
                       
                yield! sys.HwSystemDefs
                          |> Seq.collect(fun h ->  [($"{h.Name}_IN", h.InTag); ($"{h.Name}_OUT", h.InTag)])
            ] 
            |> Seq.filter (fun (_, tag) -> tag.IsNonNull()) |> Seq.toList

        // Helper to find duplicate elements and group them by API names
        let findDuplicates list =
            list 
            |> Seq.groupBy snd
            |> Seq.filter (fun (_, items) -> Seq.length items > 1)
            |> Seq.map (fun (addr, items) -> addr, items |> Seq.map fst |> Seq.distinct |> Seq.toList)

          // Find and handle duplicates
        let duplicates = findDuplicates allAddresses

        if not (Seq.isEmpty duplicates) then
            duplicates 
            |> Seq.iter (fun (tag, apis) -> 
                tag.AliasNames.AddRange apis 
             )

    let setSimulationEmptyAddress(sys:DsSystem) = 
        sys.Jobs.ForEach(fun j->
            j.TaskDefs.ForEach(fun d-> 
                        if d.InAddress.IsNullOrEmpty() then  d.InAddress <- (TextAddrEmpty)
                        if d.OutAddress.IsNullOrEmpty() then d.OutAddress <- (TextAddrEmpty)
                        if d.MaunualAddress.IsNullOrEmpty() then d.MaunualAddress <- (TextAddrEmpty)
                )
            )
        sys.HWLamps.ForEach(fun l -> 
                        if l.OutAddress.IsNullOrEmpty() then  l.OutAddress <-TextAddrEmpty
                        )
        sys.HWButtons.ForEach(fun b->                                         
                         if b.InAddress.IsNullOrEmpty() then   b.InAddress <-TextAddrEmpty
                         if b.OutAddress.IsNullOrEmpty() then  b.OutAddress <- TextAddrEmpty
                        )   
        sys.HWConditions.ForEach(fun c->                                         
                         if c.InAddress.IsNullOrEmpty() then   c.InAddress <-TextAddrEmpty
                         if c.OutAddress.IsNullOrEmpty() then  c.OutAddress <- TextAddrEmpty
                        )   

    let checkJobs(sys:DsSystem) = 
        for j in sys.Jobs do
            for td in j.TaskDefs do
                if td.ExistOutput
                then 
                    let outParam = td.GetOutParam(j)
                    if j.ActionType = Push 
                    then 
                        if outParam.Type = DuBOOL
                            then 
                                failWithLog $"{td.Name} {j.ActionType} 은 bool 타입만 지원합니다." 
                    else 
                        if outParam.Type <> DuBOOL && outParam.DevValue.IsNull() 
                        then 
                            failWithLog $"{td.Name} {td.OutAddress} 은 value 값을 입력해야 합니다." 
