[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with


    member s.E2_PLCOnly() =
        let fn = getFuncName()
        let rst = s._off.Expr
            (*clear btn  => _ready_btn  동시 동작   /  누름 유지시 _home_btn 동작*)
        [ 
            
            for btn in s.ClearHWButtons do
                let set = btn.ActionINFunc
                let tm = s.GetTempTimer(btn)
                yield set --@ (tm, 2000u, fn)
                yield (set , rst) --| (s._clear_btn, fn)  //flow.clear_btn 은 drive에 처리
                yield (set , rst) --| (s._ready_btn, fn)  //flow.ready_btn 은 drive에 처리


                    //누름 2초 유지시 _home_btn 동시 동작
                for f in btn.SettingFlows do
                    yield (tm.DN.Expr , rst) --| (f.home_btn, fn)

            for flow in s.Flows do
                let set = flow.drive_btn.Expr
                yield (set, rst) --| (flow.clear_btn, fn)
                yield (set, rst) --| (flow.ready_btn, fn)

        ]
