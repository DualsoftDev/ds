[<AutoOpen>]
module Engine.CodeGenCPU.ConvertCall

open System.Linq
open Engine.CodeGenCPU
open Engine.Core
open Dual.Common.Core.FS

type VertexManager with
    member v.C1_CallMemo(): CommentedStatement  =
        let v, call  = v :?> VertexMCoin, v.Vertex :?> Call 
        let dop, mop = v.Flow.dop.Expr, v.Flow.mop.Expr
        
        let sets = 
            (
                call.StartPointExpr
                <||> (dop <&&> v.ST.Expr)
                <||> (mop <&&> v.SF.Expr)
            )
            //<&&> 
            //!!call.MutualResetCalls.Select(fun c-> c.EndAction).ToOrElseOff()  //들뜨면 RX 에러 발생해서 잡음
            
        let rsts =
            let plan = call.EndPlan
            let action = call.EndAction

            (plan <&&> v._sim.Expr)
            <||>
            (action <&&> !!v._sim.Expr)

        (sets, rsts) ==| (call.VC.MM, getFuncName())

