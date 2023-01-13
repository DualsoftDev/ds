[<AutoOpen>]
module Engine.CodeGenCPU.ConvertSystem

open System.Linq
open Engine.Core
open Engine.CodeGenCPU

type DsSystem with
    member s.S1_SystemBitSetFlow(): CommentedStatement list=
        [
            for flow in s.Flows do
                yield (s._auto.Expr  , s._off.Expr) --| (flow.auto,   "S1" )
                yield (s._manual.Expr, s._off.Expr) --| (flow.manual, "S1" )
                yield (s._emg.Expr   , s._off.Expr) --| (flow.emg,    "S1" )
                yield (s._drive.Expr , s._off.Expr) --| (flow.drive,  "S1" )
                yield (s._stop.Expr  , s._off.Expr) --| (flow.stop,   "S1" )
                yield (s._clear.Expr , s._off.Expr) --| (flow.clear,  "S1" )
                yield (s._test.Expr  , s._off.Expr) --| (flow.test,   "S1" )
        ]