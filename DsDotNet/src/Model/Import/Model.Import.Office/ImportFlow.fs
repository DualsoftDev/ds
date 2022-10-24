// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Diagnostics
open System.Collections.Generic
open System.Linq
open Engine.Core

[<AutoOpen>]
module CoreFlow =

    /// Flow Edge
    [<AbstractClass>]
    type FlowBase(name) as this =
        inherit Name(name)
        //엣지연결 리스트
        let edges = HashSet<IEdge>() 
        //ChildFlow 일 경우 : 인과처리 대상 DAG 의 Head 부분
        //RootFlow  일 경우 : 인과처리 아님 Spare
        let singleNodes = HashSet<IVertex>() 
        let srcs = edges.GetStartCaual() |> Seq.map(fun edge -> edge.Source) |> Seq.distinct    
        let tgts = edges.GetStartCaual() |> Seq.map(fun edge -> edge.Target) |> Seq.distinct
    
        interface IFlow with
            member _.Edges = edges
            member _.Nodes = edges.GetNodes() |> Seq.append singleNodes |> Seq.distinct  

        member x.Nodes = (this :> IFlow).Nodes
        member x.Edges = (this :> IFlow).Edges
        member x.AddEdge(edge:IEdge)  = edges.Add(edge)
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
        member x.HeadNodes = (srcs |> Seq.except tgts) |> Seq.append singleNodes
        ///Flow Edge 연결상 끝점
        member x.TailNodes = (tgts |> Seq.except srcs) |> Seq.append singleNodes


    type RootFlow(name, system:SysBase) =
        inherit FlowBase(name)
        //do system.AddFlow(this) |> ignore

        member x.QualifiedName = $"{system.Name}.{name}";

        //Flow 내부에 사용된 모든 Node (ChildFlow 내부도 포함)
        member x.UsedSegs = x.Nodes |> Seq.cast<IActive> 
                                    |> Seq.collect(fun parent ->parent.Children)
                                    |> Seq.append x.Nodes
        
                

    type ChildFlow(name) as this =
        inherit FlowBase(name)
        let rec search(currNode:IVertex, isBack:bool) = 
               if(isBack)
               then this.PrevNodes(currNode) |> Seq.collect(fun node -> search(node, isBack))
               else this.NextNodes(currNode) |> Seq.collect(fun node -> search(node, isBack))
  
        member x.GetStartEdges() = this.Edges.GetStartCaual()
        member x.GetResetEdges() = this.Edges.GetResetCaual()
        
        member x.IsBackward(currNode:IVertex, queryNode:IVertex) =
               search(currNode, true).Contains(queryNode)
        member x.IsForward (currNode:IVertex, queryNode:IVertex) = 
               search(currNode, false).Contains(queryNode)
           