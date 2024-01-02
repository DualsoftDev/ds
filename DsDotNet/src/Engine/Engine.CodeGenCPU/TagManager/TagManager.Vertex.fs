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
            if v :? Real && (v :?> Real).Finished
            then et.Value <- true           
            et
        let originBit      = createTag "OG"   VertexTag.origin
        let pauseBit       = createTag "PA"   VertexTag.pause
        let errorTxBit     = createTag "E1"   VertexTag.errorTx
        let errorRxBit     = createTag "E2"   VertexTag.errorRx
        let errorErrTRXBit = createTag "ErrTRX"   VertexTag.errorTRx
        let readyBit       = createTag "R"    VertexTag.ready
        let goingBit       = createTag "G"    VertexTag.going
        let finishBit      = createTag "F"    VertexTag.finish
        let homingBit      = createTag "H"    VertexTag.homing
                           
        let forceStartBit  = createTag "SF"   VertexTag.forceStart
        let forceResetBit  = createTag "RF"   VertexTag.forceReset
        let forceOnBit     = createTag "ON"   VertexTag.forceOn
        let forceOffBit    = createTag "OFF"  VertexTag.forceOff


        interface ITagManager with
            member x.Target = v
            member x.Storages = s


        member _.Name   = v.QualifiedName
        member _.Vertex = v
        member _.Flow   = v.Parent.GetFlow()
        member _.System = v.Parent.GetFlow().System
        member _.Storages = s

        member _._on    = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.on)   :?> PlanVar<bool>
        member _._off   = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.off)  :?> PlanVar<bool>
        member _._sim   = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.sim)  :?> PlanVar<bool>

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
        ///Error Tx Monitor
        member _.E1         =  errorTxBit
        ///Error Rx Monitor
        member _.E2         =  errorRxBit
        ///Error TRx Monitor
        member _.ErrTRX         =  errorErrTRXBit

        member _.CreateTag(name) = createTag name

        member _.GetVertexTag (vt:VertexTag) :IStorage =
            match vt with 
            | VertexTag.startTag            -> startTagBit           :> IStorage
            | VertexTag.resetTag            -> resetTagBit           :> IStorage
            | VertexTag.endTag              -> endTagBit             :> IStorage
            | VertexTag.ready               -> readyBit              :> IStorage
            | VertexTag.going               -> goingBit              :> IStorage
            | VertexTag.finish              -> finishBit             :> IStorage
            | VertexTag.homing              -> homingBit             :> IStorage
            | VertexTag.origin              -> originBit             :> IStorage
            | VertexTag.pause               -> pauseBit              :> IStorage
            | VertexTag.errorTx             -> errorTxBit            :> IStorage
            | VertexTag.errorRx             -> errorRxBit            :> IStorage
            | VertexTag.errorTRx            -> errorErrTRXBit        :> IStorage
                                                                  
            | VertexTag.forceStart            -> forceStartBit       :> IStorage
            | VertexTag.forceReset            -> forceResetBit       :> IStorage
            | VertexTag.forceOn               -> forceOnBit          :> IStorage
            | VertexTag.forceOff              -> forceOffBit         :> IStorage
              

            | VertexTag.realOriginAction    -> (v.TagManager:?> VertexMReal).RO    :> IStorage
            | VertexTag.relayReal           -> (v.TagManager:?> VertexMReal).RR    :> IStorage
            | VertexTag.goingRealy          -> (v.TagManager:?> VertexMReal).GG    :> IStorage

            | VertexTag.counter             
            | VertexTag.timerOnDelay        -> failwithlog $"Error : Time Counter Type {vt} not support!!"

            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         

    and VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let mutable originInfo:OriginInfo = defaultOriginInfo (v:?> Real)
        let createTag name = this.CreateTag name

        let relayGoingBit     = createTag "GG" VertexTag.goingRealy
        let relayRealBit      = createTag "RR" VertexTag.relayReal
        let realOriginAction  = createTag "RO" VertexTag.realOriginAction

        member x.OriginInfo
            with get() = originInfo
            and set(v) = originInfo <- v

        /// Real Origin Action
        member _.RO         = realOriginAction
        ///Real Init Relay
        member _.RR         = relayRealBit
        ///Real Going Relay
        member _.GG         = relayGoingBit
     


    and VertexMCoin(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let createTag name = this.CreateTag name
        let sys = this.System

        let counterBit    = counter  s "CTR"  sys
        let timerOnDelayBit = timer  s "TON"  sys 

        /////Call Done Relay
        //member _.CR     = relayCallBit

        ///Ring Counter
        member _.CTR     = counterBit
        ///Timer on delay
        member _.TDON    = timerOnDelayBit


