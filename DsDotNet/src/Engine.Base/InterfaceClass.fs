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

    /// Segment Container
    [<AbstractClass>]
    type SysBase(name)  =
        interface ISystem with
            member _.Name = name
       
    /// Real Segment
    [<AbstractClass>]
    type SegBase(active:VertexBase, sysBase:SysBase) =
        interface IActive with
            member _.Active : IVertex = active 
            member val Passives  =  HashSet<IVertex>() 

        member x.SysBase = sysBase

        member x.Vertex   = (x :> IActive).Active
        member x.Children = (x :> IActive).Passives

        member x.Add   (child:IVertex) = x.Children.Add(child) 
        member x.Remove(child:IVertex) = x.Children.Remove(child) 
    /// Call Segment
    and
        CallBase(call:VertexBase, parent:SegBase) =
        interface ICall with
            member val TXs  =  HashSet<IVertex>() 
            member val RXs  =  HashSet<IVertex>() 
   
    /// Segment Edge
    [<AbstractClass>]
    type EdgeBase(source:IVertex, target:IVertex , edgeCausal:EdgeCausal) =
        interface IEdge with
            member _.Source = source
            member _.Target = target
            member _.Causal = edgeCausal

        member x.ToText() = $"{source} {edgeCausal.ToText()} {target}";
