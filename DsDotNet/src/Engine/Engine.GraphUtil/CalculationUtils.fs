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
    let getAllResets (graph:Graph<VertexBase, InRealEdge>) =
        let getCallMap (graph:Graph<VertexBase, InRealEdge>) =
            let callMap = new Dictionary<string, ResizeArray<Call>>()
            graph.Vertices 
            |> Seq.cast<Call>
            |> Seq.iter(fun v -> 
                let apiName =  $"{v.GetSystem().Name}.{v.Name}" 
                if not (callMap.ContainsKey(apiName)) then
                    callMap.Add(apiName, new ResizeArray<Call>(0))
                callMap.[apiName].Add(v)
            )
            callMap

        let makeName (system:string) (info:ApiResetInfo) = 
            let src = info.Operand1.Replace("\"", "")
            let tgt = info.Operand2.Replace("\"", "")
            $"{system}.{src}", info.Operator, $"{system}.{tgt}"

        let getResetInfo (node:Call) = 
            node.GetSystem().ApiResetInfos |> Seq.map(makeName (node.GetSystem().Name))

        let generateResetRelationShips 
                (callMap:Dictionary<string, ResizeArray<Call>>)
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
        |> Seq.cast<Call>
        |> Seq.collect(getResetInfo) |> Seq.distinct
        |> Seq.collect(generateResetRelationShips callMap)

    /// Get ordered graph nodes to calculate the node index
    let getTraverseOrder (graph:Graph<VertexBase, InRealEdge>) =
        let q = Queue<VertexBase>()
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
    let visitFromSourceToTarget
            (now:Call) (target:Call) 
            (graph:Graph<VertexBase, InRealEdge>) =
        let rec searchNodes
                (now:Call) (target:Call)
                (graph:Graph<VertexBase, InRealEdge>) 
                (path:Call list) =
            [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> target then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes (node:?>Call) target graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now target graph []

    /// Get all resets
    let getOneWayResets 
            (mutualResets:Call seq seq) 
            (resets:seq<Call option * string * Call option>) =
        resets
        |> Seq.filter(fun e -> 
            let (head, r, tail) = e
            r <> TextInterlock &&
            head.IsSome && tail.IsSome
        )
        |> Seq.map(fun e -> 
            let (head, r, tail) = e
            if r = TextResetPush then
                seq { head.Value; tail.Value; }
            elif r = TextResetPushRev then
                seq { tail.Value; head.Value; }
            else
                Seq.empty
        )
        |> Seq.except(mutualResets)

    /// Get mutual resets
    let getMutualResets (resets:seq<Call option * string * Call option>) =
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
            (sourceSeq:VertexBase seq) (shatteredSeqs:VertexBase seq seq) =
        shatteredSeqs
        |> Seq.filter(fun sr ->
            Enumerable.SequenceEqual(
                Enumerable.Intersect(sourceSeq, sr), sr
            )
        )

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
        
    /// Get mutual reset chains : All nodes are mutually resets themselves
    let getMutualResetChains (sort:bool) (resets:Call seq seq) =
        let nodes = resets |> Seq.map(fun e -> e.First()) |> Seq.distinct
        let globalChains = new ResizeArray<Call ResizeArray>(0)
        let candidates = new ResizeArray<Call list>(0)
        
        let addToChain 
                (chain:ResizeArray<Call>) (addHead:bool) (target:Call) = 
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
                (result: ResizeArray<Call list>) 
                (sort:bool) (target:Call seq) =
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
            let localChains = new ResizeArray<Call>(0)
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
                        
        // Remove duplicates
        removeDuplicatesInList candidates
        
    /// get origin map
    let getOriginMaps 
            (graphNode:Call seq) (offByOneWayBackwardResets:Call seq) 
            (offByMutualResetChains:Call seq) 
            (structedChains:seq<Map<string, seq<Call>>>) =
        let allNodes = new Dictionary<Call, int>()
        let oneWay = offByOneWayBackwardResets |> Seq.map(fun v -> v.ApiItem)
        let mutual = offByMutualResetChains |> Seq.map(fun v -> v.ApiItem)
        let toBeZero = oneWay.Concat(mutual) |> Seq.distinct

        for node in graphNode do
            if toBeZero.Contains(node.ApiItem) &&
                    not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, 0)
            else
                for resets in structedChains do
                    let nowName = node.ApiItem.QualifiedName
                    if resets.ContainsKey(nowName) && 
                            not (allNodes.ContainsKey(node)) then
                        if resets.Count = 2 then
                            let interlocks = 
                                resets.Remove(nowName)
                                |> Seq.map(fun v -> v.Value)
                                |> Seq.head
                            let isIn = 
                                Enumerable.Intersect(
                                    interlocks, offByMutualResetChains
                                )
                            if not (allNodes.ContainsKey(node)) then
                                match isIn.Count() with
                                | 0 -> allNodes.Add(node, 2)
                                | _ -> allNodes.Add(node, 1)
                        else
                            allNodes.Add(node, 2)
            if not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, 3)
        allNodes

    let getCallMap (graph:Graph<VertexBase, InRealEdge>) =
        let callMap = new Dictionary<string, ResizeArray<Call>>()
        graph.Vertices 
        |> Seq.cast<Call>
        |> Seq.iter(fun v -> 
            let apiName = $"{v.GetSystem().Name}.{v.Name}"
            if not (callMap.ContainsKey(apiName)) then
                callMap.Add(apiName, new ResizeArray<Call>(0))
            callMap.[apiName].Add(v)
        )
        callMap

    let getAliasHeads 
            (graph:Graph<VertexBase, InRealEdge>)
            (callMap:Dictionary<string, ResizeArray<Call>>) =
        [
        for calls in callMap do
            if calls.Value.Count > 1 then
                for now in calls.Value do
                for call in calls.Value do
                    if now <> call then
                        let fromTo = 
                            visitFromSourceToTarget now call graph 
                            |> removeDuplicates
                        let intersected = 
                            Enumerable.Intersect(fromTo, calls.Value)
                        if intersected.Count() = calls.Value.Count then
                            yield intersected.First()
            else
                yield calls.Value.Item(0)
        ]