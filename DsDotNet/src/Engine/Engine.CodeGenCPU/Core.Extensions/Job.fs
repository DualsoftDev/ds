namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuJob =
    let getSM(j:Job) = j.System.TagManager:?> SystemManager

    type Job with
        member j.ActionInExpr = 
            let inExprs = j.TaskDefs.Where(fun d-> d.ExistInput)
                                      .Select(fun d-> d.GetInExpr(j))
            if inExprs.any() then 
                inExprs.ToAndElseOff() |>Some  
            else None

