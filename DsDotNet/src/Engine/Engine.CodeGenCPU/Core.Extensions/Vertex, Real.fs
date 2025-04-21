namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =

    let getTime  (time:uint32 option, nameFqdn:string, mode:RuntimeMode)=
        let maxShortSpeedMSec =TimerModule.MinTickInterval|>float
        let v =
            time |> bind(fun t ->
                if mode.IsVirtualMode() then
                    match RuntimeDS.Param.TimeSimutionMode  with
                    | TimeSimutionMode.TimeNone -> None
                    | TimeSimutionMode.TimeX1 ->   Some ((t|>float)* 1.0/1.0 )
                    | TimeSimutionMode.TimeX2 ->   Some ((t|>float)* 1.0/2.0 )
                    | TimeSimutionMode.TimeX4 ->   Some ((t|>float)* 1.0/4.0 )
                    | TimeSimutionMode.TimeX8 ->   Some ((t|>float)* 1.0/8.0 )
                    | TimeSimutionMode.TimeX16 ->  Some ((t|>float)* 1.0/16.0 )
                    | TimeSimutionMode.TimeX100 -> Some ((t|>float)* 1.0/100.0 )
                    | TimeSimutionMode.TimeX0_1 -> Some ((t|>float)* 1.0/0.1 )
                    | TimeSimutionMode.TimeX0_5 -> Some ((t|>float)* 1.0/0.5 )
                else
                    Some (t|>float)
                    )

        if v.IsSome && v.Value < maxShortSpeedMSec then
            failwithf $"시뮬레이션 배속을 재설정 하세요.현재설정({RuntimeDS.Param.TimeSimutionMode}) {nameFqdn}
                        \r\n[최소동작시간 : {maxShortSpeedMSec}, 배속반영 동작 시간 : {v.Value}]"
        else
            v

    type Vertex with
        member v.V  = v.TagManager :?> VertexTagManager
        member v.VC = v.TagManager :?> CoinVertexTagManager
        member v.VR = v.TagManager :?> RealVertexTagManager
        member v._on  = v.Parent.GetSystem()._on
        member v._off = v.Parent.GetSystem()._off
        member v.Flow = v.Parent.GetFlow()

    type VariableData with
        member v.VM = v.TagManager :?> VariableManager

    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager


    type Real with
        member r.V = r.TagManager :?> RealVertexTagManager

        member x.TimeAvg = getTime (x.DsTime.AVG, x.QualifiedName, x.V.RuntimeMode)
        member x.TimeAvgExist = x.TimeAvg.IsSome && x.TimeAvg.Value <> 0.0
        member x.TimeSimMsec =
            if x.TimeAvg.IsNone then 0u
            else
                x.TimeAvg.Value|>uint32


        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.RealToken:uint32 = r.V.GetVertexTag(VertexTag.realToken).BoxedValue :?> uint32
        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.MergeToken:uint32 = r.V.GetVertexTag(VertexTag.mergeToken).BoxedValue :?> uint32
        member r.SourceToken:uint32 = r.V.GetVertexTag(VertexTag.sourceToken).BoxedValue :?> uint32

        member r.CoinSTContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ST)
        member r.CoinRTContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.RT)
        member r.CoinETContacts = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ET)
        member r.CoinAllContacts = r.Graph.Vertices.Select(getVMCall)|>Seq.collect(fun f->[f.ST;f.RT;f.ET])

        member r.CoinAlloffExpr = !@r.V.CoinAnyOnST.Expr <&&> !@r.V.CoinAnyOnRT.Expr <&&> !@r.V.CoinAnyOnET.Expr

        member r.ErrOnTimeOvers   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOnTimeOver)
        member r.ErrOnTimeUnders   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOnTimeUnder)

        member r.ErrOffTimeOvers   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOffTimeOver)
        member r.ErrOffTimeUnders   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOffTimeUnder)

        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrOpen)
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrShort)
        member r.ErrInterlock   = r.Graph.Vertices.Select(getVMCall).Select(fun f->f.ErrInterlock)

        member r.Errors     = r.ErrOnTimeOvers  @ r.ErrOnTimeUnders
                            @ r.ErrOffTimeOvers @ r.ErrOffTimeUnders
                            @ r.ErrOpens @ r.ErrShorts 
                            @ r.ErrInterlock 
                            @ [ r.VR.ErrGoingOrigin  ]

[<Extension>]
type RealExt =
    [<Extension>]
    static member GetRealToken(r:Real):uint32 option = match r.RealToken with | 0u -> None | v -> Some v
    [<Extension>]
    static member GetMergeToken(r:Real):uint32 option = match r.MergeToken with | 0u -> None | v -> Some v
    [<Extension>]
    static member GetSourceToken(r:Real):uint32 option = match r.SourceToken with | 0u -> None | v -> Some v

