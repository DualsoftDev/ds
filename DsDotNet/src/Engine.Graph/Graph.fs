namespace Engine.Graph

open Engine.Core
open QuickGraph
open QuickGraph.Algorithms
open QuickGraph.Collections
open System
open System.Linq
open System.Collections.Generic


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

    type FsGraphInfo(flows:Flow seq) =
        inherit GraphInfo(flows)
        let edges = flows |> Seq.collect(fun f -> f.Edges) |> Array.ofSeq
        let resetEdges = edges |> Array.filter(fun e -> (e :> obj) :? IResetEdge)
        let solidEdges = edges |> Array.except(resetEdges)
        let qgEdges = edges2QgEdge edges

        /// edge 연결없이 고립된 segment
        let isolatedSegments = flows |> Seq.collect(fun f -> f.Children) |> Seq.cast<V>

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

        let inits = getInits(solidGraph) |> Array.ofSeq
        let lasts = getLasts(solidGraph) |> Array.ofSeq

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
                while q.Count > 0 do
                    let v = q.Dequeue()
                    let oes = solidGraph.OutEdges(v)
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


    let analyzeFlows(flows:Flow seq) =
        let gri = FsGraphInfo(flows)
        let graph = gri.Graph
        gri


[<AutoOpen>]
module GraphResetSearch =
    let getAdjacencyGraphFromEdge(e:Edge seq) =
        (edges2QgEdge e) |> GraphExtensions.ToAdjacencyGraph

    let checkSourceInGraph(sourceFlow:Flow) (src:IVertex) =
        let gri = FsGraphInfo(seq[sourceFlow])
        let routes =
            getInits(gri.SolidGraph)
            |> Seq.map(fun s ->
                (computeDijkstra gri.SolidGraph s src)
                |> Seq.collect(fun e -> [e.Source; e.Target])
                |> Seq.distinct
            )
            |> Seq.filter(fun r -> (r |> Seq.length) <> 0)
            |> Seq.collect id
        let graphs =
            gri.SolidConnectedComponets
            |> Seq.filter(fun gs -> gs |> Seq.contains(src))
            |> Seq.collect id

        routes, graphs

    let findGraphIncludeSource(rSrc:IVertex) =
        match rSrc with
        | :? Segment as s ->
            checkSourceInGraph s.ContainerFlow rSrc
        | :? Call as c ->
            checkSourceInGraph (c.Container:?>Flow) rSrc
        | _ ->
            failwith "[error] find source native graph"

    let targetResetMarker(srcs:ITxRx seq) (tgts:ITxRx seq) =
        let sourceSegs = srcs |> Seq.map(fun v -> v:?>IVertex)
        let targetSegs = tgts |> Seq.map(fun v -> v:?>IVertex)

        targetSegs
        |> Seq.map(fun ts ->
            getIncomingEdges (getAdjacencyGraphFromEdge (ts:?>Segment).ContainerFlow.Edges) ts
        )
        |> Seq.map(fun es ->
            es
            |> Seq.filter(fun e ->
                e.OriginalEdge.Operator = "|>" || e.OriginalEdge.Operator = "|>>"
            )
            |> Seq.map(fun e ->
                findGraphIncludeSource e.Source
            )
        )
        |> Seq.collect id
        |> Seq.filter(fun rv ->
            Enumerable.SequenceEqual(Enumerable.Intersect(sourceSegs, fst(rv)), sourceSegs)
        )

    let searchCallTargets(cpts:CallPrototype seq) (tgt:CallPrototype) =
        cpts
        |> Seq.filter(fun src->
            src <> tgt && src.TXs.Count <> 0 && tgt.RXs.Count <> 0
        )
        |> Seq.iter(fun src->
            targetResetMarker src.TXs tgt.RXs
            |> Seq.iter(fun v ->
                printfn "'%A' strongly resets '%A' in progress of graph %A" src tgt (snd(v))
            )
        )

    let checkCallResetSource(task:DsTask seq) =
        task
        |> Seq.iter(fun t ->
            let cpts = t.CallPrototypes
            cpts
            |> Seq.iter(fun c ->
                searchCallTargets cpts c
            )
        )

        //flows
        //|> Seq.iter(fun f ->
        //    f.SubFlows
        //)

        //let path =
        //    let src = gri.Vertices |> Seq.find(fun v -> v.ToString() = "Vp")
        //    let tgt = gri.Vertices |> Seq.find(fun v -> v.ToString() = "Sm")
        //    gri.GetShortestPath(src, tgt)
        //gri
        //graph.Edges
        //|> Seq.iter(fun e ->
        //    printfn "%A" e.OriginalEdge
        //    printfn "%A - %A" e.Source e.Target
        //)
        //graph.Vertices
        //|> Seq.iter(fun v ->
        //    match v with
        //    | :? Named as n ->
        //        printfn "%s" n.Name
        //)
        //gri.Inits
        //|> Seq.iter(fun v ->
        //    printfn "start - %A" v
        //)
        //gri.Lasts
        //|> Seq.iter(fun v ->
        //    printfn "finish - %A" v
        //)
        //gri.Vertices
        //|> Seq.iter(fun v ->
        //    match v with
        //    //| :? Named as n ->
        //    //    printfn "%s" n.Name
        //    | :? Segment as s ->
        //        printfn "segment %s" s.Name
        //    | :? Call as c ->
        //        printfn "call %A %s [%A]" c.TXs c.Name (c.RX :?> Segment).ContainerFlow.Children
        //    | _ ->
        //        failwith "error"
        //)

