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
open System.Collections.Generic


// 이 파일에서만 사용됨
[<AutoOpen>]
module private DsParserHelperModule =


    let getAutoGenDevApi(jobNameFqdn:string array, ctx:CallListingContext): DevApiDefinition =
        let ( inParam), (outParm) =
            ctx.TryFindFirstChild<TaskDevParamInOutContext>()
            |> Option.get
            |> commonDeviceParamExtractor
        let device = jobNameFqdn.Take(2).Combine(TextDeviceSplit)
        let api = jobNameFqdn.Last()
        let TaskDevParamIO = TaskDevParamIO(inParam, outParm)

        {
            ApiFqnd = [|device; api|]
            TaskDevParamIO = TaskDevParamIO
        }

    type DsSystem with

        member x.TryFindParentWrapper(ci: NamedContextInformation): ParentWrapper option =
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
          ): ExternalSystem =
            let external =
                let param =
                    {
                        ContainerSystem = x
                        AbsoluteFilePath = absoluteFilePath
                        RelativeFilePath = relativeFilePath
                        LoadedName = loadedName
                        ShareableSystemRepository = systemRepo
                        LoadingType = DuExternal
                    }

                match systemRepo.TryFindValue(absoluteFilePath) with
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
        | Some sysName ->
            x.OptLoadedSystemName <- Some sysName
            x.Rule2SystemNameDictionary.Add(ctx, sysName)
        | _ -> ()

        match ctx.TryFindFirstChild<SysBlockContext>() with
        | Some _sysBlockCtx ->
            let name =
                options.LoadedSystemName |? (ctx.systemName().GetText())

            let repo = options.ShareableSystemRepository

            match options.LoadingType, options.AbsoluteFilePath with
            | DuExternal, Some fp when repo.ContainsKey(fp) -> x.TheSystem <- repo[fp] :?> DsSystem
            | DuExternal, _ ->
                let registerSystem (sys: DsSystem) =
                    match options.AbsoluteFilePath with
                    | Some fp -> repo.Add(fp, sys)
                    | _ -> ()

                x.TheSystem <-
                    DsSystem.Create(name) |> tee registerSystem

            | _ -> x.TheSystem <- DsSystem.Create(name)

            RuntimeDS.ReplaceSystem(x.TheSystem)

            debugfn ($"System: {name}")
        | None -> failwithlog "ERROR"

    override x.ExitSystem(_ctx: SystemContext) = x.OptLoadedSystemName <- None

    override x.EnterFlowBlock(ctx: FlowBlockContext) =
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        x.TheSystem.CreateFlow(flowName) |> ignore

    override x.EnterParentingBlock(ctx: ParentingBlockContext) =
        debugfn ($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().TryGetName().Value
        let oci = x.GetObjectContextInformation(x.TheSystem, ctx)
        let flow = oci.Flow.Value
        flow.CreateReal(name) |> ignore


    override x.EnterInterfaceDef(ctx: InterfaceDefContext) =
        let system = x.TheSystem
        let interrfaceNameCtx = ctx.TryFindFirstChild<InterfaceNameContext>().Value
        let interfaceName = interrfaceNameCtx.CollectNameComponents()[0]

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 추후에 채움..
        system.CreateApiItem(interfaceName) |> ignore

    override x.EnterInterfaceResetDef(ctx: InterfaceResetDefContext) =
        // I1 <|> I2 <|> I3;  ==> [| I1; <|>; I2; <|>; I3; |]
        let terms =
            let pred (tree: IParseTree) =
                tree :? Identifier1Context || tree :? CausalOperatorResetContext

            [|
                for des in ctx.Descendants<RuleContext>(false, pred) do
                   des.GetText()
            |]

        createApiResetInfo terms x.TheSystem

    member x.GetValidFile(fileSpecCtx: FileSpecContext): string =
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

        x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath)


    member private x.GetLayoutPath(fileSpecCtx: FileSpecContext) =
        fileSpecCtx
            .TryFindFirstChild<FilePathContext>()
            .Value.GetText()
            .DeQuoteOnDemand()

    override x.EnterLoadDeviceBlock(ctx: LoadDeviceBlockContext) =
         let file =
             match ctx.TryFindFirstChild<FileSpecContext>() with
             | Some f ->  x.GetValidFile f
             | None -> ""

         let devs = ctx.TryFindFirstChild<DeviceNameListContext>().Value.Descendants<DeviceNameContext>()
         devs.Iter(fun dev->
                let loadedName = dev.CollectNameComponents().CombineDequoteOnDemand()
                let file = if file <> "" then file else $"dsLib/{loadedName}.ds"
                let absoluteFilePath, simpleFilePath = x.GetFilePath(file)

                x.TheSystem.LoadDeviceAs(options.ShareableSystemRepository, loadedName, absoluteFilePath, simpleFilePath)    |> ignore
            )

    override x.EnterLoadExternalSystemBlock(ctx: LoadExternalSystemBlockContext) =
        let fileSpecCtx = ctx.TryFindFirstChild<FileSpecContext>().Value
        let file = x.GetValidFile fileSpecCtx
        let absoluteFilePath, simpleFilePath = x.GetFilePath(file)
        let loadedName = ctx.CollectNameComponents().CombineDequoteOnDemand()

        x.TheSystem.LoadExternalSystemAs(
            options.ShareableSystemRepository,
            loadedName,
            absoluteFilePath,
            simpleFilePath
        ) |> ignore

    //override x.EnterCodeBlock(ctx: CodeBlockContext) =
    //    let code = ctx.GetOriginalText()
    //    x.TheSystem.OriginalCodeBlocks.Add code
    //    let pureCode = code.Substring(3, code.Length - 6) // 처음과 끝의 "<@{" 와 "}@>" 제외
    //    let statements = parseCodeForTarget options.Storages pureCode runtimeTarget
    //    x.TheSystem.Statements.AddRange statements

    override x.EnterVariableBlock(ctx: VariableBlockContext) =

        let addVari varName varType (value:string) (isImmutable:bool)=
            let variableData = VariableData (varName, varType, if isImmutable then ConstType else VariableType)

            let variable = createVariableByType varName varType
            variableData.InitValue <- value
            variable.BoxedValue <-varType.ToValue(value)

            options.Storages.Add (varName, variable) |>ignore
            x.TheSystem.AddVariables variableData   |>ignore


        for vari in ctx.variableDef() do
            let varName = vari.varName().GetText()
            let varType = vari.varType().GetText() |> textToDataType
            if vari.TryFindFirstChild<InitValueContext>().IsSome then
                failWithLog $"{varName} = {vari.initValue().GetText()}; 할당은 Const 타입만 가능합니다.\nCommand를 이용하세요."
            let value = DsDataType.typeDefaultValue (varType.ToType())
            addVari varName varType  $"{value}" false

        for vari in ctx.constDef() do
            let constName = vari.constName().GetText()
            let varType = vari.varType().GetText() |> textToDataType
            if vari.TryFindFirstChild<InitValueContext>().IsNone
            then
                failWithLog $"Const 타입은 초기값 설정이 필요합니다. ({varType.ToText()} {constName})"

            let value = vari.initValue().GetText()
            addVari constName varType  value true

    override x.EnterLangVersionDef(ctx: LangVersionDefContext) =
        let langVer = Version.Parse(ctx.version().GetText())
        langVer.CheckCompatible(DsSystem.RuntimeLangVersion, "Language")
        x.TheSystem.LangVersion <- langVer

    override x.EnterEngineVersionDef(ctx: EngineVersionDefContext) =
        let engineVer = Version.Parse(ctx.version().GetText())
        engineVer.CheckCompatible(DsSystem.RuntimeEngineVersion, "Engine")
        x.TheSystem.EngineVersion <- engineVer


    /// parser rule context 에 대한 이름 기준의 정보를 얻는다.  system 이름, flow 이름, parenting 이름 등
    member x.GetContextInformation(parserRuleContext: ParserRuleContext) = // collectUpwardContextInformation
        let ctx = parserRuleContext

        let system =
            match x.Rule2SystemNameDictionary.TryFindValue(parserRuleContext) with
            | Some systemName -> Some systemName
            | None -> Some x.TheSystem.Name  //parserRuleContext.TryGetSystemName()대신에 한번 저장된거 사용해서 성능개선

        let flow =
            ctx
                .TryFindFirstAscendant<FlowBlockContext>(true)
                .Bind(fun b -> b.TryFindIdentifier1FromContext())

        let parenting =
            ctx
                .TryFindFirstAscendant<ParentingBlockContext>(true)
                .Bind(fun b -> b.TryFindIdentifier1FromContext())

        let ns = ctx.CollectNameComponents().ToFSharpList()

        {
            ContextType = ctx.GetType()
            System = if system.IsSome then Some (system.Value.DeQuoteOnDemand()) else None
            Flow = if flow.IsSome then Some (flow.Value.DeQuoteOnDemand()) else None
            Parenting = if parenting.IsSome then Some (parenting.Value.DeQuoteOnDemand()) else None
            Names = ns.Select(fun s->s.DeQuoteOnDemand()).ToFSharpList()
        }

    /// parser rule context 에 대한 객체 기준의 정보를 얻는다.  DsSystem 객체, flow 객체, parenting 객체 등
    member x.GetObjectContextInformation(system: DsSystem, parserRuleContext: ParserRuleContext) =
        let ci = x.GetContextInformation(parserRuleContext)
        assert (system.Name.DeQuoteOnDemand() = ci.System.Value)
        let flow = ci.Flow.Bind system.TryFindFlow

        let parenting =
            option {
                let! flow = flow
                let! parentingName = ci.Parenting
                return! flow.Graph.TryFindVertex<Real>(parentingName)
            }

        {
            System = system
            Flow = flow
            Parenting = parenting
            NamedContextInformation = ci
        }

    /// 인과 token 으로 사용된 context 에 해당하는 vertex 를 찾는다.
    member x.TryFindVertex(ctx: CausalTokenContext) : Vertex option =
        let ci = x.GetContextInformation ctx

        option {
            let parentWrapper =
                match x.TheSystem.TryFindParentWrapper(ci) with
                | Some pw -> pw
                | None -> failwithlog "ERROR"

            let graph = parentWrapper.GetGraph()

            let ciNamesCombined = ci.Names.Combine()
            match ci.Names with
            | [ realOrAlias ] ->
                return! graph.TryFindVertex(realOrAlias)

            | _n1 :: [ _n2 ] -> // (other flow real) or (call.api)
                match graph.TryFindVertex(ciNamesCombined) with
                | Some v -> return v
                | None ->
                    return!
                        graph.Vertices.TryFind(fun v ->
                            match v with
                            | :? Alias as a ->
                                a.TargetWrapper.RealTarget().Value.ParentNPureNames.Combine() = ciNamesCombined
                            | _ -> false)

            | _n1 :: [ _n2; _n3]  ->  //other flow call
                if parentWrapper.GetCore() :? Real
                    && parentWrapper.GetFlow().Name = _n1
                then
                    return! graph.TryFindVertex(ci.Names.Skip(1).Combine())
                else
                    return! graph.TryFindVertex(ciNamesCombined)

            | _n1 :: [ _n2; _n3; _n4 ]  -> //other flow call
                    return! graph.TryFindVertex(ciNamesCombined)

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
                    | None -> raise <| ParserError($"ERROR: failed to find [{tokenCtx.GetText()}]", ctx)

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
        let sysctx = _ctx

        let getContainerChildPair (ctx: ParserRuleContext) : (ParentWrapper  * NamedContextInformation * ParserRuleContext) option =
            let ci = x.GetContextInformation(ctx)
            let system = x.TheSystem
            let parentWrapper = system.TryFindParentWrapper(ci)
            parentWrapper.Map(fun w -> (w, ci, ctx))

        let candidateCtxs: ParserRuleContext list =
            let normalCausalContext =
                let nonCausalsContext = sysctx.TryFindChildren<NonCausalsContext>()
                [
                    if nonCausalsContext.Any() then
                        let nonCausalGroup = nonCausalsContext.Head()
                        yield!  nonCausalGroup.TryFindChildren<CausalTokenContext>().Cast<ParserRuleContext>()
                        //yield!  nonCausalGroup.TryFindChildren<Identifier1Context>().Cast<ParserRuleContext>()
                        //yield!  nonCausalGroup.TryFindChildren<IdentifierOpCmdContext>().Cast<ParserRuleContext>()
                ]
            [
                yield! normalCausalContext
                //yield! sysctx.Descendants<IdentifierOpCmdContext>().Cast<ParserRuleContext>()
                yield! sysctx.Descendants<CausalTokenContext>().Cast<ParserRuleContext>()
                yield! sysctx.Descendants<NonCausalContext>().Cast<ParserRuleContext>()
            ]

        let candidates = candidateCtxs.Choose(getContainerChildPair)

        let tokenCreator (cycle: int) =


            let isCallName (pw: ParentWrapper, Fqdn(vetexPath)) =
                let flow = pw.GetFlow()
                tryFindCall flow.System vetexPath |> Option.isSome

            let isAliasMnemonic (pw: ParentWrapper, aliasText: string) =
                let flow = pw.GetFlow()
                tryFindAliasDefWithMnemonic flow aliasText |> Option.isSome

            let getJobName (pw: ParentWrapper, names: string list) =
                match names.Length with
                | 3 -> names.Combine() //OtherFlow.Call.Api
                | 2 -> $"{pw.GetFlow().Name}.{names.Combine()}" // Dev.Api
                | _ -> failwith $"getJobName {names.Combine()} ERROR"

            let isJobName (pw, name) = tryFindJob pw name |> Option.isSome

            let isJobOrAlias (pw: ParentWrapper, Fqdn(vetexPath)) =
                isJobName (pw.GetFlow().System, vetexPath.Last())
                || isAliasMnemonic (pw, vetexPath.Combine())

            let createAlias(parent: ParentWrapper, name: string ) =
                let flow = parent.GetFlow()
                let isRoot = parent.GetCore() :? Flow
                let aliasDef = tryFindAliasDefWithMnemonic flow name |> Option.get
                let aliasFqdnCnt = aliasDef.AliasKey.Length
                                //flow.Real     //flow.Call.Api     //Real.Call.Api
                let exFlow = (aliasFqdnCnt = 2 || (aliasFqdnCnt = 3 && isRoot))
                Alias.Create(name, aliasDef.AliasTarget.Value, parent, exFlow) |> ignore

            let createCall(parent: ParentWrapper, job: Job, ctx:ParserRuleContext) =
                match ctx.TryFindFirstChild<CausalInParamContext>(), ctx.TryFindFirstChild<CausalOutParamContext>() with
                | Some _, Some _ -> ()
                | None,  None -> ()
                | Some _inParam, None    -> failWithLog $"{job.DequotedQualifiedName} ValueParam Output is empty"
                | None,  Some _outParam  -> failWithLog $"{job.DequotedQualifiedName} ValueParam Input  is empty"
                let callActionType =
                    match ctx.TryFindFirstChild<CausalCallActionTypeContext>() with
                    | Some s -> 
                        let textCallAction =  s.TryFindFirstChild<ContentContext>().Value.GetText()  
                        if textCallAction = TextCallPush        
                        then  CallActionType.Push   
                        else  CallActionType.ActionNormal  

                    | _-> CallActionType.ActionNormal  
                
                let callInput =
                    match ctx.TryFindFirstChild<CausalInParamContext>() with
                    | Some inParam ->
                        createValueParam (inParam.TryFindFirstChild<ContentContext>().Value.GetText())
                    |_-> defaultValueParam()

                let callOutput =
                    match ctx.TryFindFirstChild<CausalOutParamContext>() with
                    | Some outParam ->
                        createValueParam (outParam.TryFindFirstChild<ContentContext>().Value.GetText())
                    |_-> defaultValueParam()

                let vp = ValueParamIO(callInput, callOutput)
                job.TaskDefs.Iter(fun (td: TaskDev) ->
                        let inParam = td.TaskDevParamIO.InParam
                        let outParam = td.TaskDevParamIO.OutParam

                        if not(callInput.IsDefaultValue) then
                            if inParam.DataType <> DuBOOL && inParam.DataType <> callInput.DataType then
                                failWithLog $"Input DataType is not match {td.TaskDevParamIO.InParam.DataType} != {callInput.DataType}"

                            td.TaskDevParamIO.InParam <- TaskDevParam(inParam.Address, callInput.DataType, inParam.Symbol)

                        if not(callOutput.IsDefaultValue) then
                            if outParam.DataType <> DuBOOL && outParam.DataType <> callOutput.DataType then
                                failWithLog $"Output DataType is not match {td.TaskDevParamIO.OutParam.DataType} != {callOutput.DataType}"

                            td.TaskDevParamIO.OutParam <- TaskDevParam(outParam.Address, callOutput.DataType, outParam.Symbol)
                        )

                let call = parent.CreateCall(job, vp)  
                call.CallActionType <- callActionType

            let loop () =
                for (optParent, ctxInfo, ctx) in candidates do
                    let parent = optParent
                    let existing = parent.GetGraph().TryFindVertex(ctxInfo.GetRawName())
                    if  (ctx.TryFindFirstChild<IdentifierOpCmdContext>().IsSome && existing.IsNone)
                    then
                        let opCmd = ctxInfo.GetRawName().DeQuoteOnDemand()
                        match tryFindFunc system opCmd with
                        | Some func -> parent.CreateCall(func) |> ignore
                        | _ ->
                            if ctxInfo.ContextType = typeof<IdentifierOpCmdContext> then
                                failwithlog $"Operator or Command({opCmd}) is not exist"
                            else
                                failwithlog $"ERROR Command/Operator parsing [{opCmd}]"
                    else
                        match existing with
                        | Some v -> debugfn $"{v.Name} already exists.  Skip creating it."
                        | None ->
                            let name = ctxInfo.Names.Combine()
                            match cycle, ctxInfo.Names with
                            | 0, [ real ] when not <| (isJobOrAlias (parent, ctxInfo.Names)) ->

                                match parent.GetCore()  with
                                | :? Flow as flow->
                                    if not <| isCallName (parent, ctxInfo.Names)  then
                                        flow.CreateReal(real) |> ignore
                                |_ ->
                                    ctx.Error()


                            | 1, _ when not <| (isAliasMnemonic (parent, name)) ->
                                match tryFindJob system (getJobName (parent, ctxInfo.Names)) with
                                | Some job ->
                                    createCall(parent, job, ctx)

                                | None ->
                                    match system.Flows.TryFind(fun f->f.Name = ctxInfo.Names.Head) with
                                    | Some _f -> ()  //exFlow alisas 면  | cycle 2, _x1 :: [ _x2 ] 에서 등록
                                    |_->
                                        ctx.Error($"Job check: {name}")

                            | 2, _x1 :: [ _x2 ]  ->
                                  match parent.GetCore() with
                                  | :? Flow as myflow ->
                                        let otherFlowReal = tryFindReal system [ _x1; _x2 ] |> Option.get
                                        myflow.CreateAlias(ctxInfo.Names.Combine("_"), otherFlowReal, false) |> ignore
                                  |_ when isAliasMnemonic (parent, name) ->
                                        createAlias(parent, ctxInfo.Names.Combine("_"))
                                  |_ ->
                                    ctx.Error()

                            | 2, [ _ ]  when isAliasMnemonic (parent, name) ->
                                 createAlias(parent, ctxInfo.Names.Combine("_"))

                            | _, [ _q ] -> ()
                            | _, _ofn :: [ _ofrn ] -> ()
                            | _, _ofn :: [ _ofrn; _jobExpr ] -> ()
                            | _, _ofn :: [ _otherFlow ;_ofrn; _jobExpr ] -> ()
                            | _ ->
                                ctx.Error()

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

        let createDeviceVariable (system: DsSystem)  (taskDevParam:TaskDevParam) (stgKey:string)  =
            let tdp = taskDevParam
            if tdp.Symbol <> ""
            then
                let sym = tdp.Symbol
                let variable = createVariableByType sym tdp.DataType
                system.AddActionVariables (ActionVariable(sym, tdp.Address, stgKey, tdp.DataType)) |> ignore
                options.Storages.Add(sym, variable)


        let createTaskDevice (system: DsSystem) (ctx: JobBlockContext) =
            let callListings = commonCallParamExtractor ctx
            let createTaskDev (device:string) (apiPoint:ApiItem) (taskDevParamIO:TaskDevParamIO) =
                let taskDev = system.CreateTaskDev(device, apiPoint)
                taskDev.TaskDevParamIO <- taskDevParamIO
                taskDev

            for jobNameFqdn, apiDefCtxs, callListingCtx in callListings do

                let apiDefs =
                    if apiDefCtxs.Any() then
                        [
                            for apiDefCtx in apiDefCtxs do
                                let apiPath = apiDefCtx.CollectNameComponents()
                                let (inParam), (outParam) =
                                    match apiDefCtx.TryFindFirstChild<TaskDevParamInOutContext>() with
                                    | Some taskDevParam ->
                                        commonDeviceParamExtractor taskDevParam
                                    | None ->
                                         (defaultTaskDevParam(), defaultTaskDevParam())
                                let taskDevParamIO = TaskDevParamIO( inParam,  outParam)
                                yield {ApiFqnd = apiPath;  TaskDevParamIO = taskDevParamIO;}
                        ]
                    else
                        [ getAutoGenDevApi (jobNameFqdn,  callListingCtx) ]

                let taskList =
                    let tl =    // just for IDE
                        [
                            for ad in apiDefs do
                                let apiFqnd = ad.ApiFqnd |> Seq.toList
                                let apiPure = ad.ApiFqnd.Last().Split([|'(';')'|]).Head()
                                let devName = apiFqnd.Head
                                let taskDevParamIO =  ad.TaskDevParamIO
                                let task =
                                    match apiFqnd with
                                    | device :: [ apiName ] ->
                                        let taskFromLoaded  =
                                            option {
                                                let! apiPoint =
                                                    let allowAutoGenDevice = x.ParserOptions.AllowAutoGenDevice
                                                    match tryFindCallingApiItem system device apiName allowAutoGenDevice with
                                                    | Some api -> Some api
                                                    | None ->
                                                        let createDevice = allowAutoGenDevice && x.TheSystem.LoadedSystems.Where(fun f->f.Name = device).IsEmpty()
                                                        if createDevice then
                                                            x.CreateLoadedDeivce(device) |> ignore
                                                        None


                                                return createTaskDev devName apiPoint taskDevParamIO
                                            }

                                        match taskFromLoaded with
                                        | Some t -> t
                                        | _ ->
                                            match tryFindLoadedSystem system device with
                                            | Some dev->
                                                match  dev.ReferenceSystem.ApiItems.TryFind(fun f->f.PureName = apiPure) with
                                                | Some apiItem ->
                                                    createTaskDev device apiItem taskDevParamIO
                                                | None ->
                                                    let taskDev = dev.ReferenceSystem.CreateTaskDev(device, apiName)
                                                    createTaskDev device taskDev.ApiItem taskDevParamIO

                                            | None -> failwithlog $"device({device}) api({apiName}) is not exist"

                                    | _ ->
                                        let errText = String.Join(", ", apiFqnd)
                                        failwithlog $"loading type error ({errText})device"

                                let devApiName = $"{devName}_{apiPure}"
                                let plcName_I = getPlcTagAbleName (getInActionName(devApiName))  options.Storages
                                let plcName_O = getPlcTagAbleName (getOutActionName(devApiName)) options.Storages
                                createDeviceVariable system taskDevParamIO.InParam  plcName_I
                                createDeviceVariable system taskDevParamIO.OutParam plcName_O

                                yield task

                        ]
                    tl.Cast<TaskDev>() |> Seq.toList


                assert (taskList.Any())


                let job = Job(jobNameFqdn, system, taskList)
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
                            match flow.System.TryFindCall([ flow.Name; rc ].ToArray()) with
                            | Some v ->
                                 match v with
                                 | :? Call as c -> c |> DuAliasTargetCall
                                 | _ -> ctx.Error()
                            | _ -> ctx.Error()

                    | flowOrRealorDev :: [ rc ] -> //FlowEx.R or Real.C
                        match tryFindFlow system flowOrRealorDev with
                        | Some f ->
                            match f.Graph.TryFindVertex<Real>(rc) with
                            | Some v-> v |> DuAliasTargetReal
                            | None -> ctx.Error()
                        | None ->
                            match tryFindCall system ([ flow.Name ] @ ns.Select(fun f->f.QuoteOnDemand())) with
                            | Some v ->
                                match v with
                                | :? Call as c -> c |> DuAliasTargetCall
                                | _ -> ctx.Error()
                            | None ->
                                    ctx.Error()

                    | _flowOrReal :: [ _dev; _api ] ->
                            match tryFindCall system ([ flow.Name ] @ ns) with
                            | Some v ->
                                (v :?> Call) |> DuAliasTargetCall
                            |_ ->
                                match flow.GetVerticesOfFlow().OfType<Call>().TryFind(fun f->f.Name = ns.Combine())  with
                                | Some call -> call|>  DuAliasTargetCall
                                | _ -> ctx.Error()

                    | _ -> ctx.Error()


                flow.AliasDefs[aliasKeys].AliasTarget <- Some target
            }

        let createAliasDef (x: DsParserListener) (ctx: AliasListingContext) =
            let system = x.TheSystem
            let ci = x.GetContextInformation ctx

            option {
                let! flow = tryFindFlow system ci.Flow.Value
                let! aliasKeys = ctx.TryFindFirstChild<AliasDefContext>().Map(collectNameComponents)
                let mnemonics = ctx.Descendants<AliasMnemonicContext>().Select(getText).Select(deQuoteOnDemand).ToArray()
                let ad = AliasDef(aliasKeys, None, mnemonics)
                flow.AliasDefs.Add(aliasKeys, ad)
                return ad
            }

        let fillXywh (system: DsSystem) (listLayoutCtx: List<dsParser.LayoutBlockContext>) =
            let tryParseAndReturn (text: string) =
                match Int32.TryParse(text) with
                | true, v -> v
                | _ -> failwithlog "Conversion failed : xywh need to write into integer value"

            let genXywh (xywh: dsParser.XywhContext) =
                new Xywh(
                    tryParseAndReturn (getText (xywh.x())),
                    tryParseAndReturn (getText (xywh.y())),
                    tryParseAndReturn (getText (xywh.w())),
                    tryParseAndReturn (getText (xywh.h()))
                )


            for layoutCtx in listLayoutCtx do

                let fileSpecCtx = layoutCtx.TryFindFirstChild<FileSpecContext>();
                let filePath =
                    match fileSpecCtx with
                    | Some s ->
                        let path = x.GetLayoutPath(s)
                        if path.Contains(';') then
                            path
                        else
                            failwith $"layout format error \n ex) [layouts file=\"chName;chPath\"] \n but.. {path}"
                    | None ->
                        $"{TextEmtpyChannel}"

                let listPositionDefCtx = layoutCtx.Descendants<PositionDefContext>().ToList()

                for positionDef in listPositionDefCtx do
                    let nameCtx = positionDef.TryFindFirstChild<Identifier12Context>() |> Option.get
                    let name = getText nameCtx |> deQuoteOnDemand
                    let xywh = positionDef.TryFindFirstChild<XywhContext>() |> Option.get |> genXywh
                    let nameCompo = collectNameComponents nameCtx

                    if (nameCompo).Count() = 1 then
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

                    match real with
                    | Some r -> r.Finished <- true
                    | None -> failwith $"Couldn't find target real object name {getText (finished)}"

        let fillNoTrans (system: DsSystem) (listCtx: List<dsParser.NotransBlockContext>) =
            for notranCtx in listCtx do
                let list = notranCtx.Descendants<NotransTargetContext>().ToList()

                for notrans in list do
                    let fqdn = collectNameComponents notrans // in array.. [0] : flow, [1] : real
                    let real = tryFindReal system (fqdn |> List.ofArray)

                    match real with
                    | Some r -> r.NoTransData <- true
                    | None -> failwith $"Couldn't find target real object name {getText (notrans)}"

        let fillSourceToken (system: DsSystem) (listCtx: List<dsParser.SourcetokenBlockContext>) =
            for ctx in listCtx do
                let list = ctx.Descendants<SourcetokenTargetContext>().ToList()

                for notrans in list do
                    let fqdn = collectNameComponents notrans // in array.. [0] : flow, [1] : real
                    let real = tryFindReal system (fqdn |> List.ofArray)

                    match real with
                    | Some r -> r.IsSourceToken <- true
                    | None -> failwith $"Couldn't find target real object name {getText (notrans)}"



        let fillDisabled (system: DsSystem) (listDisabledCtx: List<dsParser.DisableBlockContext>) =
            for disabledCtx in listDisabledCtx do
                let listDisabled = disabledCtx.Descendants<DisableTargetContext>().ToList()

                for disabled in listDisabled do
                    let fqdn = collectNameComponents disabled |> List.ofArray
                    let coin = tryFindSystemInner system fqdn

                    match coin with
                    | Some (:? Call as c) -> c.Disabled <- true
                    | _ -> failwith $"Couldn't find target coin object name {getText (disabled)}"


        let fillTimes (system: DsSystem) (listTimeCtx: List<dsParser.TimesBlockContext> ) =
            let fqdnTimes = getTimes listTimeCtx
            for fqdn, t in fqdnTimes do
                let real = (tryFindSystemInner system fqdn).Value :?> Real

                t.Average.Iter( fun x -> real.DsTime.AVG <- Some (x))
                t.Std.Iter(     fun x -> real.DsTime.STD <- Some (x))


        let fillErrors (system: DsSystem) (listErrorsCtx: List<dsParser.ErrorsBlockContext> ) =
            let fqdnErrors = getErrors listErrorsCtx
            for fqdn, t in fqdnErrors do
                match tryFindSystemInner system fqdn with
                | Some (:? Call as c) ->
                    c.CallTime.TimeOut  <- t.TimeOutMaxTime
                    c.CallTime.DelayCheck  <- t.CheckDelayTime
                | _ -> failWithLog $"Couldn't find Call object name {fqdn}"

        let fillRepeats (system: DsSystem) (listRepeatCtx: List<dsParser.RepeatsBlockContext> ) =
            let fqdnRepeats = getRepeats listRepeatCtx
            for fqdn, t in fqdnRepeats do
                match tryFindSystemInner system fqdn with
                | Some (:? Real as r) ->
                    match UInt32.TryParse t with
                    | true, count -> r.RepeatCount <- Some (count)
                    | _ -> failWithLog $"Repeat count must be a positive integer. {t} is not valid."
                | _ -> failWithLog $"Couldn't find Real object name {fqdn}"

        let fillActions (system: DsSystem) (listMotionCtx: List<dsParser.MotionBlockContext> ) =
            let fqdnPath = getMotions listMotionCtx

            for fqdn, path in fqdnPath do
                if system.GetRealVertices().Choose(fun r -> r.Motion).Contains path then
                    failWithLog $"Motion path {path} is already assigned to {fqdn.Combine()} Real object."

                match tryFindSystemInner system fqdn with
                | Some (:? Real as r) -> r.Motion <- path|>Some
                | _ -> failWithLog $"Couldn't find Real object name {fqdn}"

        let fillScripts (system: DsSystem) (listScriptCtx: List<dsParser.ScriptsBlockContext>) =
            let fqdnPath = getScripts listScriptCtx

            for fqdn, script in fqdnPath do
                
                if system.GetRealVertices().Choose(fun r -> r.Script).Contains script then
                    failWithLog $"Script Name {script} is already assigned to {fqdn.Combine()} Real object."

                match tryFindSystemInner system fqdn with
                | Some (:? Real as r) ->r.Script <- script|>Some
                | _ -> failWithLog $"Couldn't find Real object name {fqdn}"

        let fillProperties (x: DsParserListener) (ctx: PropsBlockContext) =
            let theSystem = x.TheSystem
            //device, call에 layout xywh 채우기
            ctx.Descendants<LayoutBlockContext>() .ToList()      |> fillXywh     theSystem
            //Real에 finished 채우기
            ctx.Descendants<FinishBlockContext>() .ToList()      |> fillFinished theSystem
            //Real에 noTransData 채우기
            ctx.Descendants<NotransBlockContext>().ToList()      |> fillNoTrans  theSystem
            //Real에 SourceToken  채우기
            ctx.Descendants<SourcetokenBlockContext>().ToList()  |> fillSourceToken  theSystem
            //Real에 MotionBlock 채우기
            ctx.Descendants<MotionBlockContext>() .ToList()      |> fillActions  theSystem
            //Real에 scripts 채우기
            ctx.Descendants<ScriptsBlockContext>().ToList()      |> fillScripts  theSystem
            //Real에 times 채우기
            ctx.Descendants<TimesBlockContext>()  .ToList()      |> fillTimes    theSystem
            //Real에 Repeats 채우기
            ctx.Descendants<RepeatsBlockContext>() .ToList()     |> fillRepeats  theSystem
            //Job에 errors (CallTime)채우기
            ctx.Descendants<ErrorsBlockContext>() .ToList()      |> fillErrors   theSystem
            //Call에 disable 채우기
            ctx.Descendants<DisableBlockContext>().ToList()      |> fillDisabled theSystem

        let createOperator(ctx:OperatorBlockContext) =
            ctx.operatorNameOnly() |> Seq.iter (fun fDef ->
                let funcName = fDef.TryFindIdentifier1FromContext().Value
                x.TheSystem.Functions.Add(OperatorFunction(funcName.DeQuoteOnDemand())) )

            let functionDefs = ctx.operatorDef()
            for fDef in functionDefs do
                let funcName = fDef.operatorName().GetText()
                let pureCode = commonFunctionOperatorExtractor fDef

                // 추출한 함수 이름과 매개변수를 사용하여 시스템의 함수 목록에 추가
                let newFunc = OperatorFunction.Create(funcName, pureCode)
                let code = $"${funcName} = {pureCode};"
                let assignCode =
                    match options.Storages.Any(fun s->s.Key = funcName) with
                    | true ->   code // EnterJobBlock job Tag 에서 이미 만듬
                    | false ->  $"bool {funcName} = false;{code}" //op 결과 bool 변수를 임시로 만듬

                let statements = parseCodeForTarget options.Storages assignCode runtimeTarget
                for s in statements do
                    match s with
                    | DuAssign (_, _, _) -> newFunc.Statements.Add s  //비교구문 있는 Statement만 추가
                    |_ -> ()  //bool {funcName} = false 부분은 추가하지 않음

                x.TheSystem.Functions.Add(newFunc)


        let createCommand(ctx:CommandBlockContext) =
            ctx.commandNameOnly() |> Seq.iter (fun fDef ->
                let funcName = fDef.TryFindIdentifier1FromContext().Value
                x.TheSystem.Functions.Add(CommandFunction(funcName.DeQuoteOnDemand())) )

            let functionDefs = ctx.commandDef()
            for fDef in functionDefs do
                let funcName = fDef.commandName().GetText()
                let pureCode = commonFunctionCommandExtractor fDef

                // 추출한 함수 이름과 매개변수를 사용하여 시스템의 함수 목록에 추가
                let newFunc = CommandFunction.Create(funcName, pureCode)
                let statements = parseCodeForTarget options.Storages pureCode runtimeTarget
                newFunc.Statements.AddRange(statements)
                x.TheSystem.Functions.Add(newFunc)

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
            ): Device =
            let device =
                fwdLoadDevice
                <| { ContainerSystem = x
                     AbsoluteFilePath = absoluteFilePath
                     RelativeFilePath = relativeFilePath
                     LoadedName = loadedName
                     ShareableSystemRepository = systemRepo
                     LoadingType = DuDevice }

            x.AddLoadedSystem(device)
            device

