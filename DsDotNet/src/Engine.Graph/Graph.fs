namespace Engine.Graph

open Engine.Core
open QuickGraph
open QuickGraph.Algorithms
open QuickGraph.Collections
open System
open System.Linq
open System.Collections.Generic
open System.Net.Security
open Dual.Common.Graph.QuickGraph.GraphAlgorithm

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

        let inits = getInits(if isRootFlow then solidGraph else graph) |> Array.ofSeq
        let lasts = getLasts(if isRootFlow then solidGraph else graph) |> Array.ofSeq

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
                    let oes = graph.OutEdges(v)
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


    let analyzeFlows(flows:Flow seq, isRootFlow:bool) =
        let gri = FsGraphInfo(flows, isRootFlow)
        let graph = gri.Graph
        gri

[<AutoOpen>]
module GraphProgressSupportUtil =
    let private changeToChild (v:IVertex) =
        match v with
        | :? Child -> v:?>Child
        | _ -> null

    let private checkIntersect
            (sourceSeq:'T seq)
            (shatteredSeqs:'T seq seq) =
        shatteredSeqs
        |> Seq.filter(fun sr ->
            Enumerable.SequenceEqual(
                Enumerable.Intersect(sourceSeq, sr), sr
            )
        )

    let private checkIsAlias (segment:IVertex) = 
        match segment with 
        | :? Child as c -> c.IsAlias
        | _ -> false

    let private getResetEdges (allEdges:Edge array) = 
        allEdges
        |> Seq.filter(isResetEdge)
        |> edges2QgEdge

    let private getRouteFromStart
            (v:IVertex) (route:IVertex seq) =
        route 
        |> Seq.takeWhile(fun rv -> rv <> v)
        |> Seq.append(seq{v})
        |> Array.ofSeq
        
    let private getSegmentsFromEdges (edges:QgEdge seq) = 
        edges
        |> Seq.map(fun e -> seq{e.Source; e.Target;})
        |> Seq.collect id
        |> Seq.distinct
        
    let private getAliasTarget (v:IVertex) =
        let child = changeToChild v
        match child.Coin with
        | :? Call as c -> 
            if child.IsAlias then 
                c.Prototype.QualifiedName 
            else 
                c.QualifiedName
        | :? ExSegmentCall as s -> 
            s.ExternalSegment.QualifiedName
        | _ -> null

    let private getAllAliases (edges:QgEdge seq) = 
        getSegmentsFromEdges edges
        |> Seq.filter(checkIsAlias)

    let private getAliasResets (resetEdges:QgEdge seq) = 
        resetEdges
        |> Seq.filter(fun e -> checkIsAlias e.Target)

    let private getMutualDummyResets (resetEdges:QgEdge seq) =
        resetEdges
        |> Seq.map(fun e ->
            resetEdges
            |> Seq.filter(fun ee ->
                e <> ee && 
                e.Source = ee.Target && 
                ee.Source = e.Target
            )
            |> Seq.distinct
        )
        |> Seq.filter(fun e -> e.Count() <> 0)
        |> Seq.collect id
        |> Seq.distinct

    let private getReverseResets
            (myRoute:IVertex list) (resetEdges:QgEdge seq) =
        resetEdges
        |> Seq.map(fun r -> seq{r.Target; r.Source;})
        |> checkIntersect myRoute

    let private getTraverseOrder
            (inits:IVertex list) 
            (orderEdges:AdjacencyGraph<IVertex, QgEdge>) = 
        let q = Queue<V>()
        inits |> List.iter q.Enqueue
        [|
            while q.Count > 0 do
                let v = q.Dequeue()
                yield v
                orderEdges.OutEdges(v)
                |> Seq.map (fun e -> e.Target)
                |> Seq.iter q.Enqueue
        |]
        |> Array.distinct
        
    let private getIndexedMap (traverseOrder:IVertex array) =
        let mutable i = 1
        [|
            for v in traverseOrder do
                yield (v.GetQualifiedName(), i)
                i <- i + 1
        |]
        |> Map.ofArray

    let private getAllRoutes
            (inits:IVertex list) (lasts:IVertex list) 
            (orderEdges:AdjacencyGraph<IVertex, QgEdge>) =
        inits
        |> Seq.collect(fun s ->
            lasts
            |> Seq.collect(fun e ->
                getAllPaths orderEdges s e
            )
        )

    let private filterResetsInMyRoute
            (myRoute:IVertex list) 
            (resets:QgEdge seq) (isBidirectional:bool) =
        resets
        |> Seq.map(fun r -> 
            if isBidirectional = true then
                seq{seq{r.Source; r.Target;}; seq{r.Target; r.Source;};}
            else
                seq{seq{r.Source; r.Target;}}
        )
        |> Seq.map(fun arst ->
            arst |> checkIntersect myRoute
        )
        |> Seq.collect id
        |> Seq.distinct

    let private getNonCausalMutualResets
            (allRoutes:IVertex list seq) (mutualResets:QgEdge seq) =
        let mutualResetNodesInMyRoutes = 
            allRoutes
            |> Seq.map(fun r ->
                mutualResets
                |> Seq.map(fun e -> seq{e.Source; e.Target;})
                |> Seq.filter(fun e ->
                    Enumerable.SequenceEqual(
                        Enumerable.Intersect(r, e), e
                    )
                )
                |> Seq.collect id
            )
            |> Seq.collect id

        let mutualResetsInMyRoutes =
            mutualResetNodesInMyRoutes
            |> Seq.map(fun v ->
                mutualResets
                |> Seq.filter(fun e ->
                    e.Source = v || e.Target = v
                )
            )
            |> Seq.collect id
            |> Seq.distinct

        mutualResets |> Seq.except(mutualResetsInMyRoutes)

    let private getMutualResetedSegmentsInMyRoute
            (myMutualResets:IVertex seq seq) (myRoute:IVertex array) 
            (nowSegment:IVertex) (toBeOn:bool) = 
        myMutualResets
        |> Seq.map(fun e ->
            let containFirst = myRoute.Contains(e.First())
            let containLast = myRoute.Contains(e.Last())
            let getComponant (edge:IVertex seq) (toBeOn:bool) = 
                match toBeOn with 
                | true -> edge.First() 
                | _ -> edge.Last()

            if nowSegment = e.First() then getComponant e toBeOn
            elif containFirst && containLast = false then getComponant e toBeOn
            elif containFirst && containLast then getComponant (e.Reverse()) toBeOn
            else getComponant (e.Reverse()) toBeOn
        )
          
    let private getMutualResetedSegmentsWithOtherRoutes
            (nonCausalMutualResets:QgEdge seq) (nowSegment:IVertex) =
        nonCausalMutualResets
        |> Seq.filter(fun e ->
            e.Source = nowSegment || e.Target = nowSegment
        )
        |> Seq.map(fun e -> seq{e.Source; e.Target;})

    let private findMutualResets (allEdges:QgEdge seq) =
        let resets = 
            allEdges 
            |> Seq.filter(fun e -> isResetEdge e.OriginalEdge)

        resets
        |> Seq.map(fun e ->
            let orgSrc = getAliasTarget e.Source
            let orgTgt = getAliasTarget e.Target

            resets
            |> Seq.filter(fun ee ->
                let reverseSrc = getAliasTarget ee.Target
                let reverseTgt = getAliasTarget ee.Source
                e <> ee &&
                orgSrc = reverseSrc &&
                reverseTgt = orgTgt
            )
        )
        |> Seq.collect id
        
    let private getAliasMutualResetedSegmentsInMyRoute
            (myMutualResets:string seq seq) 
            (solidRouteNames:string seq) (nowSegment:IVertex) =
        let nowName = getAliasTarget nowSegment
        myMutualResets
        |> Seq.map(fun e ->
            let containFirst = solidRouteNames.Contains(e.First())
            let containLast = solidRouteNames.Contains(e.Last())

            if e.Contains(nowName) then nowName
            elif containFirst && containLast then e.Last()
            elif containFirst && containLast = false then e.First()
            else e.Last()
        )

    let private getLinkedAliases 
            (alias:IVertex) (allAliases:IVertex seq) = 
        allAliases
        |>Seq.filter(fun v -> 
            alias <> v &&
            getAliasTarget v = getAliasTarget alias
        )
    
    let private getPredictedDontCareSegments
            (route:IVertex list) (dummyResets:QgEdge seq)
            (myRoute:IVertex array) (nowSegment:IVertex) =
        getReverseResets route dummyResets
        |> Seq.map(fun e -> 
            let containFirst = myRoute.Contains(e.First())
            let containLast = myRoute.Contains(e.Last())
            
            if e.First() = nowSegment then e.Last()
            elif containFirst && containLast then e.First()
            elif containFirst && containLast = false then e.Last()
            else e.First()
        )
        |> Seq.distinct

    let private getAliasMutualResetOnList
            (nowSegment:IVertex) (solidRoutes:IVertex list seq) 
            (allEdges:QgEdge seq) (allAliases:IVertex seq) =
        solidRoutes
        |> Seq.map(fun sr ->
            let aliasMutualResets = findMutualResets allEdges
            let nowSeg = 
                match sr.Contains(nowSegment) with
                | true -> nowSegment
                | _ ->
                    let seg = 
                        aliasMutualResets
                        |> Seq.filter(fun e -> e.Target = nowSegment)
                        |> Seq.map(fun e -> e.Source)
                    match seg.Count() with
                    | 0 -> nowSegment
                    | _ -> seg.First()
                                
            let nsr = sr |> Seq.map(fun vv -> getAliasTarget vv)
            let nowNsr = 
                getRouteFromStart nowSeg sr
                |> Seq.map(fun vv -> getAliasTarget vv)
            let amr = 
                aliasMutualResets
                |> Seq.map(fun ee -> 
                    seq{getAliasTarget ee.Source; getAliasTarget ee.Target;}
                )
                |> checkIntersect nsr

            getAliasMutualResetedSegmentsInMyRoute amr nowNsr nowSeg
            |> Seq.map(fun mr ->
                sr
                |> Seq.filter(fun vv ->
                    getAliasTarget vv = mr
                )
            )
            |> Seq.collect id
            |> Seq.map(fun vv -> getLinkedAliases vv allAliases)
            |> Seq.collect id
        )
        |> Seq.collect id

    let private getCalculationTargets
            (nowSegment:IVertex) (allRoutes:IVertex list seq)
            (solidRoutes:IVertex list seq) (dummyResets:QgEdge seq) 
            (mutualResets:QgEdge seq) (allEdges:QgEdge seq) =
        let allAliases = getAllAliases allEdges
        let ncmr = getNonCausalMutualResets allRoutes mutualResets
        allRoutes
        |> Seq.filter(fun r -> r.Contains(nowSegment)) // find my route
        |> Seq.map(fun r ->
            let myRoute = getRouteFromStart nowSegment r  // in my route...
            let mr = filterResetsInMyRoute r mutualResets false

            let segmentsToOn =
                getMutualResetedSegmentsInMyRoute mr myRoute nowSegment true
                |> Seq.except(
                    getPredictedDontCareSegments r dummyResets myRoute nowSegment
                )
                |> Seq.append(
                    // append all mutually resets segments in my route to the list
                    getAliasMutualResetOnList
                        nowSegment solidRoutes allEdges allAliases
                )
                |> Seq.append(seq{nowSegment})
                |> Seq.map(fun v -> (v, 1))
            
            segmentsToOn
            |> Seq.append(
                getLinkedAliases nowSegment allAliases
                |> Seq.filter(fun l -> segmentsToOn.Contains((l, 1)) = false)
                |> Seq.map(fun l -> (l, 0))
            )
            |> Seq.append(
                getMutualResetedSegmentsInMyRoute mr myRoute nowSegment false
                |> Seq.map(fun v -> (v, 0))
            )
            |> Seq.append(
                getMutualResetedSegmentsWithOtherRoutes ncmr nowSegment
                |> Seq.map(fun e ->
                    if nowSegment = e.First() then 
                        (e.Last(), 0) 
                    else 
                        (e.First(), 0)
                )
            )
        )
        |> Seq.collect id
        |> Seq.distinct

    let private getOrigins 
            (solidRoutes:IVertex list seq) (mutualResets:QgEdge seq) 
            (allEdges:QgEdge seq) =
        let allAliases = getAllAliases allEdges
        solidRoutes
        |> Seq.map(fun r ->
            mutualResets
            |> Seq.map(fun e -> seq {e.Source; e.Target;})
            |> checkIntersect r
            |> Seq.map(fun e -> e.Last())
            |> Seq.distinct
            |> Seq.append(
                if getAliasTarget (r.Last()) <> getAliasTarget (r.First()) then
                    getAliasMutualResetOnList
                        (r.Last()) solidRoutes allEdges allAliases
                else
                    Seq.empty
            )
        )
        |> Seq.collect id
        |> Seq.distinct

    let private getProgressMap
            (traverseOrder:IVertex array) (allRoutes:IVertex list seq)
            (solidRoutes:IVertex list seq) (dummyResets:QgEdge seq) 
            (mutualResets:QgEdge seq) (allEdges:QgEdge seq) =
        traverseOrder
        |> Seq.map(fun v ->
            (
                v.GetQualifiedName(),
                getCalculationTargets 
                    v allRoutes solidRoutes dummyResets mutualResets allEdges 
            )
        )
        |> Map.ofSeq

    type ProgressInfo (gri:GraphInfo) =
        // child flow included whole reset edges
        let resetEdges = getResetEdges gri.Edges

        // mutual reset edges (dummy connection, just for calls)
        let mutualResets = getMutualDummyResets resetEdges
    
        // reset edges connected to alias node (working like causal edges)
        let aliasResets = getAliasResets resetEdges

        // without alias reset edges
        let dummyResets = resetEdges |> Seq.except(aliasResets)

        // causal edges + alias reset edges = order edges
        let orderEdges =
            gri.SolidGraph.Edges
            |> Seq.append(aliasResets)
            |> GraphExtensions.ToAdjacencyGraph

        // just causal edges
        let solidEdges = 
            gri.SolidGraph.Edges
            |> GraphExtensions.ToAdjacencyGraph
            
        // starts and ends from order edges
        let inits = getInits orderEdges
        let lasts = getLasts orderEdges

        // ends from solid edges
        let solidLasts = getLasts solidEdges

        // find traverse oder from order edges
        let traverse = getTraverseOrder inits orderEdges

        // making index n to calculating position of 2^n
        let indexedChildren = getIndexedMap traverse
            
        // find all routes to calculate theta
        let allRoutes = getAllRoutes inits lasts orderEdges
        
        let solidRoutes = getAllRoutes inits solidLasts solidEdges
        
        // making {node:estimate target list} map to predict progress theta
        let calculationTargetsInProgress = 
            getProgressMap 
                traverse allRoutes solidRoutes dummyResets 
                mutualResets gri.Graph.Edges

        // segments to be 'ON' before start the parent
        let childOrigin = 
            getOrigins solidRoutes mutualResets gri.Graph.Edges

        // print children with index
        let printIndexedChildren() =
            match indexedChildren.Count with
            | 0 -> ()
            | _ ->
                printfn "\bIndexed childrens"
                indexedChildren
                |> Seq.iter(fun m ->
                    printfn "%A : %A" m.Key m.Value
                )

        // print the child segemtns to be 'ON' in progress(Theta)
        let printPreCaculatedTargets() =
            match calculationTargetsInProgress.Count with
            | 0 -> ()
            | _ ->
                printfn "\nCheck segemtns to be 'ON' in progress(Theta)"
                calculationTargetsInProgress
                |> Seq.iter(fun m -> 
                    printf "%A : [ " m.Key
                    m.Value
                    |> Seq.iter(fun v -> printf "%A; " v)//(v.GetQualifiedName()))
                    printfn "]"
                )
                
        // print children's origin
        let printOrigin() =
            match childOrigin.Count() with
            | 0 -> ()
            | _ ->
                printfn "\nCheck segemtns to be 'ON' in origin state"
                childOrigin
                |> Seq.iter(fun v ->
                    printfn " - %A" v
                )

        member x.IndexedChildren with get() = indexedChildren
        member x.ChildOrigin with get() = childOrigin
        member x.CalculationTargetsInProgress with get() = calculationTargetsInProgress
        member x.PrintOrigin() = printOrigin()
        member x.PrintPreCaculatedTargets() = printPreCaculatedTargets()
        member x.PrintIndexedChildren() = printIndexedChildren()


[<AutoOpen>]
module GraphResetSearch =
    let getAdjacencyGraphFromEdge(e:Edge seq) =
        (edges2QgEdge e) |> GraphExtensions.ToAdjacencyGraph

    let checkSourceInGraph(sourceFlow:Flow) (src:IVertex) =
        let gri =
            let isRootFlow = true
            FsGraphInfo(seq[sourceFlow], isRootFlow)
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
            checkSourceInGraph c.Container rSrc
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
                printfn 
                    "%A strongly resets %A in progress of flow %A" 
                    src.Name tgt.Name (snd(v)|>List.ofSeq)
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