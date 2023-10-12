namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem, activeSys:DsSystem)  =
        let stg = apiItem.System.TagManager.Storages
        let cpv (n:string, api:ApiItem, sys:DsSystem, apiItemTag:ApiItemTag) = 
            createPlanVar stg $"{api.System.Name}_{apiItem.Name}_{n}" DuBOOL true  api (apiItemTag|>int) sys:?> PlanVar<bool>
        let ps = cpv ("PS", apiItem, apiItem.System, ApiItemTag.planSet)
        let pr = cpv ("PR", apiItem, apiItem.System, ApiItemTag.planRst )
        let pe = cpv ("PE", apiItem, activeSys, ApiItemTag.planEnd )
        let txerrovertime = cpv ("TXErr", apiItem, apiItem.System, ApiItemTag.txErrTimeOver  )
        let rxerrShort = cpv ("RXErrShort", apiItem, apiItem.System, ApiItemTag.rxErrShort  )
        let rxerrOpen  = cpv ("RXErrOpen", apiItem, apiItem.System, ApiItemTag.rxErrOpen  )
        let timerTimeOutBit = timer  stg "TOUT" apiItem.System   

        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        ///Timer time out
        member _.TOUT   = timerTimeOutBit
        member _.PS   = ps
        member _.PR   = pr
        member _.PE   = pe
        member _.TXErrOverTime   = txerrovertime
        member _.RXErrShort  = rxerrShort
        member _.RXErrOpen   = rxerrOpen
