namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module VertexManagerModule =

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

        let endTagBit     = bit v "ET" BitFlag.ET
        let resetTagBit   = bit v "RT" BitFlag.RT
        let startTagBit   = bit v "ST" BitFlag.ST

        let originBit     = bit v "OG" BitFlag.Origin
        let pauseBit      = bit v "PA" BitFlag.Pause
        let errorTxBit    = bit v "E1" BitFlag.ErrorTx
        let errorRxBit    = bit v "E2" BitFlag.ErrorRx

        let readyBit      = bit v "R"  BitFlag.R
        let goingBit      = bit v "G"  BitFlag.G
        let finishBit     = bit v "F"  BitFlag.F
        let homingBit     = bit v "H"  BitFlag.H

        let endForceBit   = bit v "EF" BitFlag.EF
        let resetForceBit = bit v "RF" BitFlag.RF
        let startForceBit = bit v "SF" BitFlag.SF

        let pulseBit      = bit v "PUL" BitFlag.Pulse
        let goingRelays = HashSet<DsBit>()


        interface IVertexManager with
            member x.Vertex = v

        member _.Name   = v.QualifiedName
        member _.Vertex = v
        member _.Flow   = v.Parent.GetFlow()
        member _.System = v.Parent.GetFlow().System
        member val SysManager = SystemManager(v.Parent.GetSystem())



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
        member _.GR(src:Vertex) =
           let gr =   bit v  $"GR_{src.Name}" BitFlag.RelayGoing
           goingRelays.Add gr |> ignore; gr



    type VertexMReal(v:Vertex) =
        inherit VertexManager(v)
        let endPortBit    = bit v "EP" BitFlag.EP
        let resetPortBit  = bit v "RP" BitFlag.RP
        let startPortBit  = bit v "SP" BitFlag.SP

        let relayRealBit  = bit v "RR" BitFlag.RelayReal
        let realOriginAction  = bit v "RO" BitFlag.RealOriginAction
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


    type VertexMCoin(v:Vertex) =
        inherit VertexManager(v)
        let relayCallBit  = bit v  "CR" BitFlag.RelayCall

        let counterBit    = counter v  "CTR" CounterFlag.CountRing
        let timerOnDelayBit = timer v  "TON"  TimerFlag.TimerOnDely
        let timerTimeOutBit = timer v  "TOUT" TimerFlag.TimeOut

        ///Call Done Relay
        member _.CR     = relayCallBit

        ///Ring Counter
        member _.CTR    = counterBit
        ///Timer on delay
        member _.TON    = timerOnDelayBit
        ///Timer time out
        member _.TOUT   = timerTimeOutBit




