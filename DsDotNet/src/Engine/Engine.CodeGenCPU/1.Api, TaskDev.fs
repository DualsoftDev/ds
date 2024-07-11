[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS



    
type TaskDevManager with

    member d.TD1_PlanSend(activeSys:DsSystem, coins:Vertex seq) =

        let sets = coins.Select(fun c->c.VC.MM)
                        .ToOrElseOff()

        (sets, activeSys._off.Expr) --| (d.PS, getFuncName())

    member d.TD2_PlanReceive(activeSys:DsSystem) =

        let sets =  d.TaskDev.ApiItem.APIEND.Expr
        (sets, activeSys._off.Expr) --| (d.PE, getFuncName())



type ApiItemManager with

    member a.A1_ApiSet(sys:DsSystem, td:TaskDev) =

        let sets =  td.PS.Expr
        (sets, sys._off.Expr) --| (a.APISET, getFuncName())

    member a.A2_ApiEnd(sys:DsSystem) =

        let sets =  a.ApiItem.RxET.Expr
        (sets, sys._off.Expr) --| (a.APIEND, getFuncName())

    member a.A3_SensorLinking(activeSys:DsSystem, coins:Call seq) =

        let input = coins.First().GetEndAction(a.ApiItem)
        
        let sets =
            if input.IsSome
            then
                coins.Select(fun c->c.LinkExpr).ToOrElseOff()
                <&&>  
                (input.Value <&&> !@a.APIEND.Expr <&&> !@a.SL2.Expr)
            else 
                (activeSys._off.Expr)

        (sets, activeSys._off.Expr) --| (a.SL1, getFuncName())

    member a.A4_SensorLinked(activeSys:DsSystem, coins:Call seq) =

        let input = coins.First().GetEndAction(a.ApiItem)
        let sets = 
            if input.IsSome
            then
                coins.Select(fun c->c.LinkExpr).ToOrElseOff()
                <&&>( 
                          (  input.Value <&&> a.APIEND.Expr)
                     <||> (!@input.Value <&&> !@a.APIEND.Expr)
                     <||> activeSys._sim.Expr
                     )
            else 
                (activeSys._on.Expr)

        (sets, activeSys._off.Expr) ==| (a.SL2, getFuncName())
