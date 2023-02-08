namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System

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
        let createTag(mark) (vertexTag:VertexTag) =
            let vertexTag = vertexTag |> int
            let name = $"{v.QualifiedName}_{mark}"
            let t = createPlanVar  s name DuBOOL true v vertexTag
            t :?> PlanVar<bool>

        let startTagBit   = createTag "ST"  VertexTag.startTag
        let resetTagBit   = createTag "RT"  VertexTag.resetTag
        let endTagBit     = createTag "ET"  VertexTag.endTag
        let originBit     = createTag "OG"  VertexTag.origin
        let pauseBit      = createTag "PA"  VertexTag.pause
        let errorTxBit    = createTag "E1"  VertexTag.errorTx
        let errorRxBit    = createTag "E2"  VertexTag.errorRx
        let readyBit      = createTag "R"   VertexTag.ready
        let goingBit      = createTag "G"   VertexTag.going
        let finishBit     = createTag "F"   VertexTag.finish
        let homingBit     = createTag "H"   VertexTag.homing
        let startForceBit = createTag "SF"  VertexTag.startForce
        let resetForceBit = createTag "RF"  VertexTag.resetForce
        let endForceBit   = createTag "EF"  VertexTag.endForce
        let pulseBit      = createTag "PUL" VertexTag.pulse
        let goingRelays = HashSet<PlanVar<bool>>()


        interface ITagManager with
            member x.Target = v
            member x.Storages = s


        member x.Name   = v.QualifiedName
        member x.Vertex = v
        member x.Flow   = v.Parent.GetFlow()
        member x.System = v.Parent.GetFlow().System
        member x.Storages = s

        member x._on  = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.on)    :?> PlanVar<bool>
        member x._off  = (v.Parent.GetFlow().System.TagManager :?> SystemManager).GetSystemTag(SystemTag.off)  :?> PlanVar<bool>

        ///Segment Start Tag
        member _.ST         = startTagBit
        ///Segment Reset Tag
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
        ///OriGin Monitor
        member _.OG         =  originBit
        ///PAuse Monitor
        member _.PA         =  pauseBit
        ///Error Tx Monitor
        member _.E1         =  errorTxBit
        ///Error Rx Monitor
        member _.E2         =  errorRxBit

        //DummyBit
        ///PulseStart
        member _.PUL        = pulseBit
        ///Going Relay   //리셋 인과에 따라 필요
        member x.GR(src:Vertex) =
           let gr = createPlanVar s $"GR_{src.Name}" DuBOOL true v (VertexTag.goingrelay|>int):?> PlanVar<bool>
           goingRelays.Add gr |> ignore; gr

        member x.CreateTag(name) = createTag name


    type VertexMReal(v:Vertex) as this =
        inherit VertexManager(v)
        let mutable originInfo:OriginInfo = defaultOriginInfo (v:?> Real)
        let createTag name = this.CreateTag name
        let endPortBit    = createTag  "EP" VertexTag.endPort
        let resetPortBit  = createTag  "RP" VertexTag.resetPort
        let startPortBit  = createTag  "SP" VertexTag.startPort

        let relayRealBit      = createTag "RR" VertexTag.relayReal
        let realOriginAction  = createTag "RO" VertexTag.realOriginAction

        member x.OriginInfo
            with get() = originInfo
            and set(v) = originInfo <- v

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


    type VertexMCoin(v:Vertex)as this =
        inherit VertexManager(v)
        let s    = this.Storages
        let createTag name = this.CreateTag name
        let relayCallBit  = createTag  "CR" VertexTag.relayCall
        let sys = this.System

        let counterBit    = counter  s "CTR"  sys
        let timerOnDelayBit = timer  s "TON"  sys
        let timerTimeOutBit = timer  s "TOUT" sys

        ///CallDev Done Relay
        member _.CR     = relayCallBit

        ///Ring Counter
        member _.CTR    = counterBit
        ///Timer on delay
        member _.TON    = timerOnDelayBit
        ///Timer time out
        member _.TOUT   = timerTimeOutBit



