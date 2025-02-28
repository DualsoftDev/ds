namespace Engine.CodeGenCPU

open System
open System.Diagnostics
open Dual.Common.Core.FS
open Engine.Core
open System.Linq

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

    let private createTagOnVertex (v:Vertex) (autoAddr:bool) (vertexTag:VertexTag)  =
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let vertexTag = vertexTag |> int
        let name = getStorageName v vertexTag
        let t = createPlanVar  s name DuBOOL autoAddr (Some(v)) vertexTag sys
        t :?> PlanVar<bool>

    let createData(v:Vertex, vertexTag:VertexTag, dataType :DataType)  =
        let vertexTagInt = vertexTag |> int
        let name = $"{v.QualifiedName}_{getTagKindName(vertexTagInt)}" |> validStorageName
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        createPlanVar s name dataType true (Some(v)) vertexTagInt sys

    /// Vertex Manager : 소속되어 있는 DsBit 를 관리하는 컨테이어
    [<DebuggerDisplay("{Name}")>]
    [<AbstractClass>]
    type VertexTagManager (v:Vertex, isActive:bool)  =
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages

        let createTag = createTagOnVertex v

        let sysM = sys.TagManager :?> SystemManager
     
        interface ITagManager with
            member x.Target = v
            member x.Storages = s

        member _.Name   = v.QualifiedName
        member _.Vertex = v

        member x.IsOperator =
            match v with
            | :? Call as c -> c.IsOperator
            |_-> false
        member x.IsCommand =
            match v with
            | :? Call as c -> c.IsCommand
            |_-> false
        member _.Flow   = v.Parent.GetFlow()
        member _.FlowManager   = v.Parent.GetFlow().TagManager :?> FlowManager   
        member x.System =
            assert(sys = x.Flow.System)
            x.Flow.System
        member _.Storages = s

        member x._on  = sysM.GetSystemTag(SystemTag._ON)  :?> PlanVar<bool>
        member x._off = sysM.GetSystemTag(SystemTag._OFF) :?> PlanVar<bool>

        ///Segment Start Tag
        member val ST = createTag  true  VertexTag.startTag
        ///Segment Reset Tag
        member val RT = createTag  true  VertexTag.resetTag
        ///Segment End Tag
        member val ET =
            let et = createTag  true  VertexTag.endTag
            if RuntimeDS.ModelConfig.RuntimePackage.IsPackageSIM() then
                if v :? Real && (v :?> Real).Finished then
                    et.Value <- true
            et

        ///OPC Server에서 계산 endTag 횟수 
        member val CalcCount = 
            if isActive
            then createData(v, VertexTag.calcCount, DuUINT32)
            else sysM.TempDataDuUINT32
        member val CalcAverage = 
            if isActive 
            then createData(v, VertexTag.calcAverage, DuFLOAT32)
            else sysM.TempDataDuFLOAT32
        member val CalcStandardDeviation = 
            if isActive
            then createData(v, VertexTag.calcStandardDeviation, DuFLOAT32)
            else sysM.TempDataDuFLOAT32
        member val CalcWaitingDuration = 
            if isActive
            then createData(v, VertexTag.calcWaitingDuration, DuUINT32)
            else sysM.TempDataDuUINT32
        member val CalcActiveDuration = 
            if isActive
            then createData(v, VertexTag.calcActiveDuration, DuUINT32)
            else sysM.TempDataDuUINT32
        member val CalcMovingDuration = 
            if isActive
            then createData(v, VertexTag.calcMovingDuration, DuUINT32)
            else sysM.TempDataDuUINT32

        member val CalcActiveStartTime = 
            if isActive
            then createData(v, VertexTag.calcActiveStartTime, DuSTRING)
            else sysM.TempDataDuString

        member val CalcStatWorkFinish = 
            if isActive
            then createData(v, VertexTag.calcStatWorkFinish, DuBOOL)
            else sysM.TempDataDuBool
       
        member val CalcStatActionFinish = 
            if isActive
            then createData(v, VertexTag.calcStatActionFinish, DuBOOL)
            else sysM.TempDataDuBool

        ///forceOnBit HMI , forceOffBit HMI 는 RF 사용
        member val ON = createTag true VertexTag.forceOn
        ///forceOnBit HMI Pulse, forceOffBit HMI 는 RF 사용
        member val ONP = createTag true VertexTag.forceOnPulse
        ///forceStartBit HMI
        member val SF = createTag true VertexTag.forceStart
        ///forceStartBit HMI Pulse
        member val SFP = createTag true VertexTag.forceStartPulse
        ///forceResetBit HMI
        member val RF = createTag true VertexTag.forceReset
        ///forceResetBit HMI Pulse
        member val RFP = createTag true VertexTag.forceResetPulse

        //Status
        ///Ready Status
        member val R = createTag true VertexTag.ready
        ///Going Status
        member val G = createTag true VertexTag.going
        ///Finish Status
        member val F = createTag true VertexTag.finish
        ///Homing Status
        member val H = createTag true VertexTag.homing
        /// Going Pulse
        member val GP = createTag false VertexTag.goingPulse

        ///Pause Monitor
        member val PA = createTag false VertexTag.pause

        member x.GetVertexTag (vt:VertexTag) :IStorage =
            let callM() = v.TagManager:?> CoinVertexTagManager
            let realM() = v.TagManager:?> RealVertexTagManager

            match vt with
            | VertexTag.startTag        -> x.ST     :> IStorage
            | VertexTag.resetTag        -> x.RT     :> IStorage
            | VertexTag.endTag          -> x.ET     :> IStorage
            | VertexTag.ready           -> x.R      :> IStorage
            | VertexTag.going           -> x.G      :> IStorage
            | VertexTag.finish          -> x.F      :> IStorage
            | VertexTag.homing          -> x.H      :> IStorage
            | VertexTag.forceStart      -> x.SF     :> IStorage
            | VertexTag.forceStartPulse -> x.SFP    :> IStorage
            | VertexTag.forceReset      -> x.RF     :> IStorage
            | VertexTag.forceResetPulse -> x.RFP    :> IStorage
            | VertexTag.forceOn         -> x.ON     :> IStorage
            | VertexTag.forceOnPulse    -> x.ONP    :> IStorage
            | VertexTag.goingPulse      -> x.GP     :> IStorage
            | VertexTag.pause           -> x.PA     :> IStorage

            | VertexTag.calcCount               -> x.CalcCount 
            | VertexTag.calcAverage             -> x.CalcAverage 
            | VertexTag.calcStandardDeviation   -> x.CalcStandardDeviation 
            | VertexTag.calcWaitingDuration     -> x.CalcWaitingDuration
            | VertexTag.calcActiveDuration      -> x.CalcActiveDuration
            | VertexTag.calcMovingDuration      -> x.CalcMovingDuration
            | VertexTag.calcActiveStartTime     -> x.CalcActiveStartTime
            | VertexTag.calcStatWorkFinish      -> x.CalcStatWorkFinish
            | VertexTag.calcStatActionFinish    -> x.CalcStatActionFinish
            
            | VertexTag.txErrOnTimeUnder     -> callM().ErrOnTimeUnder     :> IStorage
            | VertexTag.txErrOnTimeOver      -> callM().ErrOnTimeOver      :> IStorage
            | VertexTag.txErrOffTimeUnder    -> callM().ErrOffTimeUnder    :> IStorage
            | VertexTag.txErrOffTimeOver     -> callM().ErrOffTimeOver     :> IStorage
            | VertexTag.rxErrShort           -> callM().ErrShort           :> IStorage
            | VertexTag.rxErrOpen            -> callM().ErrOpen            :> IStorage
            | VertexTag.rxErrInterlock       -> callM().ErrInterlock       :> IStorage
            | VertexTag.errorAction          -> callM().ErrAction         :> IStorage

            | VertexTag.callIn               -> callM().CallIn            :> IStorage
            | VertexTag.callOut              -> callM().CallOut           :> IStorage

            | VertexTag.origin               -> realM().OG     :> IStorage
            | VertexTag.realOriginInit       -> realM().RO :> IStorage
            | VertexTag.realOriginButton     -> realM().OB :> IStorage
            | VertexTag.realOriginAction     -> realM().OA :> IStorage
            | VertexTag.relayReal            -> realM().RR :> IStorage
            | VertexTag.goingRealy           -> realM().GG :> IStorage
            | VertexTag.realToken            -> realM().RealTokenData :> IStorage
            | VertexTag.mergeToken           -> realM().MergeTokenData :> IStorage
            | VertexTag.sourceToken          -> realM().SourceTokenData :> IStorage
            | VertexTag.errorWork            -> realM().ErrWork :> IStorage

  
            | (VertexTag.counter | VertexTag.timerOnDelay) ->
                failwithlog $"Error : Time Counter Type {vt} not support!!"

            | _ -> failwithlog $"Error : GetVertexTag {vt} type not support!!"



    and RealVertexTagManager(v:Vertex, isActive:bool) =
        inherit VertexTagManager(v, isActive)

        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let real = v:?> Real
        let hasChildren = real.Graph.Vertices.Any()
        let useScript   = real.Script.IsSome
        let useMotion   = real.Motion.IsSome
        let useTime     = real.Time.IsSome

        let sysManager = sys.TagManager :?> SystemManager
        let off = sysManager.GetSystemTag(SystemTag._OFF) :?> PlanVar<bool>
        let mutable originInfo:OriginInfo = defaultOriginInfo (real)
        let createTag = createTagOnVertex v

        //let timeOutGoingOriginTimeOut = timer  s "TOUTOrigin" sys

      
        let realToken  = createData(v, VertexTag.realToken, DuUINT32)
        let mergeToken = createData(v, VertexTag.mergeToken, DuUINT32)
        let sourceToken = createData(v, VertexTag.sourceToken, DuUINT32)

        member x.Real = x.Vertex :?> Real
        member x.OriginInfo
            with get() = originInfo
            and set(v) = originInfo <- v

        ///Real SEQ Data
        member val RealTokenData = realToken
        ///병합되기전 사라진 SEQ Data
        member val MergeTokenData = mergeToken
       ///병합되기전 사라진 SEQ Data
        member val SourceTokenData = sourceToken

        /// Real Origin Init
        member val RO         = createTag false VertexTag.realOriginInit
        /// Real Origin Btn
        member val OB         = createTag false VertexTag.realOriginButton
        /// Real Origin Action
        member val OA         = createTag false VertexTag.realOriginAction

        ///Origin Monitor
        member val OG = createTag false VertexTag.origin
        member val ErrWork = createTag false VertexTag.errorWork

        ///Real Init Relay
        member val RR         = createTag false VertexTag.relayReal
        ///Real Going Relay
        member val GG         = createTag false VertexTag.goingRealy


        ///link with physical sensors
        member val Link       = createTag false VertexTag.realLink
        ///GoingOriginErr
        member val ErrGoingOrigin = createTag false VertexTag.workErrOriginGoing

        ///DAG Coin Start Coil
        member val CoinAnyOnST  = if hasChildren
                                    then createTag false VertexTag.dummyCoinSTs
                                    else off
        ///DAG Coin Reset Coil
        member val CoinAnyOnRT  = if hasChildren
                                    then createTag false VertexTag.dummyCoinRTs
                                    else off
        ///DAG Coin End Coil
        member val CoinAnyOnET  = if hasChildren
                                    then createTag false VertexTag.dummyCoinETs
                                    else off

        ///Timer time avg
        member val TRealOnTime  = timer s ($"{v.QualifiedName}_ONTIME"|>validStorageName) sys (sysManager.TargetType)

        member x.IsFinished = x.Real.Finished
        member x.NoTransData = x.Real.NoTransData
        member x.IsSourceToken = x.Real.IsSourceToken
        

        member val ScriptStart  = if useScript then createTag true VertexTag.scriptStart else off
        member val MotionStart  = if useMotion then createTag true VertexTag.motionStart else off
        member val TimeStart    = if useTime   then createTag true VertexTag.timeStart   else off

        member val ScriptEnd    = if useScript then createTag true VertexTag.scriptEnd   else off
        member val MotionEnd    = if useMotion then createTag true VertexTag.motionEnd   else off
        member val TimeEnd      = if useTime   then createTag true VertexTag.timeEnd     else off

        member val ScriptRelay  = if useScript then createTag true VertexTag.scriptRelay else off
        member val MotionRelay  = if useMotion then createTag true VertexTag.motionRelay else off
        member val TimeRelay    = if useTime   then createTag true VertexTag.timeRelay   else off

    and CoinVertexTagManager(v:Vertex, isActive:bool) =
        inherit VertexTagManager(v, isActive)
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let sysManager = sys.TagManager :?> SystemManager
        let createTag (autoAddr:bool) (vertexTag:VertexTag) : PlanVar<bool> = createTagOnVertex v autoAddr vertexTag



        ///Ring Counter
        //member val CTR  = counter  s ($"{v.QualifiedName}_CTR"|>validStorageName) sys (sysManager.TargetType)
        /////Timer on delay
        //member val TDON = timer  s ($"{v.QualifiedName}_TON"|>validStorageName) sys (sysManager.TargetType)
        /////Timer time
        //member val TOUT = timer  s ($"{v.QualifiedName}_TOUT"|>validStorageName) sys (sysManager.TargetType)

        ///Timer time
        member val TimeMax     = timer  s ($"{v.QualifiedName}_TimeMax"|>validStorageName) sys (sysManager.TargetType)
        member val TimeCheck  =  timer  s ($"{v.QualifiedName}_TimeCheck"|>validStorageName) sys (sysManager.TargetType)

        member val PS   = createTag  true VertexTag.planStart
        member val PE   = createTag  true VertexTag.planEnd

        member val ErrOnTimeUnder     = createTag true VertexTag.txErrOnTimeUnder
        member val ErrOnTimeOver      = createTag true VertexTag.txErrOnTimeOver
        member val ErrOffTimeUnder    = createTag true VertexTag.txErrOffTimeUnder
        member val ErrOffTimeOver     = createTag true VertexTag.txErrOffTimeOver

        member val ErrShort           = createTag true VertexTag.rxErrShort
        member val ErrOpen            = createTag true VertexTag.rxErrOpen
        member val ErrInterlock       = createTag true VertexTag.rxErrInterlock
        member val ErrAction          = createTag false VertexTag.errorAction
        
        member val CallIn             =  createTag  true VertexTag.callIn
        member val CallOut            =  createTag  true VertexTag.callOut

        ///callCommandEnd
        member val CallCommandEnd     =  createTag  true VertexTag.callCommandEnd
        ///callCommandPulse
        member val CallCommandPulse   =  createTag  true VertexTag.callCommandPulse
        ///Call Operator 연산결과 값 (T/F)
        member val CallOperatorValue  =  createTag  true VertexTag.callOperatorValue


        member x.ErrorList =
            [|
                if x.ErrOnTimeUnder.Value  then yield "감지시간부족"
                if x.ErrOnTimeOver.Value      then yield "감지시간초과"
                if x.ErrOffTimeUnder.Value then yield "해지시간부족"
                if x.ErrOffTimeOver.Value     then yield "해지시간초과"
                if x.ErrShort.Value           then yield "센서감지"
                if x.ErrOpen.Value            then yield "센서오프"
                if x.ErrInterlock.Value            then yield "반대센서오프"
            |]
        member x.ErrorText   =
            let errors = x.ErrorList
            if errors.Any() then
                let errText = String.Join(",", errors)
                $"{errText} 이상"
            else
                ""
