namespace rec Engine.Parser.FS

open System
open System.Linq
open System.IO

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type DsParser
open System.Collections.Generic

[<AutoOpen>]
module ListnerCommonFunctionGenerator =
    let commonFunctionExtractor (input:ParserRuleContext) =
        input.Descendants<FuncSetContext>().ToArray()
        |> Seq.map(fun fs ->
            option {
                let! nameCtx = fs.TryFindFirstChild<Identifier2Context>()
                let! funcs = fs.Descendants<FuncDefContext>()
                return nameCtx.CollectNameComponents()[0], funcs
            } |> Option.get
        )
        |> Map.ofSeq

    let commonFunctionSetter
        (targetName:string) (functionMap:Map<string, ResizeArray<FuncDefContext>>)
      =
        if functionMap.ContainsKey(targetName) then
            [
                for func in functionMap[targetName] do
                    option {
                        let! funcName = func.TryFindFirstChild<FuncNameContext>()
                        let! parameters = func.Descendants<ArgumentContext>().Select(fun argCtx -> argCtx.GetText()).ToArray()
                        return new Func(funcName.GetText(), parameters)
                    } |> Option.get
            ]
        else
            List.empty
        |> Enumerable.ToHashSet

/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
/// Element path map 구성
///   - Parenting, Child, alias, Api
/// </summary>
type DsParserListener(parser:dsParser, options:ParserOptions) =
    inherit dsBaseListener()
    do
        parser.Reset()

    member val AntlrParser = parser
    member val ParserOptions = options with get, set

    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem*string)>()

    member val TheSystem:DsSystem = getNull<DsSystem>() with get, set

    /// 하나의 main.ds 를 loading 할 때, 외부 system 을 copy/reference 로 loading 시, 해당 system 의 구분을 위해서 사용
    member val OptLoadedSystemName:string option = None with get, set
    /// parser rule context 가 어느 시스템에 속한 것인지를 판정하기 위함.  Loaded system 의 context 와 Main system 의 context 구분 용도.
    member val internal Rule2SystemNameDictionary = Dictionary<ParserRuleContext, string>()

    override x.EnterEveryRule(ctx:ParserRuleContext) =
        match x.OptLoadedSystemName with
        | Some systemName -> x.Rule2SystemNameDictionary.Add(ctx, systemName)
        | None -> ()


    override x.EnterSystem(ctx:SystemContext) =
        match options.LoadedSystemName with
        | Some systemName ->
                x.OptLoadedSystemName <- Some systemName
                x.Rule2SystemNameDictionary.Add(ctx, systemName)
        | _ -> ()

        match ctx.TryFindFirstChild<SysBlockContext>() with
        | Some _sysBlockCtx ->
            let name = options.LoadedSystemName |? (ctx.systemName().GetText().DeQuoteOnDemand())
            let hostIp =
                let hostSpec =
                    option {
                        let! sysHeader = ctx.TryFindFirstChild<SysHeaderContext>()
                        let! hostCtx = sysHeader.TryFindFirstChild<HostContext>()
                        return hostCtx.GetText()
                    }
                match hostSpec with
                | Some name -> name
                | None -> null

            let repo = options.ShareableSystemRepository

            match options.LoadingType, options.AbsoluteFilePath with
            | DuExternal, Some fp when repo.ContainsKey(fp) -> x.TheSystem <- repo[fp]
            | DuExternal, _ ->
                let registerSystem (sys:DsSystem) =
                    match options.AbsoluteFilePath with
                    | Some fp -> repo.Add(fp, sys)
                    | _ -> ()

                x.TheSystem <- let exSys = DsSystem(name, hostIp)
                               registerSystem exSys
                               exSys
            | _ ->
                x.TheSystem <- DsSystem(name, hostIp)
            tracefn($"System: {name}")
        | None ->
            failwithlog "ERROR"

    override x.ExitSystem(_ctx:SystemContext) = x.OptLoadedSystemName <- None

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
        let api = ApiItem.Create(interfaceName, system)
        system.ApiItems.Add(api) |> ignore

    override x.EnterInterfaceResetDef(ctx:InterfaceResetDefContext) =
        // I1 <||> I2 <||> I3;  ==> [| I1; <||>; I2; <||>; I3; |]
        let terms =
            let pred = fun (tree:IParseTree) -> tree :? Identifier1Context || tree :? CausalOperatorResetContext
            [| for des in ctx.Descendants<RuleContext>(false, pred) do
                des.GetText() |]

        // I1 <||> I2 와 I2 <||> I3 에 대해서 해석
        for triple in (terms |> Array.windowed2 3 2) do
            if triple.Length = 3 then
                let opnd1, op, opnd2 = triple[0], triple[1], triple[2]
                ApiResetInfo.Create(x.TheSystem, opnd1, op.ToModelEdge(), opnd2) |> ignore

    member private x.GetFilePath(fileSpecCtx:FileSpecContext) =
        let simpleFilePath = fileSpecCtx.TryFindFirstChild<FilePathContext>().Value.GetText().DeQuoteOnDemand()
        let envPaths = collectEnvironmentVariablePaths()
        let targetPath(directory:string) = [
            simpleFilePath
            $"{directory}/{simpleFilePath}"
            for path in envPaths do
                $"{path}/{simpleFilePath}" ] |> fileExistChecker

        let absoluteFilePath =
            let dir = x.ParserOptions.ReferencePath
            targetPath dir
        absoluteFilePath, simpleFilePath

    override x.EnterLoadDeviceBlock(ctx:LoadDeviceBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let loadedName = ctx.CollectNameComponents().Combine()
        x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath) |> ignore

    override x.EnterLoadExternalSystemBlock(ctx:LoadExternalSystemBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let absoluteFilePath, simpleFilePath = x.GetFilePath(fileSpecCtx)
        let loadedName = ctx.CollectNameComponents().Combine()
        let optIpSpec =
            option {
                let! ipSpecCtx = ctx.TryFindFirstChild<IpSpecContext>()
                let! host = ipSpecCtx.TryFindFirstChild<HostContext>()
                return deQuote <| host.GetText()
            }
        x.TheSystem.LoadExternalSystemAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath, optIpSpec) |> ignore

    override x.EnterCodeBlock(ctx:CodeBlockContext) =
        let code = ctx.GetOriginalText()
        x.TheSystem.OriginalCodeBlocks.Add code
        let pureCode = code.Substring(3, code.Length-6)       // 처음과 끝의 "<@{" 와 "}@>" 제외
        let statements = pureCode |> parseCode options.Storages
        x.TheSystem.Statements.AddRange statements

    /// parser rule context 에 대한 이름 기준의 정보를 얻는다.  system 이름, flow 이름, parenting 이름 등
    member x.GetContextInformation(parserRuleContext:ParserRuleContext) =      // collectUpwardContextInformation
        let ctx = parserRuleContext
        let system =
            match x.Rule2SystemNameDictionary.TryFind(parserRuleContext) with
            | Some systemName -> Some systemName
            | None -> parserRuleContext.TryGetSystemName()

        let flow      = ctx.TryFindFirstAscendant<FlowBlockContext>(true).Bind(fun b -> b.TryFindIdentifier1FromContext())
        let parenting = ctx.TryFindFirstAscendant<ParentingBlockContext>(true).Bind(fun b -> b.TryFindIdentifier1FromContext())
        let ns        = ctx.CollectNameComponents().ToFSharpList()
        {   ContextType = ctx.GetType();
            System = system; Flow = flow; Parenting = parenting; Names = ns }

    /// parser rule context 에 대한 객체 기준의 정보를 얻는다.  DsSystem 객체, flow 객체, parenting 객체 등
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

    /// 인과 token 으로 사용된 context 에 해당하는 vertex 를 찾는다.
    member x.TryFindVertex(ctx:CausalTokenContext):Vertex option =
        let ci = x.GetContextInformation ctx
        option {
            let! parentWrapper = x.TheSystem.TryFindParentWrapper(ci)
            let graph = parentWrapper.GetGraph()
            match ci.Names with
            | _ofn::_ofrn::[] ->      // of(r)n: other flow (real) name
                return! graph.TryFindVertex(ci.Names.Combine())
            | callOrAlias::[] ->
                return! graph.TryFindVertex(callOrAlias)
            | _ ->
                failwithlog "ERROR"
        }


    member x.ProcessCausalPhrase(ctx:CausalPhraseContext) =
        let system = x.TheSystem
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
                | _ -> failwithlog "ERROR"
                |> ignore

    /// system context 아래에 기술된 모든 vertex 들을 생성한다.
    member x.CreateVertices (_ctx:SystemContext) =
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

            let isCallName (pw:ParentWrapper, Fqdn(vetexPath)) =
                let flow = pw.GetFlow()
                tryFindCall flow.System vetexPath |> Option.isSome

            let isAliasMnemonic (pw:ParentWrapper, mnemonic:string) =
                let flow = pw.GetFlow()
                tryFindAliasDefWithMnemonic flow mnemonic |> Option.isSome


            let isJobName (pw, name) =
                tryFindJob pw name |> Option.isSome

            let isJobOrAlias (pw:ParentWrapper, Fqdn(vetexPath)) =
                isJobName (pw.GetFlow().System, vetexPath.Last()) || isAliasMnemonic (pw, vetexPath.CombineQuoteOnDemand())

            let candidates = candidateCtxs.Select(getContainerChildPair)

            let loop () =
                for (optParent, ctxInfo) in candidates do
                    let parent = optParent.Value
                    let existing = parent.GetGraph().TryFindVertex(ctxInfo.GetRawName())
                    match existing with
                    | Some v -> tracefn $"{v.Name} already exists.  Skip creating it."
                    | None ->
                        match cycle, ctxInfo.Names with
                        | 0, r::[] when not <| (isJobOrAlias (parent, ctxInfo.Names)) ->
                            let flow = parent.GetCore() :?> Flow
                            if not <| isCallName (parent, ctxInfo.Names)
                            then
                                Real.Create(r, flow) |> ignore

                        | 1, c::[] when not <| (isAliasMnemonic (parent, ctxInfo.Names.CombineQuoteOnDemand())) ->
                            let job = tryFindJob system c |> Option.get
                            if job.DeviceDefs.any() then CallDev.Create(job, parent) |> ignore
                            if job.LinkDefs.any()   then CallSys.Create(job, parent) |> ignore

                        | 1, realorFlow::cr::[] when not <| isAliasMnemonic (parent, ctxInfo.Names.CombineQuoteOnDemand()) ->
                            let otherFlowReal = tryFindReal system realorFlow cr |> Option.get
                            RealOtherFlow.Create(otherFlowReal, parent) |> ignore
                            tracefn $"{realorFlow}.{cr} should already have been created."

                        | 2, q::[] when isAliasMnemonic (parent, ctxInfo.Names.CombineQuoteOnDemand()) ->
                            let flow = parent.GetFlow()
                            let aliasDef = tryFindAliasDefWithMnemonic flow (q.QuoteOnDemand()) |> Option.get
                            Alias.Create(q, aliasDef.AliasTarget.Value, parent) |> ignore

                        | _, _q::[] -> ()
                        | _, _ofn::_ofrn::[] -> ()
                        | _ ->
                            failwithlog "ERROR"
            loop

        let createRealVertex          = tokenCreator 0
        let createCallOrExRealVertex  = tokenCreator 1
        let createAliasVertex         = tokenCreator 2

        let fillInterfaceDef (system:DsSystem) (ctx:InterfaceDefContext) =
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
                let! api = system.ApiItems.TryFind(nameEq interfaceName)
                let ser =   // { start ~ end ~ reset }
                    ctx.Descendants<CallComponentsContext>()
                        .Map(collectCallComponents)
                        .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || isWildcard(cc))))
                        .Select(fun callCompnents -> callCompnents.Select(fun cc -> if isWildcard(cc) then null else cc.Prepend(system.Name).ToArray()).ToArray())
                        .ToArray()
                let lnk =
                    ctx.TryFindFirstChild<Identifier12Context>()
                        .Map(collectNameComponents)
                        .ToArray()
                let n = ser.Length

                match n with
                | 2 | 3 ->
                    api.AddTXs(findSegments(ser[0])) |> ignore
                    api.AddRXs(findSegments(ser[1])) |> ignore
                | _ ->
                    api.AddTXs(findSegments(lnk)) |> ignore
                    api.AddRXs(findSegments(lnk)) |> ignore
            } |> ignore

        let createTaskDevice (system:DsSystem) (ctx:JobBlockContext) =
            let callListings = ctx.Descendants<CallListingContext>().ToArray()
            let jobFuncs = commonFunctionExtractor ctx
            for callList in callListings do
                let getRawJobName = callList.TryFindFirstChild<EtcName1Context>().Value
                let jobName =  getRawJobName.GetText().DeQuoteOnDemand()
                let apiDefCtxs = callList.Descendants<CallApiDefContext>().ToArray()
                let getAddress (addressCtx:IParseTree) =
                    addressCtx.TryFindFirstChild<AddressItemContext>().Map(getText).Value
                let apiItems = [
                    for apiDefCtx in apiDefCtxs do
                        let apiPath = apiDefCtx.CollectNameComponents() |> List.ofSeq // e.g ["A"; "+"]
                        match apiPath with
                        | device::api::[] ->
                            let apiItem =
                                option {
                                    let! apiPoint = tryFindCallingApiItem system device api
                                    let! addressCtx = apiDefCtx.TryFindFirstChild<AddressInOutContext>()
                                    let! txAddressCtx = addressCtx.TryFindFirstChild<OutAddrContext>()
                                    let! rxAddressCtx = addressCtx.TryFindFirstChild<InAddrContext>()
                                    let tx = getAddress(txAddressCtx) |>replaceSkipAddress
                                    let rx = getAddress(rxAddressCtx) |>replaceSkipAddress

                                    tracefn $"TX={tx} RX={rx}"
                                    return TaskDev(apiPoint, rx, tx, device)
                                }
                            match apiItem with
                            | Some apiItem -> yield apiItem
                            | _ -> failwithlog "ERROR"

                        | _ -> failwithlog "ERROR"
                ]
                let funcSet = commonFunctionSetter jobName jobFuncs
                assert(apiItems.Any())
                let job = Job(jobName, apiItems.Cast<DsTask>() |> Seq.toList)
                job.SetFuncs(funcSet)
                job |> system.Jobs.Add

        let createTaskLink (system:DsSystem) (ctx:JobBlockContext) =
            let linkListings = ctx.Descendants<LinkListingContext>().ToArray()
            for linkDef in linkListings do
                let getRawLinkName = linkDef.TryFindFirstChild<EtcName1Context>().Value
                let linkName = getRawLinkName.GetText().DeQuoteOnDemand()
                let apiLinkPath =
                    linkDef.TryFindFirstChild<Identifier12Context>()
                        .Value.CollectNameComponents() |> List.ofSeq
                let linkInfo =
                    match apiLinkPath with
                    | exSys::api::[] ->
                        let apiItem = tryFindCallingApiItem system exSys api
                        match apiItem with
                        | Some apiItem -> apiItem, exSys
                        | _ -> failwithlog "ERROR"
                    | _ -> failwithlog "ERROR"
                let linkDef = TaskSys linkInfo
                let job = Job(linkName, [linkDef])
                job |> system.Jobs.Add

        let fillTargetOfAliasDef (x:DsParserListener) (ctx:AliasListingContext) =
            let system = x.TheSystem
            let ci = x.GetContextInformation ctx
            option {
                let! flow = tryFindFlow system ci.Flow.Value
                let! aliasKeys = ctx.TryFindFirstChild<AliasDefContext>().Map(collectNameComponents)
                let target =
                        let ns = aliasKeys.ToFSharpList()
                        match ns with
                        | rc::[] -> //Flow.R or Flow.C
                            match flow.System.TryFindReal flow.System flow.Name rc  with
                            | Some r -> r |> DuAliasTargetReal
                            | None ->
                                let vertex = flow.System.TryFindCall ([flow.Name;rc].ToArray()) |> Option.get
                                match vertex with
                                | :? CallSys as rs -> DuAliasTargetRealExSystem rs
                                | :? CallDev as c -> DuAliasTargetCall c
                                | _ -> failwithlog "ERROR"

                                //if call.IsNone then
                                //    let job = flow.System.TryFindRealOtherSystem ([flow.Name;rc].ToArray())
                                //    job |> Option.get |> DuAliasTargetRealExSystem
                                //else
                                //    call |> Option.get |> DuAliasTargetCall

                        | flowOrReal::rc::[] -> //FlowEx.R or Real.C
                            match tryFindFlow system flowOrReal with
                            | Some f -> f.Graph.TryFindVertex<Real>(rc)  |> Option.get |> DuAliasTargetReal
                            | None ->
                                //tryFindCall system ([flow.Name]@ns) |> Option.get |> DuAliasTargetCall
                                let vertex = tryFindCall system ([flow.Name]@ns) |> Option.get
                                match vertex with
                                | :? CallSys as rs -> DuAliasTargetRealExSystem rs
                                | :? CallDev as c -> DuAliasTargetCall c
                                | _ -> failwithlog "ERROR"
                        | _ ->
                            failwithlog "ERROR"


                flow.AliasDefs[aliasKeys].AliasTarget <- Some target
            }

        let createAliasDef (x:DsParserListener) (ctx:AliasListingContext) =
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

        let fillXywh (system:DsSystem) (listLayoutCtx:List<dsParser.LayoutBlockContext>) = 
            let tryParseAndReturn (text : string) =
                let mutable value = 0
                if Int32.TryParse(text, &value) then
                    value
                else
                    failwith "Conversion failed : xywh need to write into integer value"
            let genXywh (xywh:dsParser.XywhContext) = 
                new Xywh(
                    tryParseAndReturn(getText(xywh.x())),
                    tryParseAndReturn(getText(xywh.y())),
                    tryParseAndReturn(getText(xywh.w())),
                    tryParseAndReturn(getText(xywh.h()))
                )
            for layoutCtx in listLayoutCtx do
                let listPositionDefCtx = layoutCtx.Descendants<PositionDefContext>().ToList()
                for positionDef in listPositionDefCtx do
                    let nameCtx = positionDef.TryFindFirstChild<Identifier12Context>() |> Option.get
                    let name = getText nameCtx
                    let xywh = positionDef.TryFindFirstChild<XywhContext>() |> Option.get |> genXywh
                    match (collectNameComponents nameCtx).Count() with
                    | 1 -> 
                        let device = FindExtension.TryFindLoadedSystem(system, name) |> Option.get
                        device.Xywh <- xywh
                    | 2 -> 
                        system.LoadedSystems 
                        |> map(fun device -> device.ReferenceSystem.TryFindExportApiItem(collectNameComponents nameCtx))
                        |> iter(fun apiItem -> if not(apiItem.IsNone) then apiItem.Value.Xywh <- xywh)
                    | _ -> failwith "invalid name component"

        let fillFinished (system:DsSystem) (listFinishedCtx:List<dsParser.FinishBlockContext>) =
            for finishedCtx in listFinishedCtx do
                let listFinished = finishedCtx.Descendants<FinishTargetContext>().ToList()
                for finished in listFinished do
                    let fqdn = collectNameComponents finished // in array.. [0] : flow, [1] : real
                    let real = tryFindReal system fqdn[0] fqdn[1]
                    if not(real.IsNone) then
                        real.Value.Finished <- true
                    else
                        failwith $"Couldn't find target real object name {getText(finished)}"

        let fillDisabled (system:DsSystem) (listDisabledCtx:List<dsParser.DisableBlockContext>) =
            for disabledCtx in listDisabledCtx do
                let listDisabled = disabledCtx.Descendants<DisableTargetContext>().ToList()
                for disabled in listDisabled do
                    let fqdn = collectNameComponents disabled
                    let coin = system.TryFindCall fqdn
                    if not(coin.IsNone) then
                        (coin.Value :?> Call).Disabled <- true
                    else
                        failwith $"Couldn't find target coin object name {getText(disabled)}"
            
        let fillProperties (x:DsParserListener) (ctx:PropsBlockContext) = 
            let theSystem  = x.TheSystem
            //device, call에 layout xywh 채우기
            ctx.Descendants<LayoutBlockContext>().ToList() |> fillXywh theSystem
            //Real에 finished 채우기
            ctx.Descendants<FinishBlockContext>().ToList() |> fillFinished theSystem
            //Call에 disable, xywh 채우기
            ctx.Descendants<DisableBlockContext>().ToList() |> fillDisabled theSystem

        for ctx in sysctx.Descendants<JobBlockContext>() do
            createTaskDevice  x.TheSystem ctx
            createTaskLink x.TheSystem ctx

        for ctx in sysctx.Descendants<AliasListingContext>() do
            createAliasDef x ctx |> ignore

        //실제모델링 만들고
        createRealVertex()
        createCallOrExRealVertex()

        //AliasDef 생성후
        for ctx in sysctx.Descendants<AliasListingContext>() do
            fillTargetOfAliasDef x ctx |> ignore

        //Alias 만들기
        createAliasVertex()

        for ctx in sysctx.Descendants<InterfaceDefContext>() do
            fillInterfaceDef x.TheSystem ctx |> ignore
        
        for ctx in sysctx.Descendants<PropsBlockContext>() do
            fillProperties x ctx

        guardedValidateSystem system


[<AutoOpen>]
module ParserLoadApiModule =
    (* 외부에서 구조적으로 system 을 build 할 때에 사용되는 API *)
    type DsSystem with
        member x.LoadDeviceAs (systemRepo:ShareableSystemRepository, loadedName:string, absoluteFilePath:string, userSpecifiedFilePath:string) =
            let device =
                fwdLoadDevice <| {
                    ContainerSystem = x
                    AbsoluteFilePath = absoluteFilePath
                    UserSpecifiedFilePath = userSpecifiedFilePath
                    LoadedName = loadedName
                    ShareableSystemRepository = systemRepo
                    HostIp = None
                    LoadingType = DuDevice
                }
            x.AddLoadedSystem(device) |> ignore
            device

        member x.LoadExternalSystemAs (
            systemRepo:ShareableSystemRepository, loadedName:string
            , absoluteFilePath:string, userSpecifiedFilePath:string
            , ipSpec:string option
        ) =
            let external =
                let param = {
                    ContainerSystem = x
                    AbsoluteFilePath = absoluteFilePath
                    UserSpecifiedFilePath = userSpecifiedFilePath
                    LoadedName = loadedName
                    ShareableSystemRepository = systemRepo
                    HostIp = ipSpec
                    LoadingType = DuExternal
                }
                match systemRepo.TryFind(absoluteFilePath) with
                | Some existing ->
                    ExternalSystem(existing, param) // 기존 loading 된 system share
                | None ->
                    let exSystem = fwdLoadExternalSystem param
                    assert( systemRepo.ContainsKey(absoluteFilePath) )
                    assert( systemRepo[absoluteFilePath] = exSystem.ReferenceSystem)
                    exSystem
            x.AddLoadedSystem(external) |> ignore
            external

