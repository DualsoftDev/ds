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
            //let n = Enum.GetName(typeof<ApiItemTag>, apiItemTag)
            //let name = $"{apiItem.ApiSystem.Name}_{apiItem.Name}_{n}"
            let name = getStorageName apiItem (int apiItemTag)
            let pv:IStorage = createPlanVar stg name DuBOOL false apiItem (int apiItemTag) apiItem.ApiSystem 
            pv :?> PlanVar<bool>
            
        let apiItemSet = cpv ApiItemTag.apiItemSet
        let apiItemSetPusle = cpv ApiItemTag.apiItemSetPusle
        let apiItemSetPusleRelay = cpv ApiItemTag.apiItemSetPusleRelay
        let apiItemSetPusleHold = cpv ApiItemTag.apiItemSetPusleHold
        let pe = cpv ApiItemTag.apiItemEnd
        let sensorLinking = cpv ApiItemTag.sensorLinking
        let sensorLinked = cpv ApiItemTag.sensorLinked
   
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.GetApiTag (vt:ApiItemTag) :IStorage =
            match vt with 
            | ApiItemTag.apiItemSet           -> apiItemSet           :> IStorage
            | ApiItemTag.apiItemSetPusle      -> apiItemSetPusle      :> IStorage
            | ApiItemTag.apiItemSetPusleRelay -> apiItemSetPusleRelay :> IStorage
            | ApiItemTag.apiItemSetPusleHold  -> apiItemSetPusleHold  :> IStorage
            | ApiItemTag.apiItemEnd           -> pe                   :> IStorage
            | ApiItemTag.sensorLinking        -> sensorLinking        :> IStorage
            | ApiItemTag.sensorLinked         -> sensorLinked         :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

        member _.ApiItem = apiItem
    
        member _.APISET  = apiItemSet
        member _.ApiItemSetPusle      = apiItemSetPusle
        member _.ApiItemSetPusleRelay = apiItemSetPusleRelay
        member _.ApiItemSetPusleHold  = apiItemSetPusleHold
        member _.APIEND = pe
        member _.SL1    = sensorLinking
        member _.SL2    = sensorLinked
        