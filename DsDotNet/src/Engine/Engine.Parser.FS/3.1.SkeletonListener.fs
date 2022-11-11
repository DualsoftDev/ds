namespace Engine.Parser.FS

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
                    DsSystem.Create(name, host, theSystem)
                | None, None ->
                    let system = DsSystem.CreateTopLevel(name, host)
                    helper._theSystem <- Some system
                    system
            helper._currentSystem <- Some system

            //let sys = DsSystem.Create(name, host, x._model)
            //helper._currentSystem <- Some sys
            //helper._theSystem <- Some sys
            tracefn($"System: {name}")
            x.AddElement(getContextInformation ctx, GraphVertexType.System)
        | None ->
            failwith "ERROR"

    override x.EnterFlow(ctx:FlowContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        x._flow <- Some <| Flow.Create(flowName, x._currentSystem.Value)
        x.AddElement(getContextInformation ctx, GraphVertexType.Flow)

    override x.EnterParenting(ctx:ParentingContext) =
        tracefn($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        x._parenting <- Some <| Real.Create(name, x._flow.Value)
        let xxx = getContextInformation ctx
        x.AddCausalTokenElement(getContextInformation ctx, GraphVertexType.Segment ||| GraphVertexType.Parenting)

        let children = enumerateChildren<CausalTokenContext>(ctx)
        for ch in children do
            x.AddCausalTokenElement(getContextInformation ch, GraphVertexType.Child)

    override x.EnterIdentifier12Listing(ctx:Identifier12ListingContext) =
        x.AddCausalTokenElement(getContextInformation ctx, GraphVertexType.Segment)

    override x.EnterCausalToken(ctx:CausalTokenContext) =
        let vType = GraphVertexType.CausalToken

        // 다음 stage 에서 처리...
        //if (_parenting == null)
        //    vType |= GraphVertexType.Segment

        x.AddCausalTokenElement(getContextInformation ctx, vType)


    override x.EnterAliasListing(ctx:AliasListingContext) =
        let map = x._flow.Value.AliasMap

        let aliasDef = findFirstChild<AliasDefContext>(ctx).Value
        let ci = getContextInformation aliasDef
        x.AddElement(ci, GraphVertexType.AliaseKey)

        let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getContextInformation)
        for mne in mnemonics do
            x.AddElement(mne, GraphVertexType.AliaseMnemonic)
        let aliasesHash = mnemonics.Select(fun ctx -> ctx.Names.Combine()).ToHashSet()
        let aliasKey = ci.Names.ToArray()
        map.Add(aliasKey, aliasesHash)

        //let alias = collectNameComponents(aliasDef)
        //match alias.Length with
        //    | 2 -> // {타시스템}.{interface명} or
        //        x.AddElement(getContextInformation aliasDef, GraphVertexType.AliaseKey)
        //    | 1 -> // { (my system / flow /) segment 명 }
        //        x.AddElement(x.AppendPathElement(alias[0]), GraphVertexType.AliaseKey)
        //    | _ ->
        //        failwith "ERROR"

        //let mnemonics =
        //    enumerateChildren<AliasMnemonicContext>(ctx)
        //        .Select(fun mctx -> collectNameComponents(mctx))
        //        .Tap(fun mne -> assert(mne.Length = 1))
        //        .Select(fun mne -> mne[0])
        //        .ToHashSet()
        //map.Add(ci.Names.ToArray(), mnemonics)
        //for mne in mnemonics do
        //    x.AddElement(x.AppendPathElement(mne), GraphVertexType.AliaseMnemonic)



    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let hash = x._currentSystem.Value.ApiItems
        let interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx).Value
        let interfaceName = collectNameComponents(interrfaceNameCtx)[0]
        //let collectCallComponents(ctx:CallComponentsContext):Fqdn[] =
        //    enumerateChildren<Identifier123Context>(ctx)
        //        .Select(collectNameComponents)
        //        .ToArray()


        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
                //.Select(collectCallComponents)
                //.Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || cc[0] = "_")))
                //.Select(fun callCompnents -> callCompnents.Select(fun cc -> cc.Prepend(x._currentSystem.Value.Name).ToArray()).ToArray())
                //.ToArray()


        x.AddElement(getContextInformation interrfaceNameCtx, GraphVertexType.ApiKey)
        //for cc in ser do
        //    x.AddElement(getContextInformation cc, GraphVertexType.ApiSER)


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

    override x.ExitModel(ctx:ModelContext) =
        let adjustVertexType() =
            logInfo "---- Adjusting elements"
            let dic = helper._causalTokenElements
            let dups = dic |> seq
            for KeyValue(ctxInfo, vType) in dups do
                let nameMatches =
                    helper._elements.Where(fun (KeyValue(ctx, _)) ->
                        ctx.Names = ctxInfo.Names //&& ctx.Systems = ctxInfo.Systems && ctx.Flow = ctxInfo.Flow
                        ).ToArray()
                assert(vType.HasFlag(GraphVertexType.CausalToken))
                let vt =  (vType &&& ~~~GraphVertexType.CausalToken)
                let types = nameMatches.Select(valueOfKeyValue).Fold((|||), GraphVertexType.None)
                if vt.IsOneOf(GraphVertexType.None, GraphVertexType.Child) then
                    if nameMatches.isEmpty() then
                        match vt with
                        | GraphVertexType.None ->
                            let newVType = dic[ctxInfo] ||| GraphVertexType.Segment
                            dic[ctxInfo] <- newVType
                            logDebug $"{ctxInfo.FullName} : {newVType}  // from {vType}"
                        | GraphVertexType.Child ->
                            if ctxInfo.Names.Length = 2 then
                                let newVType = dic[ctxInfo] ||| GraphVertexType.Call
                                dic[ctxInfo] <- newVType
                                logDebug $"{ctxInfo.FullName} : {newVType}  // from {vType}"
                        | _ ->
                            failwith "ERROR"
                    else
                        match types with
                        | GraphVertexType.AliaseKey ->
                            dic[ctxInfo] <- dic[ctxInfo] ||| GraphVertexType.Call
                        | GraphVertexType.AliaseMnemonic ->
                            dic[ctxInfo] <- dic[ctxInfo] ||| GraphVertexType.AliaseMnemonic
                        | _ ->
                            failwith "ERROR"
                        logWarn $"{ctxInfo.FullName} : {dic[ctxInfo]} // from {vType}"

        let createNonParentedReals() =
            let sys = x._theSystem.Value
            for KeyValue(ctxInfo, vType) in helper._causalTokenElements do
                match vType with
                | HasFlag GraphVertexType.Segment ->
                    if vType.HasFlag(GraphVertexType.Parenting) then
                        let parent = findGraphVertex(sys, ctxInfo.NameComponents.ToArray())
                        assert(parent <> null)
                    elif ctxInfo.Names.Length = 1 then
                        let flow = findGraphVertex (sys, [| yield! ctxInfo.Systems; yield ctxInfo.Flow.Value |]) // ctxInfo.Flow.Value.N
                        Real.Create(ctxInfo.Names.Combine(), flow:?>Flow) |> ignore
                    else
                        ()  // e.g My/F/F2.Seg1
                | _ ->
                    ()
                logDebug $"{ctxInfo.FullName} : {vType}"

        let dumpTokens (tokens:Dictionary<ContextInformation, GraphVertexType>) (msg:string) =
            logInfo "%s" msg
            for KeyValue(ctxInfo, vType) in tokens do
                logDebug $"{ctxInfo.FullName} : {vType}"
        let dumpCausalTokens = dumpTokens helper._causalTokenElements

        dumpCausalTokens "---- Original Causal token elements"

        adjustVertexType()
        createNonParentedReals()

        logInfo "---- Spit results"
        logDebug "%s" <| x._theSystem.Value.Spit().Dump()

        dumpCausalTokens "---- All Causal token elements"

        logInfo "---- Non Causal token elements"
        for KeyValue(ctxInfo, vType) in helper._elements do
            logDebug $"{ctxInfo.FullName} : {vType}"
