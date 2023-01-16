[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let srcsWeek, srcsStrong  = getEdgeSources(v.Flow.Graph, v.Vertex, true)
        let rsts  = v.F.Expr
        [
            if srcsWeek.Any() then
                let sets = srcsWeek.GetCausalTags(v.System, true)
                yield (sets, rsts) ==| (v.ST, "F1" )

            if srcsStrong.Any() then
                let sets = srcsStrong.GetCausalTags(v.System, true)
                yield (sets, rsts) --| (v.ST, "F1" )
        ]

    member v.F2_RootReset() : CommentedStatement list =
        let srcsWeek, srcsStrong  = getEdgeSources(v.Flow.Graph, v.Vertex, false)  //test ahn  srcsStrong 리셋처리
        let srcs = srcsWeek
                    .Select(getVM)
                    .Select(fun s -> s, v.GR(s.Vertex)).ToList()

        let real = v.GetPureReal()
        if srcs.Any() then
            let sets  = srcs.Select(fun (src, gr) -> gr).ToAnd()
            let rsts  = (!!)real.V.EP.Expr
            //going relay rungs
            srcs.Select(fun (src, gr) -> (src.G.Expr, real.V.H.Expr) ==| (gr, "F2"))
            |> Seq.append [(sets, rsts) ==| (v.RT, "F2")] //reset tag
            |> Seq.toList
        else []



    member v.F3_RootCoinRelay() : CommentedStatement =
        let v = v :?> VertexMCoin
        let ands =
            match v.Vertex  with
            | :? RealEx as rex -> rex.V.CR.Expr
            | :? Call | :? Alias as ca ->
                match v.GetPureCall() with
                |Some call ->  if call.UsingTon
                                then call.V.TON.DN.Expr
                                else call.INs.ToAndElseOn(v.System)
                |None -> v.CR.Expr
            | _ ->
                failwithlog "Error F4_RootCoinRelay"

        let sets = ands <&&> v.ET.Expr
        let rsts = !!v.ST.Expr
        (sets, rsts) ==| (v.CR, "F4")

    //option Spec 확정 필요
     member v.F0_RootStartRealOptionPulse(): CommentedStatement list =
        let srcsWeek, srcsStrong  = getEdgeSources(v.Flow.Graph, v.Vertex, true)
        let rsts  = v.F.Expr
        [
            if srcsWeek.Any() then
                let sets = srcsWeek.GetCausalTags(v.System, true)
                        //root 시작조건 이벤트 Pulse 처리
                yield (sets, rsts) ==| (v.PUL, "F1" )
                yield (v.PUL.Expr, v.H.Expr) ==| (v.ST, "F1")

            if srcsStrong.Any() then
                let sets = srcsStrong.GetCausalTags(v.System, true)
                        //root 시작조건 이벤트 Pulse 처리
                yield (sets, rsts) --| (v.PUL, "F1" )
                yield (v.PUL.Expr, v.H.Expr) --| (v.ST, "F1")
        ]
