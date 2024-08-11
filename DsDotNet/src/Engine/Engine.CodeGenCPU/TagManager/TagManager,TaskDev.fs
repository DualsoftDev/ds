namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module TaskDevManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type TaskDevManager (td:TaskDev, sys:DsSystem)  =
        let stg = sys.TagManager.Storages

        /// Create Plan Var
        let cpv (t:TaskDevTag) =
            let name = getStorageName td (int t)
            let pv:IStorage = createPlanVar stg name DuBOOL false td (int t) sys
            pv :?> PlanVar<bool>

        /// Plan StartS
        let pss = td.ApiParams |> map(fun p -> p, cpv TaskDevTag.planStart)  |> Tuple.toReadOnlyDictionary
        /// Plan EndS
        let pes = td.ApiParams |> map(fun p -> p, cpv TaskDevTag.planEnd)    |> Tuple.toReadOnlyDictionary
        /// Plan OutputS
        let pos = td.ApiParams |> map(fun p -> p, cpv TaskDevTag.planOutput) |> Tuple.toReadOnlyDictionary

        interface ITagManager with
            member _.Target = td
            member _.Storages = stg

        member _.GetTaskDevTag (vt:TaskDevTag) :IStorage =
            match vt with
            | TaskDevTag.actionIn  -> td.InTag :> IStorage
            | TaskDevTag.actionOut -> td.OutTag :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"  //planStart, planEnd 지원 안함

        member _.TaskDev   = td

        member x.PlanStart(api:ApiParam)  = pss[api]
        member x.PlanEnd(api:ApiParam)    = pes[api]
        member x.PlanOutput(api:ApiParam) = pos[api]

        member x.PlanStart(job:Job)  = x.PlanStart(x.TaskDev.GetApiParam(job))
        member x.PlanEnd(job:Job)    = x.PlanEnd(x.TaskDev.GetApiParam(job))
        member x.PlanOutput(job:Job) = x.PlanOutput(x.TaskDev.GetApiParam(job))

