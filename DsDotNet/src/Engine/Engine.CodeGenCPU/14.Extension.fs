[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with

    member s.E1_PLCNotFunc() =
        let rsts = s._off.Expr
         (* device not func 로직 처리*)
        [
            let reverseInputs = s.Jobs.Where(fun j -> hasNot j.Func)
                                      .SelectMany(fun j->j.DeviceDefs)

            let devs = s.Jobs.SelectMany(fun j -> j.DeviceDefs)
            let orgInTag (revDev:TaskDev)=
                let orgList = devs.Except(reverseInputs)
                                  .Where(fun f->f.InAddress = revDev.InAddress)
                if orgList.any() 
                    then (orgList.First().InTag :?> Tag<bool>).Expr 
                    else (s.GetTempTag(revDev)  :?> Tag<bool>).Expr 
                    
            for revDev in reverseInputs do
                yield (orgInTag(revDev), rsts) --| (revDev.InTag, getFuncName()) //그대로 복사
                revDev.InTag.Address <- TextAddrEmpty
                
        ]


    member s.E2_LightPLCOnly() =
        let rst = s._off.Expr
            (*drive btn => _auto_btn 동시 동작
            pause btn => manual_btn 동시 동작
            clear btn  => _ready_btn  동시 동작 and  누름 유지시 _home_btn 동작*)
        [
            for btn in s.DriveHWButtons do
                let set = btn.ActionINFunc
                for flow in btn.SettingFlows do
                    yield (set, flow.manual_btn.Expr) ==| (flow.auto_btn, getFuncName())
                    yield (set, rst) --| (flow.drive_btn, getFuncName())

            for btn in s.PauseHWButtons do
                let set = btn.ActionINFunc
                for flow in btn.SettingFlows do
                    yield (set <||> flow.home_btn.Expr, flow.clear_btn.Expr <&&> !!flow.home_btn.Expr) ==| (flow.manual_btn, getFuncName())
                    yield (set, rst) --| (flow.pause_btn, getFuncName())

            for btn in s.ClearHWButtons do
                let set = btn.ActionINFunc
                let tm = s.GetTempTimer(btn)
                for flow in btn.SettingFlows do
                    //누름 2초 유지시 _home_btn 동시 동작
                    yield set --@ (tm, 2000us, getFuncName())
                    yield (tm.DN.Expr, rst) --| (flow.home_btn, getFuncName())
                    yield (set, rst) --| (flow.ready_btn, getFuncName())
                    yield (set, rst) --| (flow.clear_btn, getFuncName())
        ]
