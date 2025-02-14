//module Dual.Common.Core.FS.Graph.QuickGraph

//open System.Runtime.CompilerServices
//open System.Collections.Generic
//open QuickGraph
//open Dual.Common.Core.FS

//[<AutoOpen>]
//module QuickGraphModule =
//    /// Graph 의 vertex v 와 연결된 incoming edges 를 반환
//    let getIncomingEdges (g:AdjacencyGraph<'V, 'E>) (target:'V) =
//        g.Edges |> Seq.filter(fun e -> e.Target = target)
//    let getOutgoingEdges (g:AdjacencyGraph<'V, 'E>) (source:'V) = g.OutEdges(source)
//    let getOutgoingNodes g v = getOutgoingEdges g v |> Seq.map (fun e -> e.Target)
//    // let getIncomingNodes g v = ... see g.GetIncomingVertices()
//    let getTheEdge (g:AdjacencyGraph<'V, 'E>) (source:'V) (target:'V) =
//        g.OutEdges source |> Seq.filter (fun e -> e.Target = target) |> Seq.tryExactlyOne

///// QuickGraph 상의 graph algorithm
//[<AutoOpen>]
//module GraphAlgorithm =
//    // https://www.quora.com/What-are-strongly-and-weakly-connected-components
//    // - A Strongly connected component is a sub-graph where there is a path from every node to every other node. 
//    // - A weakly connected component is one in which all components are connected by some path, ignoring direction.
//    // https://neo4j.com/blog/graph-algorithms-neo4j-weakly-connected-components/
//    // - WCC differs from the Strongly Connected Components algorithm (SCC)
//    //   because it only needs a path to exist between pairs of nodes in one direction,
//    //   whereas SCC needs a path to exist in both directions.
//    let getConnectedComponents (undirGraph:IUndirectedGraph<'V, 'E>) =
//        let x = QuickGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm(undirGraph)
//        x.Compute()
//        x.Components

//    // strongly connected : directed graph g 의 vertex a, b 에 대해서 a -> b 및 b -> a 경로가 존재할 때 strongly connected
//    let getStronglyConnectedComponents (g:AdjacencyGraph<'V, 'E>) =
//        let x = QuickGraph.Algorithms.ConnectedComponents.StronglyConnectedComponentsAlgorithm(g)
//        x.Compute()
//        x.Components

//    /// Graph g 에서 src -> dst 로 가는 모든 path 들을 반환한다.
//    /// return type : 'V list list
//    /// - Path 검색 도중에 cycle 을 형성하는 back edge 가 존재하면 전체 결과를 empty 로 반환한다.
//    // --- 구현 아이디어 : 
//    // Graph g 에서 start node 를 제외한 g' 을 생각하면, g 의 인접 node 들로 부터 dst 까지 path 를 모두 구한
//    // 것에 start node 를 앞에 prefix 하면 전체 path 목록이 된다.
//    // winding 이 Some(start,end) 이면 winding 으로 주어진 그래프의 start, end 를 연결한 상태에서의 검색을 수행한다.
//    let getAllPathsWindable2(g:AdjacencyGraph<'V, 'E>) (src:'V) (dstSelector:'V->bool) (winding:('V*'V) option) =
//        /// history : s -> e path 이전에 이미 찾은 path.  즉 인자로 주어진 s 보다 이전에 이미 구한 path
//        let rec getAllPathsHelper (history:'V list) (s:'V) (targetSelector:'V->bool) =
//            match g.TryGetOutEdges(s) with
//            | true, _ ->
//                [
//                    if targetSelector(s) then
//                        yield [s]
//                    else
//                        let oes = g.OutEdges(s) |> Array.ofSeq
//                        let hasBackEdge = oes |> Array.exists(fun e -> history |> List.contains(e.Target))
//                        if hasBackEdge then
//                            failwithlog "Failed to get all path with cycle edge" 

//                        for oe in oes do
//                            let n = oe.Target
//                            /// history 에는 s 를 추가하고, 다음 시작 위치는 s 의 인접 node 인 n 부터 path 를 구함
//                            let subPaths = getAllPathsHelper (s::history) n targetSelector
//                            for p in subPaths |> List.filter (fun p -> not <| p.isEmpty() ) do
//                                // s 를 subpath 에 첨가시킨 path 반환
//                                yield s::p
//                ]
//            | _ -> []

//        let paths = getAllPathsHelper [] src dstSelector
//        match paths, winding with
//        | [], None -> []
//        | [], Some(startN, endN) ->
//            let p1s = getAllPathsHelper [] src ((=) endN)
//            if p1s.isEmpty() then []
//            else
//                let p2s = getAllPathsHelper [] startN dstSelector
//                [ for p1 in p1s do
//                    for p2 in p2s do
//                        p1 @ p2 ]
//        | _ ->
//            paths

//    let getAllPathsWindable (g:AdjacencyGraph<'V, 'E>) (src:'V) (dst:'V) (winding:('V*'V) option) =
//        getAllPathsWindable2 g src ((=) dst) winding

//    let getAllPaths(g:AdjacencyGraph<'V, 'E>) (src:'V) (dst:'V) =
//        getAllPathsWindable g src dst None

//    module UnusedRevertGraph =
//        let revertGraphDirection2 (g:AdjacencyGraph<'V, 'E>) (edgeCreator:'V -> 'V -> 'E) =
//            if g.EdgeCount = 0 then
//                failwithlog "Failed to revert graph direction with no edge."

//            let revertedEdges =
//                g.Edges
//                |> Seq.map (fun e -> edgeCreator e.Target e.Source)
//            revertedEdges.ToAdjacencyGraph()

//        let revertGraphDirection (g:AdjacencyGraph<'V, 'E>) =
//            if g.EdgeCount = 0 then
//                failwithlog "Failed to revert graph direction with no edge."

//            let revertedEdges =
//                g.Edges
//                |> Seq.map (fun e -> Edge(e.Target, e.Source))
//            revertedEdges.ToAdjacencyGraph()

//        //let revertGraphDirection (g:AdjacencyGraph<'V, 'E>) =
//        //    let edgeCreator s e = Edge(s, e)
//        //    revertGraphDirection2 g edgeCreator

//        /// src -> dst 를 edge 반대 방향으로 탐색한다.
//        /// winding 이 Some(start,end) 이면 winding 으로 주어진 그래프의 start, end 를 연결한 상태에서의 검색을 수행한다.
//        let getAllBackPathsWindable2(g:AdjacencyGraph<'V, 'E>) (src:'V) (dstSelector:'V->bool) (winding:('V*'V) option) =

//            let rg = revertGraphDirection g
//            let rwinding = winding |> Option.map Tuple.swap
//            getAllPathsWindable2 rg src dstSelector rwinding

//        let getAllBackPathsWindable(g:AdjacencyGraph<'V, 'E>) (src:'V) (dst:'V) (winding:('V*'V) option) =
//            getAllBackPathsWindable2 g src ((=) dst) winding

//    let getEdgeExactlyOne (g:AdjacencyGraph<'V, 'E>) (src:'V) (dst:'V) =
//        match g.TryGetEdges (src, dst) with
//        | true, edges -> edges |> List.ofSeq |> List.exactlyOne
//        | _ -> failwithlog "Edge not found"

//    let duplicateGraphByEdgeClone (g:AdjacencyGraph<'V, 'E>) (edgeCreator:'V->'V->'E) =
//        let newEdges = 
//            g.Edges
//            |> Seq.map (fun e -> edgeCreator e.Source e.Target)
//        GraphExtensions.ToAdjacencyGraph(newEdges)

//    let getInitialNodes (g:AdjacencyGraph<'V, 'E>) =
//        g.Vertices
//        |> Seq.filter (getIncomingEdges g >> Seq.isEmpty)
//        |> List.ofSeq

//    let getTerminalNodes (g:AdjacencyGraph<'V, 'E>) =
//        g.Vertices
//        |> Seq.filter (getOutgoingEdges g >> Seq.isEmpty)
//        |> List.ofSeq


//    /// node 방문 type
//    type NodeVisitType =
//        /// 맨 처음 방문하는 node
//        | Fresh
//        /// 방문 시, cycle 이 형성되는 node
//        | Cyclic
//        /// 방문 시, 재 방문인 node.  단 cycle 은 형성되지 않음.
//        | Revisit

//    /// Depth first search 수행하면서 만나는 vertex 마다 f 를 수행.
//    /// f : 특정 vertex v 를 방문 할 때, 
//    ///  - history : v 까지 온 경로
//    ///  - (NodeVisitType, v) 는 해당 vertex 를 어떻게 만났는지에 대한 정보
//    ///  - return type : any : 'R
//    /// 전체 return type : path 별 ['R] 의 list.  [['R]]
//    let dfsVisitorFrom (g:AdjacencyGraph<'V, 'E>) (starts:'V list) (f: 'V list -> (NodeVisitType * 'V) -> 'R) =
//        let freqDict = Dictionary<'V, int>()
//        let rec helper history v (results: 'R list) =
//                if freqDict.ContainsKey v then
//                    let freq = freqDict.[v] + 1
//                    freqDict.[v] <- freq
//                    let visitType = if history |> List.contains v then Cyclic else Revisit
//                    [f history (visitType, v) :: results]
//                else
//                    freqDict.Add(v, 0)
//                    let r:'R = f history (Fresh, v)

//                    let ons = getOutgoingNodes g v
//                    if ons.isEmpty() then
//                        [r::results]
//                    else
//                        ons
//                        |> Seq.collect (fun n -> helper (v::history) n (r::results))
//                        |> List.ofSeq
//        starts |> Seq.collect (fun s -> helper [] s [])

//    /// Depth first search Visitor.  모든 initial nodes 에서 출발
//    let dfsVisitor g f = dfsVisitorFrom g (getInitialNodes g) f

//    /// Cycle 을 구성하는 back edge 를 반환한다.  (backedge * forward history 경로)
//    let findBackEdges (g:AdjacencyGraph<'V, 'E>) =
//        let f (history:'V list) (visitType:NodeVisitType, v:'V) =
//            match visitType with
//            | Cyclic ->
//                getTheEdge g history.Head v  // backEdge
//                |> Option.map (fun e -> e, history |> List.rev)
//            | _ -> None

//        dfsVisitor g f |> Seq.collect id |> Seq.choose id
        
//    let isCyclic g = findBackEdges g |> Seq.any
//    let isAcyclic g = isCyclic g |> not


//#nowarn "0058"  // disables : warning FS0058: Possible incorrect indentation: this token is offside of context started at ...
//[<Extension>] // type QuickGraphExt =
//type QuickGraphExt =
//    /// Graph 의 vertex v 와 연결된 outgoing edges 를 반환
//    [<Extension>] static member GetOutgoingEdges(g:AdjacencyGraph<'V, 'E>, source) = g.OutEdges(source)

//    /// Graph 의 vertex v 와 연결된 incoming edges 를 반환
//    [<Extension>] static member GetIncomingEdges(g, target) = getIncomingEdges g target

//    [<Extension>] static member GetEdgeExactlyOne(g, source, target) = getEdgeExactlyOne g source target

//    [<Extension>] static member InDegree(g:AdjacencyGraph<'V, 'E>, target) =
//        g.GetIncomingEdges(target).length()

//    /// Graph 의 vertex v 에서 나가는 edge 로 연결된 vertices 를 반환
//    [<Extension>] static member GetOutgoingVertices(g:AdjacencyGraph<'V, 'E>, v) =
//        g.GetOutgoingEdges(v) |> Seq.map(fun e ->e.Target)

//    /// Graph 의 vertex v 로 들어오는 edge 로 연결된 vertices 를 반환
//    [<Extension>] static member GetIncomingVertices(g:AdjacencyGraph<'V, 'E>, v) =
//        g.GetIncomingEdges(v) |> Seq.map(fun e ->e.Source)

//    [<Extension>] static member GetVertices(edges:#Edge<'V> seq) =
//        edges |> Seq.collect(fun e -> [e.Source; e.Target])

//    [<Extension>] static member GetInitialVertices(g:AdjacencyGraph<'V, 'E>) =
//        g.Vertices |> Seq.filter(fun v -> g.InDegree(v) = 0)        

//    [<Extension>] static member GetTerminalVertices(g:AdjacencyGraph<'V, 'E>) =
//        g.Vertices |> Seq.filter(fun v -> g.OutDegree(v) = 0)        

//    /// box 경계 내의 정점 boxVertices 가 주어 졌을 때, edge 를 box 경계에 따라 분류
//    [<Extension>] static member PartitionEdges(edges:#Edge<'V> seq, boxVertices:'V seq ) =
//        let internalEdges, incomingEdges, outgoingEdges, externalEdges =
//            let vs = boxVertices |> HashSet
//            let grp =
//                edges
//                |> List.ofSeq
//                |> List.groupBy (fun e -> vs.Contains(e.Source), vs.Contains(e.Target))
//                |> Tuple.toDictionary
//            let k(key) = if grp.ContainsKey(key) then grp.[key] else []
//            k(true, true), k(false, true), k(true, false), k(false, false)
//        internalEdges, incomingEdges, outgoingEdges, externalEdges
//    [<Extension>] static member ContainVertex(edge:#Edge<'V>, vertex:'V) =
//        edge.Source = vertex || edge.Target = vertex
//        // or QuickGraphExt.ContainVertex(edge, vertex)

//    /// Graph g 에서 src -> dst 로 가는 모든 path 들을 반환한다.
//    /// return type : 'V list list
//    /// - Path 검색 도중에 cycle 을 형성하는 back edge 가 존재하면 전체 결과를 empty 로 반환한다.
//    [<Extension>] static member GetAllPaths (g:AdjacencyGraph<'V, 'E>, src:'V, dst:'V) =
//        getAllPaths g src dst

//    /// - Path 검색 도중에 cycle 을 형성하는 back edge 가 존재하면 전체 결과를 false 로 반환한다.
//    [<Extension>] static member IsForward (g:AdjacencyGraph<'V, 'E>, src:'V, dst:'V) =
//        let paths = getAllPaths g src dst
//        paths.length() > 0

//    /// Graph g 에 vertices vs 를 추가한다.
//    [<Extension>] static member AddVertices (g:AdjacencyGraph<'V, 'E>, vs:'V seq) =
//        vs |> Seq.iter(fun v -> g.AddVertex(v) |> ignore)

