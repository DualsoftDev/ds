namespace Engine.CodeGenCPU

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module VertexMemoryManagerModule =

//   타입 | 구분    | Tag   | Port | Force HMI |
//______________________________________________
//   PLAN | Start	| ST    | SP   | SF        |
//   PLAN | Reset	| RT	| RP   | RF        |
//   PLAN | End	    | ET	| EP   | EF        |
//______________________________________________
// ACTION | IN	    | API. I| -	   | API. I    |
// ACTION | OUT	    | API. O| -	   | API. O    |

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    type VertexMemoryManager (v:Vertex)  =
        let name = v.QualifiedName
        let bit name flag = DsBit(name, false, v, flag)
        
        let readyBit   = bit $"{name}(R)"  TagFlag.R
        let goingBit   = bit $"{name}(G)"  TagFlag.G
        let finishBit  = bit $"{name}(F)"  TagFlag.F
        let homingBit  = bit $"{name}(H)"  TagFlag.H
        let originBit  = bit $"{name}(0G)" TagFlag.Origin
        let pauseBit   = bit $"{name}(PA)" TagFlag.Pause
        let errorTxBit = bit $"{name}(E1)" TagFlag.ErrorTx
        let errorRxBit = bit $"{name}(E2)" TagFlag.ErrorRx

        let relayRealBit  = bit $"{name}(RR)" TagFlag.RelayReal
        let relayCallBit  = bit $"{name}(CR)" TagFlag.RelayCall
        let relayGoingBit = bit $"{name}(GR)" TagFlag.RelayGoing

        let endTagBit     = bit $"{name}(ET)" TagFlag.ET
        let resetTagBit   = bit $"{name}(RT)" TagFlag.RT
        let startTagBit   = bit $"{name}(ST)" TagFlag.ST

        let endPortBit    = bit $"{name}(EP)" TagFlag.EP
        let resetPortBit  = bit $"{name}(RP)" TagFlag.RP
        let startPortBit  = bit $"{name}(SP)" TagFlag.SP

        let endForceBit   = bit $"{name}(EP)" TagFlag.EF
        let resetForceBit = bit $"{name}(RP)" TagFlag.RF
        let startForceBit = bit $"{name}(SP)" TagFlag.SF

        interface IVertexMemoryManager with
            member x.Vertex = v

        member x.Name  = name

        //Tag 약어 변수는 수식정의시 사용
        ///Segment Start Tag
        member x.StartTag   = startTagBit
        member x.ST         = startTagBit |> tag2expr
        ///Segment Reset Tag
        member x.ResetTag   = resetTagBit
        member x.RT         = resetTagBit |> tag2expr
        ///Segment End Tag
        member x.EndTag     = endTagBit  
        member x.ET         = endTagBit   |> tag2expr

        //Port
        ///Segment Start Port
        member x.StartPort  = startPortBit 
        member x.SP         = startPortBit  |> tag2expr
        ///Segment Reset Port
        member x.ResetPort  = resetPortBit 
        member x.RP         = resetPortBit  |> tag2expr
        ///Segment End Port
        member x.EndPort    = endPortBit     
        member x.EP         = endPortBit    |> tag2expr

        //Force
        ///StartForce HMI
        member x.StartForce = startForceBit 
        member x.SF         = startForceBit  |> tag2expr
        ///ResetForce HMI
        member x.ResetForce = resetForceBit 
        member x.RF         = resetForceBit  |> tag2expr
        ///EndForce HMI
        member x.EndForce   = endForceBit   
        member x.EF         = endForceBit    |> tag2expr

        //Relay
        ///Real Init Relay  
        member x.RelayRealInitStart = relayRealBit 
        member x.RR                 = relayRealBit  |> tag2expr
        ///Call Done Relay 
        member x.RelayCallDone      = relayCallBit
        member x.CR                 = relayCallBit |> tag2expr
        ///Going Relay 
        member x.RrelayGoing        = relayGoingBit 
        member x.GR                 = relayGoingBit |> tag2expr

        //Status 
        ///Ready Status
        member x.Ready  = readyBit  
        member x.R      = readyBit  |> tag2expr
        ///Going Status
        member x.Going  = goingBit  
        member x.G      = goingBit  |> tag2expr
        ///Finish Status
        member x.Finish = finishBit 
        member x.F      = finishBit  |> tag2expr
        ///Homing Status
        member x.Homing = homingBit 
        member x.H      = homingBit  |> tag2expr

        //Monitor 
        ///Origin Monitor
        member x.Origin  =  originBit 
        member x.OG      =  originBit  |> tag2expr
        ///Pause Monitor
        member x.Pause   =  pauseBit  
        member x.PA      =  pauseBit   |> tag2expr
        ///Error Tx Monitor
        member x.ErrorTx =  errorTxBit
        member x.E1      =  errorTxBit |> tag2expr
        ///Error Rx Monitor
        member x.ErrorRx =  errorRxBit
        member x.E2      =  errorRxBit |> tag2expr 

