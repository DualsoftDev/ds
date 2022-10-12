namespace Engine.GraphUtil

open System.Linq
open Engine.Core

[<AutoOpen>]
module internal GraphAlgorithms = 
    /// Get node index map(key:name, value:idx)
    let getIndexedMap (graph:Graph<Child, InSegmentEdge>) =
        let traverseOrder = getTraverseOrder graph
        let mutable i = 1
        [
            for v in traverseOrder do
                yield (i, v)
                i <- i + 1
        ]
        |> Map.ofList
    
    /// Get origin status of child nodes
    let getOrigins (graph:Graph<Child, InSegmentEdge>) =
        let allRoutesPerHeads = graph |> getAllRoutes
        let allRoutes = allRoutesPerHeads |> removeDuplicates
        let rawResets = graph |> getAllResets
        let mutualResets = rawResets |> getMutualResets
        let oneWayResets = rawResets |> getOneWayResets mutualResets
        let oneWayBackwardResets = 
            allRoutes |> Seq.map(getBackwardResets oneWayResets)
        let resetChains = mutualResets |> getMutualResetChains true
        let detectedChains = 
            allRoutes |> getDetectedResetChains resetChains
        let offByOneWayBackwardResets = 
            oneWayBackwardResets 
            |> Seq.collect(Seq.map(fun e -> e.Last())) |> Seq.distinct
        let structedChains = 
            resetChains 
            |> Seq.map(fun resets -> 
                resets
                |> Seq.map(fun seg -> seg.ApiItem) 
                |> Seq.distinct
                |> Seq.map(fun seg ->
                    seg.QualifiedName,
                    resetChains 
                    |> Seq.collect(Seq.filter(fun s -> s.ApiItem = seg))
                )
                |> Map.ofSeq
            )
        let offByMutualResetChains =
            let detectedChainHeads = 
                detectedChains |> Seq.map(fun c -> c.First())
            let toBeZero = 
                resetChains
                |> Seq.map(
                    Seq.map(fun v -> 
                        (
                            v.ApiItem, 
                            detectedChainHeads.Contains(v)
                        )
                    )
                )
                |> Seq.map(fun r -> 
                    r |> Seq.filter(fun v -> snd v = true) 
                    |> Seq.distinct
                )
                |> Seq.filter(fun c -> c.Count() = 1)
                |> Seq.collect(Seq.map(fun v -> fst v))
            [
                for chain in structedChains do
                for seg in toBeZero do
                    if chain.ContainsKey(seg.QualifiedName) then
                        yield chain.[seg.QualifiedName]
            ]
            |> Seq.collect id

        getOriginMaps 
            graph.Vertices 
            offByOneWayBackwardResets offByMutualResetChains 
            structedChains

    /// Get pre-calculated targets that 
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (graph:Graph<SegmentBase, InFlowEdge>) = 
        // To do...
        ()
