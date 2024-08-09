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
        [<Obsolete("test ahn IsAnalogSensor 추후 범위로 변경 꼭 필요")>]
        member j.ActionInExpr = 
            let inExprs =
                j.TaskDefs.Where(fun d-> d.ExistInput)
                    //.Where(fun d-> not(d.IsAnalogSensor))  // <- This line IsAnalogSensor 추후 범위로 변경 꼭 필요
                    .Select(fun d-> d.GetInExpr(j))


            if inExprs.any() then
                match j.JobParam.JobSensing with
                | SensingNormal -> inExprs.ToAnd() |>Some  
                | SensingNegative -> !@inExprs.ToAnd() |>Some  
            else 
                None

