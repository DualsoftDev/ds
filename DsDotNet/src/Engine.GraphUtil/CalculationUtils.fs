namespace Engine.GraphUtil

open System.Linq
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module private GraphCalculationUtils =
    /// Remove duplicates in seq seq
    let removeDuplicates (source:'T seq) = 
        source |> Seq.collect id |> Seq.distinct

    /// Remove inclusive relationship duplicates in list list
    let removeDuplicatesInList candidates:(seq<'T list>) =
        let result = new ResizeArray<'T list>(0)
        for now in candidates do
            let mutable check = true
            for compare in candidates do
                if now <> compare then
                    if Enumerable.SequenceEqual(
                            Enumerable.Intersect(now, compare), now) &&
                            now.Count() < compare.Count() && check then
                        check <- false
            if check = true then
                result.Add(now)
        result

    /// Get reset informations from graph
    let getAllResets (graph:Graph<Child, InSegmentEdge>) =
        let getNameOfApiItem (api:ApiItem) = $"{api.System.Name}.{api.Name}"

        let getCallMap (graph:Graph<Child, InSegmentEdge>) =
            let callMap = new Dictionary<string, ResizeArray<Child>>()
            graph.Vertices 
            |> Seq.iter(fun v -> 
                let apiName = getNameOfApiItem v.ApiItem
                if not (callMap.ContainsKey(apiName)) then
                    callMap.Add(apiName, new ResizeArray<Child>(0))
                callMap.[apiName].Add(v)
            )
            callMap

        let makeName (system:string) (info:ApiResetInfo) = 
            let src = info.Operand1.Replace("\"", "")
            let tgt = info.Operand2.Replace("\"", "")
            $"{system}.{src}", info.Operator, $"{system}.{tgt}"

        let getResetInfo (node:Child) = 
            let api = node.ApiItem.System.Api
            api.ResetInfos |> Seq.map(makeName api.System.Name)

        let generateResetRelationShips 
                (callMap:Dictionary<string, ResizeArray<Child>>)
                (resets:string * string * string) =
            let (source, operator, target) = resets
            [
                if callMap.ContainsKey(source) then
                    for src in callMap.[source] do
                        if callMap.ContainsKey(target) then
                            for tgt in callMap.[target] do
                                yield Some(src), operator, Some(tgt)
                        else
                            yield Some(src), operator, None
                else
                    if callMap.ContainsKey(target) then
                        for tgt in callMap.[target] do
                            yield None, operator, Some(tgt)
            ]

        let callMap = getCallMap graph
        graph.Vertices 
        |> Seq.collect(getResetInfo) |> Seq.distinct
        |> Seq.collect(generateResetRelationShips callMap)

    /// Get ordered graph nodes to calculate the node index
    let getTraverseOrder (graph:Graph<Child, InSegmentEdge>) =
        let q = Queue<Child>()
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
            (now:Child) (tail:Child) 
            (graph:Graph<Child, InSegmentEdge>) =
        let rec searchNodes
                (now:Child) (tail:Child)
                (graph:Graph<Child, InSegmentEdge>) 
                (path:Child list) =
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
    let getAllRoutes (graph:Graph<Child, InSegmentEdge>) =
        [
            for head in graph.Inits do
            for tail in graph.Lasts do
                visitFromHeadToTail head tail graph
        ]

    /// Get all resets
    let getOneWayResets 
            (mutualResets:Child seq seq) 
            (resets:seq<Child option * string * Child option>) =
        resets
        |> Seq.filter(fun e -> 
            let (source, r, target) = e
            r <> TextInterlock &&
            source.IsSome && target.IsSome
        )
        |> Seq.map(fun e -> 
            let (source, _, target) = e
            seq { source.Value; target.Value; }
        )
        |> Seq.except(mutualResets)

    /// Get mutual resets
    let getMutualResets (resets:seq<Child option * string * Child option>) =
        resets 
        |> Seq.filter(fun e -> 
            let (source, r, target) = e
            r = TextInterlock &&
            source.IsSome && target.IsSome
        )
        |> Seq.map(fun e ->
            let (source, _, target) = e
            let edge = seq { source.Value; target.Value; }
            seq { edge; edge.Reverse(); }
        )
        |> removeDuplicates
    
    /// Check intersect between two sequences
    let checkIntersect 
            (sourceSeq:Child seq) (shatteredSeqs:Child seq seq) =
        shatteredSeqs
        |> Seq.filter(fun sr ->
            Enumerable.SequenceEqual(
                Enumerable.Intersect(sourceSeq, sr), sr
            )
        )

    /// Get foward direction resets
    let getFowardResets (resets:Child seq seq) (route:Child list) = 
        resets |> checkIntersect route
    
    /// Get backward direction resets
    let getBackwardResets (resets:Child seq seq) (route:Child list) = 
        resets |> checkIntersect (route.Reverse())

    /// Get incoming resets
    let getIncomingResets (resets:'V seq seq) (node:'V) =
        resets
        |> Seq.filter(fun e -> e.Last() = node)
        |> Seq.map(fun e -> e.First())

    /// Get outgoing resets
    let getOutgoingResets (resets:'V seq seq) (node:'V) = 
        resets 
        |> Seq.filter(fun e -> node = e.First())
        |> Seq.map(fun e -> e.Last())
        
    /// Get first detected nodes in DAG to remove aliases
    let removeAliases (allRoutes:Child list seq) =
        allRoutes
        |> Seq.map(fun route ->
            let nodeMaps = new Dictionary<ApiItem, Child>()
            route 
            |> Seq.iter(fun v ->
                let seg = v.ApiItem
                if not (nodeMaps.ContainsKey(seg)) then
                    nodeMaps.Add(seg, v)
            )
            nodeMaps
        )
        
    /// Get mutual reset chains : All nodes are mutually resets themselves
    let getMutualResetChains (sort:bool) (resets:Child seq seq) =
        let nodes = resets |> Seq.map(fun e -> e.First()) |> Seq.distinct
        let globalChains = new ResizeArray<Child ResizeArray>(0)
        let candidates = new ResizeArray<Child list>(0)

        let addToChain 
                (chain:ResizeArray<Child>) (addHead:bool) (target:Child) = 
            let targets = 
                match addHead with 
                | true -> getIncomingResets resets target
                | _ -> getOutgoingResets resets target

            let mutable added = false
            for compare in targets do
                if not (chain.Contains(compare)) then
                    match addHead with
                    | true -> 
                        chain.Reverse()
                        chain.Add(compare)
                        chain.Reverse()
                    | _ -> 
                        chain.Add(compare)
                    added <- true
            added

        let addToResult 
                (result: ResizeArray<Child list>) 
                (sort:bool) (target:Child seq) =
            let candidate = 
                let tgt = target |> Seq.distinct
                match sort with
                | true -> tgt |> Seq.sortBy(fun r -> r.Name) |> List.ofSeq
                | _ -> tgt |> List.ofSeq
            if not (result.Contains(candidate)) then
                result.Add(candidate)

        // Generate chain candidates
        for node in nodes do
            let mutable continued = true
            let checkInList = globalChains |> removeDuplicates
            let localChains = new ResizeArray<Child>(0)
            if not (checkInList.Contains(node)) then
                localChains.Add(node)
                while continued do
                    let haedIn = 
                        localChains.First() |> addToChain localChains true
                    let tailIn = 
                        localChains.Last() |> addToChain localChains false
                    continued <- haedIn || tailIn
                globalChains.Add(localChains)
    
        // Combine chain candidates
        match globalChains.Count with
        | 0 -> ()
        | 1 -> globalChains |> Seq.collect id |> addToResult candidates sort
        | _ ->
            for now in globalChains do
            for chain in globalChains do
                if now <> chain then
                    if Enumerable.Intersect(now, chain).Count() > 0 then
                        now.Concat(chain) |> addToResult candidates sort
                    else
                        now |> addToResult candidates sort

        removeDuplicatesInList candidates

    /// get detected reset chains in all routes
    let getDetectedResetChains 
            (resetChains:Child list seq) (allRoutes:Child list seq) =
        allRoutes
        |> removeAliases // leave first detected segments in each routes
        |> Seq.collect(fun segs ->
            let route = segs |> Seq.map(fun s -> s.Value)
            resetChains
            |> Seq.map(fun chain ->
                Enumerable.Intersect(route, chain) |> List.ofSeq
            )
        )
        |> Seq.distinct
        |> Seq.filter(fun r -> r.Count() > 0) |> removeDuplicatesInList
        |> Seq.map(fun r -> r |> Seq.ofList) |> getMutualResetChains false
        |> removeAliases |> Seq.map(Seq.map(fun s -> s.Value))
        
    /// get origin map
    let getOriginMaps 
            (graphNode:Child seq) (offByOneWayBackwardResets:Child seq) 
            (offByMutualResetChains:Child seq) (resetChains:Child list seq) =
        let allNodes = new Dictionary<Child, int>()
        let orgOneWay = offByOneWayBackwardResets |> Seq.map(fun v -> v.ApiItem)
        let orgMutual = offByMutualResetChains |> Seq.map(fun v -> v.ApiItem)
        for node in graphNode do
            
                
            if orgOneWay.Contains(node.ApiItem) &&
                    not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, 0)
            elif orgMutual.Contains(node.ApiItem) &&
                    not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, 0)
            else
                for resets in resetChains do
                    let nowName = node.QualifiedName
                    let nameOfResets = 
                        resets 
                        |> Seq.map(fun r -> r.QualifiedName, r) 
                        |> Map.ofSeq
                    if nameOfResets.ContainsKey(nowName) && 
                            not (allNodes.ContainsKey(node)) then
                        if nameOfResets.Count = 1 then
                            allNodes.Add(node, 3)
                        elif nameOfResets.Count = 2 then
                            let interlock = 
                                nameOfResets.Remove(nowName)
                                |> Seq.map(fun v -> v.Value)
                                |> Seq.head

                            if offByMutualResetChains.Contains(interlock) then
                                 allNodes.Add(node, 1)
                            else
                                allNodes.Add(node, 2)
                        else
                            allNodes.Add(node, 2)

            if not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, 3)
        
        allNodes