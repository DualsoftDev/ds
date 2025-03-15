module Dual.Common.FS.LSIS.Graph.QuickGraph

open System.Runtime.CompilerServices
open System.Collections.Generic
open QuickGraph
open Dual.Common.FS.LSIS

/// QuickGraph 상의 graph algorithm
[<AutoOpen>]
module GraphAlgorithm =
    // https://www.quora.com/What-are-strongly-and-weakly-connected-components
    // - A Strongly connected component is a sub-graph where there is a path from every node to every other node. 
    // - A weakly connected component is one in which all components are connected by some path, ignoring direction.
    // https://neo4j.com/blog/graph-algorithms-neo4j-weakly-connected-components/
    // - WCC differs from the Strongly Connected Components algorithm (SCC)
    //   because it only needs a path to exist between pairs of nodes in one direction,
    //   whereas SCC needs a path to exist in both directions.
    let getConnectedComponents (undirGraph:IUndirectedGraph<'V, 'E>) =
        let x = QuickGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm(undirGraph)
        x.Compute()
        x.Components

    /// Graph g 에서 src -> dst 로 가는 모든 path 들을 반환한다.
    /// return type : 'V list list
    /// - Path 검색 도중에 cycle 을 형성하는 back edge 가 존재하면 전체 결과를 empty 로 반환한다.
    // --- 구현 아이디어 : 
    // Graph g 에서 start node 를 제외한 g' 을 생각하면, g 의 인접 node 들로 부터 dst 까지 path 를 모두 구한
    // 것에 start node 를 앞에 prefix 하면 전체 path 목록이 된다.
    let getAllPaths(g:AdjacencyGraph<'V, 'E>) (src:'V) (dst:'V) =
        /// history : s -> e path 이전에 이미 찾은 path.  즉 인자로 주어진 s 보다 이전에 이미 구한 path
        let rec getAllPathsHelper (history:'V list) (s:'V) (e:'V) =
            [
                if s = e then
                    yield [s]
                else
                    let oes = g.OutEdges(s) |> Array.ofSeq
                    let hasBackEdge = oes |> Array.exists(fun e -> history |> List.contains(e.Target))
                    if hasBackEdge then
                        failwith "Failed to get all path with cycle edge" 

                    for oe in oes do
                        let n = oe.Target
                        /// history 에는 s 를 추가하고, 다음 시작 위치는 s 의 인접 node 인 n 부터 path 를 구함
                        let subPaths = getAllPathsHelper (s::history) n e
                        for p in subPaths |> List.filter (fun p -> not <| p.isEmpty() ) do
                            // s 를 subpath 에 첨가시킨 path 반환
                            yield s::p
            ]

        let paths = getAllPathsHelper [] src dst
        paths

#nowarn "0058"  // disables : warning FS0058: Possible incorrect indentation: this token is offside of context started at ...
[<Extension>] // type QuickGraphExt =
type QuickGraphExt =
    /// Graph 의 vertex v 와 연결된 outgoing edges 를 반환
    [<Extension>] static member GetOutgoingEdges(g:AdjacencyGraph<'V, 'E>, source) = g.OutEdges(source)

    /// Graph 의 vertex v 와 연결된 incoming edges 를 반환
    [<Extension>] static member GetIncomingEdges(g:AdjacencyGraph<'V, 'E>, target) =
        g.Edges
        |> Seq.filter(fun e -> e.Target = target)

    [<Extension>] static member InDegree(g:AdjacencyGraph<'V, 'E>, target) =
        g.GetIncomingEdges(target).length()

    /// Graph 의 vertex v 에서 나가는 edge 로 연결된 vertices 를 반환
    [<Extension>] static member GetOutgoingVertices(g:AdjacencyGraph<'V, 'E>, v) =
        g.GetOutgoingEdges(v) |> Seq.map(fun e ->e.Target)

    /// Graph 의 vertex v 로 들어오는 edge 로 연결된 vertices 를 반환
    [<Extension>] static member GetIncomingVertices(g:AdjacencyGraph<'V, 'E>, v) =
        g.GetIncomingEdges(v) |> Seq.map(fun e ->e.Source)

    [<Extension>] static member GetVertices(edges:#Edge<'V> seq) =
        edges |> Seq.collect(fun e -> [e.Source; e.Target])

    [<Extension>] static member GetInitialVertices(g:AdjacencyGraph<'V, 'E>) =
        g.Vertices |> Seq.filter(fun v -> g.InDegree(v) = 0)        

    [<Extension>] static member GetTerminalVertices(g:AdjacencyGraph<'V, 'E>) =
        g.Vertices |> Seq.filter(fun v -> g.OutDegree(v) = 0)        

    /// box 경계 내의 정점 boxVertices 가 주어 졌을 때, edge 를 box 경계에 따라 분류
    [<Extension>] static member PartitionEdges(edges:#Edge<'V> seq, boxVertices:'V seq ) =
        let internalEdges, incomingEdges, outgoingEdges, externalEdges =
            let vs = boxVertices |> HashSet
            let grp =
                edges
                |> List.ofSeq
                |> List.groupBy (fun e -> vs.Contains(e.Source), vs.Contains(e.Target))
                |> Functions.toDictionary
            let k(key) = if grp.ContainsKey(key) then grp.[key] else []
            k(true, true), k(false, true), k(true, false), k(false, false)
        internalEdges, incomingEdges, outgoingEdges, externalEdges
    [<Extension>] static member ContainVertex(edge:#Edge<'V>, vertex:'V) =
        edge.Source = vertex || edge.Target = vertex
        // or QuickGraphExt.ContainVertex(edge, vertex)

    /// Graph g 에서 src -> dst 로 가는 모든 path 들을 반환한다.
    /// return type : 'V list list
    /// - Path 검색 도중에 cycle 을 형성하는 back edge 가 존재하면 전체 결과를 empty 로 반환한다.
    [<Extension>] static member GetAllPaths (g:AdjacencyGraph<'V, 'E>, src:'V, dst:'V) =
        getAllPaths g src dst

