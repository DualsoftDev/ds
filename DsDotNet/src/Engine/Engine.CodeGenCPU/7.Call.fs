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
            //!!call.MutualResetCalls.Select(fun c-> c.EndAction).ToOrElseOff()  //��߸� RX ���� �߻��ؼ� ����
            
        let rsts =
            (
            if call.UsingTon 
            then v.TDON.DN.Expr 
            else (call.EndPlan <&&> v._sim.Expr) <||> call.EndAction 
            )
            <||>
            (call.V.Flow.pause.Expr)

        (sets, rsts) ==| (v.MM, getFuncName())

