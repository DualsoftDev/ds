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
        let createTag(mark) autoAddr (vertexTag:VertexTag)  =
            let vertexTag = vertexTag |> int
            let name = $"{v.QualifiedName}_{mark}"
            let t = createPlanVar  s name DuBOOL autoAddr v vertexTag sys
            t :?> PlanVar<bool>

        let startTagBit   = createTag "ST" true  VertexTag.startTag
        let resetTagBit   = createTag "RT" true  VertexTag.resetTag
        let endTagBit     =
            let et = createTag "ET" true  VertexTag.endTag
            if RuntimeDS.Package.IsPackageSIM()
            then 
                if v :? Real && (v :?> Real).Finished
                then et.Value <- true           
            et

        let originBit      = createTag "OG" false  VertexTag.origin
        let pauseBit       = createTag "PA" false  VertexTag.pause

        let readyBit       = createTag "R" true  VertexTag.ready
        let goingBit       = createTag "G" true  VertexTag.going
        let finishBit      = createTag "F" true  VertexTag.finish
        let homingBit      = createTag "H" true  VertexTag.homing
                           
        let forceStartBit  = createTag "SF"  true VertexTag.forceStart
        let forceResetBit  = createTag "RF"  true VertexTag.forceReset
        let forceOnBit     = createTag "ON"  true VertexTag.forceOn
        let forceOffBit    = createTag "OFF" true VertexTag.forceOff
        let actionSync     = createTag "actionSync" true VertexTag.actionSync
        let actionStart    = createTag "actionStart" true VertexTag.actionStart
        let actionEnd      = createTag "actionEnd" true VertexTag.actionEnd

        let errorErrTRXBit = createTag "ErrTRX" false   VertexTag.errorTRx

        

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

        member _._on           = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.on)   :?> PlanVar<bool>
        member _._off          = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.off)  :?> PlanVar<bool>
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
        member _.ActionSync     =  actionSync
        member _.ActionStart    =  actionStart
        member _.ActionEnd      =  actionEnd
        
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
        let sys =  v.Parent.GetSystem()
        let s =  sys.TagManager.Storages
        let real = v:?> Real
        let mutable originInfo:OriginInfo = defaultOriginInfo (real)
        let createTag name = this.CreateTag name

        let relayGoingBit     = createTag "GG"                  false     VertexTag.goingRealy
        let relayRealBit      = createTag "RR"                  false     VertexTag.relayReal
        let realOriginInit    = createTag "RO"                  false     VertexTag.realOriginInit
        let realOriginButton  = createTag "OB"                  false     VertexTag.realOriginButton
        let realOriginAction  = createTag "OA"                  false     VertexTag.realOriginAction
        
        let realLink          = createTag "Link"                false     VertexTag.realLink
        let dummyCoinSTs      = createTag "CoinAnyOnST"         false     VertexTag.dummyCoinSTs
        let dummyCoinRTs      = createTag "CoinAnyOnRT"         false     VertexTag.dummyCoinRTs
        let dummyCoinETs      = createTag "CoinAnyOnET"         false     VertexTag.dummyCoinETs
        let originGoingErr    = createTag "OriginGoingErr"      false     VertexTag.workErrOriginGoing
        //let timeOutGoingOriginTimeOut = timer  s "TOUTOrigin" sys 
        
        let realData  = 
            let vertexTag = VertexTag.realData |> int
            let name = $"{v.QualifiedName}_RD"
            createPlanVar  s name DuUINT16 true v vertexTag sys  
            

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
        member _.RD         = realData
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

        member _.IsFinished = (v :?> Real).Finished

    and VertexMCall(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let sys = this.System
        let sysManager = sys.TagManager :?> SystemManager
        let createTag name = this.CreateTag name 

        let counterBit    = counter  s $"{v.Name}_CTR"  sys (sysManager.TargetType)
        let timerOnDelayBit = timer  s $"{v.Name}_TON"  sys (sysManager.TargetType)
        let memo           = createTag "Memo" false VertexTag.callMemo
        
        let callCommandEnd  = createTag "callCommandEnd" false VertexTag.callCommandEnd
        let callOperatorValue  = createTag "callOperatorValue" false VertexTag.callOperatorValue
   
        let timerTimeOutBit  = timer  s $"{v.Name}_TOUT" sys (sysManager.TargetType)
       
        let txErrOnTimeShortage     = createTag "txErrOnTimeShortage"   true    VertexTag.txErrOnTimeShortage   
        let txErrOnTimeOver         = createTag "txErrOnTimeOver"       true    VertexTag.txErrOnTimeOver  
        let txErrOffTimeShortage    = createTag "txErrOffTimeShortage"  true    VertexTag.txErrOffTimeShortage   
        let txErrOffTimeOver        = createTag "txErrOffTimeOver"      true    VertexTag.txErrOffTimeOver   
        let rxErrShort              = createTag "rxErrShort"            true    VertexTag.rxErrShort      
        let rxErrShortRising        = createTag "rxErrShortRising"      true    VertexTag.rxErrShortRising      
        let rxErrOpen               = createTag "rxErrOpen"             true    VertexTag.rxErrOpen    
        let rxErrOpenRising         = createTag "rxErrOpenRising"       true    VertexTag.rxErrOpenRising          

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
        ///Call Operator 연산결과 값 (T/F)
        member _.CallOperatorValue    =  callOperatorValue

        