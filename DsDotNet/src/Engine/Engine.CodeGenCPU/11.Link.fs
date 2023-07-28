[<AutoOpen>]
module Engine.CodeGenCPU.ConvertLink

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type VertexManager with
    //test ahn
    member v.L1_LinkStart(): CommentedStatement list =
        let v = v :?> VertexMCoin

        let rsts  = v.F.Expr
        [
            match getStartEdgeSources(v.Flow.Graph, v.Vertex) with
            | DuEssWeak ws when ws.Any() ->
                let sets = ws.GetCausalTags(v.System, true)
                yield (sets, rsts) ==| (v.ST, getFuncName() )
            | DuEssStrong ss when ss.Any() ->
                let sets = ss.GetCausalTags(v.System, true)
                yield (sets, rsts) --| (v.ST, getFuncName() )
            | _ -> ()
        ]


        //test ahn
    member v.L2_LinkReset(): CommentedStatement  =
        (v.PA.Expr, v._off.Expr) --| (v.PA, getFuncName() )
        //test ahn
    member v.L3_LinkStartReset(): CommentedStatement  =
        (v.PA.Expr, v._off.Expr) --| (v.PA, getFuncName())
