namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System.Linq
open System
open Dual.Common.Core.FS

[<AutoOpen>]
module TagManagerModule =

    //    타입 | 구분       | Tag    | Port  (Real Only) | Force HMI |
    //__________________________________________________________________
    //   PLAN | Start	| ST    | SP              | SF        |
    //   PLAN | Reset	| RT	| RP              | RF        |
    //   PLAN | End	    | ET	| EP              | EF        |
    //__________________________________________________________________
    // ACTION | IN	    | API. I| -	              | API. I    |
    // ACTION | OUT	    | API. O| -	              | API. O    |

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    [<DebuggerDisplay("{Name}")>]
    [<AbstractClass>]
    type VertexManager (v:Vertex)  =
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let createTag(mark) (vertexTag:VertexTag) =
            let vertexTag = vertexTag |> int
            let name = $"{v.QualifiedName}_{mark}"
            let t = createPlanVar  s name DuBOOL true v vertexTag sys
            t :?> PlanVar<bool>

        let startTagBit   = createTag "ST"   VertexTag.startTag
        let resetTagBit   = createTag "RT"   VertexTag.resetTag
        let endTagBit     =
            let et = createTag "ET"   VertexTag.endTag
            if RuntimeDS.Package.IsPackageSIM()
            then 
                if v :? Real && (v :?> Real).Finished
                then et.Value <- true           
            et

        let originBit      = createTag "OG"   VertexTag.origin
        let pauseBit       = createTag "PA"   VertexTag.pause

        let readyBit       = createTag "R"    VertexTag.ready
        let goingBit       = createTag "G"    VertexTag.going
        let finishBit      = createTag "F"    VertexTag.finish
        let homingBit      = createTag "H"    VertexTag.homing
                           
        let forceStartBit  = createTag "SF"   VertexTag.forceStart
        let forceResetBit  = createTag "RF"   VertexTag.forceReset
        let forceOnBit     = createTag "ON"   VertexTag.forceOn
        let forceOffBit    = createTag "OFF"  VertexTag.forceOff


        let txErrTimeShortage    = createTag "txErrTimeShortage"      VertexTag.txErrTimeShortage   
        let txErrTimeOver    = createTag "txErrTimeOver"      VertexTag.txErrTimeOver   
        let rxErrShort       = createTag "rxErrShort"         VertexTag.rxErrShort      
        let rxErrOpen        = createTag "rxErrOpen"          VertexTag.rxErrOpen    

        let errorErrTRXBit = createTag "ErrTRX"    VertexTag.errorTRx

        let errors = 
            let err1 = if txErrTimeShortage.Value   then "시간부족" else ""
            let err2 = if txErrTimeOver.Value   then "시간초과" else ""
            let err3 = if rxErrShort.Value      then "센서감지" else ""
            let err4 = if rxErrOpen.Value       then "센서오프" else ""
            [err1;err2;err3;err4]|> Seq.where(fun f->f <> "")

        interface ITagManager with
            member x.Target = v
            member x.Storages = s

       

        member _.Name   = v.QualifiedName
        member _.Vertex = v
        member _.Flow   = v.Parent.GetFlow()
        member _.System = v.Parent.GetFlow().System
        member _.Storages = s

        member _._on           = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.on)   :?> PlanVar<bool>
        member _._off          = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.off)  :?> PlanVar<bool>
        member _._sim          = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.sim)  :?> PlanVar<bool>

        ///Segment Start Tag
        member _.ST         = startTagBit
        ///Segment Reset Tag
        member _.RT         = resetTagBit
        ///Segment End Tag
        member _.ET         = endTagBit

        //Force
        ///forceOnBit HMI
        member _.ON         = forceOnBit
        ///forceOffBit HMI
        member _.OFF         = forceOffBit
        ///forceStartBit HMI
        member _.SF         = forceStartBit
        ///forceResetBit HMI
        member _.RF         = forceResetBit

        //Status
        ///Ready Status
        member _.R          = readyBit
        ///Going Status
        member _.G          = goingBit
        ///Finish Status
        member _.F          = finishBit
        ///Homing Status
        member _.H          = homingBit

        //Monitor
        ///Origin Monitor
        member _.OG         =  originBit
        ///PAuse Monitor
        member _.PA         =  pauseBit

        member _.ErrTimeShortage   = txErrTimeShortage 
        member _.ErrTimeOver   =  txErrTimeOver 
        member _.ErrShort       = rxErrShort    
        member _.ErrOpen       =  rxErrOpen     
     
        member _.ErrTRX         =  errorErrTRXBit
        
        member _.CreateTag(name) = createTag name

        member _.GetVertexTag (vt:VertexTag) :IStorage =
            match vt with 
            | VertexTag.startTag            -> startTagBit         :> IStorage
            | VertexTag.resetTag            -> resetTagBit         :> IStorage
            | VertexTag.endTag              -> endTagBit           :> IStorage
            | VertexTag.ready               -> readyBit            :> IStorage
            | VertexTag.going               -> goingBit            :> IStorage
            | VertexTag.finish              -> finishBit           :> IStorage
            | VertexTag.homing              -> homingBit           :> IStorage
            | VertexTag.origin              -> originBit           :> IStorage
            | VertexTag.pause               -> pauseBit            :> IStorage

            | VertexTag.txErrTimeShortage       -> txErrTimeShortage   :> IStorage
            | VertexTag.txErrTimeOver       -> txErrTimeOver   :> IStorage
            | VertexTag.rxErrShort          -> rxErrShort      :> IStorage
            | VertexTag.rxErrOpen           -> rxErrOpen       :> IStorage
            | VertexTag.errorTRx            -> errorErrTRXBit  :> IStorage
                                      



            | VertexTag.forceStart          -> forceStartBit       :> IStorage
            | VertexTag.forceReset          -> forceResetBit       :> IStorage
            | VertexTag.forceOn             -> forceOnBit          :> IStorage
            | VertexTag.forceOff            -> forceOffBit         :> IStorage

            | VertexTag.realOriginAction    -> (v.TagManager:?> VertexMReal).RO    :> IStorage
            | VertexTag.relayReal           -> (v.TagManager:?> VertexMReal).RR    :> IStorage
            | VertexTag.goingRealy          -> (v.TagManager:?> VertexMReal).GG    :> IStorage

            | VertexTag.counter             
            | VertexTag.timerOnDelay        -> failwithlog $"Error : Time Counter Type {vt} not support!!"

            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         
        member _.ErrorList   =  errors
        member _.ErrorText   = 
            if errors.any()
            then
                let errText = String.Join(",", errors)
                $"{_.Name} {errText} 이상"
            else 
                ""

    and VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let real = v:?> Real
        let mutable originInfo:OriginInfo = defaultOriginInfo (real)
        let createTag name = this.CreateTag name

        let relayGoingBit     = createTag "GG"         VertexTag.goingRealy
        let relayRealBit      = createTag "RR"         VertexTag.relayReal
        let realOriginAction  = createTag "RO"         VertexTag.realOriginAction
        let realSync          = createTag "Sync"       VertexTag.realSync
        let dummyCoinSTs      = createTag "CoinAnyOnST"        VertexTag.dummyCoinSTs
        let dummyCoinRTs      = createTag "CoinAnyOnRT"        VertexTag.dummyCoinRTs
        let dummyCoinETs      = createTag "CoinAnyOnET"        VertexTag.dummyCoinETs
        
        let realData  = 
            let vertexTag = VertexTag.realData |> int
            let name = $"{v.QualifiedName}_RD"
            createPlanVar  s name DuUINT8 true v vertexTag sys  
            

        member x.OriginInfo
            with get() = originInfo
            and set(v) = originInfo <- v

        /// Real Origin Action
        member _.RO         = realOriginAction
        ///Real Init Relay
        member _.RR         = relayRealBit
        ///Real Going Relay
        member _.GG         = relayGoingBit
        ///Real Data
        member _.RD         = realData
        ///Synchronized with physical sensors
        member _.SYNC       = realSync

        ///DAG Coin Start Coil
        member _.CoinAnyOnST         = dummyCoinSTs
        ///DAG Coin Reset Coil
        member _.CoinAnyOnRT         = dummyCoinRTs
        ///DAG Coin End Coil
        member _.CoinAnyOnET         = dummyCoinETs

        member _.IsFinished = (v :?> Real).Finished

    and VertexMCoin(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let sys = this.System
        let createTag name = this.CreateTag name

        let counterBit    = counter  s "CTR"  sys
        let timerOnDelayBit = timer  s "TON"  sys 
        let memo           = createTag "Memo" VertexTag.callMemo

   
        let rxErrShortOn     = createTag "rxErrShortOn"       VertexTag.rxErrShortOn    
        let rxErrShortRising = createTag "rxErrShortRising"   VertexTag.rxErrShortRising
        let rxErrShortTemp   = createTag "rxErrShortTemp"     VertexTag.rxErrShortTemp  
        let rxErrOpenOff     = createTag "rxErrOpenOff"       VertexTag.rxErrOpenOff    
        let rxErrOpenRising  = createTag "rxErrOpenRising"    VertexTag.rxErrOpenRising 
        let rxErrOpenTemp    = createTag "rxErrOpenTemp"      VertexTag.rxErrOpenTemp   
        let timerTimeOutBit  = timer  s "TOUT" sys 
       

        ///Ring Counter
        member _.CTR     = counterBit
        ///Timer on delay
        member _.TDON    = timerOnDelayBit

        member _.MM           =  memo

        member _.TOUT   = timerTimeOutBit


        member _.RXErrOpenOff       = rxErrShortOn    
        member _.RXErrOpenTemp      = rxErrShortRising
        member _.RXErrOpenRising    = rxErrShortTemp  
        member _.RXErrShortOn       = rxErrOpenOff    
        member _.RXErrShortRising   = rxErrOpenRising 
        member _.RXErrShortTemp     = rxErrOpenTemp   

