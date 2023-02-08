[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let rsts  = v.F.Expr

        match getStartEdgeSources(v.Flow.Graph, v.Vertex) with
        | DuEssWeak ws when ws.Any() ->
            let sets = ws.GetCausalTags(v.System, true)
            [ (sets, rsts) ==| (v.ST, getFuncName()) ]
        | DuEssStrong ss when ss.Any() ->
            let sets = ss.GetCausalTags(v.System, true)
            [ (sets, rsts) --| (v.ST, getFuncName()) ]
        | _ -> []


    member v.F2_RootReset() : CommentedStatement list =
        let real = v.GetPureReal()
        match getResetEdgeSources(v.Flow.Graph, v.Vertex) with  //test ahn  srcsStrong 리셋처리
        | DuEssWeak ws when ws.Any() ->
            let srcs = ws.Select(getVM).Select(fun s -> s, v.GR(s.Vertex))
            let sets  = srcs.Select(fun (_src, gr) -> gr).ToAnd()
            let rsts  = (!!)real.V.EP.Expr
            //going relay rungs
            srcs.Select(fun (src, gr) -> (src.G.Expr, real.V.H.Expr) ==| (gr, getFuncName()))
            |> Seq.append [(sets, rsts) ==| (v.RT, getFuncName())] //reset tag
            |> Seq.toList
        | _ -> []



    member v.F3_RootCoinRelay() : CommentedStatement =
        let v = v :?> VertexMCoin
        let ands =
            match v.Vertex  with
            | :? RealExF as rf -> rf.V.CR.Expr
            | :? CallSys as rs -> rs.V.CR.Expr
            | :? CallDev | :? Alias ->
                match v.GetPureCall() with
                | Some call ->  if call.UsingTon
                                then call.V.TON.DN |> var2expr
                                else call.INs.ToAndElseOn(v.System)
                | None -> v.CR.Expr
            | _ ->
                failwithlog $"Error"

        let sets = ands <&&> v.ET.Expr
        let rsts = !!v.ST.Expr
        (sets, rsts) ==| (v.CR, getFuncName())

    //option Spec 확정 필요
     member v.F0_RootStartRealOptionPulse(): CommentedStatement list =
        let rsts  = v.F.Expr
        [
            match getStartEdgeSources(v.Flow.Graph, v.Vertex) with
            | DuEssWeak ws when ws.Any() ->
                let sets = ws.GetCausalTags(v.System, true)
                        //root 시작조건 이벤트 Pulse 처리
                yield (sets, rsts) ==| (v.PUL, getFuncName() )
                yield (v.PUL.Expr, v.H.Expr) ==| (v.ST, getFuncName())
            | DuEssStrong ss when ss.Any() ->
                let sets = ss.GetCausalTags(v.System, true)
                        //root 시작조건 이벤트 Pulse 처리
                yield (sets, rsts) --| (v.PUL, getFuncName())
                yield (v.PUL.Expr, v.H.Expr) --| (v.ST, getFuncName())
            | _ ->
                ()
        ]
