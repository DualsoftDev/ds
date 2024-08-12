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

        let apiItemTags = [|
            ApiItemTag.apiItemSet
            ApiItemTag.apiItemSetPusle
            ApiItemTag.apiItemSetPusleRelay
            ApiItemTag.apiItemSetPusleHold
            ApiItemTag.apiItemEnd
            ApiItemTag.sensorLinking
            ApiItemTag.sensorLinked
        |]
        let apiItemDic = apiItemTags |> map (fun t -> t, cpv t) |> Tuple.toReadOnlyDictionary


        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member x.GetApiTag (vt:ApiItemTag) =
            match apiItemDic.TryGetValue(vt) with
            | true, planVar -> planVar
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"


        member _.ApiItem = apiItem

        member val ApiItemSet           = apiItemDic[ApiItemTag.apiItemSet]
        member val ApiItemSetPusle      = apiItemDic[ApiItemTag.apiItemSetPusle]
        member val ApiItemSetPusleRelay = apiItemDic[ApiItemTag.apiItemSetPusleRelay]
        member val ApiItemSetPusleHold  = apiItemDic[ApiItemTag.apiItemSetPusleHold]
        member val ApiItemEnd           = apiItemDic[ApiItemTag.apiItemEnd]
        member val SL1                  = apiItemDic[ApiItemTag.sensorLinking]
        member val SL2                  = apiItemDic[ApiItemTag.sensorLinked]
