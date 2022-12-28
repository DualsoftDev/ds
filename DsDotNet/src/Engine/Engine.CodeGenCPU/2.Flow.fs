[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement Option =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EP).ToAnd()
            let rsts  = v.H.Expr
            //root 시작조건 처리
            (sets, rsts) ==| (v.ST, "F1")  |> Some
        else None

     member v.F1_RootStartOptionPulse(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EP).ToAnd()
            let rsts  = v.OFF.Expr
            [ 
                //root 시작조건 이벤트 Pulse 처리
                (sets, rsts) --^ (v.PUL, "F1") 
                //Pulse start Tag relay
                (v.PUL.Expr, v.H.Expr) ==| (v.ST, "F1") 
            ]
        else []

    member v.F2_RootReset() : CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, ResetEdge)
                    .Select(getVM)
                    .Select(fun s -> s, s.GR(v.Vertex))

        if srcs.Any() then
            let sets  = srcs.Select(fun (src, gr) -> gr).ToAnd()
            let rsts  = (!!)v.EP.Expr

            //going relay rungs
            srcs.Select(fun (src, gr) -> (src.G.Expr, v.H.Expr) ==| (gr, "F2"))
            |> Seq.append [(sets, rsts) ==| (v.RT, "F2")] //reset tag  
            |> Seq.toList
        else []