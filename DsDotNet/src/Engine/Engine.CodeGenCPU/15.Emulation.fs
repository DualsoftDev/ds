[<AutoOpen>]
module Engine.CodeGenCPU.ConvertEmulation

open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type TaskDev with

    member d.SensorEmulation(sys:DsSystem) =

        let set = d.ApiItem.PE.Expr
        let rst = sys._off.Expr

        (set, rst) --| (d.InTag, getFuncName())


