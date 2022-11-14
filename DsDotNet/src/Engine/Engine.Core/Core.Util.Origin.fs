namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS
open System.Collections.Generic
open GraphModule

[<AutoOpen>]
module OriginModule =
    //해당 Child Origin 기준
    type InitialType  =
        | Off        //반드시 행위값 0
        | On         //반드시 행위값 1
        | NeedCheck  //인터락 구성요소 중 NeedCheck Child가 1개만이 On 인지 체크
        | NotCare    //On, Off 상관없음

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

    /// Get vertex target
    let getVertexTarget (vertex:Vertex) =
        match vertex with
        | :? Call as c -> c
        | :? Alias as a ->
            match a.Target with
            | CallTarget ct -> ct
            | _ -> failwith $"type error of {vertex}"
        | _ -> failwith $"type error of {vertex}"

    /// Get call maps
    let getCallMap (graph:DsGraph) =
        let callMap = new Dictionary<string, ResizeArray<Vertex>>()
        graph.Vertices
        |> Seq.iter(fun v ->
            let apiName =
                (getVertexTarget v).ApiItem.QualifiedName
            if not (callMap.ContainsKey(apiName)) then
                callMap.Add(apiName, new ResizeArray<Vertex>(0))
            callMap.[apiName].Add(v)
        )
        callMap

    /// Get reset informations from graph
    let getAllResets (graph:DsGraph) =
        let makeName (system:string) (info:ApiResetInfo) =
            let src = info.Operand1
            let tgt = info.Operand2
            $"{system}.{src}", info.Operator.ToText(), $"{system}.{tgt}"

        let getResetInfo (node:Vertex) =
            let vertexSystem = (getVertexTarget node).ApiItem.System
            vertexSystem.ApiResetInfos
            |> Seq.map(makeName (vertexSystem.QualifiedName))

        let generateResetRelationShips
                (callMap:Dictionary<string, ResizeArray<Vertex>>)
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
    let getTraverseOrder (graph:DsGraph) =
        let q = Queue<Vertex>()
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
            (now:Vertex) (target:Vertex)
            (graph:DsGraph) =
        let rec searchNodes
                (now:Vertex) (target:Vertex)
                (graph:DsGraph)
                (path:Vertex list) =
            [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> target then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes node target graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now target graph []

    /// Get all resets
    let getOneWayResets
            (mutualResets:Vertex seq seq)
            (resets:seq<Vertex option * string * Vertex option>) =
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
    let getMutualResets (resets:seq<Vertex option * string * Vertex option>) =
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
            (sourceSeq:Vertex seq) (shatteredSeqs:Vertex seq seq) =
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
    let getMutualResetChains (sort:bool) (resets:Vertex seq seq) =
        let nodes = resets |> Seq.map(fun e -> e.First()) |> Seq.distinct
        let globalChains = new ResizeArray<Vertex ResizeArray>(0)
        let candidates = new ResizeArray<Vertex list>(0)

        let addToChain
                (chain:ResizeArray<Vertex>) (addHead:bool) (target:Vertex) =
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
                (result: ResizeArray<Vertex list>)
                (sort:bool) (target:Vertex seq) =
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
            let localChains = new ResizeArray<Vertex>(0)
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

    /// Get origin map
    let getOriginMaps
            (graphNode:Vertex seq) (offByOneWayBackwardResets:Vertex seq)
            (offByMutualResetChains:Vertex seq)
            (structedChains:seq<Map<string, seq<Vertex>>>) =
        let allNodes = new Dictionary<Vertex, InitialType>()
        let oneWay = offByOneWayBackwardResets |> Seq.map(getVertexTarget)
        let mutual = offByMutualResetChains |> Seq.map(getVertexTarget)
        let toBeZero = oneWay.Concat(mutual) |> Seq.distinct

        for node in graphNode do
            if toBeZero.Contains(getVertexTarget node) &&
                    not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, Off)
            else
                for resets in structedChains do
                    let nowName = (getVertexTarget node).QualifiedName
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
                                | 0 -> allNodes.Add(node, NeedCheck)
                                | _ -> allNodes.Add(node, On)
                        else
                            allNodes.Add(node, NeedCheck)
            if not (allNodes.ContainsKey(node)) then
                allNodes.Add(node, NotCare)
        allNodes

    // Get aliases in front of graph
    let getAliasHeads
            (graph:DsGraph)
            (callMap:Dictionary<string, ResizeArray<Vertex>>) =
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

     /// Get node index map(key:name, value:idx)
    let getIndexedMap (graph:DsGraph) =
        let traverseOrder = getTraverseOrder graph
        let mutable i = 1
        [
            for v in traverseOrder do
                yield (i, v)
                i <- i + 1
        ]
        |> Map.ofList


    /// Get origin status of child nodes
    let getOrigins (graph:DsGraph) =
        let rawResets = graph |> getAllResets
        let mutualResets = rawResets |> getMutualResets
        let oneWayResets = rawResets |> getOneWayResets mutualResets
        let resetChains = mutualResets |> getMutualResetChains true
        let structedChains =
            resetChains
            |> Seq.map(fun resets ->
                resets
                |> Seq.map(getVertexTarget)
                |> Seq.distinct
                |> Seq.map(fun seg ->
                    seg.QualifiedName,
                    resetChains
                    |> Seq.collect(Seq.filter(fun s -> getVertexTarget s = seg))
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
            graph.Vertices
            offByOneWayBackwardResets offByMutualResetChains
            structedChains

    /// Get pre-calculated targets that
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (graph:DsGraph) =
        // To do...
        ()

[<Extension>]
type OriginHelper =
    /// Get origin status of child nodes
    [<Extension>] static member GetOrigins(graph:DsGraph)    = graph |> getOrigins
     /// Get node index map(key:name, value:idx)
    [<Extension>] static member GetIndexedMap(graph:DsGraph) = graph |> getIndexedMap
    /// Get reset informations from graph
    [<Extension>] static member GetAllResets(graph:DsGraph)  = graph |> getAllResets
    /// Get pre-calculated targets thatchild segments to be 'ON' in progress(Theta)
    [<Extension>] static member GetThetaTargets(graph:DsGraph) = graph |> getThetaTargets
