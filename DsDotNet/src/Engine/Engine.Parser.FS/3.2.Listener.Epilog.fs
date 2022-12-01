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
        member x.ProcessButtonsBlocks(ctx:ButtonsBlocksContext) =
            let first = ctx.TryFindFirstChild<ParserRuleContext>().Value     // {Emergency, Auto, Start, Reset}ButtonsContext
            let system = x.TheSystem
            let targetDic =
                match first with
                | :? EmergencyButtonBlockContext -> system.EmergencyButtons
                | :? AutoButtonBlockContext      -> system.AutoButtons
                | :? StartButtonBlockContext     -> system.StartButtons
                | :? ResetButtonBlockContext     -> system.ResetButtons
                | _ -> failwith "ERROR"

            let category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
            let key = (system, category)
            if x.ButtonCategories.Contains(key) then
                failwith $"Duplicated button category {category} near {ctx.GetText()}"
            else
                x.ButtonCategories.Add(key) |> ignore

            let buttonDefs = first.Descendants<ButtonDefContext>().ToArray()
            for bd in buttonDefs do
                let buttonName = bd.TryFindFirstChild<ButtonNameContext>().Value.GetText()
                let flows =
                    bd.Descendants<FlowNameContext>()
                        .Select(fun flowCtx -> flowCtx.GetText())
                        .Tap(fun flowName -> verifyM $"Flow [{flowName}] not exists!" (system.Flows.Any(fun f -> f.Name = flowName)))
                        .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName))
                        .ToArray()


                if not (targetDic.ContainsKey(buttonName)) then
                    targetDic.Add(buttonName, new HashSet<Flow>())

                flows.ForEach(fun flow ->
                    targetDic[buttonName].Add(flow) |> verifyM $"Flow [{flow.Name}] already added!"
                        )


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

            let sysNames, flowName, parenting_, ns_ = (x.GetContextInformation ctx).Tuples
            let curSystem = x.TheSystem

            let tryFindRealOrCall (ns:Fqdn) =
                option {
                    match ns.ToFSharpList() with
                    | flowOrReal::realOrCall::[] ->
                        match curSystem.TryFindFlow(flowOrReal) with
                        |Some (flow) ->
                            let! vertex = flow.Graph.TryFindVertex(realOrCall)
                            match vertex with
                            | :? Real as r -> return SafetyConditionReal (r)
                            | :? Call as c -> return SafetyConditionCall (c)
                            | :? RealOtherFlow as o -> return SafetyConditionRealEx (o)
                            | _-> failwith "Error"

                        |None ->        
                            let c = curSystem.TryFindCall(ns) |> Option.get
                            return SafetyConditionCall (c)

                    | f::r::c::[] ->
                         let! c = curSystem.TryFindCall(ns)
                         return SafetyConditionCall c
                
                    | _ ->
                        failwith "ERROR"
                }

            for (key, values) in safetyKvs do
                option {
                    let! safetyKey = tryFindRealOrCall key
                    let safetyConditions = [ for value in values -> tryFindRealOrCall value ] |> Seq.choose id
                    let holder = safetyKey.Core :?> ISafetyConditoinHolder
                    tracefn "%A = {%A}" holder safetyConditions
                    safetyConditions.Iter(fun sc -> holder.SafetyConditions.Add(sc) |> verifyM $"Duplicated safety condition[{ (sc.Core :?> INamed).Name}]")
                } |> ignore

        member private x.CreateFunctionApplication(context:FunApplicationContext):FunctionApplication =
            let funName = context.TryFindFirstChild<FunNameContext>().Value.GetText()
            let argGroups =
                context.Descendants<ArgumentGroupContext>()
                    .Select(fun argGrpCtx ->
                        argGrpCtx.Descendants<ArgumentContext>()
                            .Select(fun arg -> arg.GetText())
                            .ToArray())
                    .ToArray()

            FunctionApplication(funName, argGroups)

        member x.ProcessVariableDef(context:VariableDefContext) =
            let varName = context.TryFindFirstChild<VarNameContext>().Value.GetText()
            let varType = context.TryFindFirstChild<VarTypeContext>().Value.GetText()
            let init    = context.TryFindFirstChild<ArgumentContext>().Value.GetText()
            x.TheSystem.Variables.Add(new Variable(varName, varType, init))

        member x.ProcessCommandDef(context:CommandDefContext) =
            let cmdName    = context.TryFindFirstChild<CmdNameContext>().Value.GetText()
            let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
            let funAppl    = x.CreateFunctionApplication(funApplCtx)
            let command    = new Command(cmdName, funAppl)
            x.TheSystem.Commands.Add(command)

        member x.ProcessObserveDef(context:ObserveDefContext) =
            let obsName    = context.TryFindFirstChild<ObserveNameContext>().Value.GetText()
            let funApplCtx = context.TryFindFirstChild<FunApplicationContext>().Value
            let funAppl    = x.CreateFunctionApplication(funApplCtx)
            let observes   = new Observe(obsName, funAppl)
            x.TheSystem.Observes.Add(observes)


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
                    call.Xywh <- new Xywh(x, y, w, h)
                | Int32Pattern x, Int32Pattern y, null, null ->
                    call.Xywh <- new Xywh(x, y, Nullable(), Nullable())
                | _ ->
                    failwith "ERROR"
