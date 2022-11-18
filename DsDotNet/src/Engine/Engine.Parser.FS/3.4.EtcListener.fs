namespace Engine.Parser.FS

open System
open System.Linq

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open System.Collections.Generic

/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
type EtcListener(parser:dsParser, helper:ParserHelper) =
    inherit ListenerBase(parser, helper)

    do
        base.UpdateModelSpits()


    override x.EnterButtonsBlocks(ctx:ButtonsBlocksContext) =
        let first = tryFindFirstChild<ParserRuleContext>(ctx).Value     // {Emergency, Auto, Start, Reset}ButtonsContext
        let system = x._theSystem.Value
        let targetDic =
            match first with
            | :? EmergencyButtonBlockContext -> system.EmergencyButtons
            | :? AutoButtonBlockContext      -> system.AutoButtons
            | :? StartButtonBlockContext     -> system.StartButtons
            | :? ResetButtonBlockContext     -> system.ResetButtons
            | _ -> failwith "ERROR"

        let category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
        let key = (system, category)
        if x.ParserHelper.ButtonCategories.Contains(key) then
            failwith $"Duplicated button category {category} near {ctx.GetText()}"
        else
            x.ParserHelper.ButtonCategories.Add(key) |> ignore

        let buttonDefs = enumerateChildren<ButtonDefContext>(first).ToArray()
        for bd in buttonDefs do
            let buttonName = tryFindFirstChild<ButtonNameContext>(bd).Value.GetText()
            let flows =
                enumerateChildren<FlowNameContext>(bd)
                    .Select(fun flowCtx -> flowCtx.GetText())
                    .Tap(fun flowName -> verifyM $"Flow [{flowName}] not exists!" (system.Flows.Any(fun f -> f.Name = flowName)))
                    .Select(fun flowName -> system.Flows.First(fun f -> f.Name = flowName))
                    .ToArray()


            if not (targetDic.ContainsKey(buttonName)) then
                targetDic.Add(buttonName, new HashSet<Flow>())

            flows.ForEach(fun flow ->
                targetDic[buttonName].Add(flow) |> verifyM $"Flow [{flow.Name}] already added!"
                    )


    override x.EnterSafetyBlock(ctx:SafetyBlockContext) =
        let safetyDefs = enumerateChildren<SafetyDefContext>(ctx)
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
                        let safety = tryFindFirstChild(safetyDef, fun (t:IParseTree) -> t :? SafetyKeyContext).Value
                        collectNameComponents(safety)   // ["Main"] or ["My", "Flow", "Main"]
                    let valueHeader = enumerateChildren<SafetyValuesContext>(safetyDef).First()
                    let values      = enumerateChildren<Identifier123Context>(valueHeader).Select(collectNameComponents).ToArray()
                    (key, values)
            ]

        let sysNames, flowName, parenting_, ns_ = (getContextInformation ctx).Tuples
        let curSystem = x.ParserHelper.TheSystem.Value

        let test =
            let f (n:int) = n.ToString()
            let a = Some 1
            a |> Option.map(f)

        for (FList(key), values) in safetyKvs do
            match key with
            | flow::real::[] ->
                let realSeg = curSystem.TryFindFlow(flow).Bind(fun f -> f.Graph.TryFindVertex(real)).Map(fun v -> v :?> Real)
                match realSeg with
                | Some realSeg ->
                    for value in values do
                        let vertex = curSystem.FindGraphVertex(value)
                        ()

                        //let added = realSeg.SafetyConditions.Add(cond)
                        //if not added then
                        //    raise <| ParserException($"Safety condition [{cond.QualifiedName}] duplicated on safety key[{key.Combine()}]", ctx)
                        //let x = api
                        //()
                        ////let safetySeg = curSystem.FindExportApiItem  TryFindFlow(flow).Map(fun f -> f.Graph.TryFindVertex(value))
                        ////match safetySeg with
                        ////| Some safetySeg ->
                        ////    safetySeg.Safety <- Some realSeg
                        ////| None -> failwith $"Safety segment [{value}] not exists!"
                | _ ->
                    failwith "ERROR"

                ()
            | _ ->
                failwith "ERROR"


            //assert(key.Length = 2)
            //let (flow::real::[]) = key
            //let seg:Real =
            //    match key.Length with
            //        | 1 ->
            //            assert(ctx.Parent :? FlowBlockContext)
            //            curSystem.FindGraphVertex<Real>(x.AppendPathElement(key[0]))
            //        | 3 ->
            //            assert(ctx.Parent :? ModelPropertyBlockContext)
            //            curSystem.FindGraphVertex<Real>(key)
            //        | _ ->
            //            raise <| ParserException($"Invalid safety key[{key.Combine()}]", ctx)

            //for cond in values.Select(fun v -> curSystem.FindGraphVertex(v) |> box :?> Real) do
            //    let added = seg.SafetyConditions.Add(cond)
            //    if not added then
            //        raise <| ParserException($"Safety condition [{cond.QualifiedName}] duplicated on safety key[{key.Combine()}]", ctx)


    member private x.CreateFunctionApplication(context:FunApplicationContext):FunctionApplication =
        let funName = tryFindFirstChild<FunNameContext>(context).Value.GetText()
        let argGroups =
            enumerateChildren<ArgumentGroupContext>(context)
                .Select(fun argGrpCtx ->
                    enumerateChildren<ArgumentContext>(argGrpCtx)
                        .Select(fun arg -> arg.GetText())
                        .ToArray())
                .ToArray()

        FunctionApplication(funName, argGroups)

    override x.EnterVariableDef(context:VariableDefContext) =
        let varName = tryFindFirstChild<VarNameContext>(context).Value.GetText()
        let varType = tryFindFirstChild<VarTypeContext>(context).Value.GetText()
        let init    = tryFindFirstChild<ArgumentContext>(context).Value.GetText()
        helper.TheSystem.Value.Variables.Add(new Variable(varName, varType, init))

    override x.EnterCommandDef(context:CommandDefContext) =
        let cmdName    = tryFindFirstChild<CmdNameContext>(context).Value.GetText()
        let funApplCtx = tryFindFirstChild<FunApplicationContext>(context).Value
        let funAppl    = x.CreateFunctionApplication(funApplCtx)
        let command    = new Command(cmdName, funAppl)
        helper.TheSystem.Value.Commands.Add(command)

    override x.EnterObserveDef(context:ObserveDefContext) =
        let obsName    = tryFindFirstChild<ObserveNameContext>(context).Value.GetText()
        let funApplCtx = tryFindFirstChild<FunApplicationContext>(context).Value
        let funAppl    = x.CreateFunctionApplication(funApplCtx)
        let observes   = new Observe(obsName, funAppl)
        helper.TheSystem.Value.Observes.Add(observes)


    override x.ExitSystem(ctx:SystemContext) =
        base.ExitSystem(ctx)
        x.UpdateModelSpits()
        (* [layouts] = {
               L.T.Cp = (30, 50)            // xy
               L.T.Cm = (60, 50, 20, 20)    // xywh
        } *)

        let layouts = enumerateChildren<LayoutBlockContext>(ctx).ToArray()
        if layouts.Length > 1 then
            raise <| ParserException("Layouts block should exist only once", ctx)

        let positionDefs = enumerateChildren<PositionDefContext>(ctx).ToArray()
        for posiDef in positionDefs do
            let callName = posiDef.callName().GetText()
            let xywh = posiDef.xywh()
            let call = tryFindCall helper.TheSystem.Value callName |> Option.get

            match xywh.x().GetText(), xywh.y().GetText(), xywh.w().GetText(), xywh.h().GetText() with
            | Int32Pattern x, Int32Pattern y, Int32Pattern w, Int32Pattern h ->
                call.Xywh <- new Xywh(x, y, w, h)
            | Int32Pattern x, Int32Pattern y, null, null ->
                call.Xywh <- new Xywh(x, y, Nullable(), Nullable())
            | _ ->
                failwith "ERROR"

        //(*
        //    [sys] / [prop] / [addresses] = {
        //        ApiName = (Start, End) Tag address
        //        A."" + "" = (% Q1234.2343, % I1234.2343)
        //        A."" - "" = (START, END)
        //    }
        //*)
        //let api2Address =
        //    [|
        //        for sysCtx in enumerateChildren<SystemContext>(ctx) do
        //        for addrDefCtx in enumerateChildren<AddressDefContext>(sysCtx) do
        //            let apiPath = collectNameComponents(addrDefCtx.apiPath())
        //            let sre = addrDefCtx.addressTxRx()
        //            let s =
        //                let s = sre.tx()
        //                if isNull s then null else s.GetText()
        //            let e =
        //                let e = sre.rx()
        //                if isNull e then null else e.GetText()
        //            (sysCtx, apiPath, new Addresses(s, e))
        //    |]

        for o in x._modelSpits do
            match o.GetCore() with
            | :? Call as call ->
                failwith "ERROR"
                //let map = call.GetSystem().ApiAddressMap
                //if map.ContainsKey(call.NameComponents) then
                //    let address = map[call.NameComponents]
                //    assert(isNull call.Addresses || call.Addresses = address)
                //    call.Addresses <- address
            | _ -> ()
