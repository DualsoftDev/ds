namespace Engine.GraphUtil

open System.Linq
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module private GraphCalculationUtils =
    /// Remove duplicates in seq seq
    let removeDuplicates (source:'T seq) = 
        source |> Seq.collect id |> Seq.distinct

    /// Get reset informations from graph
    let getAllResets (graph:Graph<'V, 'E>) =
        let getChildMap (graph:Graph<'V, 'E>) =
            graph.Vertices |> Seq.map(fun v -> v.Name, v) |> Map.ofSeq

        let makeName (system:string) (info:ApiResetInfo) = 
            $"{system}.{info.Operand1}", 
            $"{system}.{info.Operand2}", 
            info.Operator
            
        let getResetInfo 
                (childMap:Map<string, 'V>) (node:IChildVertex) = 
            let api = (node:?>Child).ApiItem.System.Api
            api.ResetInfos
            |> Seq.map(makeName api.System.Name)
            |> Seq.map(fun v -> 
                let (source, target, operator) = v
                childMap.Item(source), operator, childMap.Item(target)
            )
            
        graph.Vertices 
        |> Seq.collect(getChildMap graph |> getResetInfo) |> Seq.distinct

    /// Get ordered graph nodes to calculate the node index
    let getTraverseOrder (graph:Graph<'V, 'E>) =
        let q = Queue<'V>()
        graph.Inits |> Seq.iter q.Enqueue
        [|
            while q.Count > 0 do
                let v = q.Dequeue()
                let oes = graph.GetOutgoingVertices(v)
                oes |> Seq.iter q.Enqueue
                yield v
        |]
        |> Array.distinct
    
    /// Get ordered routes from start to end
    let visitFromHeadToTail
            (now:'V) (tail:'V) (graph:Graph<'V, 'E>) =
        let rec searchNodes
                (now:'V) (tail:'V)
                (graph:Graph<'V, 'E>) (path:'V list) =
            [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> tail then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes node tail graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now tail graph []

    /// Get all ordered routes of child DAGs
    let getAllRoutes (graph:Graph<'V, 'E>) =
        [
            for head in graph.Inits do
            for tail in graph.Lasts do
                visitFromHeadToTail head tail graph
        ]

    /// Get all resets
    let getOneWayResets 
            (mutualResets:'V seq seq) (resets:seq<'V * string * 'V>) =
        resets
        |> Seq.filter(fun e -> 
            let (_, r, _) = e
            r <> TextInterlock
        )
        |> Seq.map(fun e -> 
            let (source, _, target) = e
            seq { source; target; }
        )
        |> Seq.except(mutualResets)

    /// Get mutual resets
    let getMutualResets (resets:seq<'V * string * 'V>) =
        resets 
        |> Seq.filter(fun e -> 
            let (_, r, _) = e
            r = TextInterlock
        )
        |> Seq.map(fun e ->
            let (source, _, target) = e
            let edge = seq { source; target; }
            seq { edge; edge.Reverse(); }
        )
        |> removeDuplicates
    
    /// Check intersect between two sequences
    let checkIntersect (sourceSeq:'V seq) (shatteredSeqs:'V seq seq) =
        shatteredSeqs
        |> Seq.filter(fun sr ->
            Enumerable.SequenceEqual(
                Enumerable.Intersect(sourceSeq, sr), sr
            )
        )

    /// Get foward direction resets
    let getFowardResets (resets:'V seq seq) (route:'V list) = 
        resets |> checkIntersect route
    
    /// Get backward direction resets
    let getBackwardResets (resets:'V seq seq) (route:'V list) = 
        resets |> checkIntersect (route.Reverse())

    /// Get incoming resets
    let getIncomingResets (resets:'V seq seq) (node:'V) =
        resets
        |> Seq.filter(fun e -> e.Last() = node)
        |> Seq.map(fun e -> e.First())

    /// Get outgoing resets
    let getOutgoingResets 
        (resets:'V seq seq) (node:'V) = 
        resets 
        |> Seq.filter(fun e -> node = e.First())
        |> Seq.map(fun e -> e.Last())
        
    /// Get first detected nodes in DAG to remove aliases
    //let removeAliases (indices:Map<int, 'V>) = 
    //    let originals =
    //        indices 
    //        |> Seq.map(fun v ->
    //            let nowSeg = v.Value:?>Child
    //            match nowSeg.IsAlias with
    //            | true -> v.Key, nowSeg.Alias.Value
    //            | _ -> v.Key, nowSeg
    //        )
    //        |> Map.ofSeq

    //    let mutable nodeMaps:Map<string, int * 'V> = Map.empty
    //    originals 
    //    |> Seq.iter(fun v -> 
    //        if not (nodeMaps.ContainsKey(v.Value.Name)) then
    //            nodeMaps <- 
    //                nodeMaps.Add(v.Value.Name, (v.Key, indices.Item(v.Key)))
    //    )
    //    nodeMaps

    /// Get mutual reset chains : All nodes are mutually resets themselves
    let getMutualResetChains (resets:IChildVertex seq seq) =
        let globalChains = new ResizeArray<IChildVertex ResizeArray>(0)
        let nodes = resets |> Seq.map(fun e -> e.First()) |> Seq.distinct
        let result = new ResizeArray<IChildVertex list>(0)

        let addToChain (chain:ResizeArray<IChildVertex>) (target:IChildVertex) = 
            let targets = getOutgoingResets resets target
            for compare in targets do
                if not (chain.Contains(compare)) then
                    chain.Add(compare)
        
        let AddToResult 
            (result: ResizeArray<IChildVertex list>) (target:IChildVertex seq) =
            let candidate = 
                target 
                |> Seq.distinct 
                |> Seq.sortBy(fun r -> (r:?>Child).Name)
                |> List.ofSeq
            if not (result.Contains(candidate)) then
                result.Add(candidate)

        for node in nodes do
            let checkInList = globalChains |> removeDuplicates
            let localChains = new ResizeArray<IChildVertex>(0)
            if not (checkInList.Contains(node)) then
                localChains.Add(node)
                localChains.Last() |> addToChain localChains
                globalChains.Add(localChains)
    
        match globalChains.Count with
        | 0 -> ()
        | 1 -> globalChains |> Seq.collect id |> AddToResult result
        | _ ->
            for now in globalChains do
                for chain in globalChains do
                    if now <> chain then
                        if Enumerable.Intersect(now, chain).Count() > 0 then
                            now.Concat(chain) |> AddToResult result
                        else
                            now |> AddToResult result

        result

    /// Get 