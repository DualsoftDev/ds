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
        let srcs = edges |> Seq.map(fun edge -> edge.Source) |> Seq.distinct    
        let tgts = edges |> Seq.map(fun edge -> edge.Target) |> Seq.distinct

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

        ///Start Edge 기준으로 다음 Vertex 들을 찾음
        member x.NextNodes(currNode:IVertex) = edges.GetNextNodes(currNode)
        ///Start Edge 기준으로 이전 Vertex 들을 찾음
        member x.PrevNodes(currNode:IVertex) = edges.GetPrevNodes(currNode)
        ///Flow Edge 연결상 시작점
        member x.HeadNodes = srcs |> Seq.except tgts
        ///Flow Edge 연결상 끝점
        member x.TailNodes = tgts |> Seq.except srcs
       
         
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
        //Flow 내부에 사용된 모든 Node (ChildFlow 내부도 포함)
        member x.UsedSegs = x.Nodes |> Seq.cast<IActive> 
                                    |> Seq.collect(fun parent ->parent.Children)
                                    |> Seq.append x.Nodes
    
    [<DebuggerDisplay("{name}")>]
    type ChildFlow(name) as this =
        inherit Flow(name)
  
        member x.GetStartEdges() = this.Edges.GetStartCaual()
        member x.GetResetEdges() = this.Edges.GetResetCaual()
        
        member x.IsBackward(a:IVertex, b:IVertex) = Some(true)  
        member x.IsForward (a:IVertex, b:IVertex) = Some(true)  
           