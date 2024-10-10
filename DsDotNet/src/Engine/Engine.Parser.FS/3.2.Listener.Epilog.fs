namespace Engine.Parser.FS

open System
open System.Linq

open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Common
open Engine.Core
open type Engine.Parser.dsParser

[<AutoOpen>]
module EtcListenerModule =

    let getHwSysItem (hwItem:HwSysItemDefContext)=

        let nameNAddr = hwItem.TryFindFirstChild<HwSysItemNameAddrContext>().Value
        let name = nameNAddr.TryFindFirstChild<HwSysItemNameContext>().Value.GetText()

        let (inAddr, inParam), (outAddr, outParm) =
            match nameNAddr.TryFindFirstChild<TaskDevParamInOutContext>() with
            | Some devParam ->
                commonDeviceParamExtractor devParam
            | None ->
                (TextAddrEmpty, defaultTaskDevParam()),(TextAddrEmpty, defaultTaskDevParam())
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
                        | :? AutoBlockContext       -> DuAutoBTN
                        | :? ManualBlockContext     -> DuManualBTN
                        | :? DriveBlockContext      -> DuDriveBTN
                        | :? PauseBlockContext      -> DuPauseBTN
                        | :? ClearBlockContext      -> DuClearBTN
                        | :? ErrorOrEmgBlockContext -> DuEmergencyBTN
                        | :? TestBlockContext       -> DuTestBTN
                        | :? HomeBlockContext       -> DuHomeBTN
                        | :? ReadyBlockContext      -> DuReadyBTN
                        | _ -> failwith $"button type error {fstType}"

                    let category = first.GetChild(1).GetText() // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
                    let key = (system, category)

                    if x.ButtonCategories.Contains(key) then
                        failwith $"중복 button category {category} near {ctx.GetText()}"
                    else
                        x.ButtonCategories.Add(key) |> ignore

                    let buttonDefs = first.Descendants<HwSysItemDefContext>().ToArray()

                    let flowBtnInfo =
                        [
                            for bd in buttonDefs do
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
                                }
                        ]

                    for fbi in flowBtnInfo |> List.choose id do
                        let targetBtnType, btnName, inParam, outParam, flows, inAddr, outAddr = fbi

                        for flow in flows do
                            system.AddButtonDef(targetBtnType, btnName, TaskDevParamIO(Some inParam, Some outParam), Addresses(inAddr, outAddr), flow)

        member x.ProcessLampBlock(ctx: LampBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem

                    let targetLmpType =
                        let fstType = first.GetType()

                        match first with
                        | :? AutoBlockContext       -> DuAutoModeLamp
                        | :? ManualBlockContext     -> DuManualModeLamp
                        | :? DriveBlockContext      -> DuDriveStateLamp
                        | :? ErrorOrEmgBlockContext -> DuErrorStateLamp
                        | :? TestBlockContext       -> DuTestDriveStateLamp
                        | :? ReadyBlockContext      -> DuReadyStateLamp
                        | :? IdleBlockContext       -> DuIdleModeLamp
                        | :? OriginBlockContext     -> DuOriginStateLamp
                        | _ -> failwith $"lamp type error {fstType}"

                    let lampDefs = first.Descendants<HwSysItemDefContext>().ToArray()

                    let flowLampInfo =
                        [
                            for ld in lampDefs do
                                option {
                                    let flowNameCtxs = ld.Descendants<FlowNameContext>() |> toList
                                    let lmpName, inParam, outParam, inAddr, outAddr = getHwSysItem ld
                                    match flowNameCtxs with
                                    | [] -> return targetLmpType, lmpName ,TaskDevParamIO(Some inParam, Some outParam), Addresses(inAddr, outAddr), None
                                    | h::[] ->
                                        let! flow = h.GetText() |> system.TryFindFlow
                                        return targetLmpType, lmpName, TaskDevParamIO(Some inParam, Some outParam), Addresses(inAddr, outAddr), Some flow
                                    | _ ->
                                        let flowNames = String.Join(", ", flowNameCtxs.Select(fun f->f.GetText()))
                                        failwith $"lamp flow assign error [ex: flow lamp : 1Lamp=1Flow, system lamp : 1Lamp=0Flow] ({lmpName} : {flowNames})"
                                }
                        ]

                    flowLampInfo |> List.choose id |> List.iter (system.AddLampDef)


        member private x.ExtractFlowConditionActionInfo(conditionDefs: HwSysItemDefContext[], system: DsSystem) =
            [
                for cd in conditionDefs do
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

                        return cndName, inParam, outParam, flows, inAddr, outAddr
                    }
            ]

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
                    let flowConditionInfo = x.ExtractFlowConditionActionInfo(conditionDefs, system)

                    for fci in flowConditionInfo |> List.choose id do
                        let cndName, inp, outp,  flows, inAddr, outAddr = fci

                        for flow in flows do
                            system.AddCondition(targetCndType, cndName, TaskDevParamIO(Some inp, Some outp), Addresses(inAddr, outAddr),  flow)

        member x.ProcessActionBlock(ctx: ActionBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem

                    let targetActionType =
                        let fstType = first.GetType()

                        match first with
                        | :? ErrorOrEmgBlockContext -> DuEmergencyAction
                        | :? PauseBlockContext -> DuPauseAction
                        | _ -> failwith $"action type error {fstType}"

                    let conditionDefs = first.Descendants<HwSysItemDefContext>().ToArray()
                    let flowActionInfo = x.ExtractFlowConditionActionInfo(conditionDefs, system)

                    for fci in flowActionInfo |> List.choose id do
                        let actionName, inp, outp,  flows, inAddr, outAddr = fci

                        for flow in flows do
                            system.AddAction(targetActionType, actionName, TaskDevParamIO(Some inp, Some outp), Addresses(inAddr, outAddr),  flow)


        member x.ProcessSafetyBlock(ctx: SafetyBlockContext) =
            let safetyDefs = ctx.Descendants<SafetyAutoPreDefContext>()
            let safetyKvs = getSafetyAutoPreDefs safetyDefs
            let curSystem = x.TheSystem

            for (key, values) in safetyKvs do
                let safetyholder=  getSafetyAutoPreCall curSystem key
                let safetyConditions =
                    [
                        for value in values do
                            match  curSystem.Jobs.TryFind(fun job-> job.DequotedQualifiedName = (value.Combine())) with
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
                            match  curSystem.Jobs.TryFind(fun job-> job.DequotedQualifiedName = (value.Combine())) with
                            | Some j -> yield j
                            | None -> failWithLog $"{value} is not job Name"
                    ]
                    |> Seq.map(fun sc -> DuSafetyAutoPreConditionCall sc)

                for sc in autopreConditions do
                    autopreKey.AutoPreConditions.Add(sc)
                    |> verifyM $"중복 autopre condition[{(sc.Core :?> INamed).Name}]"
