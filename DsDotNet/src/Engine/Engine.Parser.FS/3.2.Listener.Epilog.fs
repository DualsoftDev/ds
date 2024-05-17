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

        let inParam, outParm =
            match nameNAddr.TryFindFirstChild<DevParamInOutContext>() with
            |Some devParam -> 
                commonDeviceParamExtractor devParam 
            |None ->
                TextAddrEmpty|>defaultDevParam, TextAddrEmpty|>defaultDevParam
        name, inParam, outParm

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
                                  let btnName, inParam, outParam = getHwSysItem bd
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
                                      return targetBtnType, btnName, inParam, outParam, flows
                                  else
                                      failwithlog "There are no flows in button"
                              } ]

                    flowBtnInfo
                    |> List.choose id
                    |> List.iter (fun ps ->
                        let targetBtnType, btnName, inParam, outParam, flows = ps

                        flows
                        |> Seq.iter (fun flow ->
                            system.AddButton(targetBtnType, btnName, inParam, outParam, flow)))

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
                                  
                                  let lmpName, inParam, outParam = getHwSysItem ld
                                  if flowNameCtxs.length() > 1
                                  then 
                                       let flowNames = String.Join(", ", flowNameCtxs.Select(fun f->f.GetText()))
                                       failwith $"lamp flow assign error [ex: flow lamp : 1Lamp=1Flow, system lamp : 1Lamp=0Flow] ({lmpName} : {flowNames})"
                                    
                                  if flowNameCtxs.length() = 0
                                  then
                                       return targetLmpType, lmpName, inParam, outParam, None
                                  else
                                       let! flow = flowNameCtxs.First().GetText() |> system.TryFindFlow
                                       return targetLmpType, lmpName, inParam, outParam, Some flow
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

                                  let cndName, inParam, outParam = getHwSysItem cd
                                  let flows =
                                      cd
                                          .Descendants<FlowNameContext>()
                                          .Select(fun flowCtx -> flowCtx.GetText())
                                          .Tap(fun flowName ->
                                              verifyM
                                                  $"Flow [{flowName}] not exists!"
                                                  (system.Flows.Any(fun f -> f.Name = flowName)))
                                          .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName))
                                          .ToHashSet()

                                  return targetCndType, cndName, inParam, outParam, flows
                              } ]

                    flowConditionInfo
                    |> List.choose id
                    |> List.iter (fun ps ->
                        let targetCndType, cndName, inParam, outParam,  flows = ps

                        flows
                        |> Seq.iter (fun flow -> system.AddCondtion(targetCndType, cndName, inParam, outParam,  flow)))

        member x.ProcessSafetyBlock(ctx: SafetyBlockContext) =
            let safetyDefs = ctx.Descendants<SafetyDefContext>()
            (*
             * safety block 을 parsing 해서 key / value 의 dictionary 로 저장
             *
            [safety] = {
                F.Main = {A."+"; B."+"}
            }
            => "Main" = {A."+"; B."+"}
             *)
            let safetyKvs =
                [ for safetyDef in safetyDefs do
                      let key =
                          let safety =
                              safetyDef.TryFindFirstChild(fun (t: IParseTree) -> t :? SafetyKeyContext).Value

                          safety.CollectNameComponents() // ["Main"] or ["My", "Flow", "Main"]

                      let valueHeader = safetyDef.Descendants<SafetyValuesContext>().First()

                      let values =
                          valueHeader
                              .Descendants<Identifier23Context>()
                              .Select(collectNameComponents)
                              .ToArray()

                      (key, values) ]

            let curSystem = x.TheSystem

            let tryFindRealOrCall (ns: Fqdn) =
                option {
                    match ns.ToFSharpList() with
                    | flowOrReal :: [ realOrCall ] ->
                        match curSystem.TryFindFlow(flowOrReal) with
                        | Some(flow) ->
                            let! vertex = flow.Graph.TryFindVertex(realOrCall)

                            match vertex with
                            | :? Real as r -> return DuSafetyConditionReal r
                            | :? Call as c -> return DuSafetyConditionCall c
                            | :? RealOtherFlow as o -> return DuSafetyConditionRealExFlow o
                            | _ -> failwithlog "Error"

                        | None ->
                            let! vertex = curSystem.TryFindCall(ns)

                            match vertex with
                            | :? Call as c -> return DuSafetyConditionCall c
                            | _ -> failwithlog "ERROR"

                    | _f :: _r :: [ _c ] ->
                        let! vertex = curSystem.TryFindCall(ns)

                        match vertex with
                        | :? Call as c -> return DuSafetyConditionCall c
                        | _ -> failwithlog "ERROR"

                    | _ -> failwithlog "ERROR"
                }

            for (key, values) in safetyKvs do
                option {
                    let! safetyKey = tryFindRealOrCall key

                    let safetyConditions =
                        [ for value in values -> tryFindRealOrCall value ] |> Seq.choose id

                    let holder = safetyKey.Core :?> ISafetyConditoinHolder
                    debugfn "%A = {%A}" holder safetyConditions

                    safetyConditions.Iter(fun sc ->
                        holder.SafetyConditions.Add(sc)
                        |> verifyM $"중복 safety condition[{(sc.Core :?> INamed).Name}]")
                }
                |> ignore



//member private x.CreateFunctionApplication(context:FunApplicationContext):FunctionApplication =
//    let funName = context.TryFindFirstChild<FunNameContext>().Value.GetText()
//    let argGroups =
//        context.Descendants<ArgumentGroupContext>()
//            .Select(fun argGrpCtx ->
//                argGrpCtx.Descendants<ArgumentContext>()
//                    .Select(fun arg -> arg.GetText())
//                    .ToArray())
//            .ToArray()

//    FunctionApplication(funName, argGroups)



//TaskDev 여기에 저장 및 불러오기로 이동
//member x.ProcessCommandDef(context:CommandDefContext) =
//    let cmdName    = context.TryFindFirstChild<CmdNameContext>().Value.GetText()
//    let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
//    let funAppl    = x.CreateFunctionApplication(funApplCtx)
//    let command    = new Command(cmdName, funAppl)
//    x.TheSystem.Commands.Add(command)

//TaskDev 여기에 저장 및 불러오기로 이동
//member x.ProcessObserveDef(context:ObserveDefContext) =
//    let obsName    = context.TryFindFirstChild<ObserveNameContext>().Value.GetText()
//    let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
//    let funAppl    = x.CreateFunctionApplication(funApplCtx)
//    let observes   = new Observe(obsName, funAppl)
//    x.TheSystem.Observes.Add(observes)


//member x.ProcessLayouts(ctx:SystemContext) =
//    (* [layouts] = {
//           L.T.Cp = (30, 50)            // xy
//           L.T.Cm = (60, 50, 20, 20)    // xywh
//    } *)
//
//    let layouts = ctx.Descendants<LayoutBlockContext>().ToArray()
//    if layouts.Length > 1 then
//        raise <| ParserException("Layouts block should exist only once", ctx)
//
//    let positionDefs = ctx.Descendants<PositionDefContext>().ToArray()
//    for posiDef in positionDefs do
//        let deviceOrApiNamePath = posiDef.deviceOrApiName().TryCollectNameComponents()|> Option.get
//        let xywh = posiDef.xywh()
//        let call = tryFindCall x.TheSystem deviceOrApiNamePath |> Option.get
//
//        match xywh.x().GetText(), xywh.y().GetText(), xywh.w().GetText(), xywh.h().GetText() with
//        | Int32Pattern x, Int32Pattern y, Int32Pattern w, Int32Pattern h ->
//            match call with
//            | :? Call -> (call:?>Call).Xywh <- new Xywh(x, y, w, h)
//            | _ -> ()
//        | Int32Pattern x, Int32Pattern y, null, null ->
//            match call with
//            | :? Call -> (call:?>Call).Xywh <- new Xywh(x, y, Nullable(), Nullable())
//            | _ -> ()
//        | _ ->
//            failwithlog "ERROR"
