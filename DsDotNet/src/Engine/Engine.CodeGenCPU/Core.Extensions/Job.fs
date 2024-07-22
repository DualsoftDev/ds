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
                                    .Select(fun d->
                                                if d.IsRootOnlyDevice
                                                then d.GetPE(j).Expr
                                                else d.GetInExpr(j)
                                            )
            if inExprs.any() 
            then
                match j.JobParam.JobSensing with
                | SensingNormal -> inExprs.ToAnd() |>Some  
                | SensingNegative -> !@inExprs.ToAnd() |>Some  
            else 
                None

