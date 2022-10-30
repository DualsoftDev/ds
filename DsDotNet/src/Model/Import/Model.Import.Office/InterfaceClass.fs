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

    
 ///인과의 노드 종류
    type NodeType =
        | MY            //실제 나의 시스템 1 bit
        | TR            //지시관찰 TX RX 
        | TX            //지시만
        | RX            //관찰만
        | IF            //인터페이스
        | COPY          //시스템복사 
        | DUMMY         //그룹더미 
        | BUTTON        //버튼 emg,start, ...
        with
            member x.IsReal =   match x with
                                |MY  -> true
                                |_ -> false
            member x.IsCall =   match x with
                                |TR |TX |RX -> true
                                |_ -> false

            member x.IsRealorCall =  x.IsReal || x.IsCall 
    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //이   MFlow        내부 행위정의
        | OtherFlow        //다른 MFlow     에서 행위 가져옴
        | ExBtn            //버튼(call) 가져옴

