[<AutoOpen>]
module Engine.CodeGenCPU.ConvertButtonLamp

open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS
open System.Linq

type DsSystem with
    member s.B1_HWButtonOutput() =
        let fn = getFuncName()
        [|
            for btn in s.HWButtons do
                if btn.OutTag.IsNonNull() then  //OutAddress 주소가 있어야 IN-OUT btn 연결
                    let sets = btn.ActionINFunc
                    let out = btn.OutTag :?> Tag<bool>
                    yield (sets, s._off.Expr) --| (out, fn)
        |]


    member s.B2_SWButtonOutput() =
        let fn = getFuncName()
        let off = s._off.Expr
        [|
            yield (s._auto_btn.Expr  , off) --| (s._auto_lamp  , fn)
            yield (s._manual_btn.Expr, off) --| (s._manual_lamp, fn)
            yield (s._drive_btn.Expr , off) --| (s._drive_lamp , fn)
            yield (s._pause_btn.Expr , off) --| (s._pause_lamp , fn)
            yield (s._emg_btn.Expr   , off) --| (s._emg_lamp   , fn)
            yield (s._test_btn.Expr  , off) --| (s._test_lamp  , fn)
            yield (s._clear_btn.Expr , off) --| (s._clear_lamp , fn)
            yield (s._home_btn.Expr  , off) --| (s._home_lamp  , fn)
            yield (s._ready_btn.Expr , off) --| (s._ready_lamp , fn)
        |]
        

    member s.B3_HWModeLamp() =
        let fn = getFuncName()
        [|
            for sysLamp in s.HWLamps.Filter(fun f-> f.IsGlobalSystemHw) do
                let modeBit =
                    match sysLamp.LampType with
                    | DuAutoModeLamp   -> s._autoMonitor.Expr   
                    | DuManualModeLamp -> s._manualMonitor.Expr 
                    | DuDriveStateLamp -> 
                        let drive = s._driveMonitor.Expr 
                        let test = s._testMonitor.Expr 
                        let going = s._goingMonitor.Expr 

                        (drive <&&> !@going) 
                        <||> 
                        (drive <&&> s._goingMonitor.Expr <&&> s._flicker1sec.Expr)
                        <||> 
                        (test  <&&> s._flicker100msec.Expr)
                                    

                    | DuErrorStateLamp      -> s._emgState.Expr <||> (s._errorMonitor.Expr <&&> s._flicker1sec.Expr)
                    | DuTestDriveStateLamp -> s._testMonitor.Expr   
                    | DuReadyStateLamp     -> (s._readyMonitor.Expr  <&&> !@s._pause.Expr )<||> (s._pause.Expr <&&> s._flicker1sec.Expr)
                    | DuIdleModeLamp      ->  s._idleMonitor.Expr
                    | DuOriginStateLamp    ->
                        let originActions =
                            s.GetRealVertices()
                                .Select(getVMReal)
                                .Select(fun r->r.OA)
                                .ToOrElseOff()
                        
                        s._originMonitor.Expr <||> (originActions <&&> s._flicker1sec.Expr) 
                
                let sets =
                    if sysLamp.InTag.IsNull() then
                        modeBit
                    else
                        modeBit <||> sysLamp.ActionINFunc //강제 체크 비트

                let out = sysLamp.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, fn)



            for lamp in s.HWLamps.Filter(fun f-> not(f.IsGlobalSystemHw)) do
                let modeBit =
                    let f = lamp.SettingFlows.Head()   //램프는 하나의 Flow에 타입별로 하나씩  
                    match lamp.LampType with
                    | DuIdleModeLamp      -> f.iop
                    | DuAutoModeLamp      -> f.aop
                    | DuManualModeLamp    -> f.mop
                    | DuDriveStateLamp     -> f.d_st
                    | DuErrorStateLamp     -> f.e_st
                    | DuTestDriveStateLamp -> f.t_st
                    | DuReadyStateLamp     -> f.r_st
                    | DuOriginStateLamp    -> f.o_st


                let sets =
                    if lamp.InTag.IsNull() then
                        modeBit.Expr
                    else
                        modeBit.Expr <||>  lamp.ActionINFunc //강제 체크 비트

                let out = lamp.OutTag :?> Tag<bool>
                yield (sets, s._off.Expr) --| (out, fn)
        |]

    member s.B4_SWModeLamp() =
        let fn = getFuncName()
        [|
            let off = s._off.Expr
            for f in s.Flows do
                yield (f.auto_btn.Expr  , off) --| (f.auto_lamp  , fn)
                yield (f.manual_btn.Expr, off) --| (f.manual_lamp, fn)
                yield (f.drive_btn.Expr , off) --| (f.drive_lamp , fn)
                yield (f.pause_btn.Expr , off) --| (f.pause_lamp , fn)
                yield (f.emg_btn.Expr   , off) --| (f.emg_lamp   , fn)
                yield (f.test_btn.Expr  , off) --| (f.test_lamp  , fn)
                yield (f.home_btn.Expr  , off) --| (f.home_lamp  , fn)
                yield (f.clear_btn.Expr , off) --| (f.clear_lamp , fn)
                yield (f.ready_btn.Expr , off) --| (f.ready_lamp , fn)
        |]
   
