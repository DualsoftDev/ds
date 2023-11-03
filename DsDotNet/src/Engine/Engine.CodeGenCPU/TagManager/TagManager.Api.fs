namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module ApiTagManagerModule =
    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (apiItem:ApiItem, activeSys:DsSystem)  =
        let stg = apiItem.System.TagManager.Storages

        let cpv2 (sys:DsSystem) (apiItemTag:ApiItemTag) =
            let n = Enum.GetName(typeof<ApiItemTag>, apiItemTag)
            let name = $"{sys.Name}_{apiItem.Name}_{n}"
            let pv:IStorage = createPlanVar stg name DuBOOL true apiItem (int apiItemTag) sys 
            pv :?> PlanVar<bool>
        /// create plan var
        let cpv = cpv2 apiItem.System

        let ps = cpv ApiItemTag.planSet
        let pr = cpv ApiItemTag.planRst
        let pe = cpv2 activeSys ApiItemTag.planEnd

        let txerrtrendout     = cpv ApiItemTag.txErrTrendOut  
        let txerrovertime     = cpv ApiItemTag.txErrTimeOver  
        let rxerrShort        = cpv ApiItemTag.rxErrShort  
        let rxErrShortOn      = cpv ApiItemTag.rxErrShortOn  
        let rxErrShortRising  = cpv ApiItemTag.rxErrShortRising  
        let rxErrShortTemp    = cpv ApiItemTag.rxErrShortTemp  
        let rxerrOpen         = cpv ApiItemTag.rxErrOpen  
        let rxErrOpenOff      = cpv ApiItemTag.rxErrOpenOff  
        let rxErrOpenRising   = cpv ApiItemTag.rxErrOpenRising  
        let rxErrOpenTemp     = cpv ApiItemTag.rxErrOpenTemp
        let trxErr            = cpv ApiItemTag.trxErr

        let timerTimeOutBit = timer  stg "TOUT" apiItem.System   
        
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.ErrorText   = 
            let err1 = if txerrtrendout.Value   then "동작편차" else ""
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

        member _.TXErrTrendOut    = txerrtrendout
        member _.TXErrOverTime   = txerrovertime

        member _.RXErrShort  = rxerrShort
        member _.RXErrShortOn  = rxErrShortOn
        member _.RXErrShortRising  = rxErrShortRising
        member _.RXErrShortTemp  = rxErrShortTemp

        member _.RXErrOpen   = rxerrOpen
        member _.RXErrOpenOff  = rxErrOpenOff
        member _.RXErrOpenRising  = rxErrOpenRising
        member _.RXErrOpenTemp  = rxErrOpenTemp

        member _.TRxErr  = trxErr
