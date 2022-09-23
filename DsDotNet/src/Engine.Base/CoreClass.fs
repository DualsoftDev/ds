// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic
open Engine.Core.CoreFlow
open Engine.Core

[<AutoOpen>]
module CoreClass =

    /// Real segment 
    [<DebuggerDisplay("{ToText()}")>]
    type Segment(name:string, childFlow:ChildFlow)  =
        inherit SegBase(name,  childFlow)
        let mutable status4 = Status4.Homing
            
        override x.ToText() = childFlow.QualifiedName
        member x.Name = name
        member val Status4 = status4 with get, set
       
        member x.ChildFlow = childFlow
        member x.RootFlow = childFlow.ContainerFlow
      
    
    /// Call segment 
    [<DebuggerDisplay("{ToText()}")>]
    type CallSeg(name:string, parent:Segment) =
        inherit CallBase(name,  parent)
        let mutable status4 = Status4.Homing

        override x.ToText() = $"{parent.RootFlow.QualifiedName}.{name}({parent.Name})"
            
        member x.Name = name
        member val Status4 = status4 with get, set
            
     and
        /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
        [<DebuggerDisplay("{Source.ToText()}{Causal.ToText()}{Target.ToText()}")>]
        DsEdge(src:Segment, tgt:Segment, causal:EdgeCausal) as this =
            inherit EdgeBase(src, tgt, causal)
            member x.Nodes = (this:>EdgeBase).Nodes
            member x.Source = src
            member x.Target = tgt
     