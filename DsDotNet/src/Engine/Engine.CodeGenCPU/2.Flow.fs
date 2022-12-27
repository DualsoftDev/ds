[<AutoOpen>]
module Engine.CodeGenCPU.ConvertFlow

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.F1_RootStart(): CommentedStatement option =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, StartEdge).Select(getVM)
        if srcs.Any() then
            let sets  = srcs.Select(fun f->f.EndPort).ToAnd()
            let rsts  = v.ET
            (sets, rsts) ==| (v.StartTag, "F1") |> Some
        else
            None

    member v.F2_RootReset() : CommentedStatement list =
        let srcs = v.Flow.Graph.FindEdgeSources(v.Vertex, ResetEdge).Select(getVM)
        
        let goingRelays = srcs.Select(fun c -> DsTag($"{c.Name}(gr)", false) :> Tag<bool>) 
        let sets  = goingRelays |> toAnd
        let rsts  = v.H
        let grs =  
            srcs.Select(fun c ->
                    c.RelayGoing <== (sets <&&> ((!!)rsts))) |> Seq.toList
        grs  //RootReset 추가 필요
        |> List.map(fun statement -> statement |> withExpressionComment "F2")

    //member v.F3_RootReset(): CommentedStatement option =
    //    if goingSrcs.Any() then
    //        //going relay srcs
    //        let sets  =  toAnd goingSrcs 
    //        let rsts  = !! [real.EndTag].ToAnd()
    //        (sets, rsts) ==| (real.ResetTag, "") |> Some
    //    else
    //        None
