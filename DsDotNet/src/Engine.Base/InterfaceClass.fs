// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System

[<AutoOpen>]
module InterfaceClass =

    // 이름이 필요한 객체
    [<AbstractClass>]
    type Named(name) =
        let mutable name = name
        interface INamed with
            member _.Name with get () = name and set (v) = name <- v

        member val Name : string = name with get, set
        member x.ValidName = NameUtil.GetValidName(name)
        abstract ToText : unit -> string
  
    /// 인과 연결가능 객체
    [<AbstractClass>]
    type VertexBase(name)  =
        inherit Named(name)
        interface IVertex  
        //사용 아직 안함(필요시 오픈)
        //    member _.ID  = Guid.NewGuid().ToString()
        //member x.ID  = (x:>IVertex).ID

        member val Alias : VertexBase option = None  with get, set
        member x.IsAlias = x.Alias.IsSome
     
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
    type SegBase(name:string, childFlow:IFlow) =
        inherit VertexBase(name)
        interface IActive with
            member _.Children  =  childFlow.Nodes

        member x.Children = (x :> IActive).Children

    /// Call Segment
    and
        [<AbstractClass>]
        CallBase(name:string) as this =
        inherit VertexBase(name)
        let txs = List<IVertex>() 
        let rxs = List<IVertex>() 
        interface ICall with
            member _.Node = this :> IVertex
            member _.TXs  = txs
            member _.RXs  = rxs

        member val TXs = txs with get,set
        member val RXs = rxs with get,set
  