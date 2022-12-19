namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS
open System.Collections.Generic

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

    let compareIsIncludedWithOrder (now:'T seq) (compare:'T seq) = 
        Enumerable.SequenceEqual(Enumerable.Intersect(compare, now), now)

    /// Remove inclusive relationship duplicates in list list
    let removeDuplicatesInList candidates:(seq<'T list>) =
        let result = new ResizeArray<'T list>(0)
        for now in candidates do
            let mutable check = true
            for compare in candidates do
                if now <> compare then
                    if (compareIsIncludedWithOrder now compare) &&
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
            match a.TargetWrapper with
            | DuAliasTargetCall ct -> ct
            | _ -> failwith $"type error of {vertex}"
        | _ -> failwith $"type error of {vertex}"

    /// Get reset informations from graph
    let getAllResets (graph:DsGraph) =
        let makeName (system:string) (info:ApiResetInfo) =
            let src = info.Operand1
            let tgt = info.Operand2
            $"{system}.{src}", info.Operator.ToText(), $"{system}.{tgt}"

        let getJobDefs (call:Call) =
            call.CallTarget.JobDefs

        let getResetInfo (jd:JobDef) =
            let apiOwnSystem = jd.ApiItem.System
            apiOwnSystem.ApiResetInfos
            |> Seq.map(makeName (apiOwnSystem.QualifiedName))

        graph.Vertices
        |> Seq.map(getVertexTarget)
        |> Seq.map(getJobDefs) |> removeDuplicates
        |> Seq.collect(getResetInfo) |> Seq.distinct

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
            (now:Vertex) (target:Vertex) (graph:DsGraph) =
        let rec searchNodes
                (now:Vertex) (target:Vertex)
                (graph:DsGraph) (path:Vertex list) =
            [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> target then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes node target graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now target graph []

    /// Get ordered jobdef routes
    let visitFromSourceToTargetInList
            (now:int * int) (target:int * int) (route:JobDef list list) =
        let rec searchList
                (now:int * int) (target:int * int)
                (route:JobDef list list) (path:JobDef list) =
            [
                let nv, nj = now
                let nowPath = path.Append(route.[nv].[nj]) |> List.ofSeq
                if now <> target && nv + 1 < route.Length then
                    for jdn = 0 to route.[nv + 1].Length - 1 do
                        yield! searchList (nv + 1, jdn) target route nowPath
                else
                    yield nowPath
            ]
        searchList now target route []

    /// Get all ordered routes of child DAGs
    let getAllVertexRoutes (graph:DsGraph) =
        [
            for head in graph.Inits do
                for tail in graph.Lasts do
                    visitFromSourceToTarget head tail graph
        ]
        |> removeDuplicates

    /// Get all ordered routes of api
    let getAllJobDefRoutes (allRoutes:seq<Vertex list>) =
        let allRouteSet = 
            allRoutes
            |> Seq.map(fun route ->
                route
                |> Seq.map(fun v ->
                    let c = v :?> Call
                    c.CallTarget.JobDefs
                )
                |> List.ofSeq
            )
            |> List.ofSeq
        [
            for route in allRouteSet do
                for sj = 0 to route[0].Length - 1 do
                    for ej = 0 to route[route.Length - 1].Length - 1 do
                        yield visitFromSourceToTargetInList 
                            (0, sj) (route.Length - 1, ej) route
        ]

    /// Get all resets
    let getOneWayResets
            (mutualResets:string seq seq)
            (resets:seq<string * string * string>) =
        resets
        |> Seq.filter(fun e ->
            let (_, o, _) = e
            o <> TextInterlock || o <> TextInterlockWeak
        )
        |> Seq.map(fun e ->
            let (head, r, tail) = e
            if r = TextResetEdge || r = TextResetPush then
                seq { head; tail; }
            elif r = TextResetEdgeRev || r = TextResetPushRev  then
                seq { tail; head; }
            else
                Seq.empty
        )
        |> Seq.except(mutualResets)
        |> Seq.filter(fun r -> r.Count() > 0)

    /// Get mutual resets
    let getMutualResets (resets:seq<string * string * string>) =
        resets
        |> Seq.filter(fun e ->
            let _, o, _ = e
            o = TextInterlock || o = TextInterlockWeak
        )
        |> Seq.map(fun e ->
            let (source, _, target) = e
            let edge = seq { source; target; }
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
    let getMutualResetChains (sort:bool) (resets:string seq seq) =
        let nodes = resets |> Seq.map(fun e -> e.First()) |> Seq.distinct
        let globalChains = new ResizeArray<string ResizeArray>(0)
        let candidates = new ResizeArray<string list>(0)

        let addToChain
                (chain:ResizeArray<string>) (addHead:bool) (target:string) =
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
                (result: ResizeArray<string list>)
                (sort:bool) (target:string seq) =
            let candidate =
                let tgt = target |> Seq.distinct
                match sort with
                | true -> tgt |> Seq.sortBy(fun r -> r) |> List.ofSeq
                | _ -> tgt |> List.ofSeq
            if not (result.Contains(candidate)) then
                result.Add(candidate)

        // Generate chain candidates
        for node in nodes do
            let mutable continued = true
            let checkInList = globalChains |> removeDuplicates
            let localChains = new ResizeArray<string>(0)
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
            (allRoutes:seq<JobDef list>) 
            (offByOneWayBackwardResets:string list)
            (offByMutualResetChains:string list)
            (structedChains:seq<Map<string, seq<string>>>) =
        let allNodes = new Dictionary<string, InitialType>()
        let oneWay = offByOneWayBackwardResets
        let mutual = offByMutualResetChains
        let toBeZero = oneWay.Concat(mutual) |> Seq.distinct
        let allJobs = allRoutes |> removeDuplicates
        for job in allJobs do
            let jobName = job.ApiName
            if toBeZero.Contains(jobName) &&
                    not (allNodes.ContainsKey(jobName)) then
                allNodes.Add(jobName, Off)
            else
                for resets in structedChains do
                    let nowName = jobName
                    if resets.ContainsKey(nowName) &&
                            not (allNodes.ContainsKey(jobName)) then
                        if resets.Count = 2 then
                            let interlocks =
                                resets.Remove(nowName)
                                |> Seq.map(fun v -> v.Value)
                                |> Seq.head
                            let isIn =
                                Enumerable.Intersect(
                                    interlocks, offByMutualResetChains
                                )
                            if not (allNodes.ContainsKey(jobName)) then
                                match isIn.Count() with
                                | 0 -> allNodes.Add(jobName, NeedCheck)
                                | _ -> allNodes.Add(jobName, On)
                        else
                            allNodes.Add(jobName, NeedCheck)
            if not (allNodes.ContainsKey(jobName)) then
                allNodes.Add(jobName, NotCare)
        allNodes

    /// Get aliases in front of graph
    let getAliasHeads
            (graph:DsGraph) (callMap:Dictionary<string, ResizeArray<Vertex>>) =
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
                |> Seq.distinct
                |> Seq.map(fun jd ->
                    jd,
                    resetChains 
                    |> Seq.filter(fun s -> s.Contains(jd)) 
                    |> Seq.collect id
                )
                |> Map.ofSeq
            )
        let allRoutes = 
            graph 
            |> getAllVertexRoutes 
            |> getAllJobDefRoutes 
            |> removeDuplicates
            |> Seq.map(fun r -> List.distinct r)
        let offByOneWayBackwardResets =
            [
                for reset in oneWayResets do
                    let tgt = seq { reset.Last(); reset.First(); }
                    let backward = 
                        allRoutes
                        |> Seq.filter(fun route -> route.Count() > 0)
                        |> Seq.map(fun route -> 
                            route |> Seq.map(fun j -> j.ApiName)
                        )
                        |> Seq.filter(compareIsIncludedWithOrder tgt)
                    if backward.Count() > 0 then yield tgt.First()
            ]
        let offByMutualResetChains =
            allRoutes.Collect(fun route ->
                let nameRoute = route |> Seq.map(fun j -> j.ApiName)
                [
                    for chain in resetChains do
                        let collision = Enumerable.Intersect(chain, nameRoute)
                        if collision.Count() = chain.Count() then
                            yield collision.First()
                ]
            )
            |> Seq.filter(fun c -> c.length() > 0)
            |> List.ofSeq

        getOriginMaps
            allRoutes
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
    [<Extension>] 
    static member GetOrigins(graph:DsGraph) = getOrigins graph

    /// Get node index map(key:name, value:idx)
    [<Extension>] 
    static member GetIndexedMap(graph:DsGraph) = getIndexedMap graph

    /// Get reset informations from graph
    [<Extension>] 
    static member GetAllResets(graph:DsGraph) = getAllResets graph

    /// Get pre-calculated targets that
    /// child segments to be 'ON' in progress(Theta)
    [<Extension>] 
    static member GetThetaTargets(graph:DsGraph) = getThetaTargets graph