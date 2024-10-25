namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCpuVertex =

    type Vertex with
        member v.V  = v.TagManager :?> VertexTagManager
        member v.VC = v.TagManager :?> CoinVertexTagManager
        member v.VR = v.TagManager :?> RealVertexTagManager
        member v._on  = v.Parent.GetSystem()._on
        member v._off = v.Parent.GetSystem()._off
        member v.Flow = v.Parent.GetFlow()
        member v.MutualResetCoins =
            let mts = v.Parent.GetSystem().S.MutualCalls
            match v with
            | :? Call as c 
                -> if c.IsJob then mts[v]
                              else []
            | _ -> mts[v]

    type VariableData with
        member v.VM = v.TagManager :?> VariableManager

    type ActionVariable  with
        member av.VM = av.TagManager :?> ActionVariableManager


    type Real with
        member r.V = r.TagManager :?> RealVertexTagManager

        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.RealToken:uint32 = r.V.GetVertexTag(VertexTag.realToken).BoxedValue :?> uint32
        /// 실제 Token >= 1.   Token 없을 경우, 0 return.
        // 원래 token 없으면 null 을 반환해야 함!!
        member r.MergeToken:uint32 = r.V.GetVertexTag(VertexTag.mergeToken).BoxedValue :?> uint32

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

        member r.Errors     = r.ErrOnTimeOvers  @ r.ErrOnTimeUnders
                            @ r.ErrOffTimeOvers @ r.ErrOffTimeUnders
                            @ r.ErrOpens @ r.ErrShorts  @ [ r.VR.ErrGoingOrigin  ]

[<Extension>]
type RealExt =
    [<Extension>]
    static member GetRealToken(r:Real):uint32 option = match r.RealToken with | 0u -> None | v -> Some v
    [<Extension>]
    static member GetMergeToken(r:Real):uint32 option = match r.MergeToken with | 0u -> None | v -> Some v

