namespace Engine.Parser.FS

open System
open System.Linq

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Core
open type Engine.Parser.dsParser
open System.Collections.Generic

[<AutoOpen>]
module EtcListenerModule =

    let getHwSysItem (hwItem:HwSysItemDefContext)= 

        let nameNAddr = hwItem.TryFindFirstChild<HwSysItemNameAddrContext>().Value
        let name = nameNAddr.TryFindFirstChild<HwSysItemNameContext>().Value.GetText()

        let (inAddr, inParam), (outAddr, outParm) =
            match nameNAddr.TryFindFirstChild<TaskDevParaInOutContext>() with
            |Some devParam -> 
                commonDeviceParamExtractor devParam 
            |None ->
                (TextAddrEmpty, defaultTaskDevPara()),(TextAddrEmpty, defaultTaskDevPara())
        name, inParam, outParm, inAddr, outAddr

    (* 모든 vertex 가 생성 된 이후, edge 연결 작업 수행 *)
    type DsParserListener with


        member x.ProcessButtonBlock(ctx: ButtonBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem

                    let targetBtnType =
                        let fstType = first.GetType()

                        match first with
                        | :? AutoBlockContext -> DuAutoBTN
                        | :? ManualBlockContext -> DuManualBTN
                        | :? DriveBlockContext -> DuDriveBTN
                        | :? PauseBlockContext -> DuPauseBTN
                        | :? ClearBlockContext -> DuClearBTN
                        | :? ErrorOrEmgBlockContext -> DuEmergencyBTN
                        | :? TestBlockContext -> DuTestBTN
                        | :? HomeBlockContext -> DuHomeBTN
                        | :? ReadyBlockContext -> DuReadyBTN
                        | _ -> failwith $"button type error {fstType}"

                    let category = first.GetChild(1).GetText() // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
                    let key = (system, category)

                    if x.ButtonCategories.Contains(key) then
                        failwith $"중복 button category {category} near {ctx.GetText()}"
                    else
                        x.ButtonCategories.Add(key) |> ignore

                    let buttonDefs = first.Descendants<HwSysItemDefContext>().ToArray()
               
                    let flowBtnInfo =
                        [ for bd in buttonDefs do
                              option {
                                  let btnName, inParam, outParam, inAddr, outAddr = getHwSysItem bd
                                  let flows =
                                      bd
                                          .Descendants<FlowNameContext>()
                                          .Select(fun flowCtx -> flowCtx.GetText())
                                          .Tap(fun flowName ->
                                              verifyM
                                                  $"Flow [{flowName}] not exists!"
                                                  (system.Flows.Any(fun f -> f.Name = flowName.DeQuoteOnDemand())))
                                          .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName.DeQuoteOnDemand()))
                                          .ToHashSet()

                                  if flows.Count > 0 then
                                      return targetBtnType, btnName, inParam, outParam, flows, inAddr, outAddr
                                  else
                                      failwithlog "There are no flows in button"
                              } ]

                    flowBtnInfo
                    |> List.choose id
                    |> List.iter (fun ps ->
                        let targetBtnType, btnName, _, _, flows, inAddr, outAddr = ps

                        flows
                        |> Seq.iter (fun flow ->
                            system.AddButton(targetBtnType, btnName, inAddr, outAddr, flow)))

        member x.ProcessLampBlock(ctx: LampBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem

                    let targetLmpType =
                        let fstType = first.GetType()

                        match first with
                        | :? AutoBlockContext -> DuAutoModeLamp
                        | :? ManualBlockContext -> DuManualModeLamp
                        | :? DriveBlockContext -> DuDriveStateLamp
                        | :? ErrorOrEmgBlockContext -> DuErrorStateLamp
                        | :? TestBlockContext -> DuTestDriveStateLamp
                        | :? ReadyBlockContext -> DuReadyStateLamp
                        | :? IdleBlockContext -> DuIdleModeLamp
                        | :? OriginBlockContext -> DuOriginStateLamp
                        | _ -> failwith $"lamp type error {fstType}"

                    let lampDefs = first.Descendants<HwSysItemDefContext>().ToArray()

                    let flowLampInfo =
                        [ for ld in lampDefs do
                              option {
                                  let! flowNameCtxs = ld.Descendants<FlowNameContext>().ToArray()
                                  
                                  let lmpName, _, _, inAddr, outAddr = getHwSysItem ld
                                  if flowNameCtxs.length() > 1
                                  then 
                                       let flowNames = String.Join(", ", flowNameCtxs.Select(fun f->f.GetText()))
                                       failwith $"lamp flow assign error [ex: flow lamp : 1Lamp=1Flow, system lamp : 1Lamp=0Flow] ({lmpName} : {flowNames})"
                                    
                                  if flowNameCtxs.length() = 0
                                  then
                                       return targetLmpType, lmpName , inAddr, outAddr, None
                                  else
                                       let! flow = flowNameCtxs.First().GetText() |> system.TryFindFlow
                                       return targetLmpType, lmpName,  inAddr, outAddr, Some flow 
                              } ]

                    flowLampInfo |> List.choose id |> List.iter (system.AddLamp)

        member x.ProcessConditionBlock(ctx: ConditionBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem

                    let targetCndType =
                        let fstType = first.GetType()

                        match first with
                        | :? DriveBlockContext -> DuDriveState
                        | :? ReadyBlockContext -> DuReadyState
                        | _ -> failwith $"condition type error {fstType}"

                    let conditionDefs = first.Descendants<HwSysItemDefContext>().ToArray()

                    let flowConditionInfo =
                        [ for cd in conditionDefs do
                              option {

                                  let cndName, inParam, outParam, inAddr, outAddr = getHwSysItem cd
                                  let flows =
                                      cd
                                          .Descendants<FlowNameContext>()
                                          .Select(fun flowCtx -> flowCtx.GetText())
                                          .Tap(fun flowName ->
                                              verifyM
                                                  $"Flow [{flowName}] not exists!"
                                                  (system.Flows.Any(fun f -> f.Name = flowName.DeQuoteOnDemand())))
                                          .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName.DeQuoteOnDemand()))
                                          .ToHashSet()

                                  return targetCndType, cndName, inParam, outParam, flows, inAddr, outAddr 
                              } ]

                    flowConditionInfo
                    |> List.choose id
                    |> List.iter (fun ps ->
                        let targetCndType, cndName, _, _,  flows, inAddr, outAddr = ps

                        flows
                        |> Seq.iter (fun flow -> system.AddCondtion(targetCndType, cndName, inAddr, outAddr,  flow)))


        member x.ProcessSafetyBlock(ctx: SafetyBlockContext) =
            let safetyDefs = ctx.Descendants<SafetyAutoPreDefContext>()
            let safetyKvs = getSafetyAutoPreDefs safetyDefs
            let curSystem = x.TheSystem
           
            for (key, values) in safetyKvs do
                let safetyholder=  getSafetyAutoPreCall curSystem key 
                let safetyConditions =
                    [
                        for value in values do
                            match  curSystem.Jobs.TryFind(fun job-> job.UnqualifiedName = (value.Combine())) with
                            | Some j -> yield j
                            | None -> failWithLog $"{value} is not job Name"
                    ] 
                    |> Seq.map(fun sc -> DuSafetyAutoPreConditionCall sc)

                safetyConditions.Iter(fun sc ->
                    safetyholder.SafetyConditions.Add(sc)
                    |> verifyM $"중복 safety condition[{(sc.Core :?> INamed).Name}]")

        member x.ProcessAutoPreBlock(ctx: AutoPreBlockContext) =
            let autopreDefs = ctx.Descendants<SafetyAutoPreDefContext>()
            let autopreKvs = getSafetyAutoPreDefs autopreDefs
              
            let curSystem = x.TheSystem

            for (key, values) in autopreKvs do
                    let autopreKey =  getSafetyAutoPreCall curSystem key 
                    let autopreConditions =
                        [
                            for value in values do
                                match  curSystem.Jobs.TryFind(fun job-> job.UnqualifiedName = (value.Combine())) with
                                | Some j -> yield j
                                | None -> failWithLog $"{value} is not job Name"
                        ] 
                        |> Seq.map(fun sc -> DuSafetyAutoPreConditionCall sc)

                    autopreConditions.Iter(fun sc ->
                        autopreKey.AutoPreConditions.Add(sc)
                        |> verifyM $"중복 autopre condition[{(sc.Core :?> INamed).Name}]")
