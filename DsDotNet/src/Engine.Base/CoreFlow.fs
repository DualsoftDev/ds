// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreFlow =

    /// Flow Edge
    [<AbstractClass>]
    type Flow(name) =
        inherit Named(name)
        //엣지연결 리스트
        let edges = HashSet<IEdge>() 
        //ChildFlow 일 경우 : 인과처리 대상 DAG 의 Head 부분
        //RootFlow  일 경우 : 인과처리 아님 Spare
        let singleNodes = HashSet<IVertex>() 
        interface IFlow with
            member _.Edges = edges
            member _.Nodes = edges.GetNodes() |> Seq.append singleNodes 

        member x.Nodes = (x :> IFlow).Nodes
        member x.Edges = (x :> IFlow).Edges
        member x.AddEdge(edge) = edges.Add(edge)
        member x.RemoveEdge(edge) = edges.Remove(edge)

        member x.Singles = singleNodes
        //Add    singleNodes
        member x.AddSingleNode(node)    = singleNodes.Add(node)
        //Remove singleNodes
        member x.RemoveSingleNode(node) = singleNodes.Remove(node)


         
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        let assignFlows = HashSet<IFlow>() 
        interface ICpu with
            member _.Name = name

        member x.CpuName = (x:> ICpu).Name
        member x.AssignFlows = assignFlows


    [<DebuggerDisplay("{name}")>]
    type RootFlow(name)  =
        inherit Flow(name)
        member x.UsedSegs = x.Nodes |> Seq.cast<IActive> 
                                    |> Seq.collect(fun parent ->parent.Children)
                                    |> Seq.append x.Nodes
    
    [<DebuggerDisplay("{name}")>]
    type ChildFlow(name) as this =
        inherit Flow(name)
        let srcs = this.Edges |> Seq.map(fun edge -> edge.Source) |> Seq.distinct    
        let tgts = this.Edges |> Seq.map(fun edge -> edge.Target) |> Seq.distinct

        member x.HeadNodes = srcs |> Seq.except tgts
        member x.TailNodes = tgts |> Seq.except srcs
        member x.NextNodes(currNode:IVertex) = this.Edges.GetNextNodes(currNode)
        member x.PrevNodes(currNode:IVertex) = this.Edges.GetPrevNodes(currNode)
             
        