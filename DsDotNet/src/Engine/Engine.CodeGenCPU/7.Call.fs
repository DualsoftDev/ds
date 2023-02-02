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

                yield (sets, rsts) --| (jd.ApiItem.PS, getFuncName())
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                if jd.ApiItem.TXs.any()
                then yield (jd.ApiItem.PS.Expr, rsts) --| (jd.ActionOut, getFuncName())
        ]

    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                let sets = jd.RXs.ToAndElseOn(coin.System)
                yield (sets, rsts) --| (jd.ApiItem.PE, getFuncName() )
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
                yield (sets, rsts) --| (sharedCall.V.ET, getFuncName() )
        ]


type VertexManager with
    member v.C1_CallPlanSend()   : CommentedStatement list = (v :?> VertexMCoin).C1_CallPlanSend()
    member v.C2_CallActionOut()  : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    member v.C3_CallPlanReceive(): CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()
    member v.C4_CallActionIn()   : CommentedStatement list = (v :?> VertexMCoin).C4_CallActionIn()
