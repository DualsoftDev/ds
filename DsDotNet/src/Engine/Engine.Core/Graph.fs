// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System
open System.Linq

[<AutoOpen>]
module GraphModule =
    type INamedVertex =
        inherit IVertex
        inherit INamed
    type IEdge<'V> =
        abstract Source  :'V 
        abstract Target  :'V

    /// vertex on a flow
    type IFlowVertex =
        inherit INamedVertex

    /// vertex on a segment
    type IChildVertex =
        inherit INamedVertex

    [<Flags>]
    type EdgeType =
    | Default = 0b0000000    // Start, Weak
    | Reset   = 0b0000001    // else start
    | Strong  = 0b0000010    // else weak

    [<AbstractClass>]
    type EdgeBase<'T>(source:'T, target:'T, edgeType:EdgeType) =
        interface IEdge<'T> with
            member _.Source = source
            member _.Target = target
        member val EdgeType = EdgeType.Default with get, set

    type InFlowEdge(source:IFlowVertex, target:IFlowVertex, edgeType:EdgeType) =
        inherit EdgeBase<IFlowVertex>(source, target, edgeType)

    type InSegmentEdge(source:IChildVertex, target:IChildVertex, edgeType:EdgeType) =
        inherit EdgeBase<IChildVertex>(source, target, edgeType)
    
    type ICoin =
        inherit IChildVertex

        
    type Graph<'V, 'E
        when 'V : equality
        and 'E: equality
        and 'E :> IEdge<'V>>(
            vertices:'V seq, edges:'E seq) =
        let edgeComparer = {
            new IEqualityComparer<'E> with
                member _.Equals(x:'E, y:'E) = x.Source = y.Source && x.Target = y.Target
                member _.GetHashCode(x) = x.GetHashCode()
        }
        let vs = vertices.ToHashSet()
        let es = new HashSet<'E>(edges, edgeComparer)
        new () = Graph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>)
        member _.Vertices = vs.ToArray()
        member _.Edges = es.ToArray()
        member _.AddEdges(edges:'E seq) =
            edges
            |> Seq.forall(fun e ->
                [ e.Source; e.Target ]
                |> Seq.filter(fun v -> not <| vs.Contains(v))
                |> Seq.iter(fun v -> vs.Add(v) |> ignore)
                es.Add(e))
        member _.RemoveEdges(edges:'E seq)       = edges    |> Seq.forall es.Remove
        member _.AddVertices(vertices:'V seq)    = vertices |> Seq.forall vs.Add
        member _.RemoveVertices(vertices:'V seq) = vertices |> Seq.forall vs.Remove
        member x.AddEdge(edge:'E)        = x.AddEdges([edge])
        member x.RemoveEdge(edge:'E)     = x.RemoveEdges([edge])
        member x.AddVertex(vertex:'V)    = x.AddVertices([vertex])
        member x.RemoveVertex(vertex:'V) = x.RemoveVertices([vertex])

