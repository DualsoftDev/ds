// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

[<AutoOpen>]
module InterfaceClass =

    // 이름이 필요한 객체
    [<AbstractClass>]
    type Named(name)  =
        interface INamed with
            member _.Name = name
        member   x.ToText() = $"{name}[{x.GetType().Name}]"
  
    /// Segment Container
    [<AbstractClass>]
    type SysBase(name)  =
        interface ISystem with
            member _.Name = name
       
    /// Segment
    [<AbstractClass>]
    type SegBase(active:IVertex, passives:IVertex seq, baseSys:SysBase) =
        interface IActive with
            member x.Active  : IVertex     = active 
            member x.Passives: IVertex seq = passives

        member x.Vertex = (x :> IActive).Active
        member x.BaseSys = baseSys
     
    /// Segment Edge
    [<AbstractClass>]
    type EdgeBase(srcs:SegBase seq, tgt:SegBase) =
        interface IEdge with
            member _.SourceVertexes = srcs |> Seq.map(fun src -> src.Vertex)
            member _.TargetVertex   = tgt.Vertex

        member x.Sources = srcs
        member x.Target = tgt
        
    /// Causal Arrow
    [<AbstractClass>]
    type CausalBase(source, target, edgeCausal:EdgeCausal) =
        member x.Source:SegBase = source
        member x.Target:SegBase = target
        member x.Nodes = [source;target]
        member x.Causal:EdgeCausal = edgeCausal
        member x.IsReset = edgeCausal.IsReset
        member x.ToText() = $"{source} {edgeCausal.ToText()} {target}";
