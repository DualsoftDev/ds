[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStartReal(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EP).ToAnd()
            let rsts  = v.F.Expr
            //root 시작조건 처리
            [(sets, rsts) ==| (v.ST, "F1")]
        else []


    //option Spec 확정 필요  
     member v.F1_RootStartRealOptionPulse(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EP).ToAnd()
            let rsts  = v.System._off.Expr
            [ 
                //root 시작조건 이벤트 Pulse 처리
                (sets, rsts) --^ (v.PUL, "F1") 
                //Pulse start Tag relay
                (v.PUL.Expr, v.H.Expr) ==| (v.ST, "F1") 
            ]
        else []

    member v.F2_RootResetReal() : CommentedStatement list =
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

    member v.F3_RootStartCall(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EP).ToAnd()
            let rsts  = v.CR.Expr
            //root 시작조건 처리
            [(sets, rsts) ==| (v.ST, "F3")]
        else []

    member v.F4_RootCallRelay() : CommentedStatement =
        let call = getPureCall  v.Vertex

        let ends = 
            if call.UsingTon 
            then call.V.TON.Expr 
            else call.INs.EmptyOnElseToAnd(v.System) 

        let sets = ends <&&> v.EP.Expr
        let rsts = !!v.SP.Expr
        (sets, rsts) ==| (v.CR, "F4")
