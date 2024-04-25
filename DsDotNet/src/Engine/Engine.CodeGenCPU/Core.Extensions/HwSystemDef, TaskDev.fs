namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuTaskDev =

    type TaskDev with
        member td.AO = td.OutTag :?> Tag<bool>

    type HwSystemDef with
        member s.ActionINFunc = 
            match  s.InTag with
            | :? Tag<bool> as inTag -> 
                if hasNot (s.OperatorFunction)
                then !!inTag.Expr else inTag.Expr
            | _ -> failwithf $"{s.Name} input address is empty."
