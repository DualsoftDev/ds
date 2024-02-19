[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo(): CommentedStatement  =
        let v, call  = v :?> VertexMCoin, v.Vertex :?> Call 
        let dop, mop = v.Flow.dop.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr)
                <||> (mop <&&> v.SF.Expr)
            )
            <&&> 
            !!call.MutualResets.Select(fun d->d.ActionINFunc).ToOrElseOff()
            
        let rsts =
            let action = call.EndAction
            let plan = call.GetCallApis().Select(fun f->f.PE).ToAndElseOff()

            (plan <&&> v._sim.Expr)
            <||>
            (action <&&> !!v._sim.Expr)

        (sets, rsts) ==| (call.VC.MM, getFuncName())


type ApiItemManager with

    member a.A1_PlanSend(activeSys:DsSystem, calls:Call seq) : CommentedStatement  =
        let sets =  calls.Select(fun c->c.VC.MM).ToOrElseOff()
        (sets, activeSys._off.Expr) --| (a.PS, getFuncName())

    member a.A2_PlanReceive(activeSys:DsSystem) : CommentedStatement  =
        let sets =  a.ApiItem.RXTags.ToAndElseOn() 
        (sets, activeSys._off.Expr) --| (a.PE, getFuncName())

    member a.A3_ActionOut(calls:Call seq) : CommentedStatement list   =
        [
            for call in calls do
                let rstNormal = call._off.Expr
                let rop = call.Parent.GetFlow().rop.Expr
                for td in call.TargetJob.DeviceDefs do
                    if td.ApiItem.TXs.any()
                    then 
                        let sets = td.ApiItem.PE.Expr <&&> td.ApiItem.PS.Expr 
                        if call.TargetJob.ActionType = JobActionType.Push 
                        then 
                             let rstPush = td.MutualResetExpr(call.System)
                        
                             yield (sets, rstPush   <||> !!rop) ==| (td.ActionOut, getFuncName())
                        else 
                             yield (sets, rstNormal <||> !!rop) --| (td.ActionOut, getFuncName())
        ]
