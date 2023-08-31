namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem, activeSys:DsSystem)  =
        let stg = apiItem.System.TagManager.Storages
        let cpv (n:string, api:ApiItem, sys:DsSystem) = 
            createPlanVar stg $"{api.System.Name}_{apiItem.Name}_{n}" DuBOOL true  api (ApiItemTag.planSet|>int) sys:?> PlanVar<bool>
        let ps = cpv ("PS", apiItem, apiItem.System )
        let pr = cpv ("PR", apiItem, apiItem.System )
        let pe = cpv ("PE", apiItem, activeSys )
        let pp = cpv ("PP", apiItem, activeSys )

        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg

        member f.GetApiTag(at:ApiItemTag) =
            match at with
            | ApiItemTag.planSet   -> ps
            | ApiItemTag.planRst   -> pr
            | ApiItemTag.planEnd   -> pe
            | ApiItemTag.planPulse -> pp
            | _ -> failwithlog $"Error : GetApiTag {at} type not support!!"

