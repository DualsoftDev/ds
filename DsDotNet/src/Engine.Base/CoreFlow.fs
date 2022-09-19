// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreFlow =

    /// Flow Edge
    [<AbstractClass>]
    type Flow() =
        //엣지연결 리스트
        let edges = HashSet<IEdge>() 
        //엣지연결 없이 혼자있는 자식
        //ChildFlow 일 경우 : 인과처리 대상 DAG 의 Head 부분
        //RootFlow  일 경우 : 인과처리 아님 Spare
        let singles = HashSet<IVertex>() 
        interface IFlow with
            member _.Edges = edges
            member _.Nodes = edges.GetNodes() |> Seq.append singles

        member x.Edges = (x :> IFlow).Edges
        member x.AddEdge(edge) = edges.Add(edge)
        member x.RemoveEdge(edge) = edges.Remove(edge)
        member x.Nodes = (x :> IFlow).Nodes
        member x.Singles = singles
                                
         
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        let assignFlows = HashSet<IFlow>() 
        interface ICpu with
            member _.Name = name

        member x.CpuName = (x:> ICpu).Name
        member x.AssignFlows = assignFlows

    [<DebuggerDisplay("{name}")>]
    type RootFlow()  =
        inherit Flow()
    
    [<DebuggerDisplay("{name}")>]
    type ChildFlow()  =
        inherit Flow()

        


        