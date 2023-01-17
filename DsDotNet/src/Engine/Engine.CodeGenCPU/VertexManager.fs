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
        let s =  (v.Parent.GetSystem().TagManager :?> SystemManager).Storages

        let endTagBit     = bit v s "ET" BitFlag.ET
        let resetTagBit   = bit v s "RT" BitFlag.RT
        let startTagBit   = bit v s "ST" BitFlag.ST

        let originBit     = bit v s "OG" BitFlag.Origin
        let pauseBit      = bit v s "PA" BitFlag.Pause
        let errorTxBit    = bit v s "E1" BitFlag.ErrorTx
        let errorRxBit    = bit v s "E2" BitFlag.ErrorRx

        let readyBit      = bit v s "R"  BitFlag.R
        let goingBit      = bit v s "G"  BitFlag.G
        let finishBit     = bit v s "F"  BitFlag.F
        let homingBit     = bit v s "H"  BitFlag.H

        let endForceBit   = bit v s "EF" BitFlag.EF
        let resetForceBit = bit v s "RF" BitFlag.RF
        let startForceBit = bit v s "SF" BitFlag.SF

        let pulseBit      = bit v s "PUL" BitFlag.Pulse
        let goingRelays = HashSet<DsBit>()


        interface ITagManager with
            member x.Target = v

        member x.Name   = v.QualifiedName
        member x.Vertex = v
        member x.Flow   = v.Parent.GetFlow()
        member x.System = v.Parent.GetFlow().System
        member x.Storages = s



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
           let gr =   bit v s $"GR_{src.Name}" BitFlag.RelayGoing
           goingRelays.Add gr |> ignore; gr



    type VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let endPortBit    = bit v s "EP" BitFlag.EP
        let resetPortBit  = bit v s "RP" BitFlag.RP
        let startPortBit  = bit v s "SP" BitFlag.SP

        let relayRealBit  = bit v s "RR" BitFlag.RelayReal
        let realOriginAction  = bit v s "RO" BitFlag.RealOriginAction
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
        let relayCallBit  = bit v s "CR" BitFlag.RelayCall


        let counterBit    = counter v s "CTR" CounterFlag.CountRing
        let timerOnDelayBit = timer v s "TON"  TimerFlag.TimerOnDely
        let timerTimeOutBit = timer v s "TOUT" TimerFlag.TimeOut

        ///Call Done Relay
        member _.CR     = relayCallBit

        ///Ring Counter
        member _.CTR    = counterBit
        ///Timer on delay
        member _.TON    = timerOnDelayBit
        ///Timer time out
        member _.TOUT   = timerTimeOutBit




