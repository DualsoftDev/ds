namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module TaskDevManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type TaskDevManager (td:TaskDev, sys:DsSystem)  =
        let stg = sys.TagManager.Storages

        let cpv (t:TaskDevTag) =
            let name = getStorageName td (int t)
            let pv:IStorage = createPlanVar stg name DuBOOL false td (int t) sys 
            pv :?> PlanVar<bool>
            
        let ps = cpv TaskDevTag.planStart
        let pe = cpv TaskDevTag.planEnd
   
        interface ITagManager with
            member _.Target = td
            member _.Storages = stg


        member _.GetTaskDevTag (vt:TaskDevTag) :IStorage =
            match vt with 
            | TaskDevTag.planStart          -> ps  :> IStorage
            | TaskDevTag.planEnd            -> pe  :> IStorage
            | TaskDevTag.actionIn           -> td.InTag :> IStorage
            | TaskDevTag.actionOut          -> td.OutTag :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         
        member _.TaskDev   = td
      
        member _.PS   = ps
        member _.PE   = pe

        