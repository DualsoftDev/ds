[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type TaskDevManager with

    member d.TD1_PlanSend(activeSys:DsSystem, coins:Vertex seq) =
        [
            for c in coins.OfType<Call>() do
                yield (c.VC.MM.Expr, activeSys._off.Expr) --| (d.PlanStart(c.TargetJob), getFuncName())
        ]
    
    member d.TD2_PlanReceive(activeSys:DsSystem) =
        [
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let apiParam = kv.Value

                let sets = 
                    let inParam = apiParam.TaskDevParamIO.InParam

                    if inParam.IsSome && inParam.Value.Type <> DuBOOL then 
                        apiParam.ApiItem.APIEND.Expr <&&> d.PlanStart(apiParam).Expr
                    else 
                        apiParam.ApiItem.APIEND.Expr 

                yield (sets, activeSys._off.Expr) --| (d.PlanEnd(apiParam), getFuncName())
        ]

    member d.TD3_PlanOutput(activeSys:DsSystem) =
        [
            for kv in d.TaskDev.DicTaskTaskDevParamIO do
                let apiPara = kv.Value
                let sets =  d.PlanStart(apiPara).Expr <&&> d.PlanEnd(apiPara).Expr
                yield (sets, activeSys._off.Expr) --| (d.PlanOutput(apiPara), getFuncName())
        ]

    member d.A1_ApiSet(call:Call) :  CommentedStatement list=
        [
            let a = d.TaskDev.GetApiItem(call.TargetJob) 
            let ps = d.TaskDev.GetPlanStart(call.TargetJob)
            yield! (ps.Expr , call.System) --^ (a.ApiItemSetPusle, getFuncName())
            yield  (a.ApiItemSetPusle.Expr, a.TX.VR.ET.Expr) ==| (a.APISET, getFuncName())
        ]

    member d.A2_ApiEnd() =
        [
            for a in d.TaskDev.ApiItems do
                let sets = a.RxET.Expr
                yield (sets, a.ApiSystem._off.Expr) --| (a.APIEND, getFuncName())
        ]
        
    member d.A3_SensorLinking(call:Call) =
        [
            for a in d.TaskDev.ApiItems do
                if not(call.IsFlowCall)
                    then
                    let input =  d.TaskDev.GetInExpr(call.TargetJob)
                    let _off = d.TaskDev.ParnetSystem._off.Expr
                    let sets =
                            call.RealLinkExpr <&&>  
                            (input <&&> !@a.APIEND.Expr <&&> !@a.SL2.Expr)

                    yield (sets, _off) --| (a.SL1, getFuncName())
        ]

    member d.A4_SensorLinked(call:Call) =
        [
            for a in d.TaskDev.ApiItems do
                if not(call.IsFlowCall)
                then
                    let input =  d.TaskDev.GetInExpr(call.TargetJob)
                    let _off = d.TaskDev.ParnetSystem._off.Expr
                    let sets =
                        call.RealLinkExpr
                        <&&> 
                            (   (  input <&&> a.APIEND.Expr)
                        <||> (!@input <&&> !@a.APIEND.Expr)
                        <||> call.System._sim.Expr
                            )

                    yield (sets, _off) ==| (a.SL2, getFuncName())
        ]
    


  