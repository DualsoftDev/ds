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

        let cpv (name) (jobTag:JobTag) =
            let pv:IStorage = createPlanVar stg name job.DataType.Value true job (int jobTag) job.System 
            pv 
            
        let jobValueTag = cpv job.Name JobTag.JobValueTag
   
        do 
            if job.DataType.Value <> DuBOOL && job.DeviceDefs.Count() > 1
            then 
                failWithLog $"Job {job.Name} {job.DataType} bool 타입이 아니면 하나의 Device만 할당 가능합니다."
        
        interface ITagManager with
            member _.Target = job
            member _.Storages = stg
        
        member _.JobValueTag   = jobValueTag 
        member _.JobBoolValueTag   = jobValueTag  :?> PlanVar<bool>    
        member _.Job   = job