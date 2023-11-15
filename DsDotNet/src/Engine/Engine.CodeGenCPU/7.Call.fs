[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexMCoin with
    member coin.C1_CallPlanSend(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let dop, mop = coin.Flow.dop.Expr, coin.Flow.mop.Expr
        let sharedCalls = coin.GetSharedCall().Select(getVM)
        let startTags   = ([coin.ST] @ sharedCalls.STs()).ToOr()
        let forceStarts = ([coin.SF] @ sharedCalls.SFs()).ToOr()

        let getStartPointExpr(coin:Call, td:TaskDev) =
            match coin.Parent.GetCore() with
            | :? Real as r ->
                let tasks = r.V.OriginInfo.Tasks
                if tasks.Where(fun (_,ty) -> ty = InitialType.On) //NeedCheck 처리 필요 test ahn
                        .Select(fun (t,_)->t).Contains(td)
                    then r.V.RO.Expr <&&> if td.ExistIn then !!td.ActionINFunc else call._on.Expr
                    else r.V.RO.Expr <&&> call._off.Expr
            | _ -> 
                call._off.Expr

        [
            for td in call.CallTargetJob.DeviceDefs do
                let sets = 
                    ((dop <&&> startTags <||> getStartPointExpr (call, td)) <||> (mop <&&> forceStarts))
                <&&>
                    !!td.MutualReset(coin.System)
                        .Select(fun f -> f.ApiItem.PS)
                        .ToAndElseOff(coin.System)

                let rsts =
                    let action =
                        if td.ExistIn
                        then
                            if call.UsingTon
                                then call.V.TDON.DN.Expr   //On Delay
                                else td.ActionINFunc
                        else call._off.Expr

                    (action <||> coin._sim.Expr)
                    <&&> td.ApiItem.PE.Expr
                    <||> !!dop

                yield (sets, rsts) ==| (td.ApiItem.PS, getFuncName())
        ]


    member coin.C2_CallActionOut(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        let rstNormal = coin._off.Expr
        [
            for td in call.CallTargetJob.DeviceDefs do
                if td.ApiItem.TXs.any()
                then 
                    let sets = td.ApiItem.PE.Expr <&&> td.ApiItem.PS.Expr 
                           <&&> coin.Flow.dop.Expr

                    if td.ApiItem.ActionType = ApiActionType.Push 
                    then 
                         let rstPush = td.MutualResetExpr(call.System)
                        
                         yield (sets, rstPush  ) ==| (td.ActionOut, getFuncName())
                    else yield (sets, rstNormal) --| (td.ActionOut, getFuncName())
        ]

  
    member coin.C3_CallPlanReceive(): CommentedStatement list =
        let call = coin.Vertex :?> Call
        [
            for td in call.CallTargetJob.DeviceDefs do

                let sets =  td.RXTags.ToAndElseOn(coin.System) 

                yield (sets, coin._off.Expr) --| (td.ApiItem.PE, getFuncName() )
        ]


type VertexManager with
    member v.C1_CallPlanSend()       : CommentedStatement list = (v :?> VertexMCoin).C1_CallPlanSend()
    member v.C2_CallActionOut()      : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    member v.C3_CallPlanReceive()    : CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()
