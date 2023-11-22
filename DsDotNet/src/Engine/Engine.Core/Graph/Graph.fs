// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module GraphModule =
    type INamedVertex =
        inherit IVertex
        inherit INamed

    type IEdge<'V> =
        abstract Source :'V    //방향을 고려안한 위치상 왼쪽   Vertex
        abstract Target :'V    //방향을 고려안한 위치상 오른쪽 Vertex
        abstract EdgeType  :EdgeType


    [<AbstractClass>]
    type EdgeBase<'V>(source:'V, target:'V, edgeType:EdgeType) =
        interface IEdge<'V> with
            member x.Source = x.Source
            member x.Target = x.Target
            member x.EdgeType  = x.EdgeType

        member _.Source = source
        member _.Target = target
        member _.EdgeType = edgeType

    type Graph<'V, 'E
            when 'V :> INamed and 'V : equality
            and 'E :> IEdge<'V> and 'E: equality> (
            vertices_:'V seq, edges_:'E seq) =
        let edgeComparer = {
            new IEqualityComparer<'E> with
                member _.Equals(x:'E, y:'E) = x.Source = y.Source && x.Target = y.Target && x.EdgeType = y.EdgeType
                member _.GetHashCode(x) = x.Source.GetHashCode()/2 + x.Target.GetHashCode()/2
        }
        let vertices = vertices_ @ edges_.Collect(fun e -> [e.Source; e.Target]).Distinct()
        let vs = new HashSet<'V>(vertices, nameComparer<'V>())
        let es = new HashSet<'E>(edges_, edgeComparer)
        new () = Graph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>)
        member _.Vertices = vs
        member _.Edges = es
        /// 중복 edge 삽입은 허용되지 않으나, 중복된 항목이 존재하면 이를 무시하고 false 를 반환.  중복이 없으면 true 반환
        member _.AddEdges(edges:'E seq) =
            edges
            |> Seq.forall(fun e ->
                [ e.Source; e.Target ]
                |> Seq.filter(fun v -> not <| vs.Contains(v))
                |> Seq.iter(fun v -> vs.Add(v) |> ignore)
                es.Add(e))  // |> verifyMessage $"Duplicated edge [{e.Source.Name} -> {e.Target.Name}]"

        member _.RemoveEdges(edges:'E seq)       = edges    |> Seq.forall es.Remove
        member _.AddVertices(vertices:'V seq)    = vertices |> Seq.forall vs.Add
        member _.RemoveVertices(vertices:'V seq) = vertices |> Seq.forall vs.Remove
        member x.AddEdge(edge:'E)        = x.AddEdges([edge])
        member x.RemoveEdge(edge:'E)     = x.RemoveEdges([edge])
        member x.AddVertex(vertex:'V)    = x.AddVertices([vertex])
        member x.RemoveVertex(vertex:'V) = x.RemoveVertices([vertex])
        member _.TryFindVertex(name:string) = vs |> Seq.tryFind(fun v -> v.Name = name)
        member x.TryFindVertex<'T>(name:string) = x.TryFindVertex(name).Filter(isType<'T>).Map(forceCast<'T>)
        member _.FindVertex(name:string) = vs.FirstOrDefault(fun v -> v.Name = name)
        member _.FindEdges(source:string, target:string) = es.Where(fun e -> e.Source.Name = source && e.Target.Name = target)
        member _.FindEdges(source:'V, target:'V) = es.Where(fun e -> e.Source = source && e.Target = target)

        member private x.ConnectedVertices = x.Edges |> Seq.collect(fun e -> [e.Source; e.Target]) |> Seq.distinct
        member x.Islands = x.Vertices.Except(x.ConnectedVertices)
        member x.GetIncomingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Target = vertex)
        member x.GetOutgoingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Source = vertex )
        member x.GetEdges(vertex:'V) = x.GetIncomingEdges(vertex).Concat(x.GetOutgoingEdges(vertex))
        member x.GetIncomingVertices(vertex:'V) = x.GetIncomingEdges(vertex).Select(fun e -> e.Source)
        member x.GetOutgoingVertices(vertex:'V) = x.GetOutgoingEdges(vertex).Select(fun e -> e.Target)
        member x.Inits =
            let inits =
                x.Edges.Select(fun e -> e.Source)
                    .Where(fun src -> not <| x.GetIncomingEdges(src).Any())
                    .Distinct()
            x.Islands @ inits
        member x.Lasts =
            let lasts =
                x.Edges.Select(fun e -> e.Target)
                    .Where(fun tgt -> not <| x.GetOutgoingEdges(tgt).Any())
                    .Distinct()
            x.Islands @ lasts


