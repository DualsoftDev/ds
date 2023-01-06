namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core
open System.Collections.Generic

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
        let timerBit   mark flag = DsBit($"{name}({mark})", false, v, flag)
        let counterBit mark flag = DsBit($"{name}({mark})", false, v, flag)
        
        let readyBit      = bit "R"  TagFlag.R
        let goingBit      = bit "G"  TagFlag.G
        let finishBit     = bit "F"  TagFlag.F
        let homingBit     = bit "H"  TagFlag.H
        let originBit     = bit "0G" TagFlag.Origin
        let pauseBit      = bit "PA" TagFlag.Pause
        let errorTxBit    = bit "E1" TagFlag.ErrorTx
        let errorRxBit    = bit "E2" TagFlag.ErrorRx

        let relayRealBit  = bit "RR" TagFlag.RelayReal
        let relayCallBit  = bit "CR" TagFlag.RelayCall
     

        let endTagBit     = bit "ET" TagFlag.ET
        let resetTagBit   = bit "RT" TagFlag.RT
        let startTagBit   = bit "ST" TagFlag.ST

        let endPortBit    = bit "EP" TagFlag.EP
        let resetPortBit  = bit "RP" TagFlag.RP
        let startPortBit  = bit "SP" TagFlag.SP

        let endForceBit   = bit "EF" TagFlag.EF
        let resetForceBit = bit "RF" TagFlag.RF
        let startForceBit = bit "SF" TagFlag.SF

        let pulseBit      = bit "PUL" TagFlag.Pulse
        let counterBit    = counterBit "CTR" TagFlag.Counter
        let timerDelayBit = timerBit "TON" TagFlag.TimerTx

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
           let gr =   bit $"GR_{tgt.Name}" TagFlag.RelayGoing
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
        ///Timer TON   
        member x.TON    = timerDelayBit 
           
          
        
         
