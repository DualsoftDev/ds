namespace Engine.GraphUtil

open System.Linq
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module internal GraphAlgorithms = 
    /// Get node index map(key:name, value:idx)
    let getIndexedMap (graph:Graph<Vertex, Edge>) =
        let traverseOrder = getTraverseOrder graph
        let mutable i = 1
        [
            for v in traverseOrder do
                yield (i, v)
                i <- i + 1
        ]
        |> Map.ofList
    
    /// Get origin status of child nodes
    let getOrigins (graph:Graph<Vertex, Edge>) =
        let rawResets = graph |> getAllResets
        let mutualResets = rawResets |> getMutualResets
        let oneWayResets = rawResets |> getOneWayResets mutualResets
        let resetChains = mutualResets |> getMutualResetChains true
        let structedChains = 
            resetChains 
            |> Seq.map(fun resets -> 
                resets
                |> Seq.cast<Call>
                |> Seq.map(fun seg -> seg.ApiItem) 
                |> Seq.distinct
                |> Seq.map(fun seg ->
                    seg.QualifiedName,
                    resetChains 
                    |> Seq.collect(Seq.filter(fun s -> s.ApiItem = seg))
                )
                |> Map.ofSeq
            )
        let callMap = getCallMap graph
        let aliasHeads = getAliasHeads graph callMap
        let offByOneWayBackwardResets = 
            [
            for reset in oneWayResets do
                let src = reset.First()
                let tgt = reset.Last()
                let backward = visitFromSourceToTarget tgt src graph
                if backward.Count() > 0 then
                    yield tgt
            ]
        let offByMutualResetChains =
            let detectedChain = 
                resetChains.Where(fun chain -> 
                    Enumerable.Intersect(chain, aliasHeads).Count() > 0
                )
            [
            for chain in detectedChain do
                for now in chain do
                for node in chain do
                    if now <> node then
                        let fromTo = 
                            visitFromSourceToTarget now node graph 
                            |> removeDuplicates
                        let intersected = 
                            Enumerable.Intersect(fromTo, chain)
                        if intersected.Count() = chain.Count() then
                            yield now
            ]

        getOriginMaps 
            (graph.Vertices |> Seq.cast<Call>)
            offByOneWayBackwardResets offByMutualResetChains 
            structedChains

    /// Get pre-calculated targets that 
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (graph:Graph<Vertex, Edge>) = 
        // To do...
        ()