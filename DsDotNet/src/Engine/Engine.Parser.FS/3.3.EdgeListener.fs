namespace Engine.Parser.FS

open Engine.Core.CoreModule

open Engine.Common.FS
open Engine.Parser
open System.Linq
open Engine.Core
open type Engine.Parser.dsParser
open Engine.Common.FS
open Engine.Parser
open System.Linq
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser
open Antlr4.Runtime.Tree
open Antlr4.Runtime
open Engine.Common.FS
open Engine.Common.FS.Functions



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
            let graph = parent.Graph
            let existing = graph.TryFindVertex(name)
            if existing.IsNone then
                let target2 =
                    match target with
                    | :? AliasTargetReal as real ->
                        let realTarget = modelSpitCores.OfType<Real>().First(fun r -> r.NameComponents.IsStringArrayEqaul(real.TargetFqdn))
                        RealTarget realTarget
                    | :? AliasTargetDirectCall as directCall ->
                        let apiTarget = modelSpitCores.OfType<ApiItem>().First(fun a -> a.NameComponents.IsStringArrayEqaul(directCall.TargetFqdn))
                        let dummyCall = Call.CreateNowhere(apiTarget, parent)
                        CallTarget dummyCall
                    | :? AliasTargetApi as api ->
                        let dummyCall = Call.CreateNowhere(api.ApiItem, parent)
                        CallTarget dummyCall
                Alias.Create(name, target2, parent) |> ignore

        x.UpdateModelSpits()


    override x.EnterCausalPhrase(ctx:CausalPhraseContext) =
        let children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
        children.Iter((ctx, n) => Assert( n % 2 == 0 ? ctx is CausalTokensDNFContext : ctx is CausalOperatorContext))

        let findToken(ctx:CausalTokenContext):obj =
            let ns = collectNameComponents(ctx)
            let mutable path = x.AppendPathElement(ns)
            if path.Length = 5 then
                path <- x.AppendPathElement(ns.Combine())
            let matches =
                x._modelSpits
                    .Where(fun spit -> spit.NameComponents.IsStringArrayEqaul(path)
                                    || spit.NameComponents.IsStringArrayEqaul(x.AppendPathElement( [| ns.Combine() |] ))
                    )

            let token =
                matches
                    .Select(fun spit -> spit.GetCore())
                    .OfType<Vertex>()
                    .FirstOrDefault()

            assert(token != null)
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
            let lefts = enumerateChildren<CausalTokenContext>(triple[0])
            let op = triple[1].GetText()
            let rights = enumerateChildren<CausalTokenContext>(triple[2])

            for left in lefts do
                for right in rights do
                    let l = findToken(left)
                    let r = findToken(right)
                    if isNull l then
                        raise <| ParserException($"ERROR: failed to find [{left.GetText()}]", ctx)
                    if isNull r then
                        raise <| ParserException($"ERROR: failed to find [{right.GetText()}]", ctx)

                    match x._parenting with
                    | Some parent ->
                        parent.CreateEdges(l :?> Vertex, r :?> Vertex, op)
                    | None ->
                        x._flow.Value.CreateEdges(l :?> Vertex, r :?> Vertex, op)
                    |> ignore
