[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with


    member s.Y1_SystemActiveBtnForPassiveFlow(activeSys:DsSystem) =
        let fn = getFuncName()
        let rst = s._off.Expr
        [| 
           for flow in s.Flows do
                yield (activeSys._auto_btn.Expr  , rst) --| (flow.auto_btn,   fn)
                yield (activeSys._manual_btn.Expr, rst) --| (flow.manual_btn, fn)
                yield (activeSys._drive_btn.Expr , rst) --| (flow.drive_btn,  fn)
                yield (activeSys._pause_btn.Expr , rst) --| (flow.pause_btn,  fn)
                yield (activeSys._emg_btn.Expr   , rst) --| (flow.emg_btn,    fn)
                yield (activeSys._test_btn.Expr  , rst) --| (flow.test_btn,   fn)
                yield (activeSys._home_btn.Expr  , rst) --| (flow.home_btn,   fn)
                yield (activeSys._clear_btn.Expr , rst) --| (flow.clear_btn,  fn)
                yield (activeSys._ready_btn.Expr , rst) --| (flow.ready_btn,  fn)
        |]

    member s.Y2_SystemPause() =
        let sets =  s.Flows.Select(fun f -> f.p_st).ToOrElseOff()
        (sets, s._off.Expr) --| (s._pause, getFuncName())


    member s.Y3_SystemState() =
        let fn = getFuncName()
        let off = s._off.Expr
        [
            (s.Flows.Select(fun f -> f.iop)     .ToAndElseOff(), off) --| (s._idleMonitor  , fn)
            (s.Flows.Select(fun f -> f.aop)     .ToAndElseOff(), off) --| (s._autoMonitor  , fn)
            (s.Flows.Select(fun f -> f.mop)     .ToAndElseOff(), off) --| (s._manualMonitor, fn)
            (s.Flows.Select(fun f -> f.d_st)    .ToAndElseOff(), off) --| (s._driveMonitor , fn)
            (s.Flows.Select(fun f -> f.e_st)    .ToOrElseOff(),  off) --| (s._errorMonitor , fn)
            (s.Flows.Select(fun f -> f.t_st)    .ToAndElseOff(), off) --| (s._testMonitor  , fn)
            (s.Flows.Select(fun f -> f.r_st)    .ToAndElseOff(), off) --| (s._readyMonitor , fn)
            (s.Flows.Select(fun f -> f.o_st)    .ToAndElseOff(), off) --| (s._originMonitor, fn)
            (s.Flows.Select(fun f -> f.g_st)    .ToOrElseOff() , off) --| (s._goingMonitor , fn)
            (s.Flows.Select(fun f -> f.emg_st)  .ToOrElseOff() , off) --| (s._emergencyMonitor , fn)
        ]



    member s.Y4_SystemConditionError() =
        let fn = getFuncName()
        [
            for condi in s.HWConditions do
                yield (!@condi.ActionINFunc, s._off.Expr) --| (condi.ErrorCondition, fn)
        ]

    member s.Y5_SystemEmgAlramError() =
        let fn = getFuncName()
        [
            for emg in s.HWButtons.Where(fun f -> f.ButtonType = DuEmergencyBTN) do
                yield (emg.ActionINFunc, s._off.Expr) --| (emg.ErrorEmergency, fn)
        ]

    member s.Y6_SystemClearBtnForFlow() =
        let fn = getFuncName()
        let rst = s._off.Expr
        [|
            for btn in s.ClearHWButtons do
                let set = btn.ActionINFunc
                let tm = s.GetTempTimer(btn)
                yield set --@ (tm, 2000u, fn)
                yield (set , rst) --| (s._clear_btn, fn)  //flow.clear_btn 은 drive에 처리
                yield (set , rst) --| (s._ready_btn, fn)  //flow.ready_btn 은 drive에 처리
                //누름 2초 유지시 _home_btn 동시 동작
                if btn.IsGlobalSystemHw then
                    for f in s.Flows do
                    yield (tm.DN.Expr , rst) --| (f.home_btn, fn)
                else
                    for f in btn.SettingFlows do
                        yield (tm.DN.Expr , rst) --| (f.home_btn, fn)

            for flow in s.Flows do
                let set = flow.drive_btn.Expr
                yield (set, rst) --| (flow.clear_btn, fn)
                yield (set, rst) --| (flow.ready_btn, fn)
        |]

    //// 외부신호 초기값 변화를 연산하기 위해 강제로 수식 추가
    //member s.Y6_SystemDeviceTrigger() =
    //    let sets =
    //        s.GetCallVertices().Where(fun c ->c.Parent.GetCore() :? Flow)
    //         .Select(getVM).Select(fun f->f.ET).ToOrElseOff()
    //    //_originMonitor 변화시 한번 체크하여 강제 연산 유도
    //    (sets <&&> s._originMonitor.Expr, s._off.Expr) --| (s._deviceTrigger, getFuncName())

