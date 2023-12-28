[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS



type DsSystem with

    member s.E1_AlwaysOnOff(): CommentedStatement =
        (!!s._off.Expr, s._off.Expr) --| (s._on, getFuncName())


    member s.E2_LightPLCOnly(): CommentedStatement list=
        [
            for btn in s.DriveHWButtons do
                if btn.InTag.IsNonNull() 
                then
                    let sets = btn.ActionINFunc
                    let rsts = s._off.Expr

                    yield (sets, rsts) --| (s._clear_btn, getFuncName())
                    yield (sets, rsts) --| (s._auto_btn, getFuncName())
                    yield (sets, rsts) --| (s._ready_btn, getFuncName())
        ]
