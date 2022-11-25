namespace rec Engine.Parser.FS

open System.Linq
open System.IO

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type DsParser
open System.Collections.Generic


/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
/// Element path map 구성
///   - Parenting, Child, alias, Api
/// </summary>
type DsParserListener(parser:dsParser, options:ParserOptions) =
    inherit dsParserBaseListener()
    do
        parser.Reset()

    member val OptLoadedSystemName:string option = None with get, set
    member val ParserOptions = options with get, set
    member val AntlrParser = parser

    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val TheSystem:DsSystem = getNull<DsSystem>() with get, set

    /// parser rule context 가 어느 시스템에 속한 것인지를 판정하기 위함.  Loaded system 의 context 와 Main system 의 context 구분 용도.
    member val internal RuleDictionary = Dictionary<ParserRuleContext, string>()

    override x.EnterEveryRule(ctx:ParserRuleContext) =
        match x.OptLoadedSystemName with
        | Some systemName -> x.RuleDictionary.Add(ctx, systemName)
        | None -> ()


    override x.EnterSystem(ctx:SystemContext) =
        match options.LoadedSystemName with
        | Some systemName ->
                x.OptLoadedSystemName <- Some systemName
                x.RuleDictionary.Add(ctx, systemName)
        | _ -> ()

        match ctx.TryFindFirstChild<SysBlockContext>() with
        | Some sysBlockCtx_ ->
            let name = options.LoadedSystemName |? (ctx.systemName().GetText().DeQuoteOnDemand())
            let host =
                let hostSpec =
                    option {
                        let! sysHeader = ctx.TryFindFirstChild<SysHeaderContext>()
                        let! hostCtx = sysHeader.TryFindFirstChild<HostContext>()
                        return hostCtx.GetText()
                    }
                match hostSpec with
                | Some name -> name
                | None -> null
            x.TheSystem <- DsSystem.Create(name, host)
            tracefn($"System: {name}")
        | None ->
            failwith "ERROR"

    override x.ExitSystem(ctx:SystemContext) = x.OptLoadedSystemName <- None

    override x.EnterFlowBlock(ctx:FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        Flow.Create(flowName, x.TheSystem) |> ignore

    override x.EnterParentingBlock(ctx:ParentingBlockContext) =
        tracefn($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().TryGetName().Value
        let oci = x.GetObjectContextInformation(x.TheSystem, ctx)
        let flow = oci.Flow.Value
        Real.Create(name, flow) |> ignore


    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let system = x.TheSystem
        let interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>().Value
        let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 추후에 채움..
        let api = ApiInterface.Create(interfaceName, system)
        system.ApiInterfaces.Add(api) |> ignore

    override x.EnterInterfaceResetDef(ctx:InterfaceResetDefContext) =
        // I1 <||> I2 <||> I3;  ==> [| I1; <||>; I2; <||>; I3; |]
        let terms =
            let pred = fun (tree:IParseTree) -> tree :? Identifier1Context || tree :? CausalOperatorResetContext
            ctx.Descendants<RuleContext>(false, pred)
                .Select(fun ctx -> ctx.GetText())
                .ToArray()

        // I1 <||> I2 와 I2 <||> I3 에 대해서 해석
        for triple in (terms |> Array.windowed2 3 2) do
            if triple.Length = 3 then
                let opnd1, op, opnd2 = triple[0], triple[1], triple[2]
                let ri_ = ApiResetInfo.Create(x.TheSystem, opnd1, op.ToModelEdge(), opnd2)
                ()

    member private x.GetFilePath(fileSpecCtx:FileSpecContext) =
        let simpleFilePath = fileSpecCtx.TryFindFirstChild<FilePathContext>().Value.GetText().DeQuoteOnDemand()
        let absoluteFilePath =
            let dir = x.ParserOptions.ReferencePath
            [simpleFilePath; $"{dir}\\{simpleFilePath}"].First(fun f -> File.Exists(f))
        absoluteFilePath, simpleFilePath


    override x.EnterLoadDeviceBlock(ctx:LoadDeviceBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let loadedName = ctx.CollectNameComponents().Combine()
        x.TheSystem.LoadDeviceAs(loadedName, absoluteFilePath, simpleFilePath) |> ignore

    override x.EnterLoadExternalSystemBlock(ctx:LoadExternalSystemBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let loadedName = ctx.CollectNameComponents().Combine()
        x.TheSystem.LoadExternalSystemAs(loadedName, absoluteFilePath, simpleFilePath) |> ignore


    member x.CreateVertices(ctx:SystemContext) = createVertices x ctx

    member x.GetContextInformation(parserRuleContext:ParserRuleContext) =      // collectUpwardContextInformation
        let ctx = parserRuleContext
        let system =
            match x.RuleDictionary.TryFind(parserRuleContext) with
            | Some ruleName -> Some ruleName
            | None -> parserRuleContext.TryGetSystemName()

        let flow      = ctx.TryFindFirstAscendant<FlowBlockContext>(true).Bind(fun b -> b.TryFindIdentifier1FromContext())
        let parenting = ctx.TryFindFirstAscendant<ParentingBlockContext>(true).Bind(fun b -> b.TryFindIdentifier1FromContext())
        let ns        = ctx.CollectNameComponents().ToFSharpList()
        {   ContextType = ctx.GetType();
            System = system; Flow = flow; Parenting = parenting; Names = ns }

    member x.GetObjectContextInformation(system:DsSystem, parserRuleContext:ParserRuleContext) =
        let ci = x.GetContextInformation(parserRuleContext)
        assert(system.Name = ci.System.Value)
        let flow = ci.Flow.Bind(fun fn -> system.TryFindFlow(fn))
        let parenting =
            option {
                let! flow = flow
                let! parentingName = ci.Parenting
                return! flow.Graph.TryFindVertex<Real>(parentingName)
            }
        { System = system; Flow = flow; Parenting = parenting; NamedContextInformation = ci }

    member x.TryFindVertex(ctx:CausalTokenContext):Vertex option =
        let ci = x.GetContextInformation ctx
        option {
            let! parentWrapper = x.TheSystem.TryFindParentWrapper(ci)
            let graph = parentWrapper.GetGraph()
            match ci.Names with
            | ofn_::ofrn_::[] ->      // of(r)n: other flow (real) name
                return! graph.TryFindVertex(ci.Names.Combine())
            | callOrAlias::[] ->
                return! graph.TryFindVertex(callOrAlias)
            | _ ->
                failwith "ERROR"
        }


    member x.ProcessCausalPhrase(ctx:CausalPhraseContext) =
        let system = x.TheSystem
        let ci = x.GetContextInformation ctx
        let oci = x.GetObjectContextInformation(system, ctx)

        let children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
        for (n, ctx) in children|> Seq.indexed do
            assert( if n % 2 = 0 then ctx :? CausalTokensCNFContext else ctx :? CausalOperatorContext)



        (*
            children[0] > children[2] > children[4]     where (child[1] = '>', child[3] = '>')
            ===> children[0] > children[2],
                    children[2] > children[4]

            e.g "A, B > C, D > E"
            ===> children[0] = {A; B},
                    children[2] = {C; D},
                    children[4] = {E},

            *)
        for triple in (children |> Array.windowed2 3 2) do
            if triple.Length = 3 then
                let lefts = triple[0].Descendants<CausalTokenContext>()
                let op = triple[1].GetText()
                let rights = triple[2].Descendants<CausalTokenContext>()

                let findVertex tokenCtx =
                    match x.TryFindVertex tokenCtx with
                    | Some v -> v
                    | None -> raise <| ParserException($"ERROR: failed to find [{tokenCtx.GetText()}]", ctx)
                let lvs = lefts.Select(findVertex)
                let rvs = rights.Select(findVertex)
                let mei = ModelingEdgeInfo<Vertex>(lvs, op, rvs)
                match oci.Parenting, oci.Flow with
                | Some parenting, _ -> parenting.CreateEdge(mei)
                | None, Some flow -> flow.CreateEdge(mei)
                | _ -> failwith "ERROR"
                |> ignore

[<AutoOpen>]
module ParserRuleContextModule =
    let private createCallDef (system:DsSystem) (ctx:CallListingContext) =
        let callName =  ctx.TryFindFirstChild<CallNameContext>().Map(getText).Value
        let apiDefCtxs = ctx.Descendants<CallApiDefContext>().ToArray()
        let getAddress (addressCtx:IParseTree) =
            addressCtx.TryFindFirstChild<AddressItemContext>().Map(getText).Value
        let apiItems =
            [   for apiDefCtx in apiDefCtxs do
                let apiPath = apiDefCtx.CollectNameComponents() |> List.ofSeq // e.g ["A"; "+"]
                match apiPath with
                | device::api::[] ->
                    let apiItem =
                        option {
                            let! apiPoint = tryFindCallingApiItem system device api
                            let! addressCtx = ctx.TryFindFirstChild<AddressTxRxContext>()
                            let! txAddressCtx = addressCtx.TryFindFirstChild<TxContext>()
                            let! rxAddressCtx = addressCtx.TryFindFirstChild<RxContext>()
                            let tx = getAddress(txAddressCtx)
                            let rx = getAddress(rxAddressCtx)

                            tracefn $"TX={tx} RX={rx}"
                            return ApiCallDef(apiPoint, tx, rx)
                        }
                    match apiItem with
                    | Some apiItem -> yield apiItem
                    | _ -> failwith "ERROR"

                | _ -> failwith "ERROR"
            ]
        assert(apiItems.Any())
        Call(callName, apiItems) |> system.Calls.Add



    let private fillInterfaceDef (system:DsSystem) (ctx:InterfaceDefContext) =
        let findSegments(fqdns:Fqdn[]):Real[] =
            fqdns
                .Where(fun fqdn -> fqdn <> null)
                .Select(fun s -> system.TryFindGraphVertex<Real>(s))
                .Tap(fun x -> assert(x.IsSome))
                .Choose(id)
                .ToArray()
        let isWildcard(cc:Fqdn):bool = cc.Length = 1 && cc[0] = "_"
        let collectCallComponents(ctx:CallComponentsContext):Fqdn[] =
            ctx.Descendants<Identifier123Context>()
                .Select(collectNameComponents)
                .ToArray()
        option {
            let! interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>()
            let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]
            let! api = system.ApiInterfaces.TryFind(nameEq interfaceName)
            let ser =   // { start ~ end ~ reset }
                ctx.Descendants<CallComponentsContext>()
                    .Map(collectCallComponents)
                    .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || isWildcard(cc))))
                    .Select(fun callCompnents -> callCompnents.Select(fun cc -> if isWildcard(cc) then null else cc.Prepend(system.Name).ToArray()).ToArray())
                    .ToArray()

            let n = ser.Length

            assert(n = 2 || n = 3)
            api.AddTXs(findSegments(ser[0])) |> ignore
            api.AddRXs(findSegments(ser[1])) |> ignore
        } |> ignore



    let private createAliasDef (x:DsParserListener) (ctx:AliasListingContext) =
        let system = x.TheSystem
        let ci = x.GetContextInformation ctx
        option {
            let! flow = tryFindFlow system ci.Flow.Value
            let! aliasKeys = ctx.TryFindFirstChild<AliasDefContext>().Map(collectNameComponents)
            let mnemonics = ctx.Descendants<AliasMnemonicContext>().Select(getText).ToArray()
            let ad = AliasDef(aliasKeys, None, mnemonics)
            flow.AliasDefs.Add(aliasKeys, ad)
            return ad
        }

    let private fillTargetOfAliasDef (x:DsParserListener) (ctx:AliasListingContext) =
        let system = x.TheSystem
        let ci = x.GetContextInformation ctx
        option {
            let! flow = tryFindFlow system ci.Flow.Value
            let mnemonics = ctx.Descendants<AliasMnemonicContext>().Select(getText).ToArray()
            let! aliasKeys = ctx.TryFindFirstChild<AliasDefContext>().Map(collectNameComponents)
            let target =
                match aliasKeys.ToFSharpList() with
                | t::[] ->
                    let realTargetCandidate = flow.Graph.TryFindVertex<Real>(t)
                    let callTargetCandidate = tryFindCall system t
                    match realTargetCandidate, callTargetCandidate with
                    | Some real, None -> Some (AliasTargetReal real)
                    | None, Some call -> Some (AliasTargetCall call)
                    | Some _, Some _ -> failwith "Name duplicated."
                    | _ -> failwith "Failed to find"
                | otherFlow::real::[] ->
                    match tryFindReal system otherFlow real with
                    | Some otherFlowReal -> Some (AliasTargetReal otherFlowReal)
                    | _ -> failwith "Failed to find"
                | _ ->
                    failwith "ERROR"

            flow.AliasDefs[aliasKeys].AliasTarget <- target
        }


    /// system context 아래에 기술된 모든 vertex 들을 생성한다.
    let createVertices (x:DsParserListener) (ctx:SystemContext) =
        let system = x.TheSystem
        let sysctx = x.AntlrParser.system()

        let getContainerChildPair(ctx:ParserRuleContext) : ParentWrapper option * NamedContextInformation =
            let ci = x.GetContextInformation(ctx)
            let system = x.TheSystem
            let parentWrapper = system.TryFindParentWrapper(ci)
            parentWrapper, ci

        let tokenCreator (cycle:int) =
            let candidateCtxs:ParserRuleContext list = [
                yield! sysctx.Descendants<Identifier12ListingContext>().Cast<ParserRuleContext>()
                yield! sysctx.Descendants<CausalTokenContext>().Cast<ParserRuleContext>()
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
                            VertexOtherFlowRealCall.Create(otherFlowReal, parent) |> ignore

                            tracefn $"{ofn}.{ofrn} should already have been created."
                        | _, q::[] -> ()
                        | _, ofn::ofrn::[] -> ()
                        | _ ->
                            failwith "ERROR"
                        noop()
            loop

        let createNonParentedRealVertex = tokenCreator 0
        let createCallOrAliasVertex = tokenCreator 1


        for ctx in sysctx.Descendants<CallListingContext>() do
            createCallDef x.TheSystem ctx


        for ctx in sysctx.Descendants<AliasListingContext>() do
            createAliasDef x ctx |> ignore

        createNonParentedRealVertex()

        for ctx in sysctx.Descendants<AliasListingContext>() do
            fillTargetOfAliasDef x ctx |> ignore

        createCallOrAliasVertex()


        for ctx in sysctx.Descendants<InterfaceDefContext>() do
            fillInterfaceDef x.TheSystem ctx |> ignore

        guardedValidateSystem system


[<AutoOpen>]
module ParserLoadApiModule =
    let externalSystemCaches = Dictionary<string, DsSystem>()

    (* 외부에서 구조적으로 system 을 build 할 때에 사용되는 API *)
    type DsSystem with
        member x.LoadDeviceAs (loadedName:string, absoluteFilePath:string, userSpecifiedFilePath:string) =
            let device =
                fwdLoadDevice <| {
                    ContainerSystem = x
                    AbsoluteFilePath = absoluteFilePath
                    UserSpecifiedFilePath = userSpecifiedFilePath
                    LoadedName = loadedName }
            x.AddDevice(device) |> ignore
            device

        member x.LoadExternalSystemAs (loadedName:string, absoluteFilePath:string, userSpecifiedFilePath:string) =
            let external =
                let param = {
                    ContainerSystem = x
                    AbsoluteFilePath = absoluteFilePath
                    UserSpecifiedFilePath = userSpecifiedFilePath
                    LoadedName = loadedName
                }
                match externalSystemCaches.TryFind(absoluteFilePath) with
                | Some existing ->
                    ExternalSystem(existing, param)
                | None ->
                    let exSystem = fwdLoadExternalSystem param
                    externalSystemCaches.Add(absoluteFilePath, exSystem.ReferenceSystem) |> ignore
                    exSystem
            x.AddDevice(external) |> ignore
            external

        static member ClearExternalSystemCaches() = externalSystemCaches.Clear()
        static member ExternalSystemCaches = externalSystemCaches |> seq
