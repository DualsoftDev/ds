namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem)  =
        let stg = apiItem.System.TagManager.Storages

        let cpv (apiItemTag:ApiItemTag) =
            let n = Enum.GetName(typeof<ApiItemTag>, apiItemTag)
            let name = $"{apiItem.System.Name}_{apiItem.Name}_{n}"
            let pv:IStorage = createPlanVar stg name DuBOOL true apiItem (int apiItemTag) apiItem.System 
            pv :?> PlanVar<bool>
            
        let ps = cpv ApiItemTag.planSet
        let pe = cpv ApiItemTag.planEnd
        let actionSend = cpv ApiItemTag.actionSend
        let actionLink = cpv ApiItemTag.actionLink
        
   
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.GetApiTag (vt:ApiItemTag) :IStorage =
            match vt with 
            | ApiItemTag.planSet            -> ps  :> IStorage
            | ApiItemTag.planEnd            -> pe  :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

        member _.ApiItem   = apiItem

    
        member _.AS   = actionSend
        member _.AL   = actionLink
        member _.PS   = ps
        member _.PE   = pe

        