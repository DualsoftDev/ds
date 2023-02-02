[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS

type VertexMCoin with
    member coin.C1_CallPlanSend(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let dop, mop, rop = coin.Flow.dop.Expr, coin.Flow.mop.Expr, coin.Flow.rop.Expr
        let sharedCalls = coin.GetSharedCall().Select(getVM)
        let startTags   = ([coin.ST] @ sharedCalls.STs()).ToOr()
        let forceStarts = ([coin.SF] @ sharedCalls.SFs()).ToOr()
        let getStartPointExpr(coin:CallDev, jd:TaskDev) =
            match coin.Parent.GetCore() with
            | :? Real as r ->
                if r.V.OriginInfo.Tasks.Select(fun (t,_)->t).Contains(jd)
                    then call._on.Expr <&&> r.V.RO.Expr
                    else call._off.Expr
            | _ ->       call._off.Expr

        [
            for jd in call.CallTargetJob.DeviceDefs do
                let sets = (dop <&&> startTags) <||>
                           (mop <&&> forceStarts) <||>
                           (rop <&&> getStartPointExpr (call, jd))

                let rsts = jd.MutualResets(coin.System)
                             .Select(fun f -> f.ApiItem.PS)
                             .ToOrElseOff(coin.System)

                yield (sets, rsts) --| (jd.ApiItem.PS, getFuncName())
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                if jd.ApiItem.TXs.any()
                then yield (jd.ApiItem.PS.Expr, rsts) --| (jd.ActionOut, getFuncName())
        ]

    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for jd in call.CallTargetJob.DeviceDefs do
                let sets = jd.RXs.ToAndElseOn(coin.System)
                yield (sets, rsts) --| (jd.ApiItem.PE, getFuncName() )
        ]

    member coin.C4_CallActionIn(): CommentedStatement list =
        let sharedCalls = coin.GetSharedCall()
        let call = coin.Vertex :?> CallDev
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
