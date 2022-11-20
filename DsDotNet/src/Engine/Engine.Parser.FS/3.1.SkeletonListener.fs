namespace Engine.Parser.FS

open System.Linq
open System.IO

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type DsParser


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
            let name = helper.ParserOptions.LoadedSystemName |? (ctx.systemName().GetText().DeQuoteOnDemand())
            let host =
                match tryFindFirstChild<HostContext>(ctx) with
                | Some hostCtx -> hostCtx.GetText()
                | None -> null
            //let name = helper.ParserOptions.LoadedSystemName
            helper.TheSystem <- DsSystem.Create(name, host)
            tracefn($"System: {name}")
        | None ->
            failwith "ERROR"

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        helper._flow <- Flow.Create(flowName, helper.TheSystem)

    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        helper._parentingBlockContexts.Add(ctx)
        tracefn($"Parenting: {ctx.GetText()}")
        let name = tryGetName(ctx.identifier1()).Value
        helper._parenting <- Real.Create(name, helper._flow)



    override x.EnterCausalPhrase(ctx:CausalPhraseContext) =
        helper._causalPhraseContexts.Add(ctx)

    override x.EnterIdentifier12Listing(ctx:Identifier12ListingContext) =
        helper._identifier12ListingContexts.Add(ctx)

    override x.EnterCausalToken(ctx:CausalTokenContext) =
        helper._causalTokenContext.Add(ctx)




    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        helper._interfaceDefContexts.Add(ctx)

        let system = helper.TheSystem
        let interrfaceNameCtx = tryFindFirstChild<InterfaceNameContext>(ctx).Value
        let interfaceName = collectNameComponents(interrfaceNameCtx)[0]

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 추후에 채움..
        let api = ApiItem4Export.Create(interfaceName, system)
        let hash = system.ApiItems4Export
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
                let ri_ = ApiResetInfo.Create(helper.TheSystem, opnd1, op.ToModelEdge(), opnd2)
                ()

    member private x.GetFilePath(fileSpecCtx:FileSpecContext) =
        let simpleFilePath = tryFindFirstChild<FilePathContext>(fileSpecCtx).Value.GetText().DeQuoteOnDemand()
        let absoluteFilePath =
            let dir = helper.ParserOptions.ReferencePath
            [simpleFilePath; $"{dir}\\{simpleFilePath}"].First(fun f -> File.Exists(f))
        absoluteFilePath, simpleFilePath


    override x.EnterLoadDeviceBlock(ctx:LoadDeviceBlockContext) =
        helper._deviceBlockContexts.Add(ctx)
        let fileSpecCtx = tryFindFirstChild<FileSpecContext>(ctx).Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let device =
            let loadedName = collectNameComponents(ctx).Combine()
            fwdLoadDevice helper.TheSystem (absoluteFilePath, simpleFilePath) loadedName
        helper.TheSystem.Devices.Add(device) |> ignore

    override x.EnterLoadExternalSystemBlock(ctx:LoadExternalSystemBlockContext) =
        helper._externalSystemBlockContexts.Add(ctx)
        let fileSpecCtx = tryFindFirstChild<FileSpecContext>(ctx).Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let external =
            let ipSpecCtx = tryFindFirstChild<IpSpecContext>(ctx).Value
            let ip = tryFindFirstChild<EtcNameContext>(ipSpecCtx).Value.GetText()
            let loadedName = collectNameComponents(ctx).Combine()
            fwdLoadExternalSystem helper.TheSystem (absoluteFilePath, simpleFilePath) loadedName
        helper.TheSystem.Devices.Add(external) |> ignore

    override x.EnterAliasListing(ctx:AliasListingContext) =
        helper._aliasListingContexts.Add(ctx)

    override x.EnterCallListing(ctx:CallListingContext) =
        helper._callListingContexts.Add(ctx)

    override x.ExitSystem(ctx:SystemContext) =
        base.ExitSystem(ctx)
        let system = helper.TheSystem

        let getContainerChildPair(ctx:ParserRuleContext) : ParentWrapper option * NamedContextInformation =
            let ci = getContextInformation ctx
            let system = helper.TheSystem
            let parentWrapper = tryFindParentWrapper system ci
            parentWrapper, ci

        let tokenCreator (cycle:int) =
            let candidateCtxs:ParserRuleContext list = [
                yield! helper._identifier12ListingContexts.Cast<ParserRuleContext>()
                yield! helper._causalTokenContext.Cast<ParserRuleContext>()
            ]

            let isCallName (parentWrapper:ParentWrapper) name = tryFindCall system name |> Option.isSome
            let isAliasMnemonic (parentWrapper:ParentWrapper) name =
                let flow = parentWrapper.GetFlow()
                tryFindAliasDefWithMnemonic flow name |> Option.isSome
            let isCallOrAlias pw name =
                if name = "Ap1" then
                    noop()
                let xxx = isCallName pw name
                let yyy = isAliasMnemonic pw name
                isCallName pw name || isAliasMnemonic pw name

            let tryCreateCallOrAlias (parentWrapper:ParentWrapper) name =
                let flow = parentWrapper.GetFlow()
                let tryCall = tryFindCall system name
                let tryAliasDef = tryFindAliasDefWithMnemonic flow name
                option {
                    match tryCall, tryAliasDef with
                    | Some call, None ->
                        return VertexCall.Create(name, call, parentWrapper) :> Indirect
                    | None, Some aliasDef ->
                        let aliasTarget = tryFindAliasTarget flow name |> Option.get
                        return VertexAlias.Create(name, aliasTarget, parentWrapper) :> Indirect
                    | None, None -> return! None
                    | _ ->
                        failwith "ERROR: duplicated"
                }
            let candidates = candidateCtxs.Select(getContainerChildPair)

            let loop () =
                for (optParent, ctxInfo) in candidates do
                    let parent = optParent.Value
                    let existing = parent.GetGraph().TryFindVertex(ctxInfo.GetRawName())
                    match existing with
                    | Some v -> tracefn $"{v.Name} already exists.  Skip creating it."
                    | None ->
                        match cycle, ctxInfo.Names with
                        | 0, q::[] when not <| isCallOrAlias parent q ->
                            let flow = parent.GetCore() :?> Flow
                            Real.Create(q, flow) |> ignore

                        | 1, q::[] when (isCallOrAlias parent q) ->
                            match tryCreateCallOrAlias parent q with
                            | Some _ -> ()
                            | None ->
                                failwith "ERROR"
                        | 1, ofn::ofrn::[] ->
                            let otherFlowReal = tryFindReal system ofn ofrn |> Option.get
                            VertexOtherFlowRealCall.Create(ofn, ofrn, otherFlowReal, parent) |> ignore

                            tracefn $"{ofn}.{ofrn} should already have been created."
                        | _, q::[] -> ()
                        | _, ofn::ofrn::[] -> ()
                        | _ ->
                            failwith "ERROR"
                        noop()
            loop

        let createNonParentedRealVertex = tokenCreator 0
        let createCallOrAliasVertex = tokenCreator 1


        //let dumpTokens (tokens:Dictionary<ContextInformation, GVT>) (msg:string) =
        //    logInfo "%s" msg
        //    for KeyValue(ctxInfo, vType) in tokens do
        //        logDebug $"{ctxInfo.FullName} : {vType}"
        //let dumpCausalTokens = dumpTokens helper._causalTokenElements

        //dumpCausalTokens "---- Original Causal token elements"

        createCallDefs helper
        createAliasDefs helper
        createNonParentedRealVertex()
        fillAliasDefsTarget helper
        createCallOrAliasVertex()
        fillInterfaceDefs helper

        guardedValidateSystem system

        //dumpCausalTokens "---- All Causal token elements"

