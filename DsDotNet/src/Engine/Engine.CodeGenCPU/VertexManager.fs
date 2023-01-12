namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module VertexManagerModule =

    //   타입 | 구분    | Tag   | Port | Force HMI |
    //                          (Real Only) 
    //______________________________________________
    //   PLAN | Start	| ST    | SP   | SF        |
    //   PLAN | Reset	| RT	| RP   | RF        |
    //   PLAN | End	    | ET	| EP   | EF        |
    //______________________________________________
    // ACTION | IN	    | API. I| -	   | API. I    |
    // ACTION | OUT	    | API. O| -	   | API. O    |

    let bit (v:Vertex)  mark flag = DsBit($"{v.QualifiedName}({mark})", false, v, flag)
    let timer (v:Vertex)  mark flag = 
            let ts = TimerStruct.Create(TimerType.TON, Storages(), $"{v.QualifiedName}({mark}:TON)", 0us, 0us) 
            DsTimer($"{v.QualifiedName}({mark})", false, v, flag, ts)
    let counter (v:Vertex)  mark flag = 
            let cs = CTRStruct.Create(CounterType.CTR, Storages(), $"{v.QualifiedName}({mark}:CTR)", 0us, 0us) 
            DsCounter($"{v.QualifiedName}({mark})", false, v, flag, cs)
        
    
    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    [<DebuggerDisplay("{name}")>]
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

        member x.Name  = v.QualifiedName
        member x.Vertex  = v
        member x.Flow    = v.Parent.GetFlow()
        member x.System  = v.Parent.GetFlow().System

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
        ///Going Relay   //리셋 인과에 따라 필요
        member x.GR(src:Vertex) = 
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

       
    type VertexMCoin(v:Vertex) =
        inherit VertexManager(v)
        let relayCallBit  = bit v  "CR" BitFlag.RelayCall

        let counterBit    = counter v  "CTR" CounterFlag.CountRing
        let timerOnDelayBit = timer v  "TON"  TimerFlag.TimerOnDely
        let timerTimeOutBit = timer v  "TOUT" TimerFlag.TimeOut

        ///Call Done Relay 
        member x.CR         = relayCallBit 

        ///Ring Counter 
        member x.CTR    = counterBit 
        ///Timer on delay
        member x.TON    = timerOnDelayBit 
        ///Timer time out   
        member x.TOUT    = timerTimeOutBit 

           
          
           
