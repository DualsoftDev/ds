namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem)  =
        let stg = apiItem.ApiSystem.TagManager.Storages

        let cpv (apiItemTag:ApiItemTag) =
            let n = Enum.GetName(typeof<ApiItemTag>, apiItemTag)
            let name = $"{apiItem.ApiSystem.Name}_{apiItem.Name}_{n}"
            let pv:IStorage = createPlanVar stg name DuBOOL false apiItem (int apiItemTag) apiItem.ApiSystem 
            pv :?> PlanVar<bool>
            
        let ps = cpv ApiItemTag.planSet
        let pe = cpv ApiItemTag.planEnd
        let sensorLinking = cpv ApiItemTag.sensorLinking
        let sensorLinked = cpv ApiItemTag.sensorLinked
        
   
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.GetApiTag (vt:ApiItemTag) :IStorage =
            match vt with 
            | ApiItemTag.planSet            -> ps  :> IStorage
            | ApiItemTag.planEnd            -> pe  :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

        member _.ApiItem   = apiItem

    
        member _.SL1   = sensorLinking
        member _.SL2   = sensorLinked
        member _.PS   = ps
        member _.PE   = pe

        