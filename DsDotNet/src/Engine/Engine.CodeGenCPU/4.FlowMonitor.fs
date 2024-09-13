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
        [
            yield (set, rst) --| (f.readyCondition, getFuncName())
            for ready in f.HWReadyConditions do
                yield (f.ManuExpr <&&> !@ready.ActionINFunc , f.ClearExpr) --| (ready.ErrorCondition, getFuncName())
        ]

    member f.F4_FlowDriveCondition() =
        let set = f.HWDriveConditionsToAndElseOn
        let rst = f._off.Expr
        [
            yield (set, rst) --| (f.driveCondition, getFuncName())
            for drive in f.HWDriveConditions do
                yield (f.AutoExpr <&&> !@drive.ActionINFunc , f.ClearExpr) --| (drive.ErrorCondition, getFuncName())
        ]

    member f.F5_FlowPauseAnalogAction() =
        [
            for pause in f.HWPauseAnalogActions do
                let valExpr = pause.TaskDevParamIO.OutParam.Value.WriteValue |> any2expr
                yield (f.p_st.Expr, valExpr) --> (pause.OutTag, getFuncName())
        ]

    member f.F6_FlowEmergencyAnalogAction() =
        [
            for emg in f.HWEmergencyAnalogActions do
                let valExpr = emg.TaskDevParamIO.OutParam.Value.WriteValue |> any2expr
                yield (f.emg_st.Expr, valExpr) --> (emg.OutTag, getFuncName())
        ]
