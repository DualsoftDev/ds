namespace Engine.Parser.FS

open System.Linq
open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser


/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
/// Element path map 구성
///   - Parenting, Child, alias, Api
/// </summary>
type SkeletonListener(parser:dsParser, helper:ParserHelper) =
    inherit ListenerBase(parser, helper)


    override x.EnterSystem(ctx:SystemContext) =
        match findFirstChild<SysBlockContext>(ctx) with
        | Some sysBlockCtx_ ->
            let name = ctx.systemName().GetText().DeQuoteOnDemand()
            let host =
                match findFirstChild<HostContext>(ctx) with
                | Some hostCtx -> hostCtx.GetText()
                | None -> null

            let system =
                match x._theSystem, x._currentSystem with
                | _, Some system ->
                    DsSystem.Create(name, host, system)
                | Some theSystem, None ->
                    theSystem
                | None, None ->
                    let system = DsSystem.CreateTopLevel(name, host)
                    helper._theSystem <- Some system
                    system
            helper._currentSystem <- Some system

            //let sys = DsSystem.Create(name, host, x._model)
            //helper._currentSystem <- Some sys
            //helper._theSystem <- Some sys
            tracefn($"System: {name}")
            x.AddElement(x.CurrentPathElements, GraphVertexType.System)
        | None ->
            failwith "ERROR"

    override x.EnterFlow(ctx:FlowContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        x._flow <- Some <| Flow.Create(flowName, x._currentSystem.Value)
        x.AddElement(x.CurrentPathElements, GraphVertexType.Flow)

    override x.EnterParenting(ctx:ParentingContext) =
        tracefn($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        x._parenting <- Some <| Real.Create(name, x._flow.Value)
        x.AddElement(x.CurrentPathElements, GraphVertexType.Segment ||| GraphVertexType.Parenting)

        let children =
            enumerateChildren<CausalTokenContext>(ctx)
                .Select(fun ctctx -> collectNameComponents(ctctx).ToArray())
                .Tap(fun childNameComponts -> assert (childNameComponts.Length = 1 || childNameComponts.Length = 2))
                .Select(fun childNameComponts -> x.AppendPathElement(childNameComponts))
                .ToArray()

        for ch in children do
            x.AddElement(ch, GraphVertexType.Child)

    override x.EnterIdentifier12Listing(ctx:Identifier12ListingContext) =
        let ns = x.AppendPathElement(collectNameComponents(ctx))
        x.AddElement(ns, GraphVertexType.Segment)

    override x.EnterCausalToken(ctx:CausalTokenContext) =
        let path = x.AppendPathElement(collectNameComponents(ctx))
        let vType = GraphVertexType.Call

        // 다음 stage 에서 처리...
        //if (_parenting == null)
        //    vType |= GraphVertexType.Segment

        x.AddElement(path, vType)


    override x.EnterAliasListing(ctx:AliasListingContext) =
        let map = x._flow.Value.AliasMap

        let aliasDef = findFirstChild<AliasDefContext>(ctx).Value
        let alias = collectNameComponents(aliasDef)
        match alias.Length with
            | 2 -> // {타시스템}.{interface명} or
                x.AddElement(alias, GraphVertexType.AliaseKey)
            | 1 -> // { (my system / flow /) segment 명 }
                x.AddElement(x.AppendPathElement(alias[0]), GraphVertexType.AliaseKey)
            | _ ->
                failwith "ERROR"

        let mnemonics =
            enumerateChildren<AliasMnemonicContext>(ctx)
                .Select(fun mctx -> collectNameComponents(mctx))
                .Tap(fun mne -> assert(mne.Length = 1))
                .Select(fun mne -> mne[0])
                .ToHashSet()
        map.Add(alias, mnemonics)
        for mne in mnemonics do
            x.AddElement(x.AppendPathElement(mne), GraphVertexType.AliaseMnemonic)



    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let hash = x._currentSystem.Value.ApiItems
        let interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx)
        let interfaceName = collectNameComponents(interrfaceNameCtx.Value)[0]
        let collectCallComponents(ctx:CallComponentsContext):Fqdn[] =
            enumerateChildren<Identifier123Context>(ctx)
                .Select(collectNameComponents)
                .ToArray()


        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
                .Select(collectCallComponents)
                .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || cc[0] = "_")))
                .Select(fun callCompnents -> callCompnents.Select(fun cc -> cc.Prepend(x._currentSystem.Value.Name).ToArray()).ToArray())
                .ToArray()


        x.AddElement(x.AppendPathElement(interfaceName), GraphVertexType.ApiKey)
        for cc in ser.Collect(id) do
            x.AddElement(cc, GraphVertexType.ApiSER)


        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 다음 listener stage 에서 채움..
        let api = ApiItem.Create(interfaceName, x._currentSystem.Value)
        hash.Add(api) |> ignore

    override x.EnterInterfaceResetDef(ctx:InterfaceResetDefContext) =
        // I1 <||> I2 <||> I3;  ==> [| I1; <||>; I2; <||>; I3; |]
        let terms =
            let pred = fun (tree:IParseTree) -> tree :? Identifier1Context || tree :? CausalOperatorResetContext
            enumerateChildren<RuleContext>(ctx, false, pred)
                .Select(fun ctx -> ctx.GetText())
                .ToArray()

        // I1 <||> I2 와 I2 <||> I3 에 대해서 해석
        for triple in (terms |> Array.windowed2 3 2) do
            if triple.Length = 3 then
                let opnd1, op, opnd2 = triple[0], triple[1], triple[2]
                let ri_ = ApiResetInfo.Create(x._currentSystem.Value, opnd1, op.ToModelEdge(), opnd2)
                ()
