// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreClass =

   /// 사용자가 모델링을 통해서 만든 segment (SegEditor = User)
    [<DebuggerDisplay("{ToText()}")>]
    type Segment(name:string, baseSys:SysBase) as this =
            inherit SegBase(VertexBase(name),  baseSys)
            let mutable status4 = Status4.Homing
            let mEdges  = HashSet<DsEdge>()

            new (name, baseSys) = Segment (name, baseSys)
            
            member x.Name = name
            member x.Status4 = status4 
            member x.SetStatus(status:Status4) = 
                status4 <- status
                ChangeStatus(this, status)


     and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("{Source.ToText()}{Causal.ToText()}{Target.ToText()}")>]
        DsEdge(src:Segment, tgt:Segment, causal:EdgeCausal) =
            inherit EdgeBase(src.Vertex, tgt.Vertex, causal)
