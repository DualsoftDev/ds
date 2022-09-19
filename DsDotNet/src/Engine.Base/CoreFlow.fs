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
        //ChildFlow 일 경우 : 인과처리 대상 DAG 의 Head 부분
        //RootFlow  일 경우 : 인과처리 아님 Spare
        let singleNodes = HashSet<IVertex>() 
        interface IFlow with
            member _.Edges = edges
            member _.Nodes = edges.GetNodes() |> Seq.append singleNodes 

        member x.Edges = (x :> IFlow).Edges
        member x.AddEdge(edge) = edges.Add(edge)
        member x.RemoveEdge(edge) = edges.Remove(edge)

        //Add    인과처리 대상 DAG 의 Head 부분
        member x.AddSingleNode(node)    = singleNodes.Add(node)
        //Remove 인과처리 대상 DAG 의 Head 부분
        member x.RemoveSingleNode(node) = singleNodes.Remove(node)
        member x.Singles = singleNodes
        member x.Nodes = (x :>IFlow).Nodes


         
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        let assignFlows = HashSet<IFlow>() 
        interface ICpu with
            member _.Name = name

        member x.CpuName = (x:> ICpu).Name
        member x.AssignFlows = assignFlows


    [<DebuggerDisplay("{name}")>]
    type RootFlow(name)  =
        inherit Flow()
        member x.FlowName = name
        member x.ValidName = NameUtil.GetValidName(name)
    
    [<DebuggerDisplay("{name}")>]
    type ChildFlow()  =
        inherit Flow()
        
        