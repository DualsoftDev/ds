// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open Engine.Core.CoreFlow
open Engine.Core

[<AutoOpen>]
module CoreClass =
   

    [<DebuggerDisplay("{ToText()}")>]
    type Segment(name:string, childFlow:ChildFlow)  =
        inherit Named(name)
        let mutable status4 = Status4.Homing
        interface IVertex 
        interface IActive with
            member _.Children  =  childFlow.Nodes


        member x.Children = (x :> IActive).Children
        member x.Name = name
        member val Status4 = status4 with get, set
        member x.ChildFlow = childFlow
   
   /// Modeled Edge : 사용자가 작성한 모델 상의 segment 간의 연결 edge (Wire)
    [<DebuggerDisplay("ToText()")>]
    type DsEdge(src:Segment, tgt:Segment, causal:EdgeCausal) =
        interface IEdge with
            member _.Source  = src
            member _.Target  = tgt
            member _.Causal  = causal
            member _.ToText()  = $"{src.Name}{causal.ToText()}{tgt.Name}"

        //inherit EdgeBase(src, tgt, causal)
        member x.Nodes = [src;tgt]
        member x.Source = src
        member x.Target = tgt
        member x.Causal = causal
