// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreFlow =

    /// Flow Edge
    [<AbstractClass>]
    type Flow(cpu:ICpu) =
        //엣지연결 리스트
        let edges = HashSet<IEdge>() 
        //엣지연결 없이 혼자있는 자식
        //ChildFlow 일 경우 : 인과처리 대상 DAG 의 Head 부분
        //RootFlow  일 경우 : 인과처리 아님 Spare
        let singles = HashSet<IVertex>() 
        interface IFlow with
            member _.Edges = edges

        member x.Edges = (x :> IFlow).Edges
        member x.Singles = singles
        member x.Cpu = cpu
        member x.GetNodes() = x.Edges.GetNodes() 
                                |> Seq.append singles
         
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        interface ICpu with
            member _.Name = name

        member x.CpuName = (x:> ICpu).Name

    [<DebuggerDisplay("{name}")>]
    type RootFlow(cpu:DsCpu)  =
        inherit Flow(cpu)
    
    [<DebuggerDisplay("{name}")>]
    type ChildFlow(cpu:DsCpu)  =
        inherit Flow(cpu)

        


        