[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge)
        if srcs.Any() then
            let sets  = srcs.GetCausalTags(v.System, true)
            let rsts  = v.F.Expr
            [(sets, rsts) ==| (v.ST, "F1")]
        else []

    member v.F2_RootReset() : CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, ResetEdge)
                    .Select(getVM)
                    .Select(fun s -> s, v.GR(s.Vertex))

        let real = v.GetPureReal()
        if srcs.Any() then
            let sets  = srcs.Select(fun (src, gr) -> gr).ToAnd()
            let rsts  = (!!)real.V.EP.Expr
            //going relay rungs
            srcs.Select(fun (src, gr) -> (src.G.Expr, real.V.EP.Expr) ==| (gr, "F2"))
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
                failwith "Error F4_RootCoinRelay"

        let sets = ands <&&> v.ET.Expr
        let rsts = !!v.ST.Expr
        (sets, rsts) ==| (v.CR, "F4")

    //option Spec 확정 필요  
     member v.F0_RootStartRealOptionPulse(): CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.F).ToAnd()
            let rsts  = v.System._off.Expr
            [ 
                //root 시작조건 이벤트 Pulse 처리
                (sets, rsts) --^ (v.PUL, "F1") 
                //Pulse start Tag relay
                (v.PUL.Expr, v.H.Expr) ==| (v.ST, "F1") 
            ]
        else []
        
    //member v.F1_RootStartReal(): CommentedStatement list =
    //    let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
    //    if srcs.Any() then
    //        let sets  = srcs.Select(fun f->f.EP).ToAnd()
    //        let rsts  = v.F.Expr
    //        //root 시작조건 처리
    //        [(sets, rsts) ==| (v.ST, "F1")]
    //    else []

    //member v.F3_RootStartCoin(): CommentedStatement list =
    //    let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
    //    if srcs.Any() then
    //        let sets  = srcs.Select(fun f->f.EP).ToAnd()
    //        let rsts  = v.CR.Expr
    //        //root 시작조건 처리
    //        [(sets, rsts) ==| (v.ST, "F3")]
    //    else []
