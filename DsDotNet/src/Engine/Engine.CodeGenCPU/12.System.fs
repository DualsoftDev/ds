[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
    member s.Y1_SystemBitSetFlow(): CommentedStatement list = [
            for flow in s.Flows do
                yield (s._auto.Expr  , s._off.Expr) --| (flow.auto,   getFuncName())
                yield (s._manual.Expr, s._off.Expr) --| (flow.manual, "")
                yield (s._drive.Expr , s._off.Expr) --| (flow.drive,  "")
                yield (s._stop.Expr  , s._off.Expr) --| (flow.stop,   "")
                yield (s._emg.Expr   , s._off.Expr) --| (flow.emg,    "")
                yield (s._test.Expr  , s._off.Expr) --| (flow.test,   "")
                yield (s._clear.Expr , s._off.Expr) --| (flow.clear,  "")
                yield (s._home.Expr  , s._off.Expr) --| (flow.home,   "")
                yield (s._ready.Expr , s._off.Expr) --| (flow.ready,  "")
        ]
        
    member s.Y2_SystemError(): CommentedStatement  =
        let sets =  s.Flows.Select(fun f->f.error).ToOrElseOff(s)
        (sets, s._off.Expr) --| (s._err, getFuncName())

    member s.Y3_SystemPause(): CommentedStatement  =
        let sets =  s.Flows.Select(fun f->f.pause).ToOrElseOff(s)
        (sets, s._off.Expr) --| (s._pause, getFuncName())
    
