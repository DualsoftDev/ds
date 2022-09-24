// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open Engine.Core.CoreFlow
open Engine.Core

[<AutoOpen>]
module CoreEdge =

    /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
    [<DebuggerDisplay("{Source.ToText()}{Causal.ToText()}{Target.ToText()}")>]
    type DsEdge(src:SegmentBase, tgt:SegmentBase, causal:EdgeCausal) as this =
        inherit EdgeBase(src, tgt, causal)
        member x.Nodes = (this:>EdgeBase).Nodes
        member x.Source = src
        member x.Target = tgt
        member x.Causal = causal

    type Edge(flow:Flow, srcs:IVertex seq, op:string, tgt:IVertex) =
        member x.Flow = flow
        member x.Sources = srcs
        member x.Target = tgt
        member x.Operator = op
        member x.GetDsEdges() = 
                    let causal = EdgeCausalType(op)
                    let target = tgt:?>SegmentBase
                    srcs |> Seq.map(fun src -> DsEdge(src:?>SegmentBase, target, causal))

    type WeakResetEdge(flow:Flow, srcs:IVertex seq, op:string, tgt:IVertex) =
        inherit Edge(flow, srcs, op, tgt)
    type WeakSetEdge(flow:Flow, srcs:IVertex seq, op:string, tgt:IVertex) =
        inherit Edge(flow, srcs, op, tgt)
    type StrongResetEdge(flow:Flow, srcs:IVertex seq, op:string, tgt:IVertex) =
        inherit Edge(flow, srcs, op, tgt)
    type StrongSetEdge(flow:Flow, srcs:IVertex seq, op:string, tgt:IVertex) =
        inherit Edge(flow, srcs, op, tgt)
