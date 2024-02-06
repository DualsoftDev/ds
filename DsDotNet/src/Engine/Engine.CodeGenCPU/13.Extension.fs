[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with

    member s.E1_PLCNotFunc(): CommentedStatement list=
        let rsts = s._off.Expr
         (* device not func 로직 처리*)
        [
       
            let devs = s.Jobs.SelectMany(fun j -> j.DeviceDefs)
            let reverseInputs = devs.Where(fun w-> hasNot w.Funcs)
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


    member s.E2_LightPLCOnly(): CommentedStatement list=
        let rsts = s._off.Expr
            (*drive btn => _clear_btn,_auto_btn, _ready_btn 동시 동작*)
        [
            for btn in s.DriveHWButtons do
                if btn.InTag.IsNonNull() 
                then
                    let sets = btn.ActionINFunc

                    yield (sets, rsts) --| (s._clear_btn, getFuncName())
                    yield (sets, rsts) --| (s._auto_btn, getFuncName())
                    yield (sets, rsts) --| (s._ready_btn, getFuncName())

            for btn in s.StopHWButtons do
                if btn.InTag.IsNonNull() 
                then
                    let sets = btn.ActionINFunc
                    yield (sets, rsts) --| (s._manual_btn, getFuncName())
       ]
