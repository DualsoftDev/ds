namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem)  =
        let sys = apiItem.System
        let stg = sys.TagManager.Storages
        let cpv (n:string) = createPlanVar stg $"{sys.Name}_{apiItem.Name}_{n}" DuBOOL true apiItem  (ApiItemTag.planSet|>int) :?> PlanVar<bool>
        let ps = cpv "PS"
        let pr = cpv "PR"

        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg

        member f.GetApiTag(at:ApiItemTag) =
            match at with
            | ApiItemTag.planSet -> ps
            | ApiItemTag.planRst -> pr
            | _ -> failwithlog $"Error : GetApiTag {at} type not support!!"

