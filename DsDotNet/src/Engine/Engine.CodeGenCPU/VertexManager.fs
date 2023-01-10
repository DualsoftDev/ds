namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core
open System.Collections.Generic
open System

[<AutoOpen>]
module VertexManagerModule =

    //   타입 | 구분    | Tag   | Port | Force HMI |
    //______________________________________________
    //   PLAN | Start	| ST    | SP   | SF        |
    //   PLAN | Reset	| RT	| RP   | RF        |
    //   PLAN | End	    | ET	| EP   | EF        |
    //______________________________________________
    // ACTION | IN	    | API. I| -	   | API. I    |
    // ACTION | OUT	    | API. O| -	   | API. O    |

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    type VertexManager (v:Vertex)  =
        let name = v.QualifiedName
        let goingRelays = HashSet<DsBit>()
        let bit mark flag = DsBit($"{name}({mark})", false, v, flag)
        [<Obsolete("<ahn> Storages() 는 singleton instance 로 관리되어야 함.")>]
        let timer mark flag =
                let ts = TimerStruct.Create(TimerType.TON, Storages(), $"{name}({mark}:TON)", 0us, 0us)
                DsTimer($"{name}({mark})", false, v, flag, ts)
        [<Obsolete("<ahn> Storages() 는 singleton instance 로 관리되어야 함.")>]
        let counter mark flag =
                let cs = CTRStruct.Create(CounterType.CTR, Storages(), $"{name}({mark}:CTR)", 0us, 0us)
                DsCounter($"{name}({mark})", false, v, flag, cs)

        let readyBit      = bit "R"  BitFlag.R
        let goingBit      = bit "G"  BitFlag.G
        let finishBit     = bit "F"  BitFlag.F
        let homingBit     = bit "H"  BitFlag.H
        let originBit     = bit "0G" BitFlag.Origin
        let pauseBit      = bit "PA" BitFlag.Pause
        let errorTxBit    = bit "E1" BitFlag.ErrorTx
        let errorRxBit    = bit "E2" BitFlag.ErrorRx

        let relayRealBit  = bit "RR" BitFlag.RelayReal
        let relayCallBit  = bit "CR" BitFlag.RelayCall


        let endTagBit     = bit "ET" BitFlag.ET
        let resetTagBit   = bit "RT" BitFlag.RT
        let startTagBit   = bit "ST" BitFlag.ST

        let endPortBit    = bit "EP" BitFlag.EP
        let resetPortBit  = bit "RP" BitFlag.RP
        let startPortBit  = bit "SP" BitFlag.SP

        let endForceBit   = bit "EF" BitFlag.EF
        let resetForceBit = bit "RF" BitFlag.RF
        let startForceBit = bit "SF" BitFlag.SF

        let pulseBit      = bit "PUL" BitFlag.Pulse
        let counterBit    = counter "CTR" CounterFlag.CountRing
        let timerOnDelayBit = timer "TON"  TimerFlag.TimerOnDely
        let timerTimeOutBit = timer "TOUT" TimerFlag.TimeOut

        interface IVertexManager with
            member x.Vertex = v

        member x.Name  = name
        member x.Vertex  = v
        member x.Flow    = v.Parent.GetFlow()
        member x.System  = v.Parent.GetFlow().System


        //Relay
        ///Real Init Relay
        member x.RR         = relayRealBit
        ///Call Done Relay
        member x.CR         = relayCallBit
        ///Going Relay   //리셋 인과에 따라 필요
        member x.GR(tgt:Vertex) =
           let gr =   bit $"GR_{tgt.Name}" BitFlag.RelayGoing
           goingRelays.Add gr |> ignore; gr

        ///Segment Start Tag
        member x.ST         = startTagBit
        ///Segment Reset Tag
        member x.ResetTag   = resetTagBit
        member x.RT         = resetTagBit
        ///Segment End Tag
        member x.ET         = endTagBit

        //Port
        ///Segment Start Port
        member x.SP         = startPortBit
        ///Segment Reset Port
        member x.RP         = resetPortBit
        ///Segment End Port
        member x.EP         = endPortBit

        //Force
        ///StartForce HMI
        member x.SF         = startForceBit
        ///ResetForce HMI
        member x.RF         = resetForceBit
        ///EndForce HMI
        member x.EF         = endForceBit


        //Status
        ///Ready Status
        member x.R      = readyBit
        ///Going Status
        member x.G      = goingBit
        ///Finish Status
        member x.F      = finishBit
        ///Homing Status
        member x.H      = homingBit

        //Monitor
        ///Origin Monitor
        member x.OG      =  originBit
        ///Pause Monitor
        member x.PA      =  pauseBit
        ///Error Tx Monitor
        member x.E1      =  errorTxBit
        ///Error Rx Monitor
        member x.E2      =  errorRxBit

        //DummyBit
        ///PulseStart
        member x.PUL    = pulseBit
        ///Ring Counter
        member x.CTR    = counterBit
        ///Timer on delay
        member x.TON    = timerOnDelayBit
        ///Timer time out
        member x.TOUT    = timerTimeOutBit




