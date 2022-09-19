// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic

[<AutoOpen>]
module InterfaceClass =

    // 이름이 필요한 객체
    type Named(name)  =
        interface INamed with
            member _.Name = name
        member x.ToText() = $"{name}[{x.GetType().Name}]"
        member x.ValidName = NameUtil.GetValidName(name)
  
    /// 인과 연결가능 객체
    type VertexBase(name)  =
        interface IVertex with
            member _.Name = name
        member x.Name = (x:>IVertex).Name
     
    /// Segment Edge
    [<AbstractClass>]
    type EdgeBase(source:IVertex, target:IVertex , edgeCausal:EdgeCausal) =
        interface IEdge with
            member _.ToText() = $"{source} {edgeCausal.ToText()} {target}";
            member _.Source   = source
            member _.Target   = target
            member _.Causal   = edgeCausal

        member x.Nodes = [source;target]
        member x.ToText() = (x:>IEdge).ToText()


    /// Segment Container
    [<AbstractClass>]
    type SysBase(name)  =
        inherit Named(name)
        let rootFlows = HashSet<IFlow>() 
        interface ISystem with
            member _.Flows = rootFlows

        member x.RootFlows = (x:>ISystem).Flows
       
    /// Real Segment
    [<AbstractClass>]
    type SegBase(vertex:VertexBase, childFlow:IFlow) =
        interface IActive with
            member _.Children  =  childFlow.Nodes
        interface IVertex with
            member this.Name: string = vertex.Name

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
  