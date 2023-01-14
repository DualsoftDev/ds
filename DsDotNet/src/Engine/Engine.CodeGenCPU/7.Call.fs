[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexManager with

    member v.C1_CallPlanSend(): CommentedStatement list =
        let v = v :?> VertexMCoin
        let call = v.Vertex :?> Call
        let rop, mop = v.Flow.rop.Expr, v.Flow.mop.Expr
        let startTags   = ([v.ST] @ v.GetSharedCall().Select(getVM).STs()).ToOr()
        let forceStarts = ([v.SF] @ v.GetSharedCall().Select(getVM).SFs()).ToOr()
        [
            for jd in call.CallTargetJob.JobDefs do
                let startPointExpr = getStartPointExpr(call, jd)
                let sets = (rop <&&> startTags) <||>
                           (mop <&&> forceStarts) <||>
                           (rop <||> mop <&&> startPointExpr)

                let rsts = jd.MutualResets(v.System)
                             .Select(fun f -> f.ApiItem.PS)
                             .ToOrElseOff(v.System)

                yield (sets, rsts) --| (jd.ApiItem.PS, "C1" )
        ]


    member v.C2_CallActionOut(): CommentedStatement list =
        let v = v :?> VertexMCoin
        let call = v.Vertex :?> Call
        let rsts = v.System._off.Expr
        [
            for jd in call.CallTargetJob.JobDefs do
                yield (jd.ApiItem.PS.Expr, rsts) --| (jd.ActionOut, "C2" )
        ]

    member v.C3_CallPlanReceive(): CommentedStatement list =
        let v = v :?> VertexMCoin
        let call = v.Vertex :?> Call
        let rsts = v.System._off.Expr
        [
            for jd in call.CallTargetJob.JobDefs do
                let sets = jd.RXs.ToAndElseOn(v.System)
                yield (sets, rsts) --| (jd.ApiItem.PR, "C3" )
        ]

    member v.C4_CallActionIn(): CommentedStatement list =
        let v = v :?> VertexMCoin
        let coins = v.GetSharedCall()
        let call = v.Vertex :?> Call
        let rsts = v.System._off.Expr
        [
            for coin in coins do
                let sets =
                    if call.UsingTon
                        then call.V.TON.DN.Expr   //On Delay
                        else call.INs.ToAndElseOn(v.System)
                yield (sets, rsts) --| (coin.V.ET, "C4" )
        ]