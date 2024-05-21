[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(sys:DsSystem, job:Job) =
        
        let rst = sys._off.Expr
        (d.GetInExpr(job.Name), rst) --| (d.InTag, getFuncName())

type DsSystem with

    member s.SetFlagForEmulation() =

        let set = s._on.Expr
        let rst = s._off.Expr

        (set, rst) --| (s._emulation, getFuncName())
