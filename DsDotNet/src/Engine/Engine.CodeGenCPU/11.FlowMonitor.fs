[<AutoOpen>]
module Engine.CodeGenCPU.FlowMonitor

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS


type Flow with


    member f.F1_FlowError(): CommentedStatement =
        let set = f.Graph.Vertices.OfType<Real>().SelectMany(fun r->r.Errors).ToOrElseOff(f.System)
        let rst = f._off.Expr
        (set, rst) --| (f.stopError, getFuncName())

    member f.F2_FlowPause(): CommentedStatement =
        let set = f.sop.Expr <&&> !!f.stopError.Expr
        let rst = f._off.Expr
        (set, rst) --| (f.stopPause, getFuncName())


