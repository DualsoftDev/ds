// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic

[<AutoOpen>]
module InterfaceClass =

    // 이름이 필요한 객체
    type Named(name)  =
        interface INamed with
            member _.Name = name
        member   x.ToText() = $"{name}[{x.GetType().Name}]"
  
    /// 인과 연결가능 객체
    type VertexBase(name)  =
        interface IVertex with
            member _.Name = name
     
    /// Segment Edge
    [<AbstractClass>]
    type EdgeBase(source:IVertex, target:IVertex , edgeCausal:EdgeCausal) =
        interface IEdge with
            member _.Source = source
            member _.Target = target
            member _.Causal = edgeCausal

        member x.ToText() = $"{source} {edgeCausal.ToText()} {target}";


    /// Segment Container
    [<AbstractClass>]
    type SysBase(name)  =
        let rootFlows = HashSet<IFlow>() 
        interface ISystem with
            member _.Name = name

        member x.RootFlows = rootFlows
       
    /// Real Segment
    [<AbstractClass>]
    type SegBase(vertex:VertexBase, childFlow:IFlow) =
        interface IActive with
            member _.Children  = 
                childFlow.Edges
                |> Seq.collect (fun edge -> [edge.Source;edge.Target])     

        member x.Vertex = vertex
        member x.Children = (x :> IActive).Children

    /// Call Segment
    and
        [<AbstractClass>]
        CallBase(call:VertexBase, parent:SegBase) =
        let txs = HashSet<IVertex>() 
        let rxs = HashSet<IVertex>() 
        interface ICall with
            member _.Node = call
            member _.TXs  = txs
            member _.RXs  = rxs

        member x.TXs = (x :>ICall).TXs
        member x.RXs = (x :>ICall).RXs
  