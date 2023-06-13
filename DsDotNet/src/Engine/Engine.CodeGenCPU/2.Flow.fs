[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let real = v.GetPureReal()
        let rsts  = real.V.RP.Expr <||> real.V.F.Expr

        let ws =  getStartWeakEdgeSources(v.Flow.Graph, v.Vertex)
        let sets = ws.GetCausalTags(v.System, true)
        [ (sets, rsts) ==| (v.ST, getFuncName()) ]


    member v.F2_RootReset() : CommentedStatement list=
        let real = v.GetPureReal()
        let wr =  getResetWeakEdgeSources(v.Flow.Graph, v.Vertex)
        let srcs = wr.Select(getVM).Select(fun s -> v.GR(s.Vertex))
        let sets = if srcs.any()
                   then srcs.ToAnd()
                   else v._off.Expr
        let rsts = (!!)real.V.ET.Expr
        [(sets, rsts) ==| (v.RT, getFuncName())] //reset tag

    member v.F3_RootGoingRelay() : CommentedStatement list =
        let real = v.GetPureReal()
        let wr =  getResetWeakEdgeSources(v.Flow.Graph, v.Vertex)
        let srcs = wr.Select(getVM).Select(fun s -> s, v.GR(s.Vertex))
        ////going relay rungs
        [
            yield! srcs.Select(fun (src, _ ) -> (src.G.Expr, v._off.Expr)      --^ (src.GPUL, "RootGoingPulse"))
            yield! srcs.Select(fun (src, gr) -> (src.GPUL.Expr, real.V.H.Expr) ==| (gr, "RootGoingRelay"))
        ]


    member v.F4_RootCoinRelay() : CommentedStatement =
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
     //member v.F0_RootStartRealOptionPulse(): CommentedStatement list =
     //   let rsts  = v.F.Expr
     //   [
     //       let ws =  getStartWeakEdgeSources(v.Flow.Graph, v.Vertex)
     //       let sets = ws.GetCausalTags(v.System, true)
     //               //root 시작조건 이벤트 Pulse 처리
     //       yield (sets, rsts) --^ (v.PUL, getFuncName() )
     //       yield (v.PUL.Expr, v.H.Expr) ==| (v.ST, getFuncName())
     //   ]
