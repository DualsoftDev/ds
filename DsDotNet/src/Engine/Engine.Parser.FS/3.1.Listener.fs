namespace rec Engine.Parser.FS

open System
open System.Linq
open System.IO

open Antlr4.Runtime.Tree
open Antlr4.Runtime

open Dual.Common.Core.FS
open Engine.Parser
open Engine.Core
open Engine.Core.DsText
open type Engine.Parser.dsParser
open type DsParser
open System.Collections.Generic


[<AutoOpen>]
module ListnerCommonFunctionGenerator =
    let commonFunctionExtractor (funcCallCtxs: FuncCallContext array) (callName:string) (system:DsSystem) =
        if funcCallCtxs.Length > 1 
        then 
            failwithlog $"not support job multi function {callName}"

        if funcCallCtxs.any() 
            then 
                let funcName = funcCallCtxs.Head().funcCallName().GetText()
                Some (system.Functions.First(fun f->f.Name = funcName))
            else None 

/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
/// Element path map 구성
///   - Parenting, Child, alias, Api
/// </summary>
type DsParserListener(parser: dsParser, options: ParserOptions) =
    inherit dsBaseListener()
    do parser.Reset()

    member val AntlrParser = parser
    member val ParserOptions = options with get, set

    /// button category 중복 check 용
    member val ButtonCategories = HashSet<(DsSystem * string)>()

    member val TheSystem: DsSystem = getNull<DsSystem> () with get, set

    /// 하나의 main.ds 를 loading 할 때, 외부 system 을 copy/reference 로 loading 시, 해당 system 의 구분을 위해서 사용
    member val OptLoadedSystemName: string option = None with get, set
    /// parser rule context 가 어느 시스템에 속한 것인지를 판정하기 위함.  Loaded system 의 context 와 Main system 의 context 구분 용도.
    member val internal Rule2SystemNameDictionary = Dictionary<ParserRuleContext, string>()

    override x.EnterEveryRule(ctx: ParserRuleContext) =
        match x.OptLoadedSystemName with
        | Some systemName -> x.Rule2SystemNameDictionary.Add(ctx, systemName)
        | None -> ()


    override x.EnterSystem(ctx: SystemContext) =
        match options.LoadedSystemName with
        | Some systemName ->
            x.OptLoadedSystemName <- Some systemName
            x.Rule2SystemNameDictionary.Add(ctx, systemName)
        | _ -> ()

        match ctx.TryFindFirstChild<SysBlockContext>() with
        | Some _sysBlockCtx ->
            let name =
                options.LoadedSystemName |? (ctx.systemName().GetText().DeQuoteOnDemand())

            

            let repo = options.ShareableSystemRepository

            match options.LoadingType, options.AbsoluteFilePath with
            | DuExternal, Some fp when repo.ContainsKey(fp) -> x.TheSystem <- repo[fp]
            | DuExternal, _ ->
                let registerSystem (sys: DsSystem) =
                    match options.AbsoluteFilePath with
                    | Some fp -> repo.Add(fp, sys)
                    | _ -> ()

                x.TheSystem <-
                    let exSys = DsSystem(name)
                    registerSystem exSys
                    exSys
            | _ -> x.TheSystem <- DsSystem(name)

            debugfn ($"System: {name}")
        | None -> failwithlog "ERROR"

    override x.ExitSystem(_ctx: SystemContext) = x.OptLoadedSystemName <- None

    override x.EnterFlowBlock(ctx: FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        Flow.Create(flowName, x.TheSystem) |> ignore

    override x.EnterParentingBlock(ctx: ParentingBlockContext) =
        debugfn ($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().TryGetName().Value
        let oci = x.GetObjectContextInformation(x.TheSystem, ctx)
        let flow = oci.Flow.Value
        Real.Create(name, flow) |> ignore


    override x.EnterInterfaceDef(ctx: InterfaceDefContext) =
        let system = x.TheSystem
        let interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>().Value
        let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 추후에 채움..
        let api = ApiItem.Create(interfaceName, system)
        system.ApiItems.Add(api) |> ignore

    override x.EnterInterfaceResetDef(ctx: InterfaceResetDefContext) =
        // I1 <|> I2 <|> I3;  ==> [| I1; <|>; I2; <|>; I3; |]
        let terms =
            let pred =
                fun (tree: IParseTree) -> tree :? Identifier1Context || tree :? CausalOperatorResetContext

            [| for des in ctx.Descendants<RuleContext>(false, pred) do
                   des.GetText() |]

        // I1 <||> I2 와 I2 <||> I3 에 대해서 해석
        let apis = terms.Where(fun f->f <> "<|>")
        let resets = apis.AllPairs(apis)
                         .Where(fun (l, r)-> l <> r) 
                         .DistinctBy(fun (l, r)->  [l;r].Order().JoinWith(";")) 
        for tuple in resets do
            let left, right = tuple
            let opnd1, op, opnd2 = left, "<|>", right
            ApiResetInfo.Create(x.TheSystem, opnd1, op.ToModelEdge(), opnd2) |> ignore

    member x.GetValidFile(fileSpecCtx: FileSpecContext) =
          fileSpecCtx
                    .TryFindFirstChild<FilePathContext>()
                    .Value.GetText()
                    .DeQuoteOnDemand()
                |> PathManager.getValidFile

    member private x.GetFilePath(relativeFilePath: string) =
        let absoluteFilePath =
            let fullPath =
                PathManager.getFullPath (relativeFilePath.ToFile()) (x.ParserOptions.ReferencePath.ToDirectory())

            fullPath

        absoluteFilePath, relativeFilePath

        
    member private x.CreateLoadedDeivce(loadedName:string) =
        let file = $"./dsLib/AutoGen/{loadedName}.ds"

        let absoluteFilePath, simpleFilePath = x.GetFilePath(file)
        x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath)    |> ignore


    member private x.GetLayoutPath(fileSpecCtx: FileSpecContext) =
        fileSpecCtx
            .TryFindFirstChild<FilePathContext>()
            .Value.GetText()
            .DeQuoteOnDemand()

    override x.EnterLoadDeviceBlock(ctx: LoadDeviceBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let file = x.GetValidFile fileSpecCtx

        let absoluteFilePath, simpleFilePath = x.GetFilePath(file)
        let devs = ctx.TryFindFirstChild<DeviceNameListContext>().Value.Descendants<DeviceNameContext>()
       
        devs.Iter(fun dev->
             let loadedName = dev.CollectNameComponents().Combine()
             x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath)    |> ignore
            )
     

    override x.EnterLoadExternalSystemBlock(ctx: LoadExternalSystemBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let file = x.GetValidFile fileSpecCtx
        let absoluteFilePath, simpleFilePath = x.GetFilePath(file)
        let loadedName = ctx.CollectNameComponents().Combine()

       

        x.TheSystem.LoadExternalSystemAs(
            options.ShareableSystemRepository,
            loadedName,
            absoluteFilePath,
            simpleFilePath
        )
        |> ignore

    override x.EnterCodeBlock(ctx: CodeBlockContext) =
        let code = ctx.GetOriginalText()
        x.TheSystem.OriginalCodeBlocks.Add code
        let pureCode = code.Substring(3, code.Length - 6) // 처음과 끝의 "<@{" 와 "}@>" 제외
        let statements = parseCodeForTarget options.Storages pureCode runtimeTarget
        x.TheSystem.Statements.AddRange statements

    override x.EnterFunctionsBlock(ctx: FunctionsBlockContext) =
        // FunctionsBlockContext에서 모든 FunctionDefContext를 추출
        let functionDefs = ctx.functionDef()
        let functionNameOnlys = ctx.functionNameOnly()
        
        functionNameOnlys |> Seq.iter (fun fDef ->
            let funcName = fDef.TryFindIdentifier1FromContext().Value
            x.TheSystem.Functions.Add(Func(funcName)) )
        functionDefs |> Seq.iter (fun fDef ->
            // 함수 이름 추출
            let funcName = fDef.functionName().GetText()

            // 함수 호출과 관련된 매개변수 추출
            let funcCall = fDef.functionCall()
            let functionType =  funcCall.functionType().GetText() |> getFunctionType
            let args = 
                let argsCtxs = fDef.Descendants<ArgumentContext>()
                if argsCtxs.any() then   
                    argsCtxs
                    |> Seq.map (fun a -> a.GetText()) 
                    |> Seq.toArray
                else
                    [||] // 매개변수가 없는 경우 빈 배열

            // 추출한 함수 이름과 매개변수를 사용하여 시스템의 함수 목록에 추가
            let newFunc = Func.Create(funcName, functionType, args)
            x.TheSystem.Functions.Add(newFunc) )


    /// parser rule context 에 대한 이름 기준의 정보를 얻는다.  system 이름, flow 이름, parenting 이름 등
    member x.GetContextInformation(parserRuleContext: ParserRuleContext) = // collectUpwardContextInformation
        let ctx = parserRuleContext

        let system =
            match x.Rule2SystemNameDictionary.TryFind(parserRuleContext) with
            | Some systemName -> Some systemName
            | None -> parserRuleContext.TryGetSystemName()

        let flow =
            ctx
                .TryFindFirstAscendant<FlowBlockContext>(true)
                .Bind(fun b -> b.TryFindIdentifier1FromContext())

        let parenting =
            ctx
                .TryFindFirstAscendant<ParentingBlockContext>(true)
                .Bind(fun b -> b.TryFindIdentifier1FromContext())

        let ns = ctx.CollectNameComponents().ToFSharpList()

        { ContextType = ctx.GetType()
          System = system
          Flow = flow
          Parenting = parenting
          Names = ns }

    /// parser rule context 에 대한 객체 기준의 정보를 얻는다.  DsSystem 객체, flow 객체, parenting 객체 등
    member x.GetObjectContextInformation(system: DsSystem, parserRuleContext: ParserRuleContext) =
        let ci = x.GetContextInformation(parserRuleContext)
        assert (system.Name = ci.System.Value)
        let flow = ci.Flow.Bind system.TryFindFlow

        let parenting =
            option {
                let! flow = flow
                let! parentingName = ci.Parenting
                return! flow.Graph.TryFindVertex<Real>(parentingName)
            }

        { System = system
          Flow = flow
          Parenting = parenting
          NamedContextInformation = ci }

    /// 인과 token 으로 사용된 context 에 해당하는 vertex 를 찾는다.
    member x.TryFindVertex(ctx: CausalTokenContext) : Vertex option =
        let ci = x.GetContextInformation ctx

        option {
            let! parentWrapper = x.TheSystem.TryFindParentWrapper(ci)
            let graph = parentWrapper.GetGraph()

            match ci.Names with
            | _ofn :: [ _ofrn ] -> // of(r)n: other flow (real) name
                return! graph.TryFindVertex(ci.Names.Combine())
            | [ callOrAlias ] -> return! graph.TryFindVertex(callOrAlias)
            | _ -> failwithlog "ERROR"
        }


    member x.ProcessCausalPhrase(ctx: CausalPhraseContext) =
        let system = x.TheSystem
        let oci = x.GetObjectContextInformation(system, ctx)

        let children = ctx.children.ToArray() // (CausalTokensDNF CausalOperator)+ CausalTokensDNF

        for (n, ctx) in children |> Seq.indexed do
            assert
                (if n % 2 = 0 then
                     ctx :? CausalTokensCNFContext
                 else
                     ctx :? CausalOperatorContext)



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
    member x.CreateVertices(_ctx: SystemContext) =
        let system = x.TheSystem
        let sysctx = x.AntlrParser.system ()

        let getContainerChildPair (ctx: ParserRuleContext) : ParentWrapper option * NamedContextInformation =
            let ci = x.GetContextInformation(ctx)
            let system = x.TheSystem
            let parentWrapper = system.TryFindParentWrapper(ci)
            parentWrapper, ci

        let tokenCreator (cycle: int) =
            let candidateCtxs: ParserRuleContext list =
                [ 
                    let multictx = sysctx.TryFindChildren<Identifier1sListingContext>()
                    if multictx.any()
                    then 
                        yield! multictx |> Seq.collect(fun f->f.Descendants<Identifier1Context>().Cast<ParserRuleContext>())
                    else
                        yield! sysctx.Descendants<Identifier1ListingContext>().Cast<ParserRuleContext>() 
                        

                    yield! sysctx.Descendants<CausalTokenContext>().Cast<ParserRuleContext>() 
                ]


            let isCallName (pw: ParentWrapper, Fqdn(vetexPath)) =
                let flow = pw.GetFlow()
                tryFindCall flow.System vetexPath |> Option.isSome

            let isAliasMnemonic (pw: ParentWrapper, mnemonic: string) =
                let flow = pw.GetFlow()
                tryFindAliasDefWithMnemonic flow mnemonic |> Option.isSome


            let isJobName (pw, name) = tryFindJob pw name |> Option.isSome

            let isJobOrAlias (pw: ParentWrapper, Fqdn(vetexPath)) =
                isJobName (pw.GetFlow().System, vetexPath.Last())
                || isAliasMnemonic (pw, vetexPath.CombineQuoteOnDemand())

            let candidates = candidateCtxs.Select(getContainerChildPair)

            let loop () =
                for (optParent, ctxInfo) in candidates do
                    let parent = optParent.Value
                    let existing = parent.GetGraph().TryFindVertex(ctxInfo.GetRawName())

                    match existing with
                    | Some v -> debugfn $"{v.Name} already exists.  Skip creating it."
                    | None ->
                        let name = ctxInfo.Names.CombineQuoteOnDemand()
                        match cycle, ctxInfo.Names with
                        | 0, [ r ] when not <| (isJobOrAlias (parent, ctxInfo.Names)) ->
                            match parent.GetCore()  with
                            | :? Flow as flow->
                                if not <| isCallName (parent, ctxInfo.Names) then
                                    Real.Create(r, flow) |> ignore
                            |_ ->
                                failwithf $"{name} needs Job define"

                        | 1, [ c ] when not <| (isAliasMnemonic (parent, name)) ->
                            let job = tryFindJob system c |> Option.get

                            if job.DeviceDefs.any () then
                                Call.Create(job, parent) |> ignore

                            
                        | 1, realorFlow :: [ cr ] when
                            not <| isAliasMnemonic (parent, name)
                            ->
                            let otherFlowReal = tryFindReal system [ realorFlow; cr ] |> Option.get
                            RealOtherFlow.Create(otherFlowReal, parent) |> ignore
                            debugfn $"{realorFlow}.{cr} should already have been created."

                        | 2, [ q ] when isAliasMnemonic (parent, name) ->
                            let flow = parent.GetFlow()
                            let aliasDef = tryFindAliasDefWithMnemonic flow (q.QuoteOnDemand()) |> Option.get
                            Alias.Create(q, aliasDef.AliasTarget.Value, parent) |> ignore

                        | _, [ _q ] -> ()
                        | _, _ofn :: [ _ofrn ] -> ()
                        | _ -> failwithlog "ERROR"

            loop

        let createRealVertex = tokenCreator 0
        let createCallOrExRealVertex = tokenCreator 1
        let createAliasVertex = tokenCreator 2

        let fillInterfaceDef (system: DsSystem) (ctx: InterfaceDefContext) =
            let findSegments (fqdns: Fqdn[]) : Real[] =
                fqdns
                    .Where(fun fqdn -> fqdn <> null)
                    .Select(fun s -> tryFindReal system (s |> List.ofArray)) // in fqdn.. [0] : system, [1] : flow, [2] : real, [3] call...
                    .Tap(fun x -> assert (x.IsSome))
                    .Choose(id)
                    .ToArray()

            let isWildcard (cc: Fqdn) : bool = cc.Length = 1 && cc[0] = "_"

            let collectCallComponents (ctx: CallComponentsContext) : Fqdn[] =
                ctx.Descendants<Identifier123Context>().Select(collectNameComponents).ToArray()

            option {
                let! interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>()
                let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]
                let! api = system.ApiItems.TryFind(nameEq interfaceName)

                let ser = // { start ~ end ~ reset }
                    ctx
                        .Descendants<CallComponentsContext>()
                        .Map(collectCallComponents)
                        .Tap(fun callComponents ->
                            assert (callComponents.All(fun cc -> cc.Length = 2 || isWildcard (cc))))
                        .Select(fun callCompnents ->
                            callCompnents
                                .Select(fun cc ->
                                    if isWildcard (cc) then
                                        null
                                    else
                                        cc.Prepend(system.Name).ToArray())
                                .ToArray())
                        .ToArray()

                let lnk =
                    ctx
                        .TryFindFirstChild<Identifier12Context>()
                        .Map(collectNameComponents)
                        .ToArray()

                let n = ser.Length

                match n with
                | 2
                | 3 ->
                    api.AddTXs(findSegments (ser[0])) |> ignore
                    api.AddRXs(findSegments (ser[1])) |> ignore
                | _ ->
                    api.AddTXs(findSegments (lnk)) |> ignore
                    api.AddRXs(findSegments (lnk)) |> ignore
            }
            |> ignore


        let createTaskDevice (system: DsSystem) (ctx: JobBlockContext) =
            let callListings = ctx.Descendants<CallListingContext>().ToArray()

            for callList in callListings do
                let getRawJobName = callList.TryFindFirstChild<EtcName1Context>().Value
                let jobName = getRawJobName.GetText().DeQuoteOnDemand()
                let apiDefCtxs = callList.Descendants<CallApiDefContext>().ToArray()
             
                let getAddress (addressCtx: IParseTree) =
                    addressCtx.TryFindFirstChild<AddressItemContext>().Map(getText).Value

                let apiItems =
                    [ for apiDefCtx in apiDefCtxs do
                          let apiPath = apiDefCtx.CollectNameComponents() |> List.ofSeq // e.g ["A"; "+"]

                          match apiPath with
                          | device :: [ api ] ->
                              let apiItem =
                                  option {
                                      let! apiPoint =
                                            let allowAutoGenDevice = x.ParserOptions.AllowAutoGenDevice 
                                            match tryFindCallingApiItem system device api allowAutoGenDevice with
                                            | Some api -> Some api
                                            | None ->  
                                                      if allowAutoGenDevice &&
                                                         x.TheSystem.LoadedSystems.Where(fun f->f.Name = device).IsEmpty()
                                                      then x.CreateLoadedDeivce(device)
                                                      None

                                      match apiDefCtx.TryFindFirstChild<AddressInOutContext>() with
                                      |Some addressCtx -> 
                                          let! txAddressCtx = addressCtx.TryFindFirstChild<OutAddrContext>()
                                          let! rxAddressCtx = addressCtx.TryFindFirstChild<InAddrContext>()
                                          let tx = getAddress (txAddressCtx)
                                          let rx = getAddress (rxAddressCtx)

                                          debugfn $"TX={tx} RX={rx}"
                                          return TaskDev(apiPoint, rx, tx, device)
                                      |None ->
                                          return TaskDev(apiPoint, TextAddrEmpty, TextAddrEmpty, device)
                                        
                                  }

                              match apiItem with
                              | Some apiItem -> yield apiItem
                              | _ -> 
                                    match tryFindLoadedSystem system device with
                                    |Some dev-> yield createTaskDevUsingApiName dev.ReferenceSystem device api
                                    |None -> failwithlog $"device({device}) api({api}) is not exist"

                          | _ -> 
                                    let errText = String.Join(", ", apiPath.ToArray())
                                    failwithlog $"loading type error ({errText})device"
                          ]


                assert (apiItems.Any())
                let funcCallCtxs = callList.Descendants<FuncCallContext>().ToArray()
                let jobFuncs = commonFunctionExtractor funcCallCtxs jobName system
       
                let job = Job(jobName, apiItems.Cast<TaskDev>() |> Seq.toList, jobFuncs)
                job |> system.Jobs.Add


        let fillTargetOfAliasDef (x: DsParserListener) (ctx: AliasListingContext) =
            let system = x.TheSystem
            let ci = x.GetContextInformation ctx

            option {
                let! flow = tryFindFlow system ci.Flow.Value
                let! aliasKeys = ctx.TryFindFirstChild<AliasDefContext>().Map(collectNameComponents)

                let target =
                    let ns = aliasKeys.ToFSharpList()

                    match ns with
                    | [ rc ] -> //Flow.R or Flow.C
                        match flow.System.TryFindReal [ flow.Name; rc ] with
                        | Some r -> r |> DuAliasTargetReal
                        | None ->
                            let vertex = flow.System.TryFindCall([ flow.Name; rc ].ToArray()) |> Option.get

                            match vertex with
                            | :? Call as c -> DuAliasTargetCall c
                            | _ -> failwithlog "ERROR"

                    //if call.IsNone then
                    //    let job = flow.System.TryFindRealOtherSystem ([flow.Name;rc].ToArray())
                    //    job |> Option.get |> DuAliasTargetRealExSystem
                    //else
                    //    call |> Option.get |> DuAliasTargetCall

                    | flowOrReal :: [ rc ] -> //FlowEx.R or Real.C
                        match tryFindFlow system flowOrReal with
                        | Some f -> f.Graph.TryFindVertex<Real>(rc) |> Option.get |> DuAliasTargetReal
                        | None ->
                            //tryFindCall system ([flow.Name]@ns) |> Option.get |> DuAliasTargetCall
                            let vertex = tryFindCall system ([ flow.Name ] @ ns) |> Option.get

                            match vertex with
                            | :? Call as c -> DuAliasTargetCall c
                            | _ -> failwithlog "ERROR"
                    | _ -> failwithlog "ERROR"


                flow.AliasDefs[aliasKeys].AliasTarget <- Some target
            }

        let createAliasDef (x: DsParserListener) (ctx: AliasListingContext) =
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

        let fillXywh (system: DsSystem) (listLayoutCtx: List<dsParser.LayoutBlockContext>) =
            let tryParseAndReturn (text: string) =
                let mutable value = 0

                if Int32.TryParse(text, &value) then
                    value
                else
                    failwithlog "Conversion failed : xywh need to write into integer value"

            let genXywh (xywh: dsParser.XywhContext) =
                new Xywh(
                    tryParseAndReturn (getText (xywh.x ())),
                    tryParseAndReturn (getText (xywh.y ())),
                    tryParseAndReturn (getText (xywh.w ())),
                    tryParseAndReturn (getText (xywh.h ()))
                )


            for layoutCtx in listLayoutCtx do
                
                let fileSpecCtx = layoutCtx.TryFindFirstChild<FileSpecContext>();
                let filePath = 
                    match fileSpecCtx with
                    |Some s -> let path = x.GetLayoutPath(s)
                               if path.Contains(';')
                               then path
                               else failwith $"layout format error \n ex) [layouts file=\"chName;chPath\"] \n but.. {path}"
                    |None -> $"{TextEmtpyChannel}"

                let listPositionDefCtx = layoutCtx.Descendants<PositionDefContext>().ToList()

                for positionDef in listPositionDefCtx do
                    let nameCtx = positionDef.TryFindFirstChild<Identifier12Context>() |> Option.get
                    let name = getText nameCtx |> deQuoteOnDemand
                    let xywh = positionDef.TryFindFirstChild<XywhContext>() |> Option.get |> genXywh
                    let nameCompo = collectNameComponents nameCtx

                    if (nameCompo).Count() = 1
                    then 
                        let device = FindExtension.TryFindLoadedSystem(system, name) |> Option.get
                        device.ChannelPoints[filePath] <- xywh 
                    else 
                        failwithlog "invalid parser name component"

        let fillFinished (system: DsSystem) (listFinishedCtx: List<dsParser.FinishBlockContext>) =
            for finishedCtx in listFinishedCtx do
                let listFinished = finishedCtx.Descendants<FinishTargetContext>().ToList()

                for finished in listFinished do
                    let fqdn = collectNameComponents finished // in array.. [0] : flow, [1] : real
                    let real = tryFindReal system (fqdn |> List.ofArray)

                    if not (real.IsNone) then
                        real.Value.Finished <- true
                    else
                        failwith $"Couldn't find target real object name {getText (finished)}"

        let fillDisabled (system: DsSystem) (listDisabledCtx: List<dsParser.DisableBlockContext>) =
            for disabledCtx in listDisabledCtx do
                let listDisabled = disabledCtx.Descendants<DisableTargetContext>().ToList()

                for disabled in listDisabled do
                    let fqdn = collectNameComponents disabled |> List.ofArray
                    let coin = tryFindSystemInner system fqdn

                    if not (coin.IsNone) then
                        (coin.Value :?> Call).Disabled <- true
                    else
                        failwith $"Couldn't find target coin object name {getText (disabled)}"

        let fillProperties (x: DsParserListener) (ctx: PropsBlockContext) =
            let theSystem = x.TheSystem
            //device, call에 layout xywh 채우기
            ctx.Descendants<LayoutBlockContext>().ToList() |> fillXywh theSystem
            //Real에 finished 채우기
            ctx.Descendants<FinishBlockContext>().ToList() |> fillFinished theSystem
            //Call에 disable 채우기
            ctx.Descendants<DisableBlockContext>().ToList() |> fillDisabled theSystem

        for ctx in sysctx.Descendants<JobBlockContext>() do
            createTaskDevice x.TheSystem ctx

        for ctx in sysctx.Descendants<AliasListingContext>() do
            createAliasDef x ctx |> ignore

        //실제모델링 만들고
        createRealVertex ()
        createCallOrExRealVertex ()

        //AliasDef 생성후
        for ctx in sysctx.Descendants<AliasListingContext>() do
            fillTargetOfAliasDef x ctx |> ignore

        //Alias 만들기
        createAliasVertex ()

        for ctx in sysctx.Descendants<InterfaceDefContext>() do
            fillInterfaceDef x.TheSystem ctx |> ignore

        for ctx in sysctx.Descendants<PropsBlockContext>() do
            fillProperties x ctx

        apiAutoGenUpdateSystem system
        guardedValidateSystem system


[<AutoOpen>]
module ParserLoadApiModule =
    (* 외부에서 구조적으로 system 을 build 할 때에 사용되는 API *)
    type DsSystem with

        member x.LoadDeviceAs
            (
                systemRepo: ShareableSystemRepository,
                loadedName: string,
                absoluteFilePath: string,
                relativeFilePath: string
            ) =
            let device =
                fwdLoadDevice
                <| { ContainerSystem = x
                     AbsoluteFilePath = absoluteFilePath
                     RelativeFilePath = relativeFilePath
                     LoadedName = loadedName
                     ShareableSystemRepository = systemRepo
                     LoadingType = DuDevice }

            x.AddLoadedSystem(device) |> ignore
            device

        member x.LoadExternalSystemAs
            (
                systemRepo: ShareableSystemRepository,
                loadedName: string,
                absoluteFilePath: string,
                relativeFilePath: string
            ) =
            let external =
                let param =
                    { ContainerSystem = x
                      AbsoluteFilePath = absoluteFilePath
                      RelativeFilePath = relativeFilePath
                      LoadedName = loadedName
                      ShareableSystemRepository = systemRepo
                      LoadingType = DuExternal }

                match systemRepo.TryFind(absoluteFilePath) with
                | Some existing -> ExternalSystem(existing, param, false) // 기존 loading 된 system share
                | None ->
                    let exSystem = fwdLoadExternalSystem param
                    assert (systemRepo.ContainsKey(absoluteFilePath))
                    assert (systemRepo[absoluteFilePath] = exSystem.ReferenceSystem)
                    exSystem

            x.AddLoadedSystem(external) |> ignore
            external
