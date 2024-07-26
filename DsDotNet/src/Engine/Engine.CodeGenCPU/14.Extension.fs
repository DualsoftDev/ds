[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with


    member s.E2_PLCOnly() =
        let rst = s._off.Expr
            (*clear btn  => _ready_btn  동시 동작   /  누름 유지시 _home_btn 동작*)
        [ 
            
            for btn in s.ClearHWButtons do
                let set = btn.ActionINFunc
                let tm = s.GetTempTimer(btn)
                yield set --@ (tm, 2000u, getFuncName())
                yield (set , rst) --| (s._clear_btn, getFuncName())  //flow.clear_btn 은 drive에 처리
                yield (set , rst) --| (s._ready_btn, getFuncName())  //flow.ready_btn 은 drive에 처리


                    //누름 2초 유지시 _home_btn 동시 동작
                for f in btn.SettingFlows do
                    yield (tm.DN.Expr , rst) --| (f.home_btn, getFuncName())

            for flow in s.Flows do
                let set = flow.drive_btn.Expr
                yield (set, rst) --| (flow.clear_btn, getFuncName())
                yield (set, rst) --| (flow.ready_btn, getFuncName())

        ]
