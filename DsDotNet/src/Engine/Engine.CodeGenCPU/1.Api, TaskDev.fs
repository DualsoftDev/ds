[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type TaskDevManager with

    member d.TD1_PlanSend(activeSys:DsSystem, coins:Vertex seq) =
        let fn = getFuncName()
        [|
            if d.TaskDev.IsAnalog
            then
                for coin in coins do
                    let ps = d.PlanStart(coin.GetPureCall().TargetJob)
                    yield (coin.VC.MM.Expr, activeSys._off.Expr) --| (ps, fn)
            else 
                let job = coins.OfType<Call>().First().TargetJob
                let coinMemos = coins.Select(fun s->s.VC.MM).ToOr()
                yield (coinMemos, activeSys._off.Expr) --| (d.PlanStart(job), fn)
        |]
    
    member d.TD2_PlanReceive(activeSys:DsSystem) =
        let fn = getFuncName()
        [|
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let jobFqdn = kv.Key
                let apiParam = kv.Value
                let sets = 
                    if d.TaskDev.IsAnalog then
                        apiParam.ApiItem.ApiItemEnd.Expr <&&> d.PlanStart(jobFqdn).Expr
                    else 
                        apiParam.ApiItem.ApiItemEnd.Expr 

                yield (sets, activeSys._off.Expr) --| (d.PlanEnd(jobFqdn), fn)
        |]

    member d.TD3_PlanOutput(activeSys:DsSystem) =
        let fn = getFuncName()
        [|
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let jobFqdn = kv.Key
                let sets =  d.PlanStart(jobFqdn).Expr <&&> d.PlanEnd(jobFqdn).Expr
                yield (sets, activeSys._off.Expr) --| (d.PlanOutput(jobFqdn), fn)
        |]


    member d.TD4_SensorLinking(call:Call) =
        let fn = getFuncName()
        [|
            for a in d.TaskDev.ApiItems do
                let input =  d.TaskDev.GetInExpr(call.TargetJob)
                let _off = d.TaskDev.ParnetSystem._off.Expr
                let sets =
                        call.RealLinkExpr <&&>  
                        (input <&&> !@a.ApiItemEnd.Expr <&&> !@a.SL2.Expr)

                yield (sets, _off) --| (a.SL1, fn)
        |]

    member d.TD5_SensorLinked(call:Call) =
        let fn = getFuncName()
        [|
            for a in d.TaskDev.ApiItems do
                if not(call.IsFlowCall)
                then
                    let input =  d.TaskDev.GetInExpr(call.TargetJob)
                    let _off = d.TaskDev.ParnetSystem._off.Expr
                    let sets =
                        call.RealLinkExpr
                        <&&> 
                            (   (  input <&&> a.ApiItemEnd.Expr)
                        <||> (!@input <&&> !@a.ApiItemEnd.Expr)
                        <||> call.System._sim.Expr
                            )

                    yield (sets, _off) ==| (a.SL2, fn)
        |]
    

type ApiItemManager with


    member am.A1_ApiSet(td:TaskDev, calls:Vertex seq) :  CommentedStatement [] =
        let fn = getFuncName()
        [|
            let api = am.ApiItem
            let pss = calls.Select(fun c-> td.GetPlanStart(c.GetPureCall().TargetJob)).ToOr()
            yield! (pss, td.ParnetSystem) --^ (am.ApiItemSetPusle, fn)
            yield  (am.ApiItemSetPusle.Expr, api.TX.VR.ET.Expr) ==| (am.ApiItemSet, fn)
        |]
 
    member am.A2_ApiEnd() =
        let fn = getFuncName()
        let api= am.ApiItem
        (api.RxET.Expr, api.ApiSystem._off.Expr) --| (api.ApiItemEnd, fn)
        
  