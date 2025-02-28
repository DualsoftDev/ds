[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type TaskDevManager with


    member d.TD1_SensorLinking(call:Call) =
        let fn = getFuncName()
        let off = d.TaskDev.ParentSystem._off.Expr
        let input = d.TaskDev.GetInExpr(call)
        [|
            let a = d.TaskDev.ApiItem 
            let sets =
                call.RealLinkExpr <&&>
                (input <&&> !@a.ApiItemEnd.Expr <&&> !@a.SensorLinked.Expr)

            yield (sets, off) --| (a.SensorLinking, fn)
        |]

    member d.TD2_SensorLinked(call:Call) =
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
                            )

                yield (sets, off) ==| (a.SensorLinked, fn)
        |]


type ApiItemManager with


    member am.A1_ApiSet(td:TaskDev, calls:Vertex seq): CommentedStatement [] =
        let fn = getFuncName()
        [|
            let api = am.ApiItem
            let pss = calls.Select(fun s->s.VC.PS).ToOr() <&&> !@api.RxET.Expr
            let tempRising  = getSM(td.ParentSystem).GetTempBoolTag(td.QualifiedName)
            yield! (pss, td.ParentSystem) --^ (tempRising, fn)
            yield  (tempRising.Expr, api.RxET.Expr) ==|  (am.ApiItemSet, fn)
        |]

    member am.A2_ApiEnd() =
        let fn = getFuncName()
        let api= am.ApiItem
        (api.RxET.Expr, api.ApiSystem._off.Expr) --| (api.ApiItemEnd, fn)

