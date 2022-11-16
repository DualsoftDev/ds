namespace Engine.Parser.FS

open System.Linq
open System.IO
open System.Collections.Generic

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
        base.EnterSystem(ctx)

        match tryFindFirstChild<SysBlockContext>(ctx) with
        | Some sysBlockCtx_ ->
            let name = defaultArg helper.ParserOptions.LoadedSystemName (ctx.systemName().GetText().DeQuoteOnDemand())
            let host =
                match tryFindFirstChild<HostContext>(ctx) with
                | Some hostCtx -> hostCtx.GetText()
                | None -> null
            //let name = helper.ParserOptions.LoadedSystemName
            helper.TheSystem <- Some <| DsSystem.Create(name, host)
            tracefn($"System: {name}")
            x.AddElement(getContextInformation ctx, GVT.System)
        | None ->
            failwith "ERROR"

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        x._flow <- Some <| Flow.Create(flowName, helper.TheSystem.Value)
        x.AddElement(getContextInformation ctx, GVT.Flow)

    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        tracefn($"Parenting: {ctx.GetText()}")
        let name = tryGetName(ctx.identifier1()).Value
        x._parenting <- Some <| Real.Create(name, x._flow.Value)
        let xxx = getContextInformation ctx
        x.AddCausalTokenElement(getContextInformation ctx, GVT.Segment ||| GVT.Parenting)

        let children = enumerateChildren<CausalTokenContext>(ctx)
        for ch in children do
            x.AddCausalTokenElement(getContextInformation ch, GVT.Child)

    override x.EnterIdentifier12Listing(ctx:Identifier12ListingContext) =
        x.AddCausalTokenElement(getContextInformation ctx, GVT.Segment)

    override x.EnterCausalToken(ctx:CausalTokenContext) =
        let vType = GVT.CausalToken

        // 다음 stage 에서 처리...
        //if (_parenting == null)
        //    vType |= GVT.Segment

        x.AddCausalTokenElement(getContextInformation ctx, vType)


    override x.EnterAliasListing(ctx:AliasListingContext) =
        failwith "NOT yet"
        //let map = x._flow.Value.AliasDefs

        //let aliasDef = tryFindFirstChild<AliasDefContext>(ctx).Value
        //let ci = getContextInformation aliasDef
        //x.AddElement(ci, GVT.AliaseKey)

        //let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getContextInformation)
        //for mne in mnemonics do
        //    x.AddElement(mne, GVT.AliaseMnemonic)
        //let aliasesHash = mnemonics.Select(fun ctx -> ctx.Names.Combine()).ToHashSet()
        //let aliasKey = ci.Names.ToArray()
        //map.Add(aliasKey, aliasesHash)


    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let hash = helper.TheSystem.Value.ApiItems4Export
        let interrfaceNameCtx = tryFindFirstChild<InterfaceNameContext>(ctx).Value
        let interfaceName = collectNameComponents(interrfaceNameCtx)[0]
        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)

        x.AddElement(getContextInformation interrfaceNameCtx, GVT.ApiKey)

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 다음 listener stage 에서 채움..
        let api = ApiItem4Export.Create(interfaceName, helper.TheSystem.Value)
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
                let ri_ = ApiResetInfo.Create(helper.TheSystem.Value, opnd1, op.ToModelEdge(), opnd2)
                ()

    member private x.GetFilePath(fileSpecCtx:FileSpecContext) =
        let simpleFilePath = tryFindFirstChild<FilePathContext>(fileSpecCtx).Value.GetText().DeQuoteOnDemand()
        let absoluteFilePath =
            let dir = helper.ParserOptions.ReferencePath
            [simpleFilePath; $"{dir}\\{simpleFilePath}"].First(fun f -> File.Exists(f))
        absoluteFilePath, simpleFilePath


    override x.EnterLoadDeviceBlock(ctx:LoadDeviceBlockContext) =
        let fileSpecCtx = tryFindFirstChild<FileSpecContext>(ctx).Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let device =
            let loadedName = collectNameComponents(ctx).Combine()
            fwdLoadDevice x._theSystem.Value (absoluteFilePath, simpleFilePath) loadedName
        x._theSystem.Value.Devices.Add(device) |> ignore

    override x.EnterLoadExternalSystemBlock(ctx:LoadExternalSystemBlockContext) =
        let fileSpecCtx = tryFindFirstChild<FileSpecContext>(ctx).Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let external =
            let ipSpecCtx = tryFindFirstChild<IpSpecContext>(ctx).Value
            let ip = tryFindFirstChild<EtcNameContext>(ipSpecCtx).Value.GetText()
            let loadedName = collectNameComponents(ctx).Combine()
            fwdLoadExternalSystem x._theSystem.Value (absoluteFilePath, simpleFilePath) loadedName
        x._theSystem.Value.Devices.Add(external) |> ignore

    override x.ExitSystem(ctx:SystemContext) =
        base.ExitSystem(ctx)
        let system = x._theSystem.Value
        let adjustVertexType() =
            logInfo "---- Adjusting elements"
            let dic = helper._causalTokenElements
            let dups = dic |> seq
            for KeyValue(ctxInfo, vType) in dups do
                match ctxInfo.Tuples with
                | sys_, Some flow, parenting_, device::api::[] when (tryFindImportApiItem system [device; api]).IsSome ->
                    dic[ctxInfo] <- dic[ctxInfo] ||| GVT.CallApi
                | _ ->
                    let nameMatches =
                        helper._elements.Where(fun (KeyValue(ctx, _)) ->
                            ctx.Names = ctxInfo.Names && ctx.Flow = ctxInfo.Flow    // && ctx.Systems = ctxInfo.Systems
                            ).ToArray()
                    assert(vType.HasFlag(GVT.CausalToken))

                    if not x.ParserHelper.ParserOptions.IsSubSystemParsing then
                        noop()

                    let vt =  (vType &&& ~~~GVT.CausalToken)
                    let types = nameMatches.Select(valueOfKeyValue).Fold((|||), GVT.None)
                    if vt.IsOneOf(GVT.None, GVT.Child) then
                        if nameMatches.isEmpty() then
                            match vt with
                            | GVT.None ->
                                dic[ctxInfo] <- dic[ctxInfo] ||| GVT.Segment
                            | GVT.Child ->
                                failwith "ERROR"

                                //if ctxInfo.Names.Length = 2 then
                                //    dic[ctxInfo] <- dic[ctxInfo] ||| GVT.CallApi        // todo : GVT.CallFlowReal 구분
                                //    //helper._elements.TryFind(fun (KeyValue(ctx, _)) -> ctx.
                                //    //failwith "ERROR"

                            | _ ->
                                failwith "ERROR"
                        else
                            match types with
                            | GVT.AliaseKey ->
                                dic[ctxInfo] <- dic[ctxInfo] ||| GVT.CallAliasKey ||| GVT.Segment
                            | GVT.AliaseMnemonic ->
                                dic[ctxInfo] <- dic[ctxInfo] ||| GVT.AliaseMnemonic
                            | _ ->
                                failwith "ERROR"
                        logWarn $"{ctxInfo.FullName} : {dic[ctxInfo]} // from {vType}"

        let createNonParentedReals() =
            let sys = helper.TheSystem.Value
            for KeyValue(ctxInfo, vType) in helper._causalTokenElements do
                match vType with
                | HasFlag GVT.Segment ->
                    if vType.HasFlag(GVT.Parenting) then
                        let parent = findGraphVertex sys ctxInfo.NameComponents
                        assert(parent <> null)
                    elif ctxInfo.Names.Length = 1 then
                        let flow = findGraphVertex sys [yield ctxInfo.System.Value; yield ctxInfo.Flow.Value] // ctxInfo.Flow.Value.N
                        Real.Create(ctxInfo.Names[0], flow:?>Flow) |> ignore
                    else
                        ()  // e.g My/F/F2.Seg1 : 해당 real 생성은 다른 flow 의 역할임.
                | _ ->
                    ()
                logDebug $"{ctxInfo.FullName} : {vType}"

        let dumpTokens (tokens:Dictionary<ContextInformation, GVT>) (msg:string) =
            logInfo "%s" msg
            for KeyValue(ctxInfo, vType) in tokens do
                logDebug $"{ctxInfo.FullName} : {vType}"
        let dumpCausalTokens = dumpTokens helper._causalTokenElements

        dumpCausalTokens "---- Original Causal token elements"

        adjustVertexType()
        createNonParentedReals()

        logInfo "---- Spit results"
        logDebug "%s" <| helper.TheSystem.Value.Spit().Dump()

        dumpCausalTokens "---- All Causal token elements"

        logInfo "---- Non Causal token elements"
        for KeyValue(ctxInfo, vType) in helper._elements do
            logDebug $"{ctxInfo.FullName} : {vType}"
