[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexMCoin with
    member coin.C1_CallMemo(): CommentedStatement  =
        let call = coin.Vertex :?> Call
        let dop, mop = coin.Flow.dop.Expr, coin.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> coin.ST.Expr)
                <||> (mop <&&> coin.SF.Expr)
            )
            <&&> 
            !!call.MutualResets.Select(fun d->d.ActionINFunc).ToOrElseOn()
            
        let rsts =
            let action =
                if call.UsingTon
                    then call.V.TDON.DN.Expr   //On Delay
                    else call.ActionINFuncs    

            let plan = call.GetCallApis().Select(fun f->f.PE).ToAndElseOff()

            (plan <&&> coin._sim.Expr)
            <||>
            (action <||> !!coin._sim.Expr)

        (sets, rsts) ==| (call.V.MM, getFuncName())


    //member coin.C2_CallActionOut(): CommentedStatement list =
    //    let call = coin.Vertex :?> Call
    //    let sop = call.Parent.GetFlow().sop.Expr
    //    let eop = call.Parent.GetFlow().eop.Expr
    //    let rstNormal = coin._off.Expr
    //    [
    //        for td in call.TargetJob.DeviceDefs do
    //            if td.ApiItem.TXs.any()
    //            then 
    //                let sets = td.ApiItem.PE.Expr <&&> td.ApiItem.PS.Expr 
    //                       //<&&> coin.Flow.dop.Expr

    //                if call.TargetJob.ActionType = JobActionType.Push 
    //                then 
    //                     let rstPush = td.MutualResetExpr(call.System)
                        
    //                     yield (sets, rstPush  <||> sop <||> eop) ==| (td.ActionOut, getFuncName())
    //                else 
    //                     yield (sets, rstNormal <||> sop<||> eop) --| (td.ActionOut, getFuncName())
    //    ]

  
    //member coin.C3_CallPlanReceive(): CommentedStatement list =
    //    let call = coin.Vertex :?> Call
    //    [
    //        for td in call.TargetJob.DeviceDefs do

    //            let sets =  td.RXTags.ToAndElseOn(coin.System) 

    //            yield (sets, coin._off.Expr) --| (td.ApiItem.PE, getFuncName() )
    //    ]


type VertexManager with
    member v.C1_CallMemo()           : CommentedStatement  = (v :?> VertexMCoin).C1_CallMemo()
    //member v.C2_CallActionOut()      : CommentedStatement list = (v :?> VertexMCoin).C2_CallActionOut()
    //member v.C3_CallPlanReceive()    : CommentedStatement list = (v :?> VertexMCoin).C3_CallPlanReceive()

type ApiItemManager with

    member a.A1_PlanSend(activeSys:DsSystem) : CommentedStatement  =
        let sets =  a.ApiItem.RXTags.ToAndElseOn() 
        (sets, activeSys._off.Expr) --| (a.PS, getFuncName())

    member a.A2_PlanReceive(activeSys:DsSystem) : CommentedStatement  =
        let sets =  a.ApiItem.RXTags.ToAndElseOn() 
        (sets, activeSys._off.Expr) --| (a.PE, getFuncName())

    member a.A3_ActionOut(calls:Call seq) : CommentedStatement list   =
        [
            for call in calls do
                let rstNormal = call._off.Expr
                let sop = call.Parent.GetFlow().sop.Expr
                let eop = call.Parent.GetFlow().eop.Expr
                for td in call.TargetJob.DeviceDefs do
                    if td.ApiItem.TXs.any()
                    then 
                        let sets = td.ApiItem.PE.Expr <&&> td.ApiItem.PS.Expr 
                        if call.TargetJob.ActionType = JobActionType.Push 
                        then 
                             let rstPush = td.MutualResetExpr(call.System)
                        
                             yield (sets, rstPush   <||> sop <||> eop) ==| (td.ActionOut, getFuncName())
                        else 
                             yield (sets, rstNormal <||> sop<||> eop) --| (td.ActionOut, getFuncName())
        ]
