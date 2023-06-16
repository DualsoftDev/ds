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
        let getStartPointExpr(coin:CallDev, td:TaskDev) =
            match coin.Parent.GetCore() with
            | :? Real as r ->
                let tasks = r.V.OriginInfo.Tasks
                if tasks.Where(fun (_,ty) -> ty = InitialType.On) //NeedCheck 처리 필요 test ahn
                        .Select(fun (t,_)->t).Contains(td)
                    then r.V.RO.Expr
                    else call._off.Expr
            | _ ->
                call._off.Expr

        [
            for td in call.CallTargetJob.DeviceDefs do
                let sets = (dop <&&> startTags) <||>
                           (mop <&&> forceStarts) <||>
                           (rop <&&> getStartPointExpr (call, td))

                let rsts = td.MutualResets(coin.System)
                             .Select(fun f -> f.ApiItem.PS)
                             .ToOrElseOff(coin.System)

                yield (sets, rsts) --| (td.ApiItem.PS, getFuncName())
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for td in call.CallTargetJob.DeviceDefs do
                if td.ApiItem.TXs.any()
                then yield (td.ApiItem.PS.Expr, rsts) --| (td.ActionOut, getFuncName())
        ]

    //member coin.C3_CallPlanReceive(): CommentedStatement  =
    //    let call = coin.Vertex :?> CallDev
    //    let rsts = coin._off.Expr

    //    let tds = call.CallTargetJob.DeviceDefs.SelectMany(fun td -> td.RXs)
    //    let sets = tds.ToAndElseOn(coin.System)
    //    (sets, rsts) --| (coin.PE, getFuncName())
    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for td in call.CallTargetJob.DeviceDefs do
                let sets = td.RXs.ToAndElseOn(coin.System)
                yield (sets, rsts) --| (td.ApiItem.PE, getFuncName() )
        ]

    member coin.C4_CallActionIn(): CommentedStatement list =
        let sharedCalls = coin.GetSharedCall()
        let call = coin.Vertex :?> CallDev
        let rsts = coin._off.Expr
        [
            for sharedCall in sharedCalls do
                let sets =
                    let nonSim =
                        if call.UsingTon
                            then call.V.TON.DN.Expr   //On Delay
                            else call.INs.ToAndElseOn(coin.System)

                    let sim = call.CallTargetJob.DeviceDefs
                                .Select(fun f->f.ApiItem.PE)
                                .ToAndElseOn(coin.System)

                    nonSim <||> (sim <&&> coin._sim.Expr)

                yield (sets, rsts) --| (sharedCall.V.ET, getFuncName() )
        ]


type VertexManager with
    member v.C1_CallPlanSend()   : CommentedStatement list = (v :?> VertexMCoin).C1_CallPlanSend()
    member v.C2_CallActionOut()  : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    member v.C3_CallPlanReceive(): CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()
    member v.C4_CallActionIn()   : CommentedStatement list = (v :?> VertexMCoin).C4_CallActionIn()
