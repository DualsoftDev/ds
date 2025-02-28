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
        let set = 
            if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() then
                f._on.Expr
            else 
                f.HWReadyConditionsToAndElseOn

        let rst = f._off.Expr
        [
            yield (set, rst) --| (f.readyCondition, getFuncName())
            for ready in f.HWReadyConditions do
                yield ((f.ManuExpr <||> f.AutoExpr) <&&> !@ready.ActionINFunc , f.ClearExpr) --| (ready.ErrorCondition, getFuncName())
        ]

    member f.F4_FlowDriveCondition() =
        let set = 
            if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() then
                f._on.Expr
            else 
                f.HWDriveConditionsToAndElseOn
                
        let rst = f._off.Expr
        [
            yield (set, rst) --| (f.driveCondition, getFuncName())
            for drive in f.HWDriveConditions do
                yield (f.AutoExpr <&&> !@drive.ActionINFunc , f.ClearExpr) --| (drive.ErrorCondition, getFuncName())
        ]

    member f.F5_FlowPauseAnalogAction() =
        [
            for pause in f.HWPauseAnalogActions do
                let valExpr = pause.ValueParamIO.Out.WriteValue |> any2expr
                yield (f.p_st.Expr, valExpr) --> (pause.OutTag, getFuncName())
        ]

    member f.F6_FlowPauseDigitalAction() =
        let addrOuts = f.System.OutputJobAddress
        [
            for pause in f.HWPauseDigitalActions do
                if not (addrOuts.Contains(pause.OutAddress)) then //job에 존재하면  J1_JobActionOuts 여기서 처리
                    let set = if pause.DigitalOutputTarget.Value then
                                    f.p_st.Expr
                              else
                                    !@f.p_st.Expr
                    yield (set, f._off.Expr) --| (pause.OutTag, getFuncName())
        ]


    member f.F7_FlowEmergencyAnalogAction() =
        [
            for emg in f.HWEmergencyAnalogActions do
                let valExpr = emg.ValueParamIO.Out.WriteValue |> any2expr
                yield (f.emg_st.Expr, valExpr) --> (emg.OutTag, getFuncName())
        ]

    member f.F8_FlowEmergencyDigitalAction() =
        let addrOuts = f.System.OutputJobAddress
        [
            for emg in f.HWEmergencyDigitalActions do
                if not (addrOuts.Contains(emg.OutAddress)) then //job에 존재하면  J1_JobActionOuts 여기서 처리
                    let set = if emg.DigitalOutputTarget.Value then
                                    f.emg_st.Expr
                              else
                                    !@f.emg_st.Expr
                    yield (set, f._off.Expr) --| (emg.OutTag, getFuncName())
        ]

    member f.O1_IdleOperationMode() =
        let set = !@f.aop.Expr <&&> !@f.mop.Expr
        let rst = f._off.Expr

        (set, rst) --| (f.iop, getFuncName())

    member f.O2_AutoOperationMode() =
        let set = f.AutoExpr 
        let rst = !@f.r_st.Expr
        (set, rst) --| (f.aop, getFuncName())

    member f.O3_ManualOperationMode () =
        let set = f.ManuExpr
        let rst = !@f.r_st.Expr 
        
        (set, rst) --| (f.mop, getFuncName())
