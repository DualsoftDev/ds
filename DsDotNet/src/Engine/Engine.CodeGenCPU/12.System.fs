[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.Y1_SystemBitSetFlow(): CommentedStatement list = [
            for flow in s.Flows do
                yield (s._auto_btn.Expr  , s._off.Expr) --| (flow.auto_btn,   getFuncName())
                yield (s._manual_btn.Expr, s._off.Expr) --| (flow.manual_btn, "")
                yield (s._drive_btn.Expr , s._off.Expr) --| (flow.drive_btn,  "")
                yield (s._stop_btn.Expr  , s._off.Expr) --| (flow.stop_btn,   "")
                yield (s._emg_btn.Expr   , s._off.Expr) --| (flow.emg_btn,    "")
                yield (s._test_btn.Expr  , s._off.Expr) --| (flow.test_btn,   "")
                yield (s._clear_btn.Expr , s._off.Expr) --| (flow.clear_btn,  "")
                yield (s._home_btn.Expr  , s._off.Expr) --| (flow.home_btn,   "")
                yield (s._ready_btn.Expr , s._off.Expr) --| (flow.ready_btn,  "")
        ]
        
    member s.Y2_SystemError(): CommentedStatement  =
        let sets =  s.Flows.Select(fun f->f.stopError).ToOrElseOff(s)
        (sets, s._off.Expr) --| (s._stopErr, getFuncName())


    member s.Y3_SystemPause(): CommentedStatement  =
        let sets =  s.Flows.Select(fun f->f.stopPause).ToOrElseOff(s)
        (sets, s._off.Expr) --| (s._stopPause, getFuncName())


    member s.Y4_SystemDrive(): CommentedStatement  =
        let sets =  s.Flows.Select(fun f->f.dop).ToAndElseOff(s)
        (sets, s._off.Expr) --| (s._drive, getFuncName())

    
