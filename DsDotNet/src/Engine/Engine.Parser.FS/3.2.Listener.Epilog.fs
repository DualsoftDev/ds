namespace Engine.Parser.FS

open System
open System.Linq

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Engine.Common.FS
open Engine.Core
open type Engine.Parser.dsParser
open System.Collections.Generic

[<AutoOpen>]
module EtcListenerModule =
    (* 모든 vertex 가 생성 된 이후, edge 연결 작업 수행 *)
    type DsParserListener with
        member x.ProcessButtonBlock(ctx:ButtonBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem
                    let targetBtnType =
                        let fstType = first.GetType()
                        match first with
                        | :? AutoBlockContext      -> DuAutoBTN
                        | :? ManualBlockContext    -> DuManualBTN
                        | :? DriveBlockContext     -> DuDriveBTN
                        | :? StopBlockContext      -> DuStopBTN
                        | :? ClearBlockContext     -> DuClearBTN
                        | :? EmergencyBlockContext -> DuEmergencyBTN
                        | :? TestBlockContext      -> DuTestBTN
                        | :? HomeBlockContext      -> DuHomeBTN
                        | :? ReadyBlockContext     -> DuReadyBTN
                        | _ -> failwith $"button type error {fstType}"
                    let category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
                    let key = (system, category)
                    if x.ButtonCategories.Contains(key) then
                        failwith $"Duplicated button category {category} near {ctx.GetText()}"
                    else
                        x.ButtonCategories.Add(key) |> ignore

                    let buttonDefs = first.Descendants<ButtonDefContext>().ToArray()
                    let buttonFuncs = commonFunctionExtractor first
                    let flowBtnInfo = [
                        for bd in buttonDefs do
                        option {
                            let! btnNameAddr    = bd.TryFindFirstChild<BtnNameAddrContext>()
                            let! btnNameCtx     = btnNameAddr.TryFindFirstChild<ButtonNameContext>()
                            let btnName         = btnNameCtx.GetText()
                            let addrIn, addrOut =
                                match btnNameAddr.ChildCount with
                                | 2 ->
                                    option {
                                        let! inOutCtx = btnNameAddr.TryFindFirstChild<AddressInOutContext>()
                                        let! inCtx    = inOutCtx.TryFindFirstChild<InAddrContext>()
                                        let! outCtx   = inOutCtx.TryFindFirstChild<OutAddrContext>()
                                        return inCtx.GetText() |> replaceSkipAddress, outCtx.GetText() |> replaceSkipAddress
                                    } |> Option.get
                                | _ ->
                                    null, null
                            let flows =
                                bd.Descendants<FlowNameContext>()
                                    .Select(fun flowCtx -> flowCtx.GetText())
                                    .Tap(fun flowName -> verifyM $"Flow [{flowName}] not exists!" (system.Flows.Any(fun f -> f.Name = flowName)))
                                    .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName))
                                    .ToHashSet()
                            let funcSet = commonFunctionSetter btnName buttonFuncs
                            if flows.Count > 0 then
                                return ButtonDef(btnName, targetBtnType, addrIn, addrOut, flows, funcSet)
                            else
                                return ButtonDef(btnName, targetBtnType, null, null, new HashSet<Flow>(), new HashSet<Func>())
                        }
                    ]
                    flowBtnInfo
                    |> List.choose id
                    |> List.map(system.Buttons.Add)
                    |> ignore

        member x.ProcessLampBlock(ctx:LampBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem
                    let targetLmpType =
                        let fstType = first.GetType()
                        match first with
                        | :? AutoBlockContext      -> DuAutoLamp
                        | :? ManualBlockContext    -> DuManualLamp
                        | :? DriveBlockContext     -> DuDriveLamp
                        | :? StopBlockContext      -> DuStopLamp
                        | :? EmergencyBlockContext -> DuEmergencyLamp
                        | :? TestBlockContext      -> DuTestDriveLamp
                        | :? ReadyBlockContext     -> DuReadyLamp
                        | :? IdleBlockContext      -> DuIdleLamp
                        | _ -> failwith $"lamp type error {fstType}"

                    let lampDefs = first.Descendants<LampDefContext>().ToArray()
                    let lampFuncs = commonFunctionExtractor first
                    let flowLampInfo = [
                        for ld in lampDefs do
                        option {
                            let! lampNameCtx = ld.TryFindFirstChild<LampNameContext>()
                            let! flowNameCtx = ld.TryFindFirstChild<FlowNameContext>()
                            let  addrCtx  = ld.TryFindFirstChild<AddressItemContext>()
                            let! flow   = flowNameCtx.GetText() |> system.TryFindFlow
                            let lmpName = lampNameCtx.GetText()
                            let address =
                                match addrCtx with
                                | Some addr -> addr.GetText() |> replaceSkipAddress
                                | None -> null
                            let funcSet = commonFunctionSetter lmpName lampFuncs
                            return LampDef(lmpName, targetLmpType, address, flow, funcSet)
                        }
                    ]
                    flowLampInfo
                    |> List.choose id
                    |> List.map(system.Lamps.Add)
                    |> ignore

        member x.ProcessConditionBlock(ctx:ConditionBlockContext) =
            for ctxChild in ctx.children do
                if ctxChild :? ParserRuleContext then
                    let first = ctxChild.TryFindFirstChild<ParserRuleContext>().Value
                    let system = x.TheSystem
                    let targetCndType =
                        let fstType = first.GetType()
                        match first with
                        | :? DriveBlockContext     -> DuDriveState
                        | :? ReadyBlockContext     -> DuReadyState
                        | _ -> failwith $"condition type error {fstType}"

                    let conditionDefs = first.Descendants<LampDefContext>().ToArray()
                    let conditionFuncs = commonFunctionExtractor first
                    let flowConditionInfo = [
                        for cd in conditionDefs do
                        option {
                            let! lampNameCtx = cd.TryFindFirstChild<LampNameContext>()
                            let  addrCtx = cd.TryFindFirstChild<AddressItemContext>()
                            let lmpName = lampNameCtx.GetText()
                            let address =
                                match addrCtx with
                                | Some addr -> addr.GetText() |> replaceSkipAddress
                                | None -> null
                            let funcSet = commonFunctionSetter lmpName conditionFuncs
                            let flows =
                                cd.Descendants<FlowNameContext>()
                                    .Select(fun flowCtx -> flowCtx.GetText())
                                    .Tap(fun flowName -> verifyM $"Flow [{flowName}] not exists!" (system.Flows.Any(fun f -> f.Name = flowName)))
                                    .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName))
                                    .ToHashSet()
                            return ConditionDef(lmpName, targetCndType, address, flows, funcSet)
                        }
                    ]
                    flowConditionInfo
                    |> List.choose id
                    |> List.map(system.Conditions.Add)
                    |> ignore

        member x.ProcessSafetyBlock(ctx:SafetyBlockContext) =
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
                [   for safetyDef in safetyDefs do
                        let key =
                            let safety = safetyDef.TryFindFirstChild(fun (t:IParseTree) -> t :? SafetyKeyContext).Value
                            safety.CollectNameComponents()   // ["Main"] or ["My", "Flow", "Main"]
                        let valueHeader = safetyDef.Descendants<SafetyValuesContext>().First()
                        let values      = valueHeader.Descendants<Identifier23Context>().Select(collectNameComponents).ToArray()
                        (key, values)
                ]

            let curSystem = x.TheSystem

            let tryFindRealOrCall (ns:Fqdn) =
                option {
                    match ns.ToFSharpList() with
                    | flowOrReal::realOrCall::[] ->
                        match curSystem.TryFindFlow(flowOrReal) with
                        |Some (flow) ->
                            let! vertex = flow.Graph.TryFindVertex(realOrCall)
                            match vertex with
                            | :? Real as r -> return DuSafetyConditionReal r
                            | :? Call as c -> return DuSafetyConditionCall c
                            | :? RealOtherFlow as o -> return DuSafetyConditionRealExFlow o
                            | :? RealOtherSystem as o -> return DuSafetyConditionRealExSystem o
                            | _-> failwithlog "Error"

                        |None ->
                            let! vertex = curSystem.TryFindCall(ns)
                            match vertex with
                            | :? RealOtherSystem as rs -> return DuSafetyConditionRealExSystem rs
                            | :? Call as c -> return DuSafetyConditionCall c
                            | _ -> failwithlog "ERROR"

                    | _f::_r::_c::[] ->
                        let! vertex = curSystem.TryFindCall(ns)
                        match vertex with
                        | :? RealOtherSystem as rs -> return DuSafetyConditionRealExSystem rs
                        | :? Call as c -> return DuSafetyConditionCall c
                        | _ -> failwithlog "ERROR"

                    | _ ->
                        failwithlog "ERROR"
                }

            for (key, values) in safetyKvs do
                option {
                    let! safetyKey = tryFindRealOrCall key
                    let safetyConditions = [ for value in values -> tryFindRealOrCall value ] |> Seq.choose id
                    let holder = safetyKey.Core :?> ISafetyConditoinHolder
                    tracefn "%A = {%A}" holder safetyConditions
                    safetyConditions.Iter(fun sc -> holder.SafetyConditions.Add(sc) |> verifyM $"Duplicated safety condition[{ (sc.Core :?> INamed).Name}]")
                } |> ignore

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

        //DsSystem.OriginalCodeBlocks 여기에 저장 및 불러오기로 이동
        //member x.ProcessVariableDef(context:VariableDefContext) =
        //    let varName = context.TryFindFirstChild<VarNameContext>().Value.GetText()
        //    let varType = context.TryFindFirstChild<VarTypeContext>().Value.GetText()
        //    let init    = context.TryFindFirstChild<ArgumentContext>().Value.GetText()
        //    x.TheSystem.Variables.Add(new VariableData(varName, varType, init))

        //TaskDevice 여기에 저장 및 불러오기로 이동
        //member x.ProcessCommandDef(context:CommandDefContext) =
        //    let cmdName    = context.TryFindFirstChild<CmdNameContext>().Value.GetText()
        //    let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
        //    let funAppl    = x.CreateFunctionApplication(funApplCtx)
        //    let command    = new Command(cmdName, funAppl)
        //    x.TheSystem.Commands.Add(command)

        //TaskDevice 여기에 저장 및 불러오기로 이동
        //member x.ProcessObserveDef(context:ObserveDefContext) =
        //    let obsName    = context.TryFindFirstChild<ObserveNameContext>().Value.GetText()
        //    let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
        //    let funAppl    = x.CreateFunctionApplication(funApplCtx)
        //    let observes   = new Observe(obsName, funAppl)
        //    x.TheSystem.Observes.Add(observes)


        member x.ProcessLayouts(ctx:SystemContext) =
            (* [layouts] = {
                   L.T.Cp = (30, 50)            // xy
                   L.T.Cm = (60, 50, 20, 20)    // xywh
            } *)

            let layouts = ctx.Descendants<LayoutBlockContext>().ToArray()
            if layouts.Length > 1 then
                raise <| ParserException("Layouts block should exist only once", ctx)

            let positionDefs = ctx.Descendants<PositionDefContext>().ToArray()
            for posiDef in positionDefs do
                let callNamePath = posiDef.callName().TryCollectNameComponents()|> Option.get
                let xywh = posiDef.xywh()
                let call = tryFindCall x.TheSystem callNamePath |> Option.get

                match xywh.x().GetText(), xywh.y().GetText(), xywh.w().GetText(), xywh.h().GetText() with
                | Int32Pattern x, Int32Pattern y, Int32Pattern w, Int32Pattern h ->
                    match call with
                    | :? Call -> (call:?>Call).Xywh <- new Xywh(x, y, w, h)
                    | _ -> ()
                | Int32Pattern x, Int32Pattern y, null, null ->
                    match call with
                    | :? Call -> (call:?>Call).Xywh <- new Xywh(x, y, Nullable(), Nullable())
                    | _ -> ()
                | _ ->
                    failwithlog "ERROR"
