namespace Engine.CodeGenCPU

open System
open System.Diagnostics
open Dual.Common.Core.FS
open Engine.Core

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
        let createTag autoAddr (vertexTag:VertexTag)  =
            let vertexTag = vertexTag |> int
            let name = getStorageName v vertexTag
            let t = createPlanVar  s name DuBOOL autoAddr v vertexTag sys
            t :?> PlanVar<bool>
        let sysM = sys.TagManager :?> SystemManager


        interface ITagManager with
            member x.Target = v
            member x.Storages = s

        member _.Name   = v.QualifiedName
        member _.Vertex = v
        member _.IsOperator =
            match v with 
            | :? Call as c -> c.CallOperatorType = DuOPCode
            |_-> false  
        member _.IsCommand =
            match v with 
            | :? Call as c -> c.CallCommandType = DuCMDCode
            |_-> false
        member _.Flow   = v.Parent.GetFlow()
        member x.System = x.Flow.System
        member _.Storages = s

        member x._on  = sysM.GetSystemTag(SystemTag._ON)  :?> PlanVar<bool>
        member x._off = sysM.GetSystemTag(SystemTag._OFF) :?> PlanVar<bool>
        member x._sim = sysM.GetSystemTag(SystemTag.sim)  :?> PlanVar<bool>

        ///Segment Start Tag
        member val ST = createTag  true  VertexTag.startTag
        ///Segment Reset Tag
        member val RT = createTag  true  VertexTag.resetTag
        ///Segment End Tag
        member val ET =
            let et = createTag  true  VertexTag.endTag
            if RuntimeDS.Package.IsPackageSIM() then 
                if v :? Real && (v :?> Real).Finished then 
                    et.Value <- true           
            et


        //Force
        ///forceOnBit HMI , forceOffBit HMI 는 RF 사용
        member val ON = createTag true VertexTag.forceOn
        
        ///forceStartBit HMI
        member val SF = createTag true VertexTag.forceStart
        ///forceResetBit HMI
        member val RF = createTag true VertexTag.forceReset

        //Status
        ///Ready Status
        member val R = createTag true VertexTag.ready
        ///Going Status
        member val G = createTag true VertexTag.going
        ///Finish Status
        member val F = createTag true VertexTag.finish
        ///Homing Status
        member val H = createTag true VertexTag.homing

        //Monitor
        ///Origin Monitor
        member val OG = createTag false VertexTag.origin
        ///Pause Monitor
        member val PA = createTag false VertexTag.pause


        /// Going Pulse
        member val GP = createTag false VertexTag.goingPulse
        /// Going Pulse Relay
        member val GPR = createTag false VertexTag.goingPulseRelay
        /// Going Pulse Hold
        member val GPH = createTag false VertexTag.goingPulseHold
 
        member val ErrTRX = createTag false VertexTag.errorTRx
        
        member _.CreateTag(name) = createTag name

        member x.GetVertexTag (vt:VertexTag) :IStorage =
            let callM() = v.TagManager:?> VertexMCall
            let realM() = v.TagManager:?> VertexMReal

            match vt with 
            | VertexTag.startTag -> x.ST :> IStorage
            | VertexTag.resetTag -> x.RT :> IStorage
            | VertexTag.endTag   -> x.ET :> IStorage
            | VertexTag.ready    -> x.R  :> IStorage
            | VertexTag.going    -> x.G  :> IStorage
            | VertexTag.finish   -> x.F  :> IStorage
            | VertexTag.homing   -> x.H  :> IStorage
            | VertexTag.origin   -> x.OG :> IStorage
            | VertexTag.pause    -> x.PA :> IStorage

            
            | VertexTag.errorTRx            -> x.ErrTRX :> IStorage

            | VertexTag.forceStart          -> x.SF :> IStorage
            | VertexTag.forceReset          -> x.RF :> IStorage
            | VertexTag.forceOn             -> x.ON :> IStorage

            | VertexTag.goingPulse          -> x.GP :> IStorage
            | VertexTag.goingPulseRelay     -> x.GPR :> IStorage
            | VertexTag.goingPulseHold      -> x.GPH :> IStorage
            
            | VertexTag.txErrOnTimeShortage  -> callM().ErrOnTimeShortage  :> IStorage
            | VertexTag.txErrOnTimeOver      -> callM().ErrOnTimeOver      :> IStorage
            | VertexTag.txErrOffTimeShortage -> callM().ErrOffTimeShortage :> IStorage
            | VertexTag.txErrOffTimeOver     -> callM().ErrOffTimeOver     :> IStorage
            | VertexTag.rxErrShort           -> callM().ErrShort           :> IStorage
            | VertexTag.rxErrOpen            -> callM().ErrOpen            :> IStorage

            | VertexTag.realOriginInit       -> realM().RO :> IStorage
            | VertexTag.realOriginButton     -> realM().OB :> IStorage
            | VertexTag.realOriginAction     -> realM().OA :> IStorage
            | VertexTag.relayReal            -> realM().RR :> IStorage
            | VertexTag.goingRealy           -> realM().GG :> IStorage
          
            | (VertexTag.counter | VertexTag.timerOnDelay) ->
                failwithlog $"Error : Time Counter Type {vt} not support!!"

            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"
         
   

    and VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)

        let s    = this.Storages
        let sys = this.System
        let real = v:?> Real
        let sysManager = sys.TagManager :?> SystemManager
        let mutable originInfo:OriginInfo = defaultOriginInfo (real)
        let createTag name = this.CreateTag name

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
        member val RO         = createTag false VertexTag.realOriginInit
        /// Real Origin Btn
        member val OB         = createTag false VertexTag.realOriginButton
        /// Real Origin Action
        member val OA         = createTag false VertexTag.realOriginAction
        
        ///Real Init Relay
        member val RR         = createTag false VertexTag.relayReal
        ///Real Going Relay
        member val GG         = createTag false VertexTag.goingRealy
    
        ///Real Data
        //member val RD         = realData
        ///link with physical sensors
        member val Link       = createTag false VertexTag.realLink
        ///GoingOriginErr
        member val ErrGoingOrigin = createTag false VertexTag.workErrOriginGoing

        ///DAG Coin Start Coil
        member val CoinAnyOnST    = createTag false VertexTag.dummyCoinSTs
        ///DAG Coin Reset Coil
        member val CoinAnyOnRT    = createTag false VertexTag.dummyCoinRTs
        ///DAG Coin End Coil
        member val CoinAnyOnET    = createTag false VertexTag.dummyCoinETs

        ///Timer time avg
        member val TRealOnTime    = timer s ($"{v.QualifiedName}_ONTIME"|>validStorageName) sys (sysManager.TargetType)

        member x.IsFinished = x.Real.Finished

        member val ScriptStart  =  createTag true VertexTag.scriptStart
        member val MotionStart  =  createTag true VertexTag.motionStart
        member val TimeStart    =  createTag true VertexTag.timeStart  

        member val ScriptEnd    =  createTag true VertexTag.scriptEnd
        member val MotionEnd    =  createTag true VertexTag.motionEnd
        member val TimeEnd      =  createTag true VertexTag.timeEnd

        member val ScriptRelay  =  createTag true VertexTag.scriptRelay
        member val MotionRelay  =  createTag true VertexTag.motionRelay
        member val TimeRelay    =  createTag true VertexTag.timeRelay

    and VertexMCall(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let sys = this.System
        let sysManager = sys.TagManager :?> SystemManager
        let createTag name = this.CreateTag name 

        let counterBit    = counter  s ($"{v.QualifiedName}_CTR"|>validStorageName) sys (sysManager.TargetType)
        let timerOnDelayBit = timer  s ($"{v.QualifiedName}_TON"|>validStorageName) sys (sysManager.TargetType)
        let memo           = createTag  false VertexTag.callMemo
        
        let callCommandPulse  = createTag  false VertexTag.callCommandPulse
        let callCommandEnd    = createTag  false VertexTag.callCommandEnd
        let callOperatorValue  = createTag false VertexTag.callOperatorValue
   
        let timerTimeOutBit  = timer  s ($"{v.QualifiedName}_TOUT"|>validStorageName) sys (sysManager.TargetType)
       
        let txErrOnTimeShortage     = createTag true VertexTag.txErrOnTimeShortage   
        let txErrOnTimeOver         = createTag true VertexTag.txErrOnTimeOver  
        let txErrOffTimeShortage    = createTag true VertexTag.txErrOffTimeShortage   
        let txErrOffTimeOver        = createTag true VertexTag.txErrOffTimeOver   
        let rxErrShort              = createTag true VertexTag.rxErrShort      
        let rxErrShortRising        = createTag true VertexTag.rxErrShortRising      
        let rxErrOpen               = createTag true VertexTag.rxErrOpen    
        let rxErrOpenRising         = createTag true VertexTag.rxErrOpenRising          
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

        
        ///Call Operator 연산결과 값 (T/F)
        member _.CallOperatorValue    =  callOperatorValue

        