[<AutoOpen>]
module Engine.CodeGenCPU.FlowMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with


    member f.F1_FlowError() =
        let set = f.Graph.Vertices.OfType<Real>().SelectMany(fun r->r.Errors).ToOrElseOff()
        let rst = f.clear_btn.Expr
        (set, rst) ==| (f.stopError, getFuncName())

    
    member f.F2_FlowConditionErr() =
        let set = f.HWConditionsErrorExpr
        let rst = f.clear_btn.Expr
        (set, rst) ==| (f.stopConditionErr, getFuncName())

    
    member f.F3_FlowPause() =
        let set = f.pause_btn.Expr <||> f.HWBtnPauseExpr
        let rst = f.clear_btn.Expr
        (set, rst) ==| (f.pause, getFuncName())
