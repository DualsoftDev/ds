namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem)  =
        let stg = apiItem.ApiSystem.TagManager.Storages

        /// Create Plan Var
        let cpv (apiItemTag:ApiItemTag) =
            //let n = Enum.GetName(typeof<ApiItemTag>, apiItemTag)
            //let name = $"{apiItem.ApiSystem.Name}_{apiItem.Name}_{n}"
            let name = getStorageName apiItem (int apiItemTag)
            let pv:IStorage = createPlanVar stg name DuBOOL false apiItem (int apiItemTag) apiItem.ApiSystem 
            pv :?> PlanVar<bool>
            
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member x.GetApiTag (vt:ApiItemTag) :IStorage =
            match vt with 
            | ApiItemTag.apiItemSet           -> x.ApiItemSet           :> IStorage
            | ApiItemTag.apiItemSetPusle      -> x.ApiItemSetPusle      :> IStorage
            | ApiItemTag.apiItemSetPusleRelay -> x.ApiItemSetPusleRelay :> IStorage
            | ApiItemTag.apiItemSetPusleHold  -> x.ApiItemSetPusleHold  :> IStorage
            | ApiItemTag.apiItemEnd           -> x.ApiItemEnd           :> IStorage
            | ApiItemTag.sensorLinking        -> x.SL1                  :> IStorage
            | ApiItemTag.sensorLinked         -> x.SL2                  :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

        member _.ApiItem = apiItem
    
        member val ApiItemSet  = cpv ApiItemTag.apiItemSet
        member val ApiItemSetPusle      = cpv ApiItemTag.apiItemSetPusle
        member val ApiItemSetPusleRelay = cpv ApiItemTag.apiItemSetPusleRelay
        member val ApiItemSetPusleHold  = cpv ApiItemTag.apiItemSetPusleHold
        member val ApiItemEnd = cpv ApiItemTag.apiItemEnd
        member val SL1    = cpv ApiItemTag.sensorLinking
        member val SL2    = cpv ApiItemTag.sensorLinked
        