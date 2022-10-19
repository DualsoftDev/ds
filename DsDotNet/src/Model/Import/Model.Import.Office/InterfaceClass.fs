// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module InterfaceClass =

    type IEdge = 
        abstract Source  :IVertex 
        abstract Target  :IVertex
        abstract Causal  :EdgeType
        abstract ToText  :unit -> string

    type IFlow   =
        abstract Edges:IEdge seq
        abstract Nodes:IVertex seq

    type ICall = 
        abstract Node :IVertex  
        abstract TXs  :IVertex seq 
        abstract RXs  :IVertex seq

    type IActive =
        abstract Children:IVertex seq

    type ISystem    = 
        abstract Flows:IFlow seq
    // 이름이 필요한 객체
    [<AbstractClass>]
    type Name(name) =
        let mutable name = name
        interface INamed with
            member _.Name with get () = name

        member val Name : string = name with get
        member x.ValidName = NameUtil.QuoteOnDemand(name)
     
    /// Segment Container
    [<AbstractClass>]
    type SysBase(name)  =
        inherit Name(name)
        let rootFlows = HashSet<IFlow>() 
        interface ISystem with
            member _.Flows = rootFlows

        abstract Add : IFlow -> bool

 
    /// Call Segment
    type MInterface(name:string, txs:IVertex seq , rxs:IVertex seq ) as this =
        inherit Name(name)

        interface IVertex  
        interface ICall with
            member _.Node = this :> IVertex
            member _.TXs  = txs
            member _.RXs  = rxs

        member x.TXs = txs 
        member x.RXs = rxs 

        member val Alias : IVertex option = None  with get, set
        member x.IsAlias = x.Alias.IsSome
        member x.Name = name
