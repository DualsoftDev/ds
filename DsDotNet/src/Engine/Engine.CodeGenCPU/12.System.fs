[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.Y1_SystemBitSetFlow() = [
            for flow in s.Flows do
                yield (s._auto_btn.Expr  , s._off.Expr) --| (flow.auto_btn,   getFuncName())
                yield (s._manual_btn.Expr, s._off.Expr) --| (flow.manual_btn, getFuncName())
                yield (s._drive_btn.Expr , s._off.Expr) --| (flow.drive_btn,  getFuncName())
                yield (s._stop_btn.Expr  , s._off.Expr) --| (flow.stop_btn,   getFuncName())
                yield (s._emg_btn.Expr   , s._off.Expr) --| (flow.emg_btn,    getFuncName())
                yield (s._test_btn.Expr  , s._off.Expr) --| (flow.test_btn,   getFuncName())
                yield (s._clear_btn.Expr , s._off.Expr) --| (flow.clear_btn,  getFuncName())
                yield (s._home_btn.Expr  , s._off.Expr) --| (flow.home_btn,   getFuncName())
                yield (s._ready_btn.Expr , s._off.Expr) --| (flow.ready_btn,  getFuncName())
        ]
        
    member s.Y2_SystemError() =
        let sets =  s.Flows.Collect(fun f-> [f.stopError;f.stopConditionErr]).ToOrElseOff()
        (sets, s._off.Expr) --| (s._stopErr, getFuncName())


    member s.Y3_SystemPause() =
        let sets =  s.Flows.Select(fun f->f.pause).ToOrElseOff()
        (sets, s._off.Expr) --| (s._pause, getFuncName())


    member s.Y4_SystemState() =
        [
            (s.Flows.Select(fun f->f.aop).ToAndElseOff(), s._off.Expr) --| (s._autoState   , getFuncName())
            (s.Flows.Select(fun f->f.mop).ToAndElseOff(), s._off.Expr) --| (s._manualState , getFuncName())
            (s.Flows.Select(fun f->f.dop).ToAndElseOff(), s._off.Expr) --| (s._driveState  , getFuncName())
            (s.Flows.Select(fun f->f.sop).ToOrElseOff(), s._off.Expr)  --| (s._stopState   , getFuncName())
            (s.Flows.Select(fun f->f.eop).ToOrElseOff(), s._off.Expr)  --| (s._emgState    , getFuncName())
            (s.Flows.Select(fun f->f.top).ToAndElseOff(), s._off.Expr) --| (s._testState   , getFuncName())
            (s.Flows.Select(fun f->f.rop).ToAndElseOff(), s._off.Expr) --| (s._readyState  , getFuncName())
            (s.Flows.Select(fun f->f.iop).ToAndElseOff(), s._off.Expr) --| (s._idleState   , getFuncName())
            (s.Flows.Select(fun f->f.oop).ToAndElseOff(), s._off.Expr) --| (s._originState , getFuncName())
            (s.Flows.Select(fun f->f.hop).ToOrElseOff() , s._off.Expr) --| (s._homingState , getFuncName())
            (s.Flows.Select(fun f->f.gop).ToOrElseOff() , s._off.Expr) --| (s._goingState , getFuncName())
            
        ]

    
