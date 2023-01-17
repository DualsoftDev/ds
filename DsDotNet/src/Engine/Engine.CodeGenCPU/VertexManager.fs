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
        member x.ST         = startTagBit
        ///Segment Reset Tag
        member x.ResetTag   = resetTagBit
        member x.RT         = resetTagBit
        ///Segment End Tag
        member x.ET         = endTagBit

        //Force
        ///StartForce HMI
        member x.SF         = startForceBit
        ///ResetForce HMI
        member x.RF         = resetForceBit
        ///EndForce HMI
        member x.EF         = endForceBit

        //Status
        ///Ready Status
        member x.R          = readyBit
        ///Going Status
        member x.G          = goingBit
        ///Finish Status
        member x.F          = finishBit
        ///Homing Status
        member x.H          = homingBit

        //Monitor
        ///Origin Monitor
        member x.OG         =  originBit
        ///Pause Monitor
        member x.PA         =  pauseBit
        ///Error Tx Monitor
        member x.E1         =  errorTxBit
        ///Error Rx Monitor
        member x.E2         =  errorRxBit

        //DummyBit
        ///PulseStart
        member x.PUL        = pulseBit
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
        member x.RO         = realOriginAction
        ///Real Init Relay
        member x.RR         = relayRealBit
        //Port
        ///Segment Start Port
        member x.SP         = startPortBit
        ///Segment Reset Port
        member x.RP         = resetPortBit
        ///Segment End Port
        member x.EP         = endPortBit


    type VertexMCoin(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages 
        let relayCallBit  = bit v s "CR" BitFlag.RelayCall


        let counterBit    = counter v s "CTR" CounterFlag.CountRing 
        let timerOnDelayBit = timer v s "TON"  TimerFlag.TimerOnDely 
        let timerTimeOutBit = timer v s "TOUT" TimerFlag.TimeOut 

        ///Call Done Relay
        member x.CR     = relayCallBit

        ///Ring Counter
        member x.CTR    = counterBit
        ///Timer on delay
        member x.TON    = timerOnDelayBit
        ///Timer time out
        member x.TOUT   = timerTimeOutBit




