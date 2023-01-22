[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type DsSystem with
    member s.Y1_SystemBitSetFlow(): CommentedStatement list = [
            for flow in s.Flows do
                yield (s._auto.Expr  , s._off.Expr) --| (flow.auto,   "Y1" )
                yield (s._manual.Expr, s._off.Expr) --| (flow.manual, "Y1" )
                yield (s._drive.Expr , s._off.Expr) --| (flow.drive,  "Y1" )
                yield (s._stop.Expr  , s._off.Expr) --| (flow.stop,   "Y1" )
                yield (s._emg.Expr   , s._off.Expr) --| (flow.emg,    "Y1" )
                yield (s._test.Expr  , s._off.Expr) --| (flow.test,   "Y1" )
                yield (s._clear.Expr , s._off.Expr) --| (flow.clear,  "Y1" )
                yield (s._home.Expr  , s._off.Expr) --| (flow.home,   "Y1" )
                yield (s._ready.Expr , s._off.Expr) --| (flow.ready,  "Y1" )
        ]

    member s.Y2_SystemConditionReady(): CommentedStatement list = [
        for f in s.Flows do
            let readys = getConditionInputs(f, s.ReadyConditions)
            yield (readys.ToAndElseOn(s), s._off.Expr) --| (f.scr, "Y2" )
        ]

    member s.Y3_SystemConditionDrive(): CommentedStatement list = [
        for f in s.Flows do
            let drives = getConditionInputs(f, s.DriveConditions)
            yield (drives.ToAndElseOn(s), s._off.Expr) --| (f.scd, "Y3" )
        ]
