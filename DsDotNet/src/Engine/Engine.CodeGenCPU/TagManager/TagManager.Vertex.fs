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
        let createTag  autoAddr (vertexTag:VertexTag)  =
            let vertexTag = vertexTag |> int
            let name = getStorageName v vertexTag
            let t = createPlanVar  s name DuBOOL autoAddr v vertexTag sys
            t :?> PlanVar<bool>

        let startTagBit   = createTag  true  VertexTag.startTag
        let resetTagBit   = createTag  true  VertexTag.resetTag
        let endTagBit     =
            let et = createTag  true  VertexTag.endTag
            if RuntimeDS.Package.IsPackageSIM()
            then 
                if v :? Real && (v :?> Real).Finished
                then et.Value <- true           
            et

        let originBit      = createTag  false  VertexTag.origin
        let pauseBit       = createTag  false  VertexTag.pause

        let readyBit       = createTag  true  VertexTag.ready
        let goingBit       = createTag  true  VertexTag.going
        let finishBit      = createTag  true  VertexTag.finish
        let homingBit      = createTag  true  VertexTag.homing
                           
        let forceStartBit  = createTag  true VertexTag.forceStart
        let forceResetBit  = createTag  true VertexTag.forceReset
        let forceOnBit     = createTag  true VertexTag.forceOn
        let forceOffBit    = createTag  true VertexTag.forceOff


        

        let goingPulse        = createTag false     VertexTag.goingPulse
        let goingPulseRelay   = createTag false     VertexTag.goingPulseRelay
        

        let errorErrTRXBit = createTag  false   VertexTag.errorTRx

        interface ITagManager with
            member x.Target = v
            member x.Storages = s

       

        member _.Name   = v.QualifiedName
        member _.Vertex = v
        member _.IsOperator = match v with 
                              | :? Call as c -> c.CallOperatorType = DuOPCode
                              |_-> false  
        member _.IsCommand =  match v with 
                              | :? Call as c -> c.CallCommandType = DuCMDCode
                              |_-> false
        member _.Flow   = v.Parent.GetFlow()
        member _.System = v.Parent.GetFlow().System
        member _.Storages = s

        member _._on           = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag._ON)   :?> PlanVar<bool>
        member _._off          = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag._OFF)  :?> PlanVar<bool>
        member _._sim          = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.sim)  :?> PlanVar<bool>

        ///Segment Start Tag
        member _.ST         = startTagBit
        ///Segment Reset Tag
        member _.RT         = resetTagBit
        ///Segment End Tag
        member _.ET         = endTagBit

        //Force
        ///forceOnBit HMI , forceOffBit HMI 는 RF 사용
        member _.ON         = forceOnBit
        
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
        ///Pause Monitor
        member _.PA         =  pauseBit


        /// Going Pulse
        member _.GP         = goingPulse
        /// Going Pulse Relay
        member _.GPR         = goingPulseRelay
 
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

            
            | VertexTag.errorTRx            -> errorErrTRXBit  :> IStorage

            | VertexTag.forceStart          -> forceStartBit       :> IStorage
            | VertexTag.forceReset          -> forceResetBit       :> IStorage
            | VertexTag.forceOn             -> forceOnBit          :> IStorage
            | VertexTag.forceOff            -> forceOffBit         :> IStorage

            | VertexTag.goingPulse          -> goingPulse       :> IStorage
            | VertexTag.goingPulseRelay     -> goingPulseRelay  :> IStorage

            | VertexTag.txErrOnTimeShortage  -> (v.TagManager:?> VertexMCall).ErrOnTimeShortage  :> IStorage
            | VertexTag.txErrOnTimeOver      -> (v.TagManager:?> VertexMCall).ErrOnTimeOver      :> IStorage
            | VertexTag.txErrOffTimeShortage -> (v.TagManager:?> VertexMCall).ErrOffTimeShortage :> IStorage
            | VertexTag.txErrOffTimeOver     -> (v.TagManager:?> VertexMCall).ErrOffTimeOver     :> IStorage
            | VertexTag.rxErrShort           -> (v.TagManager:?> VertexMCall).ErrShort           :> IStorage
            | VertexTag.rxErrOpen            -> (v.TagManager:?> VertexMCall).ErrOpen            :> IStorage

            | VertexTag.realOriginInit       -> (v.TagManager:?> VertexMReal).RO    :> IStorage
            | VertexTag.realOriginButton     -> (v.TagManager:?> VertexMReal).OB    :> IStorage
            | VertexTag.realOriginAction     -> (v.TagManager:?> VertexMReal).OA    :> IStorage
            | VertexTag.relayReal            -> (v.TagManager:?> VertexMReal).RR    :> IStorage
            | VertexTag.goingRealy           -> (v.TagManager:?> VertexMReal).GG    :> IStorage
          
            | VertexTag.counter             
            | VertexTag.timerOnDelay        -> failwithlog $"Error : Time Counter Type {vt} not support!!"

            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         
   

    and VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let sys = this.System
        let real = v:?> Real
        let sysManager = sys.TagManager :?> SystemManager
        let mutable originInfo:OriginInfo = defaultOriginInfo (real)
        let createTag name = this.CreateTag name
        let timerOnTimeBit = timer s ($"{v.QualifiedName}_ONTIME"|>validStorageName) sys (sysManager.TargetType)

        let relayGoingBit     = createTag false     VertexTag.goingRealy

        let relayRealBit      = createTag false     VertexTag.relayReal
        let realOriginInit    = createTag false     VertexTag.realOriginInit
        let realOriginButton  = createTag false     VertexTag.realOriginButton
        let realOriginAction  = createTag false     VertexTag.realOriginAction
        
        let realLink          = createTag false     VertexTag.realLink
        let dummyCoinSTs      = createTag false     VertexTag.dummyCoinSTs
        let dummyCoinRTs      = createTag false     VertexTag.dummyCoinRTs
        let dummyCoinETs      = createTag false     VertexTag.dummyCoinETs
        let originGoingErr    = createTag false     VertexTag.workErrOriginGoing

        let scriptStart    = createTag  true VertexTag.scriptStart
        let motionStart    = createTag  true VertexTag.motionStart
        let timeStart      = createTag  true VertexTag.timeStart
        
        let scriptEnd      = createTag  true VertexTag.scriptEnd
        let motionEnd      = createTag  true VertexTag.motionEnd
        let timeEnd        = createTag  true VertexTag.timeEnd

        let scriptRelay    = createTag  true VertexTag.scriptRelay
        let motionRelay    = createTag  true VertexTag.motionRelay
        let timeRelay      = createTag  true VertexTag.timeRelay


        //let timeOutGoingOriginTimeOut = timer  s "TOUTOrigin" sys 
        //let realData  = 
        //    let vertexTag = VertexTag.realData |> int
        //    let name = $"{v.QualifiedName}_RD"
        //    createPlanVar  s name DuUINT16 true v vertexTag sys  
            

        member x.Real = x.Vertex :?> Real
        member x.OriginInfo
            with get() = originInfo
            and set(v) = originInfo <- v

        /// Real Origin Init
        member _.RO         = realOriginInit
        /// Real Origin Btn
        member _.OB         = realOriginButton
        /// Real Origin Action
        member _.OA         = realOriginAction
        
        ///Real Init Relay
        member _.RR         = relayRealBit
        ///Real Going Relay
        member _.GG         = relayGoingBit
    
        ///Real Data
        //member _.RD         = realData
        ///link with physical sensors
        member _.Link       = realLink
        ///GoingOriginErr
        member _.ErrGoingOrigin         = originGoingErr

        ///DAG Coin Start Coil
        member _.CoinAnyOnST         = dummyCoinSTs
        ///DAG Coin Reset Coil
        member _.CoinAnyOnRT         = dummyCoinRTs
        ///DAG Coin End Coil
        member _.CoinAnyOnET         = dummyCoinETs


        ///Timer time avg
        member _.TRealOnTime    = timerOnTimeBit

        member _.IsFinished = (v :?> Real).Finished

        member _.ScriptStart  =  scriptStart
        member _.MotionStart  =  motionStart
        member _.TimeStart    =  timeStart  

        member _.ScriptEnd    =  scriptEnd
        member _.MotionEnd    =  motionEnd
        member _.TimeEnd      =  timeEnd

        member _.ScriptRelay    =  scriptRelay
        member _.MotionRelay    =  motionRelay
        member _.TimeRelay      =  timeRelay

    and VertexMCall(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let sys = this.System
        let sysManager = sys.TagManager :?> SystemManager
        let createTag name = this.CreateTag name 

        let counterBit    = counter  s ($"{v.QualifiedName}_CTR"|>validStorageName) sys (sysManager.TargetType)
        let timerOnDelayBit = timer  s ($"{v.QualifiedName}_TON"|>validStorageName) sys (sysManager.TargetType)
        let memo           = createTag  false VertexTag.callMemo
        
        let callCommandPulseRelay  = createTag  false VertexTag.callCommandPulseRelay
        let callCommandPulse  = createTag  false VertexTag.callCommandPulse
        let callCommandEnd    = createTag  false VertexTag.callCommandEnd
        let callOperatorValue  = createTag false VertexTag.callOperatorValue
   
        let timerTimeOutBit  = timer  s ($"{v.QualifiedName}_TOUT"|>validStorageName) sys (sysManager.TargetType)
       
        let txErrOnTimeShortage     = createTag  true    VertexTag.txErrOnTimeShortage   
        let txErrOnTimeOver         = createTag  true    VertexTag.txErrOnTimeOver  
        let txErrOffTimeShortage    = createTag  true    VertexTag.txErrOffTimeShortage   
        let txErrOffTimeOver        = createTag  true    VertexTag.txErrOffTimeOver   
        let rxErrShort              = createTag  true    VertexTag.rxErrShort      
        let rxErrShortRising        = createTag  true    VertexTag.rxErrShortRising      
        let rxErrOpen               = createTag  true    VertexTag.rxErrOpen    
        let rxErrOpenRising         = createTag  true    VertexTag.rxErrOpenRising          

        let errors = 
            let err1 = if txErrOnTimeShortage.Value      then "감지시간부족" else ""
            let err2 = if txErrOnTimeOver.Value          then "감지시간초과" else ""
            let err3 = if txErrOffTimeShortage.Value     then "해지시간부족" else ""
            let err4 = if txErrOffTimeOver.Value         then "해지시간초과" else ""
            let err5 = if rxErrShort.Value      then "센서감지" else ""
            let err6 = if rxErrOpen.Value       then "센서오프" else ""
            [err1;err2;err3;err4;err5;err6]|> Seq.where(fun f->f <> "")

        member _.ErrorList   =  errors
        member _.ErrorText   = 
            if errors.any()
            then
                let errText = String.Join(",", errors)
                $"{_.Name} {errText} 이상"
            else 
                ""

        ///Ring Counter
        member _.CTR     = counterBit
        ///Timer on delay
        member _.TDON    = timerOnDelayBit

        member _.MM           =  memo

        member _.TOUT   = timerTimeOutBit

        member _.RXErrOpen       = rxErrOpen
        member _.RXErrShort      = rxErrShort       
        member _.RXErrOpenRising       = rxErrOpenRising
        member _.RXErrShortRising      = rxErrShortRising   

        member _.ErrOnTimeShortage = txErrOnTimeShortage 
        member _.ErrOnTimeOver     = txErrOnTimeOver 
        member _.ErrOffTimeShortage = txErrOffTimeShortage 
        member _.ErrOffTimeOver     = txErrOffTimeOver 
        member _.ErrShort        = rxErrShort    
        member _.ErrShortRising  = rxErrShortRising    
        member _.ErrOpen         = rxErrOpen     
        member _.ErrOpenRising   = rxErrOpenRising     

   
        ///callCommandEnd
        member _.CallCommandEnd           =  callCommandEnd
        ///callCommandPulse  
        member _.CallCommandPulse         =  callCommandPulse
        member _.CallCommandPulseRelay    =  callCommandPulseRelay

        
        ///Call Operator 연산결과 값 (T/F)
        member _.CallOperatorValue    =  callOperatorValue

        