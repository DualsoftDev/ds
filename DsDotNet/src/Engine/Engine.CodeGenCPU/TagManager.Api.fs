namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System
open Engine.Common.FS

[<AutoOpen>]
module ApiTagManagerModule =



    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (a:ApiItem)  =
        let sys = a.System
        let stg =  sys.TagManager.Storages

        let ps    = createPlanVar stg $"{a.Name}_PS" DuBOOL true a  (ApiItemTag.planSet|>int) :?> PlanVar<bool>
        let pr    = createPlanVar stg $"{a.Name}_PR" DuBOOL true a  (ApiItemTag.planRst|>int) :?> PlanVar<bool>
        let pe    = createPlanVar stg $"{a.Name}_PE" DuBOOL true a  (ApiItemTag.planEnd|>int) :?> PlanVar<bool>

        interface ITagManager with
            member x.Target = a
            member x.Storages = stg

        member f.GetApiTag(at:ApiItemTag)     =
            let t =
                match at with
                |ApiItemTag.planSet        -> ps
                |ApiItemTag.planRst        -> pr
                |ApiItemTag.planEnd        -> pe
                |_ -> failwithlog $"Error : GetApiTag {at} type not support!!"

            t

