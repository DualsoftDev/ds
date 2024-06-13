[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with

    //member s.E1_PLCNotFunc(skipRung:bool) =
    //    let rsts = s._off.Expr
    //     (* device not func 로직 처리*)
    //    [
    //        let reverseInputs = s.Jobs.SelectMany(fun j->j.DeviceDefs)
    //                                  .Where(fun d->d.GetInParam(jobName).IsSensorNot())

    //        let devs = s.Jobs.SelectMany(fun j -> j.DeviceDefs)
    //        let orgInTag (revDev:TaskDev)=
    //            let orgList = devs.Except(reverseInputs)
    //                              .Where(fun f->f.InAddress = revDev.InAddress)
    //            if orgList.any() 
    //                then (orgList.First().InTag :?> Tag<bool>).Expr 
    //                else (s.GetTempTag(revDev)  :?> Tag<bool>).Expr 
                    

    //        for revDev in reverseInputs do
    //            if not(skipRung)
    //            then yield (orgInTag(revDev), rsts) --| (revDev.InTag, getFuncName()) //그대로 복사

    //            revDev.InTag.Address <- TextAddrEmpty
    //    ]


    member s.E2_PLCOnly() =
        let rst = s._off.Expr
            (*clear btn  => _ready_btn  동시 동작   /  누름 유지시 _home_btn 동작*)
        [ 
            
            let driveHWButtons = s.DriveHWButtons.Select(fun b->b.ActionINFunc).ToOrElseOff()
            yield (driveHWButtons, rst) --| (s._drive_btn, getFuncName())

            let pauseHWButtons = s.PauseHWButtons.Select(fun b->b.ActionINFunc).ToOrElseOff()
            yield (pauseHWButtons, rst) --| (s._pause_btn, getFuncName())
           
            for btn in s.ClearHWButtons do
                let set = btn.ActionINFunc
                let tm = s.GetTempTimer(btn)
                yield set --@ (tm, 2000u, getFuncName())

                for flow in btn.SettingFlows do
                    //누름 2초 유지시 _home_btn 동시 동작
                    for real in flow.GetVerticesOfFlow().OfType<Real>() do
                        yield (tm.DN.Expr , rst) --| (real.VR.OA, getFuncName())

            for flow in s.Flows do
                let set = flow.drive_btn.Expr
                yield (set, rst) --| (flow.clear_btn, getFuncName())
                yield (set, rst) --| (flow.ready_btn, getFuncName())

        ]
