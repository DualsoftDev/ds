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
            let job = coins.OfType<Call>().First().TargetJob
            let coinMemos = coins.Select(fun s->s.VC.MM).ToOr()
            yield (coinMemos, activeSys._off.Expr) --| (d.PlanStart(job), fn)
        |]
    
    //member d.TD1_PlanSend(activeSys:DsSystem, coin:Vertex) =
    //    let fn = getFuncName()
    //    [|
    //        let call = coin.GetPureCall()
    //        yield (call.VC.MM.Expr, activeSys._off.Expr) --| (d.PlanStart(call.TargetJob), fn)
    //    |]


    member d.TD2_PlanReceive(activeSys:DsSystem) =
        let fn = getFuncName()
        [|
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let apiParam = kv.Value

                let sets = 
                    let inParam = apiParam.TaskDevParamIO.InParam

                    if inParam.IsSome && inParam.Value.Type <> DuBOOL then 
                        apiParam.ApiItem.ApiItemEnd.Expr <&&> d.PlanStart(apiParam).Expr
                    else 
                        apiParam.ApiItem.ApiItemEnd.Expr 

                yield (sets, activeSys._off.Expr) --| (d.PlanEnd(apiParam), fn)
        |]

    member d.TD3_PlanOutput(activeSys:DsSystem) =
        let fn = getFuncName()
        [|
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let apiPara = kv.Value
                let sets =  d.PlanStart(apiPara).Expr <&&> d.PlanEnd(apiPara).Expr
                yield (sets, activeSys._off.Expr) --| (d.PlanOutput(apiPara), fn)
        |]

    member d.A1_ApiSet(call:Call) :  CommentedStatement [] =
        let fn = getFuncName()
        [|
            let a = d.TaskDev.GetApiItem(call.TargetJob) 
            let ps = d.TaskDev.GetPlanStart(call.TargetJob)
            yield! (ps.Expr , call.System) --^ (a.ApiItemSetPusle, fn)
            yield  (a.ApiItemSetPusle.Expr, a.TX.VR.ET.Expr) ==| (a.ApiItemSet, fn)
        |]

    member d.A3_SensorLinking(call:Call) =
        let fn = getFuncName()
        [|
            for a in d.TaskDev.ApiItems do
                if not(call.IsFlowCall)
                    then
                    let input =  d.TaskDev.GetInExpr(call.TargetJob)
                    let _off = d.TaskDev.ParnetSystem._off.Expr
                    let sets =
                            call.RealLinkExpr <&&>  
                            (input <&&> !@a.ApiItemEnd.Expr <&&> !@a.SL2.Expr)

                    yield (sets, _off) --| (a.SL1, fn)
        |]

    member d.A4_SensorLinked(call:Call) =
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
 
    member a.A2_ApiEnd() =
        let fn = getFuncName()
        let api= a.ApiItem
        (api.RxET.Expr, api.ApiSystem._off.Expr) --| (api.ApiItemEnd, fn)
        
  