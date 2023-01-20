namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

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
        let s =  v.Parent.GetSystem().TagManager.Storages

        let endTagBit     = planTag  s "ET"
        let resetTagBit   = planTag  s "RT"
        let startTagBit   = planTag  s "ST"

        let originBit     = planTag  s "OG"
        let pauseBit      = planTag  s "PA"
        let errorTxBit    = planTag  s "E1"
        let errorRxBit    = planTag  s "E2"

        let readyBit      = planTag  s "R"
        let goingBit      = planTag  s "G"
        let finishBit     = planTag  s "F"
        let homingBit     = planTag  s "H"

        let endForceBit   = planTag  s "EF"
        let resetForceBit = planTag  s "RF"
        let startForceBit = planTag  s "SF"

        let pulseBit      = planTag  s "PUL"
        let goingRelays = HashSet<PlanTag<bool>>()


        interface ITagManager with
            member x.Target = v
            member x.Storages = s

        member x.Name   = v.QualifiedName
        member x.Vertex = v
        member x.Flow   = v.Parent.GetFlow()
        member x.System = v.Parent.GetFlow().System
        member x.Storages = s

        member x._on  = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSysBitTag(ON)
        member x._off  = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSysBitTag(OFF)

        ///Segment Start Tag
        member _.ST         = startTagBit
        ///Segment Reset Tag
        member _.ResetTag   = resetTagBit
        member _.RT         = resetTagBit
        ///Segment End Tag
        member _.ET         = endTagBit

        //Force
        ///StartForce HMI
        member _.SF         = startForceBit
        ///ResetForce HMI
        member _.RF         = resetForceBit
        ///EndForce HMI
        member _.EF         = endForceBit

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
        ///Pause Monitor
        member _.PA         =  pauseBit
        ///Error Tx Monitor
        member _.E1         =  errorTxBit
        ///Error Rx Monitor
        member _.E2         =  errorRxBit

        //DummyBit
        ///PulseStart
        member _.PUL        = pulseBit
        ///Going Relay   //리셋 인과에 따라 필요
        member x.GR(src:Vertex) =
           let gr =  planTag  s $"GR_{src.Name}"
           goingRelays.Add gr |> ignore; gr



    type VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let endPortBit    = planTag s "EP"
        let resetPortBit  = planTag s "RP"
        let startPortBit  = planTag s "SP"

        let relayRealBit  = planTag  s "RR"
        let realOriginAction  = planTag  s "RO"
        /// Real Origin Action
        member _.RO         = realOriginAction
        ///Real Init Relay
        member _.RR         = relayRealBit
        //Port
        ///Segment Start Port
        member _.SP         = startPortBit
        ///Segment Reset Port
        member _.RP         = resetPortBit
        ///Segment End Port
        member _.EP         = endPortBit


    type VertexMCoin(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let relayCallBit  = planTag  s "CR"


        let counterBit    = counter  s "CTR"
        let timerOnDelayBit = timer  s "TON"
        let timerTimeOutBit = timer  s "TOUT"

        ///Call Done Relay
        member _.CR     = relayCallBit

        ///Ring Counter
        member _.CTR    = counterBit
        ///Timer on delay
        member _.TON    = timerOnDelayBit
        ///Timer time out
        member _.TOUT   = timerTimeOutBit




