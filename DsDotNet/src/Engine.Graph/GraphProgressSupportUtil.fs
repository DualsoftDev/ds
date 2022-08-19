namespace Engine.Graph

open System.Linq
open System.Collections.Generic
open QuickGraph
open Engine.Common.FS.Graph.QuickGraph.GraphAlgorithm
open Engine.Core


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

