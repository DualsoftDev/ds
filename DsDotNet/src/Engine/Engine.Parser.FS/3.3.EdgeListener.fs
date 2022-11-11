namespace Engine.Parser.FS

open System.Linq

open Engine.Core.CoreModule
open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser



/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
type EdgeListener(parser:dsParser, helper:ParserHelper) =
    inherit ListenerBase(parser, helper)

    do
        base.UpdateModelSpits()


    override x.EnterModel(ctx:ModelContext) =
        let modelSpitCores = x._modelSpits.Select(fun spit -> spit.GetCore()).ToArray()
        for ac in x.ParserHelper.AliasCreators do
            let (name, parent, target) = (ac.Name, ac.Parent, ac.Target)
            let graph = parent.GetGraph()
            let existing = graph.TryFindVertex(name)
            if existing.IsNone then
                let create target = Alias.Create(name, target, parent, true) |> ignore
                match target with
                | :? AliasTargetReal as real ->
                    let realTarget = modelSpitCores.OfType<Real>().First(fun r -> r.NameComponents = real.TargetFqdn)
                    create (RealTarget realTarget)
                | :? AliasTargetDirectCall as directCall ->
                    let apiTarget = modelSpitCores.OfType<ApiItem>().First(fun a -> a.NameComponents = directCall.TargetFqdn)
                    let dummyCall = Call.CreateNowhere(apiTarget, parent)
                    create(CallTarget dummyCall)
                | :? AliasTargetApi as api ->
                    let dummyCall = Call.CreateNowhere(api.ApiItem, parent)
                    create(CallTarget dummyCall)
                | _ -> ()

        x.UpdateModelSpits()


    override x.EnterCausalPhrase(ctx:CausalPhraseContext) =
        let children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
        for (n, ctx) in children|> Seq.indexed do
            assert( if n % 2 = 0 then ctx :? CausalTokensDNFContext else ctx :? CausalOperatorContext)

        let findToken(ctx:CausalTokenContext):Vertex option =
            if ctx.GetText() = "F.Seg1" then
                noop()

            let ns = collectNameComponents(ctx)
            let mutable path = x.AppendPathElement(ns)
            if path.Length = 5 then
                path <- x.AppendPathElement(ns.Combine())
            let matches =
                x._modelSpits
                    .Where(fun spit -> spit.NameComponents = path
                                    || spit.NameComponents = x.AppendPathElement( [| ns.Combine() |] )
                    )

            let token =
                matches
                    .Select(fun spit -> spit.GetCore())
                    .OfType<Vertex>()
                    .TryHead()

            if token.IsNone then
                ()

            assert(token.IsSome)
            token

        (*
            children[0] > children[2] > children[4]     where (child[1] = '>', child[3] = '>')
            ===> children[0] > children[2],
                 children[2] > children[4]

            e.g "A, B > C, D > E"
            ===> children[0] = {A; B},
                 children[2] = {C; D},
                 children[4] = {E},

            todo: "A, B" 와 "A ? B" 에 대한 구분 없음.
         *)
        for triple in (children |> Array.windowed2 3 2) do
            if triple.Length = 3 then
                let lefts = enumerateChildren<CausalTokenContext>(triple[0])
                let op = triple[1].GetText()
                let rights = enumerateChildren<CausalTokenContext>(triple[2])

                for left in lefts do
                    for right in rights do
                        let l = findToken(left)
                        let r = findToken(right)
                        match l, r with
                        | Some l, Some r ->
                            match x._parenting with
                            | Some parent ->
                                parent.CreateEdges(ModelingEdgeInfo(l, op, r))
                            | None ->
                                x._flow.Value.CreateEdges(ModelingEdgeInfo(l, op, r))
                            |> ignore
                        | None, _ ->
                            raise <| ParserException($"ERROR: failed to find [{left.GetText()}]", ctx)
                        | _, None ->
                            raise <| ParserException($"ERROR: failed to find [{right.GetText()}]", ctx)

