[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type ApiItemManager with

    member a.A1_PlanSend(activeSys:DsSystem, coins:Vertex seq) =

        let sets = coins.Select(fun c->c.VC.MM)
                        .ToOrElseOff()

        (sets, activeSys._off.Expr) --| (a.PS, getFuncName())

    member a.A2_PlanReceive(activeSys:DsSystem) =

        let sets =  a.ApiItem.RxET.Expr
        (sets, activeSys._off.Expr) --| (a.PE, getFuncName())

    member a.A3_SensorLinking(activeSys:DsSystem, coins:Call seq) =

        let input = coins.First().GetEndAction(a.ApiItem)
        
        let sets =
            if input.IsSome
            then
                coins.Select(fun c->c.LinkExpr).ToOrElseOff()
                <&&>  
                (input.Value <&&> !!a.PE.Expr <&&> !!a.SL2.Expr)
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
                          (  input.Value <&&> a.PE.Expr)
                     <||> (!!input.Value <&&> !!a.PE.Expr)
                     <||> activeSys._sim.Expr
                     )
            else 
                (activeSys._on.Expr)

        (sets, activeSys._off.Expr) ==| (a.SL2, getFuncName())

