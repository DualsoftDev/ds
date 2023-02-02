namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS
open System.Collections.Generic
open Engine.Common.FS

[<AutoOpen>]
module OriginModule =
    //해당 Child Origin 기준
    type InitialType  =
        ///반드시 행위값 0
        | Off
        ///반드시 행위값 1
        | On
        ///인터락 구성요소 중 NeedCheck Child가 1개만이 On 인지 체크
        | NeedCheck
        ///On, Off 상관없음
        | NotCare

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
                    if check
                        && (compareIsIncludedWithOrder now compare)
                        && now.Count() < compare.Count()
                    then
                        check <- false
            if check then
                result.Add(now)
        result

    /// Get vertex target
    let getVertexTarget (vertex:Vertex) =
        match vertex with
        | :? CallDev as c -> c
        | :? Alias as a ->
            match a.TargetWrapper with
            | DuAliasTargetCall ct -> ct
            | _ -> failwith $"type error of {vertex}"
        | _ -> failwith $"type error of {vertex}"

    type EdgeDescription = {
        Source  :string
        Operator:string
        Target  : string
    }

    /// Get reset informations from graph
    let getAllResets (graph:DsGraph) =
        let makeName (system:string) (info:ApiResetInfo) : EdgeDescription =
            let src = $"{system}.{info.Operand1}"
            let tgt = $"{system}.{info.Operand2}"
            { Source=src; Operator=info.Operator.ToText(); Target=tgt}

        let getDeviceDefs (call:CallDev) =
            call.CallTargetJob.DeviceDefs

        let getResetInfo (jd:TaskDev) =
            let apiOwnSystem = jd.ApiItem.System
            apiOwnSystem.ApiResetInfos
            |> Seq.map(makeName (apiOwnSystem.QualifiedName))

        graph.Vertices
        |> Seq.map(getVertexTarget >> getDeviceDefs)
        |> removeDuplicates
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
    let visitFromSourceToTarget (now:Vertex) (target:Vertex) (graph:DsGraph) =
        let rec searchNodes
            (now:Vertex) (target:Vertex)
            (graph:DsGraph) (path:Vertex list)
          = [
                let nowPath = path.Append(now) |> List.ofSeq
                if now <> target then
                    for node in graph.GetOutgoingVertices(now) do
                        yield! searchNodes node target graph nowPath
                else
                    yield nowPath
            ]
        searchNodes now target graph []

    /// Get ordered taskDevice routes
    let visitFromSourceToTargetInList
        (now:int * int) (target:int * int) (route:TaskDev list list)
      =
        let rec searchList
                (now:int * int) (target:int * int)
                (route:TaskDev list list) (path:TaskDev list) =
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

    /// Get all ordered routes of api
    let getAllJobDefRoutes (allRoutes:seq<Vertex list>) =
        let allRouteSet = [
            for route in allRoutes do
                [ for v in route do
                    let c = getVertexTarget v
                    c.CallTargetJob.DeviceDefs |> Seq.toList
                ]
        ]

        [
            for route in allRouteSet do
                for sj = 0 to route[0].Length - 1 do
                    for ej = 0 to route[route.Length - 1].Length - 1 do
                        yield visitFromSourceToTargetInList
                            (0, sj) (route.Length - 1, ej) route
        ]

    /// Get all resets
    let private getOneWayResets
            (mutualResets:string seq seq)
            (resets:seq<EdgeDescription>) =
        resets
        |> Seq.filter(fun ({Operator=o}) ->
            o <> TextInterlock || o <> TextInterlockWeak
        )
        |> Seq.map(fun {Source=head; Operator=r; Target=tail} ->
            if r = TextResetEdge || r = TextResetPush then
                seq { head; tail; }
            elif r = TextResetEdgeRev || r = TextResetPushRev  then
                seq { tail; head; }
            else
                Seq.empty
        )
        |> Seq.except(mutualResets)
        |> Seq.filter(Seq.any)

    /// Get mutual resets
    let private getMutualResets (resets:seq<EdgeDescription>) =
        resets
        |> Seq.filter(fun ({Operator=o}) ->
            o = TextInterlock || o = TextInterlockWeak
        )
        |> Seq.map(fun ({Source=source; Target=target}) ->
            let edge = seq { source; target; }
            seq { edge; edge.Reverse(); }
        )
        |> removeDuplicates

    /// Check intersect between two sequences
    let checkIntersect (sourceSeq:Vertex seq) (shatteredSeqs:Vertex seq seq) =
        shatteredSeqs
        |> Seq.filter(fun sr ->
            Enumerable.SequenceEqual(
                Enumerable.Intersect(sourceSeq, sr), sr
            )
        )

    /// Get incoming resets
    let getIncomingResets (resets:'V seq seq) (node:'V) =
        resets
        |> Seq.filter(fun e -> node = e.Last())
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

    /// get detected reset chains in all routes
    let getDetectedResetChains
        (resetChains:seq<string list>) (allRoutes:seq<TaskDev list>)
      =
        allRoutes
        |> Seq.collect(fun route ->
            let nodes = route |> Seq.map(fun v -> v.ApiName)
            resetChains
            |> Seq.map(fun chain ->
                Enumerable.Intersect(nodes, chain) |> List.ofSeq
            )
        )
        |> Seq.distinct
        |> Seq.filter(fun r -> r.Count() > 0)
        |> removeDuplicatesInList

    let getNameStructedChains (resetChains:seq<string list>) =
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

    /// Get origin map
    let getOriginMaps
        (allJobs:seq<TaskDev>)
        (offByOneWayBackwardResets:string list)
        (offByMutualResetChains:string list)
        (structedChains:seq<Map<string, seq<string>>>)
      =
        let allNodes = new Dictionary<string, InitialType>()
        let oneWay   = offByOneWayBackwardResets
        let mutual   = offByMutualResetChains
        let toBeZero = oneWay.Concat(mutual) |> Seq.distinct
        let allJobNames = allJobs |> Seq.map(fun j -> j.ApiName)
        for job in allJobs do
            let jobName = job.ApiName
            if toBeZero.Contains(jobName) &&
                    not (allNodes.ContainsKey(jobName)) then
                allNodes.Add(jobName, Off)
            else
                for resets in structedChains do
                    let isAllResetsInNode =
                        Enumerable.Intersect(
                            resets.Values |> removeDuplicates,
                            allJobNames
                        ) |> List.ofSeq
                    if resets.ContainsKey(jobName) &&
                            resets.Count = isAllResetsInNode.Length &&
                            not (allNodes.ContainsKey(jobName)) then
                        if resets.Count = 2 then
                            let interlocks =
                                resets.Remove(jobName).Head().Value
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

        for chains in structedChains do
            for chain in chains.Values do
                let isIn = Enumerable.Intersect(chain, allJobNames)
                if isIn.Count() = chain.Count() then
                    let checker =
                        chain
                        |> Seq.map(fun v -> allNodes[v])
                        |> Seq.filter(fun v -> v = Off)
                    if checker.Count() > 1 then
                        for node in chain do
                            allNodes[node] <- NeedCheck
                elif isIn.Count() < chain.Count() then
                    for node in chain do
                        if allNodes.ContainsKey(node) then
                            allNodes[node] <- NotCare

        allNodes, allJobs

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

    let getReverseIndexedMap (graph:DsGraph) =
        let reversedMap = new Dictionary<Vertex, int>()
        for v in graph |> getIndexedMap do
            reversedMap.Add(v.Value, v.Key)
        reversedMap

    let getJobIncludedMap (graph:DsGraph) =
        let jobIncludedMap = new Dictionary<string, List<Vertex>>()
        for v in graph.Vertices.Distinct() do
            let call = (getVertexTarget v).CallTargetJob
            for jd in call.DeviceDefs do
                if not (jobIncludedMap.ContainsKey(jd.ApiName)) then
                    jobIncludedMap.Add(jd.ApiName, new List<Vertex>())
                jobIncludedMap[jd.ApiName].Add(v)

        jobIncludedMap

    /// Get origin status of child nodes
    let getOrigins (graph:DsGraph) =
        let rawResets      = getAllResets graph
        let mutualResets   = getMutualResets rawResets
        let oneWayResets   = getOneWayResets mutualResets rawResets
        let resetChains    = getMutualResetChains true mutualResets
        let structedChains = getNameStructedChains resetChains
        let jobNameMap =
            graph.Vertices
            |> Seq.map(fun v -> (getVertexTarget v).CallTargetJob.DeviceDefs)
            |> removeDuplicates
            |> Seq.map(fun jd -> jd.ApiName, jd)
            |> Map.ofSeq
        let jobIncludedMap = getJobIncludedMap graph
        let routeCalculationTargets =
            structedChains
            |> Seq.map(fun chain ->
                [
                    for name in jobNameMap do
                        if chain.ContainsKey(name.Key) then
                            for taskDevice in chain[name.Key] do
                                if jobIncludedMap.ContainsKey(taskDevice) then
                                    jobIncludedMap[taskDevice]
                ]
                |> List.distinct
            )
            |> Seq.filter(fun s -> s.Length > 0)
            |> Seq.distinct
            |> List.ofSeq
        let oneWayResetTargets =
            oneWayResets
            |> Seq.map(fun resets ->
                [
                    for name in jobNameMap do
                        if resets.Contains(name.Key) then
                            for taskDevice in resets do
                                if jobIncludedMap.ContainsKey(taskDevice) then
                                    jobIncludedMap[taskDevice]
                ]
                |> List.distinct
            )
            |> Seq.filter(fun s -> s.Length > 0)
            |> Seq.distinct
            |> List.ofSeq

        let allRoutes =
            routeCalculationTargets.Concat(oneWayResetTargets)
            |> List.ofSeq
            |> List.map(fun resetChain ->
                [
                    for startSet in resetChain do
                    for goalSet  in resetChain do
                    for s in startSet do
                    for g in goalSet  do
                        visitFromSourceToTarget s g graph
                ]
                |> List.filter(fun l -> l.Length > 0)
                |> List.collect id
            )
            |> removeDuplicates
            |> removeDuplicatesInList
            |> getAllJobDefRoutes
            |> removeDuplicates
            |> Seq.map(fun r -> List.distinct r)

        let detectedChains = allRoutes |> getDetectedResetChains resetChains
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
            let detectedChainHeads =
                detectedChains |> Seq.map(fun chain -> chain.Head)
            resetChains
            |> Seq.map(Seq.map(fun v -> v, detectedChainHeads.Contains(v)))
            |> Seq.map(Seq.filter(fun v -> snd v = true))
            |> Seq.filter(fun c -> c.Count() = 1)
            |> Seq.collect(Seq.map(fun v -> fst v))
            |> List.ofSeq
        let allJobs =
            graph.Vertices
            |> Seq.map(getVertexTarget)
            |> Seq.map(fun c -> c.CallTargetJob.DeviceDefs)
            |> removeDuplicates

        getOriginMaps
            allJobs
            offByOneWayBackwardResets offByMutualResetChains
            structedChains
        |> fun (originMap, allJobs) -> originMap, allJobs, resetChains


    /// Get pre-calculated targets that
    /// child segments to be 'ON' in progress(Theta)
    let getThetaTargets (_graph:DsGraph) =
        // To do...
        ()

    type OriginInfo = {
        Real  : Real
        Tasks : (TaskDev*InitialType) seq
        ResetChains : string list seq
    }
    let defaultOriginInfo(real) = {
        Real  = real
        Tasks = [||]
        ResetChains = [||]
    }

    [<Extension>]
    type OriginHelper =
        /// Get origin status of child nodes
        [<Extension>]
        static member GetOrigins (graph:DsGraph) =
            getOrigins graph |> Tuple.tuple1st

        [<Extension>]
        static member GetOriginsWithDeviceDefs (graph:DsGraph) =
            let originMap, allJobs, resetChains = getOrigins graph
            let getjobDef name =
                allJobs.First(fun j -> j.ApiName = name)
            let orgs = originMap |> Seq.map(fun node -> getjobDef node.Key, node.Value)
            orgs, resetChains


        /// Get node index map(key:name, value:idx)
        [<Extension>]
        static member GetIndexedMap (graph:DsGraph) = getIndexedMap graph

        /// Get reset informations from graph
        [<Extension>]
        static member GetAllResets (graph:DsGraph) = getAllResets graph

        /// Get pre-calculated targets that
        /// child segments to be 'ON' in progress(Theta)
        [<Extension>]
        static member GetThetaTargets (graph:DsGraph) = getThetaTargets graph