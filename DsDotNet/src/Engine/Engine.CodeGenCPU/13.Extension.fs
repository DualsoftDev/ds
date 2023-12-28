[<AutoOpen>]
module Engine.CodeGenCPU.ConvertExtenstion

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS



type DsSystem with

    member s.E1_AlwaysOnOff(): CommentedStatement =
        (!!s._off.Expr, s._off.Expr) --| (s._on, getFuncName())
