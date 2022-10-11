namespace Engine.GraphUtil

open System.Linq
open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module internal GraphAlgorithms = 
    /// Get node index map(key:name, value:idx)
    let getIndexedMap (graph:Graph<'V, 'E>) =
        let traverseOrder = getTraverseOrder graph
        let mutable i = 1
        [
            for v in traverseOrder do
                yield (i, v)
                i <- i + 1
        ]
        |> Map.ofList
    
    /// Get origin status of child nodes
    let getOrigins (graph:Graph<'V, 'E>) =
        let allRoutesPerHeads = getAllRoutes graph
        let allRoutes = allRoutesPerHeads |> removeDuplicates
        let rawResets = graph |> getAllResets
        let allNodes = graph.Vertices |> Seq.map(fun n -> (n, -1)) |> Seq.distinct
        let mutualResets = rawResets |> getMutualResets
        let oneWayResets = rawResets |> getOneWayResets mutualResets
        let oneWayBackwardResets = allRoutes |> Seq.map(getBackwardResets oneWayResets)
        let resetChains = mutualResets |> getMutualResetChains
        let offSegmentByOneWayBackwardResets = 
            oneWayBackwardResets 
            |> Seq.map(Seq.map(fun e -> e.Last()))
            |> removeDuplicates
        //let firstDetectedSegs = getIndexedMap graph |> removeAliases 

        ""
        
    /// Get pre-calculated targets that 
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (graph:Graph<'V, 'E>) = 
        // To do...
        ()
