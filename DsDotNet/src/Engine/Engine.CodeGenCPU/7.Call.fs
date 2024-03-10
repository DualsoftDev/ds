[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo() =
        let v = v :?> VertexMCoin
        let call = 
            match v.Vertex with
            | :? Call as c->  c
            | :? Alias as al->  al.TargetWrapper.CallTarget().Value
            |_ -> failwithf "error coin Type"


        let dop, mop = v.Flow.d_st.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr)
                <||> (mop <&&> v.SF.Expr)
            )
            <&&> call.SafetyExpr
            //<&&> 
            //!!call.MutualResetCalls.Select(fun c-> c.EndAction).ToOrElseOff()  //들뜨면 RX 에러 발생해서 잡음
            
        let rsts =
            (
            if call.UsingTon 
            then v.TDON.DN.Expr 
            else (call.EndPlan <&&> v._sim.Expr) <||> call.EndAction 
            )
            <||>
            (call.V.Flow.pause.Expr)

        (sets, rsts) ==| (v.MM, getFuncName())

    
    member v.C2_ActionOut() =
        let v = v :?> VertexMCoin
        let coin = v.Vertex :?> Call
        [
                let rstNormal = coin._off.Expr
                let rop = coin.Parent.GetFlow().r_st.Expr
                let t_st = coin.Parent.GetFlow().t_st.Expr
                for td in coin.TaskDevs do
                    let api = td.ApiItem
                    if td.OutAddress <> TextSkip
                    then 
                        let sets = api.PE.Expr <&&> api.PS.Expr 
                        if coin.TargetJob.ActionType = JobActionType.Push 
                        then 
                             let rstMemos = coin.MutualResetCalls.Select(fun c->c.VC.MM)
                             let rstPush = rstMemos.ToOr()
                        
                             yield (sets, rstPush   <||> !!rop <||> t_st) ==| (td.AO, getFuncName())
                        else 
                             yield (sets, rstNormal <||> !!rop <||> t_st) --| (td.AO, getFuncName())
        ]
