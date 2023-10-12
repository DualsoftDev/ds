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
        let txerr = cpv ("TXErr", apiItem, apiItem.System  )
        let rxerr = cpv ("RXErr", apiItem, apiItem.System  )
        let timerTimeOutBit = timer  stg "TOUT" apiItem.System  

        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        ///Timer time out
        member _.TOUT   = timerTimeOutBit
        member _.PS   = ps
        member _.PR   = pr
        member _.PE   = pe
        member _.TXErr   = txerr
        member _.RXErr   = rxerr
