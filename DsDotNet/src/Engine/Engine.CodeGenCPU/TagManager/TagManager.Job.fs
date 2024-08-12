namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

        
[<AutoOpen>]
module JobManagerModule =
     type JobManager(job:Job)  =
        let stg = job.System.TagManager.Storages

        /// Create Plan Var
        let cpv (jobTag:JobTag) =
            let name = getStorageName job (int jobTag)
            let pv:IStorage = createPlanVar stg name DuBOOL false job (int jobTag) job.System
            pv :?> PlanVar<bool>

        let jobTags = [|
            JobTag.inDetected
            JobTag.outDetected
        |]
        let jobTagsDic = jobTags |> map (fun t -> t, cpv t) |> Tuple.toReadOnlyDictionary


        interface ITagManager with
            member _.Target = job
            member _.Storages = stg


        member x.GetApiTag (vt:JobTag) =
            match jobTagsDic.TryGetValue(vt) with
            | true, planVar -> planVar
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"

        member _.Job = job
        member val InDetected = jobTagsDic[JobTag.inDetected]
        member val OutDetected = jobTagsDic[JobTag.outDetected]
