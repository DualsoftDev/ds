[<AutoOpen>]
module Engine.CodeGenCPU.ConvertLink

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Engine.Common.FS


type VertexManager with
    //test ahn
    member v.L1_LinkStart(): CommentedStatement list =
        let v = v :?> VertexMCoin

        let srcsWeek, srcsStrong  = getStartEdgeSources(v.Flow.Graph, v.Vertex)
        let rsts  = v.F.Expr
        [
            if srcsWeek.Any() then
                let sets = srcsWeek.GetCausalTags(v.System, true)
                yield (sets, rsts) ==| (v.ST, "L1" )

            if srcsStrong.Any() then
                let sets = srcsStrong.GetCausalTags(v.System, true)
                yield (sets, rsts) --| (v.ST, "L1" )
        ]
        //test ahn
    member v.L2_LinkReset(): CommentedStatement  =
        (v.PA.Expr, v._off.Expr) --| (v.PA, "L2" )
        //test ahn
    member v.L3_LinkStartReset(): CommentedStatement  =
        (v.PA.Expr, v._off.Expr) --| (v.PA, "L3" )
