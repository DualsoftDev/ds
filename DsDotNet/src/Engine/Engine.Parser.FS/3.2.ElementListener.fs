namespace Engine.Parser.FS

open System.Linq

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser


/// <summary>
/// Alias map 및 Api map 가 생성된 이후의 처리
/// </summary>
type ElementListener(parser:dsParser, helper:ParserHelper) =
    inherit ListenerBase(parser, helper)



    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let hash = x._currentSystem.Value.ApiItems
        let interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx)
        let interfaceName = collectNameComponents(interrfaceNameCtx.Value)[0]

        let collectCallComponents(ctx:CallComponentsContext):Fqdn[] =
            enumerateChildren<Identifier123Context>(ctx)
                .Select(collectNameComponents)
                .ToArray()

        let isWildcard(cc:Fqdn):bool = cc.Length = 1 && cc[0] = "_"
        let findSegments(fqdns:Fqdn[]):Real[] =
            fqdns
                .Where(fun fqdn -> fqdn <> null)
                .Select(fun s -> x._currentSystem.Value.FindGraphVertex<Real>(s))
                .Tap(fun x -> assert(not (isItNull x)))
                .ToArray()

        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
                .Select(collectCallComponents)
                .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || isWildcard(cc))))
                .Select(fun callCompnents -> callCompnents.Select(fun cc -> if isWildcard(cc) then null else cc.Prepend(x._currentSystem.Value.Name).ToArray()).ToArray())
                .ToArray()

        let item = hash.First(fun it -> it.Name = interfaceName)
        let n = ser.Length

        assert(n = 2 || n = 3)
        item.AddTXs(findSegments(ser[0])) |> ignore
        item.AddRXs(findSegments(ser[1])) |> ignore


    override x.EnterCausalToken(ctx:CausalTokenContext) =
        let spits = x._currentSystem.Value.Spit()
        let flow = x._flow.Value
        let findSpit (ns:string seq) =
            spits.TryFind(fun sp -> sp.NameComponents.IsStringArrayEqaul (ns.ToArray()))
        //let ns = collectNameComponents(ctx)
        let sysNames, flowName, parenting, ns = collectUpwardContextInformation ctx

        if ctx.GetText() = "Main2" then
            noop()

        let createAlias(soa:SpitOnlyAlias) =
            let aliasKey = soa.AliasKey
            match aliasKey.Length with
                | 3 ->     // my flow real 에 대한 alias
                    let aliasCreator =
                        let aliasTarget = new AliasTargetReal(aliasKey)
                        new AliasCreator(ns.Combine(), Flow flow, aliasTarget)
                    x.ParserHelper.AliasCreators.Add(aliasCreator) |> ignore
                | 2 ->
                    let apiItem =
                        x._modelSpitObjects
                            .OfType<ApiItem>()
                            .Where(fun api -> api.NameComponents.IsStringArrayEqaul(aliasKey))
                            .Head()

                    let name = ns.Combine()
                    match x._parenting with
                    | Some parent ->
                        let aliasCreator =
                            let aliasTarget = new AliasTargetApi(apiItem)
                            new AliasCreator(name, Real parent, aliasTarget)
                        x.ParserHelper.AliasCreators.Add(aliasCreator)
                    | None ->
                        (* flow 바로 아래에 사용되는 직접 call.  A.+ *)
                        let aliasCreator =
                            let aliasTarget = new AliasTargetDirectCall(aliasKey)
                            new AliasCreator(name, Flow flow, aliasTarget)
                        x.ParserHelper.AliasCreators.Add(aliasCreator)
                    |> ignore
                | _ ->
                    failwith "ERROR"







        let mutable goon = true
        match sysNames, flowName, parenting, ns with
        | s::[], Some f, None, n::[] ->
            tracefn $"{s}/{f}/{n}"
            match findSpit [s;f;n] with
            | Some sp ->
                match sp.SpitObj with
                | SpitOnlyAlias soa -> createAlias soa
                | SpitReal real -> noop()   // OK
                | _ -> failwith "ERROR"
            | None ->
                tracefn "Need to generate %A" [s;f;n]
                Real.Create(n, flow) |> ignore
            goon <- false
        | s::[], Some f, Some p, aliasCall::[] ->
            tracefn $"{s}/{f}/{p}/{aliasCall}"
            match findSpit [s;f;p;aliasCall] with
            | Some sp ->
                failwith "ERROR"
            | None ->
                match findSpit [s; f; aliasCall] with
                | Some sp ->
                    match sp.SpitObj with
                    | SpitOnlyAlias soa -> createAlias soa
                    | _ -> failwith "ERROR"
                | None ->
                    failwith "ERROR"

                tracefn "Need to generate %A" [s;f;p;aliasCall]

        | s::[], Some f, Some p, otherSystem::apiName::[] ->
            assert(p = x._parenting.Value.Name)
            tracefn $"{s}/{f}/{p}/{otherSystem}.{apiName}"
            match (findSpit [s; otherSystem; apiName]).OrElse(findSpit [otherSystem; apiName]) with
            | Some sp ->
                match sp.SpitObj with
                | SpitApiItem apiItem -> Call.Create(apiItem, Real x._parenting.Value) |> ignore
                | SpitOnlyAlias soa -> createAlias soa
                | _ -> failwith "Not an API item"
                goon <- false
            | None -> tracefn "Need to generate %A" [s; otherSystem; apiName]
        | _ ->
            printfn "I don't know"

        if goon then

            assert(ns.Length = 1 || ns.Length = 2)

            let path = x.AppendPathElement(ns.ToArray())
            let sysName = x._currentSystem.Value.Name

            let existing = x._modelSpits.Where(fun spit -> spit.NameComponents.IsStringArrayEqaul(path)).ToArray()
            if existing.Where(fun spit -> spit.GetCore() :? Vertex).IsEmpty() then

                let pathWithoutParenting = [|sysName; flow.Name; yield! ns|]

                // narrow match
                let matches =
                    x._modelSpits
                        .Where(fun spitResult ->
                            spitResult.NameComponents.IsStringArrayEqaul(path)
                            || spitResult.NameComponents.IsStringArrayEqaul(pathWithoutParenting))
                        .Select(fun spitResult -> spitResult.GetCore())
                        .ToArray()


                let pathAdapted = if ns.Length = 2 then [|sysName; yield! ns|] else [||]

                // 나의 시스템의 다른 flow 에 존재하는 segment 호출
                let extendedMatches =
                    x._modelSpits
                        .Where(fun spitResult ->
                            spitResult.GetCore() :? Real
                            && pathAdapted.Any() && spitResult.NameComponents.IsStringArrayEqaul(pathAdapted))
                        .Select(fun spitResult -> spitResult.GetCore())
                        .ToArray()


                let xxx =
                    x._modelSpitObjects.OfType<ApiItem>().ToArray()

                // 다른 시스템의 API 호출
                let apiCall =
                    x._modelSpitObjects
                        .OfType<ApiItem>()
                        .Where(fun api -> api.NameComponents.IsStringArrayEqaul(ns))
                        .TryHead()

                assert(matches.Length = 0 || matches.Length = 1)

                // API call 과 나의 시스템의 다른 flow 에 존재하는 segment 호출이 헷갈리지 않도록
                if (extendedMatches.OfType<Real>().Any(fun r -> r.NameComponents.IsStringArrayEqaul(pathAdapted))) then
                    match apiCall with
                    | Some apiCall ->
                        raise <| ParserException($"Ambiguous entry [{apiCall.QualifiedName}] and [{pathAdapted.Combine()}]", ctx)
                    | None ->
                        let aliasTarget = new AliasTargetReal(pathAdapted)
                        x.ParserHelper.AliasCreators.Add(new AliasCreator(ns.Combine(), Flow flow, aliasTarget))

                elif (matches.OfType<Real>().Any()) then
                    ()
                else
                    try
                        let alias = matches.OfType<SpitOnlyAlias>().TryHead()
                        match alias with
                        | Some alias ->
                            let aliasKey =
                                matches
                                    .OfType<SpitOnlyAlias>()
                                    .Where(fun alias -> alias.Mnemonic.IsStringArrayEqaul(pathWithoutParenting))
                                    .Select(fun alias -> alias.AliasKey)
                                    .FirstOrDefault()

                            match aliasKey.Length with
                                | 3 ->     // my flow real 에 대한 alias
                                        assert(aliasKey[0] = sysName && aliasKey[1] = flow.Name)
                                        let aliasCreator =
                                            let aliasTarget = new AliasTargetReal(aliasKey)
                                            new AliasCreator(ns.Combine(), Flow flow, aliasTarget)
                                        x.ParserHelper.AliasCreators.Add(aliasCreator) |> ignore
                                | 2 ->
                                    let apiItem =
                                        x._modelSpitObjects
                                            .OfType<ApiItem>()
                                            .Where(fun api -> api.NameComponents.IsStringArrayEqaul(aliasKey))
                                            .Head()

                                    let name = ns.Combine()
                                    match x._parenting with
                                    | Some parent ->
                                        let aliasCreator =
                                            let aliasTarget = new AliasTargetApi(apiItem)
                                            new AliasCreator(name, Real parent, aliasTarget)
                                        x.ParserHelper.AliasCreators.Add(aliasCreator)
                                    | None ->
                                        (* flow 바로 아래에 사용되는 직접 call.  A.+ *)
                                        let aliasCreator =
                                            let aliasTarget = new AliasTargetDirectCall(aliasKey)
                                            new AliasCreator(name, Flow flow, aliasTarget)
                                        x.ParserHelper.AliasCreators.Add(aliasCreator)
                                    |> ignore
                                | _ ->
                                    failwith "ERROR"
                        | None ->
                            match apiCall with
                            | Some apiCall ->
                                match x._parenting with
                                | Some parent ->
                                    Call.Create(apiCall, Real parent)
                                | None ->
                                    Call.Create(apiCall, Flow flow)
                                |> ignore
                            | None ->
                                match x._parenting with
                                | None ->
                                    if ns.Length <> 1 then
                                        raise <| ParserException($"ERROR: unknown token [{ns.Combine()}].", ctx)
                                    Real.Create(ns[0], flow) |> ignore
                                | Some parent ->
                                    raise <| ParserException($"ERROR: unknown token [{ns.Combine()}].", ctx)
                    finally
                        x.UpdateModelSpits()


    override x.EnterIdentifier12Listing(ctx:Identifier12ListingContext) =
        // side effects
        let path = x.AppendPathElement(collectNameComponents(ctx))
        if x._parenting.IsSome then
            raise <| new ParserException($"ERROR: identifier [{path.Combine()}] not allowed!", ctx)

        Real.Create(path.Last(), x._flow.Value) |> ignore
