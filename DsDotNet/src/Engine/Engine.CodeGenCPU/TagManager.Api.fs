namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

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
        let txerrtrend = cpv ("TXErrTrend", apiItem, apiItem.System, ApiItemTag.txErrTrend  )
        let txerrovertime = cpv ("TXErrOverTime", apiItem, apiItem.System, ApiItemTag.txErrTimeOver  )
        let rxerrShort = cpv ("RXErrShort", apiItem, apiItem.System, ApiItemTag.rxErrShort  )
        let rxErrShortOn  = cpv ("RxShortOn", apiItem, apiItem.System, ApiItemTag.rxErrShortOn  )
        let rxErrShortRising  = cpv ("RxShortRising", apiItem, apiItem.System, ApiItemTag.rxErrShortRising  )
        let rxErrShortTemp = cpv ("RxShortTemp", apiItem, apiItem.System, ApiItemTag.rxErrShortTemp  )
        let rxerrOpen  = cpv ("RXErrOpen", apiItem, apiItem.System, ApiItemTag.rxErrOpen  )
        let rxErrOpenOff  = cpv ("RxOpenOff", apiItem, apiItem.System, ApiItemTag.rxErrOpenOff  )
        let rxErrOpenRising  = cpv ("RxOpenRising", apiItem, apiItem.System, ApiItemTag.rxErrOpenRising  )
        let rxErrOpenTemp  = cpv ("RxOpenTemp", apiItem, apiItem.System, ApiItemTag.rxErrOpenTemp)



        let timerTimeOutBit = timer  stg "TOUT" apiItem.System   
        
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.ErrorText   = 
            let err1 = if txerrtrend.Value      then "동작편차" else ""
            let err2 = if txerrovertime.Value   then "동작시간" else ""
            let err3 = if rxerrShort.Value      then "센서감지" else ""
            let err4 = if rxerrOpen.Value       then "센서오프" else ""
            let errs =[err1;err2;err3;err4]|> Seq.where(fun f->f <> "")
            if errs.any()
            then
                let errText = String.Join(",", errs)
                $"{apiItem.System.Name} {errText} 이상"
            else 
                ""

        ///Timer time out
        member _.TOUT   = timerTimeOutBit
        member _.PS   = ps
        member _.PR   = pr
        member _.PE   = pe
        member _.TXErrTrend    = txerrtrend
        member _.TXErrOverTime   = txerrovertime
        member _.RXErrShort  = rxerrShort
        member _.RXErrShortOn  = rxErrShortOn
        member _.RXErrShortRising  = rxErrShortRising
        member _.RXErrShortTemp  = rxErrShortTemp



        member _.RXErrOpen   = rxerrOpen
        member _.RXErrOpenOff  = rxErrOpenOff
        member _.RXErrOpenRising  = rxErrOpenRising
        member _.RXErrOpenTemp  = rxErrOpenTemp
