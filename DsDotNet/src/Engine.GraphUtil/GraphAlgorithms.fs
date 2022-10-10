namespace Engine.GraphUtil

open System.Linq
open System.Diagnostics
open System.Collections.Generic
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
        let allNodes = 
            graph.Vertices |> Seq.map(fun n -> (n, -1)) |> Seq.distinct
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
        let offByMutualResetChains =
            let detectedChainHeads = 
                detectedChains 
                |> Seq.map(fun c -> c.First())
            resetChains
            |> Seq.map(
                Seq.map(fun v -> 
                    (
                        v, 
                        detectedChainHeads.Contains(v)
                    )
                )
            )
            |> Seq.map(Seq.filter(fun v -> snd v = true))
            |> Seq.filter(fun c -> c.Count() = 1)
            |> Seq.collect(Seq.map(fun v -> fst v))
        getOriginMaps 
            graph.Vertices 
            offByOneWayBackwardResets offByMutualResetChains 
            resetChains

    /// Get pre-calculated targets that 
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (graph:Graph<SegmentBase, InFlowEdge>) = 
        // To do...
        ()
