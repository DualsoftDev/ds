[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with


    member s.Y1_SystemSimulationForFlow() = [
            for flow in s.Flows do
                yield (s._auto_btn.Expr  , s._off.Expr) --| (flow.auto_btn,   getFuncName())
                yield (s._manual_btn.Expr, s._off.Expr) --| (flow.manual_btn, getFuncName())
                yield (s._drive_btn.Expr , s._off.Expr) --| (flow.drive_btn,  getFuncName())
                yield (s._pause_btn.Expr , s._off.Expr) --| (flow.pause_btn,  getFuncName())
                yield (s._emg_btn.Expr   , s._off.Expr) --| (flow.emg_btn,    getFuncName())
                yield (s._test_btn.Expr  , s._off.Expr) --| (flow.test_btn,   getFuncName())
                if RuntimeDS.Package.IsPCorPCSIM() then //PLC는  E2_PLCOnly 에서 처리중 
                    yield (s._home_btn.Expr  , s._off.Expr) --| (flow.home_btn,   getFuncName())
                    yield (s._clear_btn.Expr , s._off.Expr) --| (flow.clear_btn,  getFuncName())
                    yield (s._ready_btn.Expr , s._off.Expr) --| (flow.ready_btn,  getFuncName())
        ]
        
  

    member s.Y2_SystemPause() =
        let sets =  s.Flows.Select(fun f->f.p_st).ToOrElseOff()
        (sets, s._off.Expr) --| (s._pause, getFuncName())


    member s.Y3_SystemState() =
        [
            (s.Flows.Select(fun f->f.iop).ToAndElseOff(), s._off.Expr)      --| (s._idleMonitor  , getFuncName())
            (s.Flows.Select(fun f->f.aop).ToAndElseOff(), s._off.Expr)      --| (s._autoMonitor  , getFuncName())
            (s.Flows.Select(fun f->f.mop).ToAndElseOff(), s._off.Expr)      --| (s._manualMonitor, getFuncName())
            (s.Flows.Select(fun f->f.d_st).ToAndElseOff(), s._off.Expr)     --| (s._driveMonitor , getFuncName())
            (s.Flows.Select(fun f->f.e_st).ToOrElseOff(), s._off.Expr)      --| (s._errorMonitor , getFuncName())
            (s.Flows.Select(fun f->f.emg_st).ToOrElseOff(), s._off.Expr)    --| (s._emgState     , getFuncName())
            (s.Flows.Select(fun f->f.t_st).ToAndElseOff(), s._off.Expr)     --| (s._testMonitor  , getFuncName())
            (s.Flows.Select(fun f->f.r_st).ToAndElseOff(), s._off.Expr)     --| (s._readyMonitor , getFuncName())
            (s.Flows.Select(fun f->f.o_st).ToAndElseOff(), s._off.Expr)     --| (s._originMonitor, getFuncName())
            (s.Flows.Select(fun f->f.g_st).ToOrElseOff() , s._off.Expr)     --| (s._goingMonitor , getFuncName())
            
        ]

    member s.Y4_SystemConditionError() =
        [
            for condi in  s.HWConditions do
                yield (!@condi.ActionINFunc , s._off.Expr) --| (condi.ErrorCondition,   getFuncName())
        ]
        
    member s.Y5_SystemEmgAlramError() =
        [
            for emg in s.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                yield (emg.ActionINFunc , s._off.Expr) --| (emg.ErrorEmergency,   getFuncName())
        ]
        
