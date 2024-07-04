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


// 이 파일에서만 사용됨
[<AutoOpen>]
module private DsParserHelperModule =
    type DsSystem with

        member x.TryFindParentWrapper(ci: NamedContextInformation) =
            option {
                let! flowName = ci.Flow

                match ci.Tuples with
                | Some _sys, Some flow, Some parenting, _ ->
                    let! real = tryFindReal x [ flow; parenting ]
                    return DuParentReal real
                | Some _sys, Some _flow, None, _ ->
                    let! f = tryFindFlow x flowName
                    return DuParentFlow f
                | _ -> failwithlog "ERROR"
            }

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
            
            RuntimeDS.System <- x.TheSystem 

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
            let pred (tree: IParseTree) = 
                tree :? Identifier1Context || tree :? CausalOperatorResetContext

            [| for des in ctx.Descendants<RuleContext>(false, pred) do
                   des.GetText() |]

        createApiResetInfo terms x.TheSystem
        
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

        if File.Exists absoluteFilePath then
            File.Delete absoluteFilePath        //자동생성은 매번 다시만듬

        x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath)    |> ignore


    member private x.GetLayoutPath(fileSpecCtx: FileSpecContext) =
        fileSpecCtx
            .TryFindFirstChild<FilePathContext>()
            .Value.GetText()
            .DeQuoteOnDemand()

    override x.EnterLoadDeviceBlock(ctx: LoadDeviceBlockContext) =
         let file = 
             match ctx.TryFindFirstChild<FileSpecContext>() with
             |Some f ->  x.GetValidFile f
             |None -> ""

         let devs = ctx.TryFindFirstChild<DeviceNameListContext>().Value.Descendants<DeviceNameContext>()
         devs.Iter(fun dev->
                let loadedName = dev.CollectNameComponents().Combine()
                let file = if file <> "" then file else $"dsLib/{loadedName}.ds"
                let absoluteFilePath, simpleFilePath = x.GetFilePath(file)
            
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

    //override x.EnterCodeBlock(ctx: CodeBlockContext) =
    //    let code = ctx.GetOriginalText()
    //    x.TheSystem.OriginalCodeBlocks.Add code
    //    let pureCode = code.Substring(3, code.Length - 6) // 처음과 끝의 "<@{" 와 "}@>" 제외
    //    let statements = parseCodeForTarget options.Storages pureCode runtimeTarget
    //    x.TheSystem.Statements.AddRange statements
    
    override x.EnterVariableBlock(ctx: VariableBlockContext) =

        let addVari varName varType (value:string) (isImmutable:bool)= 
            let variableData = VariableData (varName, varType, if isImmutable then Immutable else Mutable)

            let variable = createVariableByType varName varType
            variableData.InitValue <- value
            variable.BoxedValue <-varType.ToValue(value)

            options.Storages.Add (varName, variable) |>ignore
            x.TheSystem.AddVariables variableData   |>ignore


        ctx.variableDef() |> Seq.iter (fun vari ->
            let varName = vari.varName().GetText()
            let varType = vari.varType().GetText() |> textToDataType
            if vari.TryFindFirstChild<InitValueContext>().IsSome
            then 
                failWithLog $"{varName} = {vari.initValue().GetText()}; 할당은 Const 타입만 가능합니다.\nCommand를 이용하세요."
            let value = DsDataType.typeDefaultValue (varType.ToType())
            addVari varName varType  $"{value}" false
            )   

        ctx.constDef() |> Seq.iter (fun vari ->
            let constName = vari.constName().GetText()
            let varType = vari.varType().GetText() |> textToDataType
            if vari.TryFindFirstChild<InitValueContext>().IsNone
            then 
                failWithLog $"Const 타입은 초기값 설정이 필요합니다. ({varType.ToText()} {constName})"

            let value = vari.initValue().GetText()
            addVari constName varType  value true
            )
            


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

        let getContainerChildPair (ctx: ParserRuleContext) : (ParentWrapper  * NamedContextInformation) option =
            let ci = x.GetContextInformation(ctx)
            let system = x.TheSystem
            let parentWrapper = system.TryFindParentWrapper(ci)
            if parentWrapper.IsSome then
                (parentWrapper.Value, ci) |> Some
            else
                None

        let candidateCtxs: ParserRuleContext list =
            let normalCausalContext =  
                let nonCausalsContext = sysctx.TryFindChildren<NonCausalsContext>()
                [
                    if nonCausalsContext.any() then
                        let nonCausalGroup = nonCausalsContext.Head()
                        yield!  nonCausalGroup.TryFindChildren<Identifier1Context>().Cast<ParserRuleContext>()
                        yield!  nonCausalGroup.TryFindChildren<IdentifierCommandNameContext>().Cast<ParserRuleContext>()
                ]
            [ 
                yield! normalCausalContext
                yield! sysctx.Descendants<IdentifierCommandNameContext>().Cast<ParserRuleContext>() 
                yield! sysctx.Descendants<IdentifierOperatorNameContext>().Cast<ParserRuleContext>() 
                yield! sysctx.Descendants<CausalTokenContext>().Cast<ParserRuleContext>() 
                yield! sysctx.Descendants<NonCausalContext>().Cast<ParserRuleContext>() 
            ]

        let tokenCreator (cycle: int) =


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

            let candidates = candidateCtxs.Choose(getContainerChildPair)

            let loop () =
                for (optParent, ctxInfo) in candidates do
                    let parent = optParent
                    let existing = parent.GetGraph().TryFindVertex(ctxInfo.GetRawName())
                    if  (ctxInfo.ContextType = typeof<IdentifierOperatorNameContext> 
                       ||ctxInfo.ContextType = typeof<IdentifierCommandNameContext>)
                         && existing.IsNone
                    then
                        let opCmd = ctxInfo.Names.Combine()
                        match tryFindFunc system opCmd with
                        | Some func -> Call.Create(func, parent) |> ignore
                        | _ -> 
                            if ctxInfo.ContextType = typeof<IdentifierCommandNameContext> then
                                failwithlog $"Command({opCmd}) is not exist"
                            elif ctxInfo.ContextType = typeof<IdentifierOperatorNameContext> then
                                failwithlog $"Operator({opCmd}) is not exist"
                            else
                                failwithlog $"ERROR Command/Operator parsing [{opCmd}]"
                    else
                        match existing with
                        | Some v -> debugfn $"{v.Name} already exists.  Skip creating it."
                        | None ->
                            let name = ctxInfo.Names.CombineQuoteOnDemand()
                            match cycle, ctxInfo.Names with
                            | 0, [ r ] when not <| (isJobOrAlias (parent, ctxInfo.Names)) ->
                                match parent.GetCore()  with
                                | :? Flow as flow->
                                    if not <| isCallName (parent, ctxInfo.Names)  then
                                        Real.Create(r, flow) |> ignore
                                |_ ->
                                    failwithf $"{name} needs Job define"

                            | 1, [ c ] when not <| (isAliasMnemonic (parent, name)) ->
                                match tryFindJob system c with
                                | Some job ->
                                      if job.DeviceDefs.any () then
                                        Call.Create(job, parent) |> ignore
                                | None -> ()
                            
                            //| 1, realorFlow :: [ cr ] //when not <| isAliasMnemonic (parent, name)
                            | 2, realorFlow :: [ cr ] 
                                ->
                                let otherFlowReal = tryFindReal system [ realorFlow; cr ] |> Option.get
                                //RealOtherFlow.Create(otherFlowReal, parent) |> ignore
                                Alias.Create(name, DuAliasTargetReal otherFlowReal, parent, false) |> ignore
                                debugfn $"{realorFlow}.{cr} should already have been created."

                            | 2, [ q ] when isAliasMnemonic (parent, name) ->
                                let flow = parent.GetFlow()
                                let aliasDef = tryFindAliasDefWithMnemonic flow (q.QuoteOnDemand()) |> Option.get
                                let exFlow = aliasDef.AliasKey.length() = 2
                                Alias.Create(q, aliasDef.AliasTarget.Value, parent, exFlow) |> ignore

                            | _, [ _q ] -> ()
                            | _, _ofn :: [ _ofrn ] -> ()
                            | _ -> failwithlog "ERROR"

            loop

        let createRealVertex = tokenCreator 0
        let createCallOrExRealVertex = tokenCreator 1
        let createAliasVertex = tokenCreator 2

        let fillInterfaceDef (system: DsSystem) (ctx: InterfaceDefContext) =
               
            let collectCallComponents (ctx: CallComponentsContext) : Fqdn[] =
                ctx.Descendants<Identifier123Context>().Select(collectNameComponents).ToArray()

            option {
                let! interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>()
                let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]
                let! api = system.ApiItems.TryFind(nameEq interfaceName)

                let ser = // { start ~ end  }
                    ctx
                        .Descendants<CallComponentsContext>()
                        .Map(collectCallComponents)
                        .Select(fun callCompnents ->
                            callCompnents
                                .Select(fun cc ->
                                    if cc[0] = "_" then
                                        failWithLog $"not support '_' ({api.Name} [startWork ~ endWork])"
                                    else
                                        cc.Prepend(system.Name).ToArray())
                                .ToArray())
                        .ToArray()

            
                let n = ser.Length
                let findSegment (fqdn: Fqdn) : Real = tryFindReal system (fqdn |> List.ofArray) |> Option.get
                match n with
                | 2 ->
                    api.TX <- findSegment (ser[0].Head())
                    api.RX <- findSegment (ser[1].Head()) 
                | _ -> failWithLog $"ERROR {api.Name} [startWork ~ endWork]"
            }
            |> ignore

        let createDeviceVariable (system: DsSystem)  (devParam:DevParam) (devName:string) address =
            match devParam.DevName with
            | Some name ->
                let dataType = devParam.Type
                let variable = createVariableByType name dataType

                system.AddActionVariables (ActionVariable(name, address, devName, dataType)) |> ignore
                options.Storages.Add(name, variable) |> ignore

            | None -> ()



        let createTaskDevice (system: DsSystem) (ctx: JobBlockContext) =
            let callListings = commonCallParamExtractor ctx 
            let dicTaskDevs = Dictionary<string,TaskDev>()

            let getTaskDev (jobName:string) (task:TaskDev) (inParam) (outParm) =
                if dicTaskDevs.ContainsKey(task.QualifiedName)
                then
                    let oldTaskDev = dicTaskDevs[task.QualifiedName]
                    oldTaskDev.AddOrUpdateInParam(jobName, inParam)
                    oldTaskDev.AddOrUpdateOutParam(jobName, outParm)
                    oldTaskDev
                else 
                    dicTaskDevs.Add(task.QualifiedName, task)   
                    task

            for jobName, jobOption, apiDefCtxs in callListings do
                let apiItems =
                    [   for apiDefCtx in apiDefCtxs do
                            let apiPath = apiDefCtx.CollectNameComponents() |> List.ofSeq // e.g ["A"; "+"]
                            let devName = String.Join("_", apiPath.ToArray())
                            let (inaddr, inParam), (outaddr, outParm) =
                                match apiDefCtx.TryFindFirstChild<DevParamInOutContext>() with
                                | Some devParam -> 
                                    commonDeviceParamExtractor devParam 
                                | None ->
                                     (TextAddrEmpty, defaultDevParam()), (TextAddrEmpty, defaultDevParam())

                   
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
                                 
                                        debugfn $"TX={inParam} RX={outParm}"
                                        let taskDev = TaskDev(apiPoint, jobName, inParam, outParm, device)
                                        taskDev.InAddress <- inaddr
                                        taskDev.OutAddress <- outaddr
                                        return getTaskDev jobName taskDev inParam outParm
                                    }

                                match apiItem with
                                | Some apiItem -> yield apiItem
                                | _ -> 
                                    match tryFindLoadedSystem system device with
                                    | Some dev->
                                        let taskDev = createTaskDevUsingApiName (dev.ReferenceSystem) jobName device api (inParam, outParm) 
                                        yield getTaskDev jobName taskDev inParam outParm
                                               
                                    | None -> failwithlog $"device({device}) api({api}) is not exist"

                            | _ -> 
                                      let errText = String.Join(", ", apiPath.ToArray())
                                      failwithlog $"loading type error ({errText})device"


                            let plcName_I = getPlcTagAbleName $"{devName}_I" options.Storages
                            let plcName_O = getPlcTagAbleName $"{devName}_O" options.Storages


                            createDeviceVariable system inParam plcName_I inaddr
                            createDeviceVariable system outParm plcName_O outaddr
                    ]


                assert (apiItems.Any())
                
                let jobParam = if jobOption.IsSome 
                                 then getParserJobType $"{jobName}[{jobOption.Value}]"  
                                 else JobParam(ActionNormal, JobTypeMulti.Single)

                let job = Job(jobName, system, apiItems.Cast<TaskDev>() |> Seq.toList)
                job.UpdateJobParam(jobParam)

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
                let isOtherFlowRealAlias = aliasKeys.Length = 2   //flowA.Real1
                let ad = AliasDef(aliasKeys, None, mnemonics, isOtherFlowRealAlias)
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

        let fillNoTrans (system: DsSystem) (listCtx: List<dsParser.NotransBlockContext>) =
            for notranCtx in listCtx do
                let list = notranCtx.Descendants<NotransTargetContext>().ToList()

                for notrans in list do
                    let fqdn = collectNameComponents notrans // in array.. [0] : flow, [1] : real
                    let real = tryFindReal system (fqdn |> List.ofArray)

                    if not (real.IsNone) then
                        real.Value.NoTransData <- true
                    else
                        failwith $"Couldn't find target real object name {getText (notrans)}"

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
        
        

        let fillTimes (system: DsSystem) (listTimeCtx: List<dsParser.TimesBlockContext> ) =
            let fqdnTimes = getTimes listTimeCtx
            for fqdn, t in fqdnTimes do
                let real = (tryFindSystemInner system fqdn).Value :?> Real
                if t.Average.IsSome then real.DsTime.AVG <- Some(t.Average.Value|>float)
                if t.Std.IsSome     then real.DsTime.STD <- Some(t.Std.Value|>float)
                if t.OnDelay.IsSome then real.DsTime.TON <- Some(t.OnDelay.Value|>float)

        let fillActions (system: DsSystem) (listMotionCtx: List<dsParser.MotionBlockContext> ) =
            let fqdnPath = getMotions listMotionCtx
            for fqdn, path in fqdnPath do
                let real = (tryFindSystemInner system fqdn).Value :?> Real
                real.DsTime.Motion <- path|>Some

        let fillScripts (system: DsSystem) (listScriptCtx: List<dsParser.ScriptsBlockContext>) =
            let fqdnPath = getScripts listScriptCtx
            for fqdn, script in fqdnPath do
                let real = (tryFindSystemInner system fqdn).Value :?> Real
                real.DsTime.Script <- script|>Some

        let fillProperties (x: DsParserListener) (ctx: PropsBlockContext) =
            let theSystem = x.TheSystem
            //device, call에 layout xywh 채우기
            ctx.Descendants<LayoutBlockContext>().ToList() |> fillXywh theSystem
            //Real에 finished 채우기
            ctx.Descendants<FinishBlockContext>().ToList() |> fillFinished theSystem
            //Real에 noTransData 채우기
            ctx.Descendants<NotransBlockContext>().ToList() |> fillNoTrans theSystem
            //Real에 MotionBlock 채우기
            ctx.Descendants<MotionBlockContext>().ToList() |> fillActions theSystem
            //Real에 scripts 채우기
            ctx.Descendants<ScriptsBlockContext>().ToList() |> fillScripts theSystem
            //Real에 times 채우기
            ctx.Descendants<TimesBlockContext>().ToList() |> fillTimes theSystem

            
            //Call에 disable 채우기
            ctx.Descendants<DisableBlockContext>().ToList() |> fillDisabled theSystem


              
        let createOperator(ctx:OperatorBlockContext) =
            ctx.operatorNameOnly() |> Seq.iter (fun fDef ->
                let funcName = fDef.TryFindIdentifier1FromContext().Value
                x.TheSystem.Functions.Add(OperatorFunction(funcName)) )
        
            let functionDefs = ctx.operatorDef()
            functionDefs |> Seq.iter (fun fDef ->
                let funcName = fDef.operatorName().GetText()
                let pureCode = commonFunctionOperatorExtractor fDef

                // 추출한 함수 이름과 매개변수를 사용하여 시스템의 함수 목록에 추가
                let newFunc = OperatorFunction.Create(funcName, pureCode)
                let code = $"${funcName} = {pureCode};"
                let assignCode = 
                    match options.Storages.any(fun s->s.Key = funcName) with
                    | true ->   code // EnterJobBlock job Tag 에서 이미 만듬 
                    | false ->  $"bool {funcName} = false;{code}" //op 결과 bool 변수를 임시로 만듬

                let statements = parseCodeForTarget options.Storages assignCode runtimeTarget
                statements.Iter(fun s->
                    match s with
                    | DuAssign (_, _, _) -> newFunc.Statements.Add s  //비교구문 있는 Statement만 추가
                    |_ -> ()  //bool {funcName} = false 부분은 추가하지 않음
                    )

                x.TheSystem.Functions.Add(newFunc) 
                )


        let createCommand(ctx:CommandBlockContext) =
            ctx.commandNameOnly() |> Seq.iter (fun fDef ->
                let funcName = fDef.TryFindIdentifier1FromContext().Value
                x.TheSystem.Functions.Add(CommandFunction(funcName)) )

            let functionDefs = ctx.commandDef()
            functionDefs |> Seq.iter (fun fDef ->
                let funcName = fDef.commandName().GetText()
                let pureCode = commonFunctionCommandExtractor fDef

                // 추출한 함수 이름과 매개변수를 사용하여 시스템의 함수 목록에 추가
                let newFunc = CommandFunction.Create(funcName, pureCode)
                let statements = parseCodeForTarget options.Storages pureCode runtimeTarget
                newFunc.Statements.AddRange(statements)
                x.TheSystem.Functions.Add(newFunc)
                )

        for ctx in sysctx.Descendants<JobBlockContext>() do
            createTaskDevice x.TheSystem ctx

        for ctx in sysctx.Descendants<OperatorBlockContext>() do
            createOperator ctx

        for ctx in sysctx.Descendants<CommandBlockContext>() do
            createCommand ctx
       
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
        // MEMO: 이 함수를 사용하는 곳이, 이 파일의 상단에 존재하는데 F# 에서 에러가 나지 않는 이유는????
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

