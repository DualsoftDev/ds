[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type TaskDevManager with



    member d.TD3_PlanOutput(activeSys:DsSystem, coins:Vertex seq) =

        let fn = getFuncName()
        let off = activeSys._off.Expr
        [|
            for coin in coins do
                let sets  = coin.VC.MM.Expr 
                            <&&> d.ApiManager.ApiItemSet.Expr 
                            <&&> d.ApiManager.ApiItemEnd.Expr
                yield (sets, off) --| (coin.VC.PO, fn)
        |]

    member d.TD4_SensorLinking(call:Call) =
        let fn = getFuncName()
        let off = d.TaskDev.ParentSystem._off.Expr
        let input = d.TaskDev.GetInExpr(call)
        [|
            let a = d.TaskDev.ApiItem 
            let sets =
                call.RealLinkExpr <&&>
                (input <&&> !@a.ApiItemEnd.Expr <&&> !@a.SL2.Expr)

            yield (sets, off) --| (a.SL1, fn)
        |]

    member d.TD5_SensorLinked(call:Call) =
        let fn = getFuncName()
        let off = d.TaskDev.ParentSystem._off.Expr
        let input =  d.TaskDev.GetInExpr(call)
        [|
            let a = d.TaskDev.ApiItem 
            if not(call.IsFlowCall) then
                let sets =
                    call.RealLinkExpr
                    <&&>
                        (   (input <&&> a.ApiItemEnd.Expr)
                            <||> (!@input <&&> !@a.ApiItemEnd.Expr)
                            <||> call.System._sim.Expr
                        )

                yield (sets, off) ==| (a.SL2, fn)
        |]


type ApiItemManager with


    member am.A1_ApiSet(td:TaskDev, calls:Vertex seq): CommentedStatement [] =
        let fn = getFuncName()
        [|
            let api = am.ApiItem
            let pss = calls.Select(fun s->s.VC.MM).ToOr()
            let tempRising  = getSM(td.ParentSystem).GetTempBoolTag(td.QualifiedName)

            yield! (pss, td.ParentSystem) --^ (tempRising, fn)
            yield  (tempRising.Expr, api.TX.VR.ET.Expr) ==| (am.ApiItemSet, fn)
        |]

    member am.A2_ApiEnd() =
        let fn = getFuncName()
        let api= am.ApiItem
        (api.RxET.Expr, api.ApiSystem._off.Expr) --| (api.ApiItemEnd, fn)

