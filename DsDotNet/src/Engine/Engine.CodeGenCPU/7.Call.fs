[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexMCoin with
    member coin.C1_CallPlanSend(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let dop, mop, rop = coin.Flow.dop.Expr, coin.Flow.mop.Expr, coin.Flow.rop.Expr
        let sharedCalls = coin.GetSharedCall().Select(getVM)
        let startTags   = ([coin.ST] @ sharedCalls.STs()).ToOr()
        let forceStarts = ([coin.SF] @ sharedCalls.SFs()).ToOr()
        [
            for jd in call.CallTargetJob.DeviceDefs do
                let startPointExpr = getStartPointExpr(call, jd)
                let sets = (dop <&&> startTags) <||>
                           (mop <&&> forceStarts) <||>
                           (rop <&&> startPointExpr)

                let rsts = jd.MutualResets(coin.System)
                             .Select(fun f -> f.ApiItem.PS)
                             .ToOrElseOff(coin.System)

                yield (sets, rsts) --| (jd.ApiItem.PS, "C1" )
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                yield (jd.ApiItem.PS.Expr, rsts) --| (jd.ActionOut, "C2" )
        ]

    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                let sets = jd.RXs.ToAndElseOn(coin.System)
                yield (sets, rsts) --| (jd.ApiItem.PR, "C3" )
        ]

    member coin.C4_CallActionIn(): CommentedStatement list =
        let sharedCalls = coin.GetSharedCall()
        let call = coin.Vertex :?> Call
        let rsts = coin._off.Expr
        [
            for sharedCall in sharedCalls do
                let sets =
                    if call.UsingTon
                        then call.V.TON.DN.Expr   //On Delay
                        else call.INs.ToAndElseOn(coin.System)
                yield (sets, rsts) --| (sharedCall.V.ET, "C4" )
        ]


type VertexManager with
    member v.C1_CallPlanSend()   : CommentedStatement list = (v :?> VertexMCoin).C1_CallPlanSend()
    member v.C2_CallActionOut()  : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    member v.C3_CallPlanReceive(): CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()
    member v.C4_CallActionIn()   : CommentedStatement list = (v :?> VertexMCoin).C4_CallActionIn()
