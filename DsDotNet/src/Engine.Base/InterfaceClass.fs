// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

[<AutoOpen>]
module InterfaceClass =

    [<AbstractClass>]
    type Named(name)  =
        interface INamed with
            member _.Name: string = name
        member   x.ToText() = $"{name}[{x.GetType().Name}]"

        /// Segment Container
    [<AbstractClass>]
    type SystemBase(name)  =
        interface ISystem with
            member _.Name: string = name

        member x.Name = (x :> ISystem).Name
       
    /// Segment Vertex
    [<AbstractClass>]
    type SegBase(name,  baseSystem:SystemBase) =
        interface IVertex with
            member _.Name: string = name

        member x.Name = (x :> IVertex).Name
        member x.BaseSys = baseSystem
     
    /// Segment Edge
    [<AbstractClass>]
    type EdgeBase(src:SegBase, tgt:SegBase, edgeCausal:EdgeCausal) =
        interface IEdge with
            member _.SourceVertex: IVertex = src :> IVertex
            member _.TargetVertex: IVertex = tgt :> IVertex

        member x.Causal = edgeCausal
        member x.Nodes = [src;tgt]
        member x.SourceVertex = src
        member x.TargetVertex = tgt
        