[<AutoOpen>]
module Engine.CodeGenCPU.ConvertApi

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type ApiItemManager with

    member a.A1_PlanSend(activeSys:DsSystem, coins:Call seq) : CommentedStatement  =
        let sets = coins.Select(fun c->c.VC.MM).ToOrElseOff()
        (sets, activeSys._off.Expr) --| (a.PS, getFuncName())

    member a.A2_PlanReceive(activeSys:DsSystem) : CommentedStatement  =
        let sets =  a.ApiItem.RxETs.ToAndElseOn() 
        (sets, activeSys._off.Expr) --| (a.PE, getFuncName())

    member a.A3_ActionSend(activeSys:DsSystem, coins:Call seq) : CommentedStatement  =
        let input = coins.First().GetEndAction(a.ApiItem)
        
        let sets = 
            coins.Select(fun c->c.SyncExpr).ToOrElseOff()
            <&&>  
            (input <&&> !!a.PE.Expr <&&> !!a.AL.Expr)

        (sets, activeSys._off.Expr) --| (a.AS, getFuncName())

    member a.A4_ActionLink(activeSys:DsSystem, coins:Call seq) : CommentedStatement  =
        let input = coins.First().GetEndAction(a.ApiItem)
        let sets = 
            coins.Select(fun c->c.SyncExpr).ToOrElseOff()
            <&&>( 
                      (  input <&&> a.PE.Expr)
                 <||> (!!input <&&> !!a.PE.Expr)
                 <||> activeSys._sim.Expr
                 )

        (sets, activeSys._off.Expr) ==| (a.AL, getFuncName())


    member a.A5_ActionOut(coins:Call seq) : CommentedStatement list   =
        [
            for coin in coins do
                let rstNormal = coin._off.Expr
                let rop = coin.Parent.GetFlow().rop.Expr
                for td in coin.TaskDevs do
                    if td.ApiItem.TXs.any()
                    then 
                        let sets = td.ApiItem.PE.Expr <&&> td.ApiItem.PS.Expr 
                        if coin.TargetJob.ActionType = JobActionType.Push 
                        then 
                             let rstPush = coin.MutualResetCalls.Select(fun c->c.VC.MM).ToOrElseOff()
                        
                             yield (sets, rstPush   <||> !!rop) ==| (td.AO, getFuncName())
                        else 
                             yield (sets, rstNormal <||> !!rop) --| (td.AO, getFuncName())
        ]
