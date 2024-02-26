[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.B1_HWButtonOutput() = [
        for btn in s.HWButtons do
            if btn.OutTag.IsNonNull()  //OutAddress 주소가 있어야 IN-OUT btn 연결
            then
                let sets = btn.ActionINFunc
                let out = btn.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, getFuncName())
    ]


    member s.B2_SWButtonOutput() = [
                yield (s._auto_btn.Expr  , s._off.Expr) --| (s._auto_lamp  , getFuncName())
                yield (s._manual_btn.Expr, s._off.Expr) --| (s._manual_lamp, getFuncName())
                yield (s._drive_btn.Expr , s._off.Expr) --| (s._drive_lamp , getFuncName())
                yield (s._stop_btn.Expr  , s._off.Expr) --| (s._stop_lamp  , getFuncName())
                yield (s._emg_btn.Expr   , s._off.Expr) --| (s._emg_lamp   , getFuncName())
                yield (s._test_btn.Expr  , s._off.Expr) --| (s._test_lamp  , getFuncName())
                yield (s._clear_btn.Expr , s._off.Expr) --| (s._clear_lamp , getFuncName())
                yield (s._home_btn.Expr  , s._off.Expr) --| (s._home_lamp  , getFuncName())
                yield (s._ready_btn.Expr , s._off.Expr) --| (s._ready_lamp , getFuncName())
        ]
        

    member s.B3_HWModeLamp() = [

        for sysLamp in s.HWLamps.Filter(fun f-> f.IsGlobalSystemHw) do
            let modeBit =
                match sysLamp.LampType with
                | DuAutoLamp      -> s._autoState.Expr   
                | DuManualLamp    -> s._manualState.Expr 
                | DuDriveLamp     -> s._driveState.Expr  
                | DuStopLamp      -> s._stopPause.Expr <||> (s._stopErr.Expr <&&> s._flicker1sec.Expr)
                | DuEmergencyLamp -> s._emgState.Expr   
                | DuTestDriveLamp -> s._testState.Expr   
                | DuReadyLamp     -> s._readyState.Expr 
                | DuIdleLamp      -> s._idleState.Expr 
                | DuHomingLamp    -> s._homingState.Expr 
                
            let sets = if sysLamp.InTag.IsNull()
                       then modeBit  
                       else modeBit <||> sysLamp.ActionINFunc //강제 체크 비트

            let out = sysLamp.OutTag :?> Tag<bool>
            yield (sets, s._off.Expr) --| (out, getFuncName())



        for lamp in s.HWLamps.Filter(fun f-> not(f.IsGlobalSystemHw)) do
            let modeBit =
                let f = lamp.SettingFlows.Head()   //램프는 하나의 Flow에 타입별로 하나씩   Engine.Parser.FS 예외체크중   
                match lamp.LampType with
                | DuAutoLamp      -> f.aop
                | DuManualLamp    -> f.mop
                | DuDriveLamp     -> f.dop
                | DuStopLamp      -> f.sop
                | DuEmergencyLamp -> f.eop
                | DuTestDriveLamp -> f.top
                | DuReadyLamp     -> f.rop
                | DuIdleLamp      -> f.iop
                | DuHomingLamp    -> f.hop


            let sets = if lamp.InTag.IsNull()
                       then modeBit.Expr 
                       else modeBit.Expr <||>  lamp.ActionINFunc //강제 체크 비트

            let out = lamp.OutTag :?> Tag<bool>
            yield (sets, s._off.Expr) --| (out, getFuncName())
    ]

    member s.B4_SWModeLamp() = [
        for f in s.Flows do
            yield (f.auto_btn.Expr  , s._off.Expr) --| (f.auto_lamp  , getFuncName())
            yield (f.manual_btn.Expr, s._off.Expr) --| (f.manual_lamp, getFuncName())
            yield (f.drive_btn.Expr , s._off.Expr) --| (f.drive_lamp , getFuncName())
            yield (f.stop_btn.Expr  , s._off.Expr) --| (f.stop_lamp  , getFuncName())
            yield (f.emg_btn.Expr   , s._off.Expr) --| (f.emg_lamp   , getFuncName())
            yield (f.test_btn.Expr  , s._off.Expr) --| (f.test_lamp  , getFuncName())
            yield (f.clear_btn.Expr , s._off.Expr) --| (f.clear_lamp , getFuncName())
            yield (f.home_btn.Expr  , s._off.Expr) --| (f.home_lamp  , getFuncName())
            yield (f.ready_btn.Expr , s._off.Expr) --| (f.ready_lamp , getFuncName())
    ]
   
