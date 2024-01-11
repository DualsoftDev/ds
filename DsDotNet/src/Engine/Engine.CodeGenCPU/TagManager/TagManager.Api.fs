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
        let timerSensorOutBit = timer  stg "TOUTSensor" apiItem.System   
        
        interface ITagManager with
            member _.Target = apiItem
            member _.Storages = stg


        member _.GetApiTag (vt:ApiItemTag) :IStorage =
            match vt with 
            | ApiItemTag.planSet            -> ps  :> IStorage
            | ApiItemTag.planEnd            -> pe  :> IStorage
            | ApiItemTag.txErrTrendOut      -> txerrtrendout    :> IStorage
            | ApiItemTag.txErrTimeOver      -> txerrovertime    :> IStorage
            | ApiItemTag.rxErrShort         -> rxerrShort       :> IStorage
            | ApiItemTag.rxErrShortOn       -> rxErrShortOn     :> IStorage
            | ApiItemTag.rxErrShortRising   -> rxErrShortRising :> IStorage
            | ApiItemTag.rxErrShortTemp     -> rxErrShortTemp   :> IStorage
            | ApiItemTag.rxErrOpen          -> rxerrOpen        :> IStorage
            | ApiItemTag.rxErrOpenOff       -> rxErrOpenOff     :> IStorage
            | ApiItemTag.rxErrOpenRising    -> rxErrOpenRising  :> IStorage
            | ApiItemTag.rxErrOpenTemp      -> rxErrOpenTemp    :> IStorage
            | ApiItemTag.trxErr             -> trxErr           :> IStorage
            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

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
        member _.TOUTSensor   = timerSensorOutBit
        member _.PS   = ps
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
