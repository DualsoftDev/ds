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

        let cpv (t:TaskDevTag) =
            let name = getStorageName td (int t)
            let pv:IStorage = createPlanVar stg name DuBOOL false td (int t) sys 
            pv :?> PlanVar<bool>
            
        let pss = Dictionary<ApiItem, PlanVar<bool>>()
        let pes = Dictionary<ApiItem, PlanVar<bool>>()


        do 
            for api in td.ApiItems.Distinct() do
                let ps = cpv TaskDevTag.planStart
                let pe = cpv TaskDevTag.planEnd
                pss.Add(api, ps)
                pes.Add(api, pe)
                
        interface ITagManager with
            member _.Target = td
            member _.Storages = stg

        member _.GetTaskDevTag (vt:TaskDevTag) :IStorage =
            match vt with 
            | TaskDevTag.actionIn           -> td.InTag :> IStorage
            | TaskDevTag.actionOut          -> td.OutTag :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"  //planStart, planEnd 지원 안함
         
        member _.TaskDev   = td
      
        member x.PS(api:ApiItem)   = pss[api]
        member x.PE(api:ApiItem)   = pes[api]
        member x.PS(job:Job)   = x.PS(x.TaskDev.GetApiItem(job))
        member x.PE(job:Job)   = x.PE(x.TaskDev.GetApiItem(job))

        