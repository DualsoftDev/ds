namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Linq
open System
open Dual.Common.Core.FS

[<AutoOpen>]
module JobManagerModule =

     type JobManager(job:Job)  =
        let stg = job.System.TagManager.Storages

        let cpv (jobTag:JobTag) =
            let name = job.QualifiedName
            let pv:IStorage = createPlanVar stg name job.DataType true job (int jobTag) job.System 
            pv :?> PlanVar<bool>
            
        let devAndExpr = cpv JobTag.JobAndExprTag
        let devOrExpr = cpv JobTag.JobOrExprTag
   
        do 
            if job.DataType <> DuBOOL && job.DeviceDefs.Count() > 1
            then 
                failWithLog $"Job {job.Name} {job.DataType} bool 타입이 아니면 하나의 Device만 할당 가능합니다."
        
        interface ITagManager with
            member _.Target = job
            member _.Storages = stg
        
        member _.JobAndExprTag = devAndExpr
        member _.JobOrExprTag = devOrExpr
        member _.Job   = job