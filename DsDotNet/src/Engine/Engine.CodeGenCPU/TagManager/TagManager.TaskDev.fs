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
            let pv:IStorage = createPlanVar stg name DuFLOAT32 false (Some(td)) (int t) sys
            pv :?> PlanVar<single>


        let taskDevTagTags = [|
            TaskDevTag.actionCount
            TaskDevTag.actionMean
            TaskDevTag.actionVariance
        |]
        let taskDevDic = taskDevTagTags |> map (fun t -> t, cpv t) |> Tuple.toReadOnlyDictionary


        interface ITagManager with
            member _.Target = td
            member _.Storages = stg

        member _.GetTaskDevTag (vt:TaskDevTag) :IStorage =
            match vt with
            | TaskDevTag.actionIn  -> td.InTag :> IStorage
            | TaskDevTag.actionOut -> td.OutTag :> IStorage
            | TaskDevTag.actionCount    -> taskDevDic[TaskDevTag.actionCount] :> IStorage
            | TaskDevTag.actionMean     -> taskDevDic[TaskDevTag.actionMean] :> IStorage
            | TaskDevTag.actionVariance -> taskDevDic[TaskDevTag.actionVariance] :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"  //planStart, planEnd 지원 안함

        member _.TaskDev       = td


