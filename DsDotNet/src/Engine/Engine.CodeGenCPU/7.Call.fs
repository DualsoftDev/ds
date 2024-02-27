[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo() =
        let v, call  = v :?> VertexMCoin, v.Vertex :?> Call 
        let dop, mop = v.Flow.dop.Expr, v.Flow.mop.Expr
        
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
            (call.EndPlan <&&> v._sim.Expr)
            <||>
            (call.EndAction <&&> !!v._sim.Expr)
            <||>
            (call.V.Flow.pause.Expr)

        (sets, rsts) ==| (call.VC.MM, getFuncName())

