// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open System
open System.Linq
open Engine.Common.FS

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
    | Default       = 0b0000000    // Start, Weak
    | Reset         = 0b0000001    // else start
    | Strong        = 0b0000010    // else weak
    | Reversed      = 0b0000100    // direction reversed : <, <|, <||, etc
    | Bidirectional = 0b0001000    // 양방향.  <||>

    [<AbstractClass>]
    type EdgeBase<'T>(source:'T, target:'T, edgeType:EdgeType) =
        interface IEdge<'T> with
            member x.Source = x.Source
            member x.Target = x.Target
        member _.Source = source
        member _.Target = target
        member val EdgeType = edgeType

    type ICoin =
        inherit IChildVertex

    type Graph<'V, 'E
        when 'V :> INamed and 'V : equality        
        and 'E :> IEdge<'V> and 'E: equality> (
            vertices:'V seq, edges:'E seq) =
        let edgeComparer = {
            new IEqualityComparer<'E> with
                member _.Equals(x:'E, y:'E) = x.Source = y.Source && x.Target = y.Target
                member _.GetHashCode(x) = x.GetHashCode()
        }
        let vs = new HashSet<'V>(vertices, nameComparer<'V>())
        let es = new HashSet<'E>(edges, edgeComparer)
        new () = Graph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>)
        member _.Vertices = vs
        member _.Edges = es
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
        member x.FindVertex(name:string) = vs.FirstOrDefault(fun v -> v.Name = name)

        member private x.ConnectedVertices = x.Edges |> Seq.collect(fun e -> [e.Source; e.Target]) |> Seq.distinct
        member x.Islands = x.Vertices.Except(x.ConnectedVertices)
        member x.GetIncomingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Target = vertex)
        member x.GetOutgoingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Source = vertex)
        member x.GetEdges(vertex:'V) = x.GetIncomingEdges(vertex).Concat(x.GetOutgoingEdges(vertex))
        member x.GetIncomingVertices(vertex:'V) = x.GetIncomingEdges(vertex).Select(fun e -> e.Source)
        member x.GetOutgoingVertices(vertex:'V) = x.GetOutgoingEdges(vertex).Select(fun e -> e.Target)
        member x.Inits =
            let inits =
                x.Edges.Select(fun e -> e.Source)
                    .Distinct()
                    .Where(fun src -> not <| x.GetIncomingEdges(src).Any())
            x.Islands.Concat(inits)
        member x.Lasts =
            let lasts =
                x.Edges.Select(fun e -> e.Target)
                    .Distinct()
                    .Where(fun tgt -> not <| x.GetOutgoingEdges(tgt).Any())
            x.Islands.Concat(lasts)

[<AutoOpen>]
module internal GraphHelperModule =
    let dumpGraph(graph:Graph<_, _>) =
        let g = graph
        let text =
            [   for e in g.Edges do
                    e.ToString()
                for i in g.Islands do
                    match box i with
                    | :? IQualifiedNamed as v -> v.QualifiedName
                    | _ -> i.Name
            ] |> String.concat "\r\n"

        if text.Any() then
            logDebug "%s" text

        text

    let groupDuplexEdges(graph:Graph<'V, 'E>) =
        let duplexEdgeComparer = {
            new IEqualityComparer<'V*'V> with
            member _.Equals(x:'V*'V, y:'V*'V) =
                let s1, t1 = x
                let s2, t2 = y
                (s1 = s2 && t1 = t2) || (s1 = t2 && t1 = s2)
            member _.GetHashCode(x:'V*'V) = //0//x.It   x.Average(fun s -> s.GetHashCode()) |> int
                let s, t = x
                s.GetHashCode()/2 + t.GetHashCode()/2
        }

        let duplexDic = Dictionary<'V*'V, HashSet<'E>>(duplexEdgeComparer)

        let g = graph
        for e in g.Edges do
            let key = (e.Source, e.Target)
            if not <| duplexDic.ContainsKey(key) then
                duplexDic.Add(key, HashSet<'E>())
            duplexDic[key].Add(e) |> verify

        duplexDic




[<Extension>]
type GraphHelper =
    [<Extension>] static member Dump(graph:Graph<_, _>) = dumpGraph(graph)
    [<Extension>]
    static member ToText(edgeType:EdgeType) =
        let t = edgeType
        if t.HasFlag(EdgeType.Reset) then
            if t.HasFlag(EdgeType.Strong) then
                if t.HasFlag(EdgeType.Bidirectional) then
                    "<||>"
                elif t.HasFlag(EdgeType.Reversed) then
                    "<||"
                else
                    "||>"
            else
                if t.HasFlag(EdgeType.Bidirectional) then
                    "<|>"
                elif t.HasFlag(EdgeType.Reversed) then
                    "<|"
                else
                    "|>"
        else
            if t.HasFlag(EdgeType.Bidirectional) then
                failwith "ERROR"
            if t.HasFlag(EdgeType.Strong) then
                if t.HasFlag(EdgeType.Reversed) then
                    "<<"
                else
                    ">>"
            else
                if t.HasFlag(EdgeType.Reversed) then
                    "<"
                else
                    ">"




