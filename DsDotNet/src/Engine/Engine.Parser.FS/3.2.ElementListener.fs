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
        let system = helper.TheSystem.Value
        let hash = system.ApiItems
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
                .Select(fun s -> system.FindGraphVertex<Real>(s))
                .Tap(fun x -> assert(not (isItNull x)))
                .ToArray()

        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
                .Select(collectCallComponents)
                .Tap(fun callComponents -> assert(callComponents.All(fun cc -> cc.Length = 2 || isWildcard(cc))))
                .Select(fun callCompnents -> callCompnents.Select(fun cc -> if isWildcard(cc) then null else cc.Prepend(system.Name).ToArray()).ToArray())
                .ToArray()

        let item = hash.First(fun it -> it.Name = interfaceName)
        let n = ser.Length

        assert(n = 2 || n = 3)
        item.AddTXs(findSegments(ser[0])) |> ignore
        item.AddRXs(findSegments(ser[1])) |> ignore


    override x.EnterCausalToken(ctx:CausalTokenContext) =
        let ci = getContextInformation ctx
        let sysNames, flowName, parenting, ns = ci.Tuples
        let flow = x._flow.Value
        let vertexType = helper._causalTokenElements[ci]
        let system = helper.TheSystem.Value
        let spits = system.Spit()
        let findSpits (ns:string seq) =
            spits.Where(fun sp -> sp.NameComponents = (ns.ToArray()) || sp.NameComponents.Combine() = ns.Combine()).ToArray()
        let findSpit ns = findSpits ns |> Array.tryHead
        //let ns = collectNameComponents(ctx)


        let createRealTargetAlias (ci:ContextInformation) (target:Real) =
            let aliasCreator =
                let aliasTarget = new AliasTargetReal(target.NameComponents)
                let parent = getParentWrapper ci x._flow x._parenting
                new AliasCreator(ci.Names.Combine(), parent, aliasTarget)
            x.ParserHelper.AliasCreators.Add(aliasCreator)

        let createAlias(soa:SpitOnlyAlias) =
            let aliasKey = soa.AliasKey
            match aliasKey.Length with
                | 1 ->     // my flow real 에 대한 alias
                    let aliasCreator =
                        let aliasTarget = new AliasTargetReal([|yield! soa.FlowFqdn; yield! aliasKey|])
                        new AliasCreator(ns.Combine(), Flow flow, aliasTarget)
                    x.ParserHelper.AliasCreators.Add(aliasCreator) |> ignore
                | 2 ->
                    let apiItem =
                        x._modelSpitObjects
                            .OfType<ApiItem>()
                            .Where(fun api -> api.NameComponents = aliasKey)
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


        let createCall (ci:ContextInformation) =
            match ci.Tuples with
            | Some s, Some f, _, device::api::[] ->     // my / flow / (parenting) / device.api
                match tryFindImportApiItem(system, [|device; api|]) with
                | Some apiItem ->
                    let parent = getParentWrapper ci x._flow x._parenting
                    Call.Create(apiItem, parent) |> ignore
                | None ->
                    match (findSpit [s; device; api]).OrElse(findSpit [device; api]) with
                    | Some sp ->
                        match sp.SpitObj with
                        | SpitOnlyAlias soa -> createAlias soa
                        | SpitReal real -> createRealTargetAlias ci real
                        | _ -> failwith "Not an API item"
                    | None -> tracefn "Need to generate %A" [s; device; api]
            | _ ->
                failwith "ERROR"

        let createAliasFromContextInformation (ci:ContextInformation) =
            let alias =
                (findSpits ci.Names)
                    .Select(fun sp -> sp.GetCore())
                    .OfType<SpitOnlyAlias>()
                    .Where(fun sp -> sp.FlowFqdn = [| ci.System.Value; ci.Flow.Value |] )
                    .ExactlyOne()

            createAlias alias



        match vertexType with
        | HasFlag GVT.Segment ->
            let existing = findSpit(ci.NameComponents)
            match existing, ns with
            | Some x, _ -> ()
            | None, y::z::[] -> createCall ci
            | _ -> failwith "ERROR"
        | HasFlag GVT.AliaseMnemonic -> createAliasFromContextInformation ci
        | HasFlag GVT.CallAliasKey ->
            failwith "ERROR"
        | HasFlag GVT.CallApi
        | HasFlag GVT.CallFlowReal
        | HasFlag GVT.Child -> createCall ci
        | _ ->
            failwith "ERROR"
        x.UpdateModelSpits()
