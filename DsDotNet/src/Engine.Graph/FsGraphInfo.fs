namespace Engine.Graph

open System.Linq
open System.Collections.Generic
open Dual.Common
open QuickGraph
open Engine.Core


type FsGraphInfo(flows:Flow seq, isRootFlow:bool) =
    inherit GraphInfo(flows)
    let edges = flows |> Seq.collect(fun f -> f.Edges) |> Array.ofSeq
    let resetEdges = edges |> Array.filter(isResetEdge)
    let solidEdges = edges |> Array.except(resetEdges)

    let validateChildren() =
        for f in flows do
            if f :? ChildFlow then
                let gr = f.Edges |> edges2QgEdge |> GraphExtensions.ToAdjacencyGraph
                for v in gr.Vertices do
                    let n = getIncomingEdges gr v |> Seq.length
                    if n > 1 then
                        failwithf "%A has multiple(%d) incoming edges" v n



    // reset edge 이면서 target 이 call 인 edge
    let voidEdges =
        edges
        |> Seq.filter(isResetEdge)
        |> Seq.filter(fun e ->
            match e.Target with
            | :? Child as child ->
                match child.Coin with
                | :? Call ->
                    true
                | _ ->
                    false
            | :? Segment ->
                false
            | :? Call ->
                true
            | _ ->
                failwith "ERROR")
        |> Array.ofSeq




    let qgEdges = edges2QgEdge edges

    /// edge 연결없이 고립된 segment
    let isolatedSegments = flows |> Seq.collect(fun f -> f.IsolatedCoins) |> Seq.cast<V>

    let vertices =
        qgEdges
        |> Seq.collect(fun e -> [e.Source; e.Target])
        |> Seq.append(isolatedSegments)
        |> Seq.distinct |> Array.ofSeq

    /// reset edge 제외한 start edge 만..
    let qgSolidEdges = edges2QgEdge solidEdges

    let graph = qgEdges |> GraphExtensions.ToAdjacencyGraph
    let solidGraph =
        let g = qgSolidEdges |> GraphExtensions.ToAdjacencyGraph
        isolatedSegments |> Seq.iter(g.AddVertex >> ignore)
        g

    let inits = isolatedSegments @@ getInits solidGraph |> Seq.distinct |> Array.ofSeq
    let lasts = isolatedSegments @@ getLasts solidGraph |> Seq.distinct |> Array.ofSeq
    //let inits = isolatedSegments @@ getInits(if isRootFlow then solidGraph else graph) |> Seq.distinct |> Array.ofSeq
    //let lasts = isolatedSegments @@ getLasts(if isRootFlow then solidGraph else graph) |> Seq.distinct |> Array.ofSeq

    let undirectedGraph =
        let g = qgEdges |> GraphExtensions.ToUndirectedGraph
        isolatedSegments |> Seq.iter(g.AddVertex >> ignore)
        g

    let undirectedSolidGraph =
        let g = qgSolidEdges |> GraphExtensions.ToUndirectedGraph
        isolatedSegments |> Seq.iter(g.AddVertex >> ignore)
        g

    let connectedComponets = getConnectedComponents(undirectedGraph)
    let solidConnectedComponets = getConnectedComponents(undirectedSolidGraph)


    /// Start causal 상에서 실행 순서에 맞게 graph 탐색해서 (V, OES)[] 를 반환.
    let traverseOrders =
        let q = Queue<V>()
        inits |> Array.iter q.Enqueue
        [|
            while not isRootFlow && q.Count > 0 do
                let v = q.Dequeue()
                let oes = graph.OutEdges(v).Where(fun e -> box e :? ISetEdge)
                let ooes = oes |> Seq.map(fun (e:QgEdge) -> e.OriginalEdge) |> Array.ofSeq
                yield VertexAndOutgoingEdges(v, ooes)
                oes |> Seq.map (fun e -> e.Target) |> Seq.iter q.Enqueue
        |]

    override x.Edges                with get() = edges
    override x.Vertices             with get() = vertices
    override x.QgEdges              with get() = qgEdges
    override x.Graph                with get() = graph
    override x.SolidGraph           with get() = solidGraph
    override x.UndirectedGraph      with get() = undirectedGraph
    override x.UndirectedSolidGraph with get() = undirectedSolidGraph
    override x.Inits                with get() = inits
    override x.Lasts                with get() = lasts
    override x.TraverseOrders       with get() = traverseOrders

    /// Reset edge 까지 고려하였을 때의 connected component
    override x.ConnectedComponets with get() = connectedComponets

    /// Reset edge 제외한 상태의 connected component
    override x.SolidConnectedComponets with get() = solidConnectedComponets

    member x.GetShortestPath(source, vertex) = computeDijkstra x.Graph source vertex


    static member AnalyzeFlows(flows:Flow seq, isRootFlow:bool) =
        let gri = FsGraphInfo(flows, isRootFlow)
        let graph = gri.Graph
        gri

