[<AutoOpen>]
module Engine.CodeGenCPU.FlowMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with


    member f.F1_FlowError() =
        let set = f.Graph.Vertices.OfType<Real>().SelectMany(fun r->r.Errors).ToOrElseOff()
        let rst = f.ClearExpr
        (set, rst) ==| (f.stopError, getFuncName())
    
    member f.F2_FlowPause() =
        let set = f.PauseExpr
        let rst = f.ClearExpr
        (set, rst) ==| (f.p_st, getFuncName())

    member f.F3_FlowReadyCondition() =
        let set = f.HWReadyConditionsToAndElseOn
        let rst = f._off.Expr
        (set, rst) --| (f.readyCondition, getFuncName())

    member f.F4_FlowDriveCondition() =
        let set = f.HWDriveConditionsToAndElseOn
        let rst = f._off.Expr
        (set, rst) --| (f.driveCondition, getFuncName())

