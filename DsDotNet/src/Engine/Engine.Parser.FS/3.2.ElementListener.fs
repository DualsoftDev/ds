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

    //override x.EnterAliasListing(ctx:AliasListingContext) =
    //    let flow = x._flow.Value
    //    let map = flow.AliasDefs
    //    let mnemonics = enumerateChildren<AliasMnemonicContext>(ctx).Select(getText).ToArray()
    //    let aliasTargetCtx = tryFindFirstChild<AliasDefContext>(ctx).Value    // {타시스템}.{interface} or {Flow}.{real}
    //    let aliasTargetName = tryGetName(aliasTargetCtx).Value
    //    let realTargetCandidate = flow.Graph.TryFindVertex<Real>(aliasTargetName)
    //    let callTargetCandidate = tryFindCall x._theSystem.Value aliasTargetName
    //    let target =
    //        match realTargetCandidate, callTargetCandidate with
    //        | Some real, None -> RealTarget real
    //        | None, Some call -> CallTarget call
    //        | Some _, Some _ -> failwith "Name duplicated."
    //        | _ -> failwith "Failed to find"

    //    { AliasTarget=target; Mnemonincs=mnemonics} |> map.Add

    //    let ci = getContextInformation aliasTargetCtx
    //    x.AddElement(ci, GVT.AliaseKey)

    //    //for mne in mnemonics do
    //    //    x.AddElement(mne, GVT.AliaseMnemonic)
    //    //let aliasesHash = mnemonics.Select(fun ctx -> ctx.Names.Combine()).ToHashSet()
    //    //let aliasKey = ci.Names.ToArray()
    //    //map.Add(aliasKey, aliasesHash)



    override x.EnterInterfaceDef(ctx:InterfaceDefContext) =
        let system = helper.TheSystem.Value
        let hash = system.ApiItems4Export
        let interrfaceNameCtx = tryFindFirstChild<InterfaceNameContext>(ctx)
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

        if ns.Contains "C1" then
            noop()


        let createRealTargetAlias (ci:ContextInformation) (target:Real) =
            let aliasCreator =
                let aliasTarget = new AliasTargetReal(target.NameComponents)
                let parent = choiceParentWrapper ci x._flow x._parenting
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
                        system.ApiItems.Where(fun api -> api.NameComponents = aliasKey)
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


        let createCall (vertexType:GVT) (ci:ContextInformation) : Indirect =
            assert(ci.System.Value = system.Name)
            assert(ci.Flow.Value = flow.Name)

            match vertexType with
            | HasFlag GVT.CallFlowReal ->
                match ci.Tuples with
                | Some s, Some f, None, ofName::ofrName::[] ->   // "ofr" Other Flow Real
                    let ofr = tryFindReal system ofName ofrName |> Option.get
                    VertexOtherFlowRealCall.Create(ofName, ofrName, ofr, Flow flow)
                | _ -> failwith "ERROR"
            | HasFlag GVT.CallAliased ->
                failwith "Not Yet"
            | HasFlag GVT.Call ->
                match ci.Tuples with
                | Some s, Some f, parenting_, callName::[] ->
                    option {
                        let xxx = tryFindCall system callName
                        let! call = tryFindCall system callName
                        let yyy = tryFindParentWrapper system ci
                        let! parent = tryFindParentWrapper system ci
                        return VertexCall.Create(callName, call, parent) :> Indirect
                    } |> Option.get
                | _ ->
                    failwith "ERROR"
            | _ ->
                failwith "ERROR"
            //match ci.Tuples with
            //| Some s, Some f, _, device::api::[] ->     // my / flow / (parenting) / device.api
            //    match tryFindImportApiItem system [device; api] with
            //    | Some apiItem ->
            //        let parent = choiceParentWrapper ci x._flow x._parenting
            //        Call.Create(apiItem, parent) |> ignore
            //    | None ->
            //        match (findSpit [s; device; api]).OrElse(findSpit [device; api]) with
            //        | Some sp ->
            //            match sp.SpitObj with
            //            | SpitOnlyAlias soa -> createAlias soa
            //            | SpitReal real -> createRealTargetAlias ci real
            //            | _ -> failwith "Not an API item"
            //        | None -> tracefn "Need to generate %A" [s; device; api]
            //| _ ->
            //    failwith "ERROR"

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
            | Some _,   _ -> ()
            | None,     _::_::[] -> createCall vertexType ci |> ignore
            | _ -> failwith "ERROR"
        | HasFlag GVT.AliaseMnemonic ->
            createAliasFromContextInformation ci
        | HasFlag GVT.CallAliased ->
            failwith "ERROR"
        | HasFlag GVT.Call
        | HasFlag GVT.CallFlowReal
        | HasFlag GVT.Child -> createCall vertexType ci |> ignore
        | _ ->
            failwith "ERROR"
        x.UpdateModelSpits()
