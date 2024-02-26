[<AutoOpen>]
module Engine.CodeGenCPU.FlowMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with


    member f.F1_FlowError() =
        let set = f.Graph.Vertices.OfType<Real>().SelectMany(fun r->r.Errors).ToOrElseOff()
        let rst = f._off.Expr
        (set, rst) --| (f.stopError, getFuncName())

    member f.F2_FlowPause() =
        let set = f.sop.Expr <&&> !!f.stopError.Expr<&&> !!f.stopConditionErr.Expr 
        let rst = f._off.Expr
        (set, rst) --| (f.stopPause, getFuncName())

    member f.F3_FlowConditionErr() =
        let set = f.HWConditionsErrorExpr
        let rst = f._off.Expr
        (set, rst) --| (f.stopConditionErr, getFuncName())



