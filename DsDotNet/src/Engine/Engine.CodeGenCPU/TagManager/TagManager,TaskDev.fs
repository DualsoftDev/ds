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
            
        let pss = Dictionary<ApiParam, PlanVar<bool>>()
        let pes = Dictionary<ApiParam, PlanVar<bool>>()
        let pos = Dictionary<ApiParam, PlanVar<bool>>()

        
        do 
            for apiParam in td.ApiParams do
                let ps = cpv TaskDevTag.planStart
                let pe = cpv TaskDevTag.planEnd
                let po = cpv TaskDevTag.planOutput
                pss.Add(apiParam, ps)
                pes.Add(apiParam, pe)
                pos.Add(apiParam, po)
                
        interface ITagManager with
            member _.Target = td
            member _.Storages = stg

        member _.GetTaskDevTag (vt:TaskDevTag) :IStorage =
            match vt with 
            | TaskDevTag.actionIn           -> td.InTag :> IStorage
            | TaskDevTag.actionOut          -> td.OutTag :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"  //planStart, planEnd 지원 안함
         
        member _.TaskDev   = td
      
        member x.PS(api:ApiParam)   = pss[api] 
        member x.PE(api:ApiParam)   = pes[api] 
        member x.PO(api:ApiParam)   = pos[api] 

        member x.PS(job:Job)   = x.PS(x.TaskDev.GetApiPara(job))
        member x.PE(job:Job)   = x.PE(x.TaskDev.GetApiPara(job)) 
        member x.PO(job:Job)   = x.PO(x.TaskDev.GetApiPara(job)) 

        