namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System
open System.IO
open System.Threading.Tasks
open System


[<AutoOpen>]
module ConvertCpuJob =
    let getSM(j:Job) = j.System.TagManager:?> SystemManager
    type Job with
        member j.ActionInExpr = 
            let inExprs =
                j.TaskDefs.Where(fun d-> d.ExistInput)
                          .Select(fun d-> d.GetInExpr(j))

            if inExprs.any() then
                match j.JobParam.JobSensing with
                | SensingNormal -> inExprs.ToAnd() |>Some  
                | SensingNegative -> !@inExprs.ToAnd() |>Some  
            else 
                None

        member j.ActionOutExpr = 
            let outExprs =
                j.TaskDefs.Where(fun d-> d.ExistOutput)
                          .Select(fun d-> d.GetOutExpr(j))

            if outExprs.any() 
            then outExprs.ToAnd()|>Some  
            else None

