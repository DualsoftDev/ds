namespace Engine.Graph

open System
open System.Collections.Generic
open QuickGraph
open QuickGraph.Algorithms
open Engine.Core

[<AutoOpen>]
module GraphBase =
    type V = Engine.Core.IVertex
    type E<'V> = QuickGraph.IEdge<'V>

[<AutoOpen>]
module GraphUtil =
    /// g 상에서 source 에서 target 으로 가는 최단 경로 구함.  edge 의 array 반환
    let computeDijkstra (g:IVertexAndEdgeListGraph<'v, 'e>) (source:'v) (target: 'v) =

        // https://github.com/eosfor/Quickgraph.Wiki/blob/master/Shortest-Path.md
        let tryGetPaths =
            let ew = System.Func<'e, float>(fun (e:'e) -> 1.0)
            g.ShortestPathsDijkstra(ew, source)

        match tryGetPaths.Invoke(target) with
        | true, path ->
            path |> Array.ofSeq
        | _ ->
            Array.empty

    let getIncomingEdges (g:AdjacencyGraph<'V, 'E>) (target:'V) =
        g.Edges |> Seq.filter(fun e -> e.Target = target)
    let getOutgoingEdges (g:AdjacencyGraph<'V, 'E>) (source:'V) = g.OutEdges(source)
    let getOutgoingNodes g v = getOutgoingEdges g v |> Seq.map (fun e -> e.Target)

    let getInits (g:AdjacencyGraph<'V, 'E>) =
        g.Vertices
        |> Seq.filter (getIncomingEdges g >> Seq.isEmpty)
        |> List.ofSeq

    let getLasts(g:AdjacencyGraph<'V, 'E>) =
        g.Vertices
        |> Seq.filter (getOutgoingEdges g >> Seq.isEmpty)
        |> List.ofSeq

    let getConnectedComponents(undirectedGraph:UndirectedGraph<'V, 'E>) =
        let cca = QuickGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm(undirectedGraph)
        cca.Compute()
        let ccs =
            cca.Components
            |> Seq.groupBy(fun kv -> kv.Value)
            |> Seq.map snd
            |> Seq.map (fun kvs -> kvs |> Seq.map(fun kv -> kv.Key) |> Array.ofSeq)
            |> Array.ofSeq
        assert(cca.ComponentCount = ccs.Length)
        ccs
    let edges2QgEdge (edges:Edge seq) =
            edges
            |> Seq.collect(fun ee ->
                ee.Sources |> Seq.map(fun s -> QgEdge(s, ee.Target, ee)))
            |> Array.ofSeq

    let isResetEdge(edge:Edge) = (edge :> obj) :? IResetEdge

