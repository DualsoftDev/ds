namespace Engine.Parser.FS


open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open System.ComponentModel

[<AutoOpen>]
module SkeletonListenerModule =
    (* 모든 vertex 가 생성 된 이후, edge 연결 작업 수행 *)
    type SkeletonListener with
        member x.ProcessCausalPhrase(ctx:CausalPhraseContext) =
            let system = x.TheSystem
            let ci = x.GetContextInformation ctx
            let oci = x.GetObjectContextInformation(system, ctx)
            let sysNames, flowName, parenting, ns = ci.Tuples

            let children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
            for (n, ctx) in children|> Seq.indexed do
                assert( if n % 2 = 0 then ctx :? CausalTokensDNFContext else ctx :? CausalOperatorContext)



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
                    let lefts = triple[0].Descendants<CausalTokenContext>()
                    let op = triple[1].GetText()
                    let rights = triple[2].Descendants<CausalTokenContext>()

                    for left in lefts do
                        for right in rights do
                            let l = x.TryFindVertex(left)
                            let r = x.TryFindVertex(right)
                            match l, r with
                            | Some l, Some r ->
                                match oci.Parenting, oci.Flow with
                                | Some parenting, _ -> parenting.CreateEdges(ModelingEdgeInfo(l, op, r))
                                | None, Some flow -> flow.CreateEdges(ModelingEdgeInfo(l, op, r))
                                | _ -> failwith "ERROR"
                                |> ignore
                            | None, _ ->
                                raise <| ParserException($"ERROR: failed to find [{left.GetText()}]", ctx)
                            | _, None ->
                                raise <| ParserException($"ERROR: failed to find [{right.GetText()}]", ctx)

