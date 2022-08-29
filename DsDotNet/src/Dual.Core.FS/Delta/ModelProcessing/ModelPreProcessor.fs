namespace Dual.Core.ModelPostProcessor


open Dual.Core
open Dual.Common
open Dual.Common.Graph.QuickGraph
open Dual.Core.Types
open Dual.Core.QGraph
open QuickGraph

module PreProcessor =
    let tagTypeChecker (t : TagType option) =
        match t with
        | Some(t) -> t
        | _ -> TagType.Dummy

    ///
    let generatePort (v:IVertex) =
        let addPort cate =
            if v.Ports.ContainsKey(cate) |> not then 
                v.Ports.Add(cate, QgPort(v, cate))
        
        addPort PortCategory.Start 
        addPort PortCategory.Finish
        addPort PortCategory.Ready 
        addPort PortCategory.Going 
        addPort PortCategory.Homing
        addPort PortCategory.Reset 
        addPort PortCategory.Sensor

        v
        
    /// 포트에 설정된 TagType으로 PLCTag를 생성
    let generatePortTag (opt:CodeGenerationOption) (segs:ISegment seq) (v:IVertex) =
        let generateTag v (f:(IVertex -> string) Option) (cate:PortCategory) (port:IPort) = 
            let name f = 
                match f with
                | Some(f) -> f v
                | None -> failwithlogf "%A가 정의되지않았습니다." f

            let tagtype = 
                match port.TagType with
                | Some(t) -> t
                | None -> 
                    let segtype = v |> IVertexExt.GetSegmentType segs
                    match segtype with
                    | Internal -> TagType.Dummy
                    | External -> 
                        match cate with
                        | Start -> TagType.Action
                        | Sensor -> TagType.State
                        | _ -> TagType.Dummy

            if port.Address.length() > 1 then
                let tags =
                    port.Address 
                    |> Seq.mapi(fun i (addr, isPosi) -> 
                        if isPosi then PLCTag(name f + i.ToString(), Some tagtype, Some addr)
                        else NegPLCTag(name f + i.ToString(), Some tagtype, Some addr) :> PLCTag
                        )
                port.PLCTags.AddRange tags
                port.EndTag <- PLCTag(name f, Some TagType.Dummy, None) |> Some
            else if port.Address.isEmpty() then
                port.PLCTags.Add (PLCTag(name f, Some TagType.Dummy, None))
            else
                let tag = 
                    let addr, isPosi = port.Address |> Seq.head
                    if isPosi then PLCTag(name f, Some TagType.Dummy, addr |> Some)
                    else NegPLCTag(name f, Some TagType.Dummy, addr |> Some) :> PLCTag
                port.PLCTags.Add tag

            port.DummyTag <- PLCTag(name f + "Temp", Some TagType.Dummy, None) |> Some
            port
            
        v.Ports <- v.Ports |> Seq.map(fun kv -> 
            let generators = 
                match kv.Key with
                | PortCategory.Start -> opt.CoilTagGenerator
                | PortCategory.Sensor -> opt.SensorTagGenerator
                | PortCategory.Ready -> opt.StandbyStateNameGenerator
                | PortCategory.Going -> opt.GoingStateNameGenerator
                | PortCategory.Homing -> opt.HomingStateNameGenerator
                | PortCategory.Reset -> opt.ResetNameGenerator
                | PortCategory.Finish -> opt.FinishStateNameGenerator
            kv.Key, kv.Value |> generateTag v generators (kv.Key)) 
            |> dict
        v

    /// Edge를 기준으로 Port의 ConnectedVertices에 Vertex를 넣어줌
    /// @@ Port to Port 관계가 중요해지면서 Vertex Edge에 의한 연결관계를 확인할수없어 사용 중단 
    //let inputConnectedvertices (graph:DAG) =
    //    graph.Edges
    //    |> Seq.iter(
    //        fun e ->
    //            let sourceName = e.Source.Name
    //            let vertexList = 
    //                [e.Source; e.Target;]

    //            vertexList
    //            |> List.iter(
    //                fun v ->
    //                    if v.Name = sourceName then
    //                        v.FinishPort.ConnectedVertices.Add vertexList.[1]
    //                    else
    //                        v.StartPort.ConnectedVertices.Add vertexList.[0]
    //            )
    //    )
    //    graph

    ///
    let resultChecker (graph:DAG) (vertex:IVertex) (reset:IVertex) =
        // 1st status check with reset edges in a graph
        let result =
            let forward = graph.GetAllPaths(reset, vertex).any()
            let reverse = graph.GetAllPaths(vertex, reset).any()

            match forward, reverse with
            | true, false -> VertexStatus.Finish
            | false, true -> VertexStatus.Ready
            | false, false -> VertexStatus.Undefined
            | _ when vertex.isSelfReset() -> VertexStatus.Ready
            | _ -> VertexStatus.Impossible

        // 2nd status check using reset edges from anywhere
        let rec depthAddrCompare (source:int list) (target:int list) (depth:int) = 
            if source.Length < depth || target.Length < depth then
                VertexStatus.Undefined
            else
                let compare = source.[depth] - target.[depth]
                if compare < 0 then
                    VertexStatus.Ready
                elif compare > 0 then
                    VertexStatus.Finish
                else
                    depthAddrCompare source target (depth + 1)

        let compareAddr (source:IVertex) (target:IVertex) =
            let srcAddr = source.GetAllDepthAddress()
            let tgtAddr = target.GetAllDepthAddress()
            depthAddrCompare srcAddr tgtAddr 0

        let resetChecker (vertex:IVertex) = vertex.ResetPort.ConnectedVertices |> Seq.iter(fun r -> vertex.InitialStatus <- compareAddr vertex r)
            
        if vertex.InitialStatus = VertexStatus.Undefined then
            match result with
            | VertexStatus.Impossible 
            | VertexStatus.Undefined -> resetChecker vertex
            | _ -> vertex.InitialStatus <- result

    let traversingToSearchResets (graph:DAG) =
        graph.Vertices
        |> Seq.iter(
            fun v -> 
                v.getResetVertices()
                |> Seq.iter(fun r -> resultChecker graph v r)
            )

        graph
    
    /// 
    let rec vertexPostProcessing (graph:DAG) =
        graph
        ////|> inputConnectedvertices
        |> traversingToSearchResets
        |> ignore
        
        graph.Vertices
        |> Seq.collect(fun v -> 
               v.DAGs 
               |> Seq.map(fun es -> es.Edges.ToAdjacencyGraph())
           ) 
        |> Seq.iter(vertexPostProcessing)

    /// root Edges부터 시작하여 모든 child의 Vertex Address를 Setting
    let rec setVertexAddress (rootGraph:AdjacencyGraph<IVertex, IEdge>) =
        /// vs들에게 address와 tuple로 만들어주고 next node가있으면 addr를 증가해 재귀
        let rec getAddress (g:AdjacencyGraph<IVertex, IEdge>) (vs:IVertex seq) (addr:int) : (IVertex * int) seq =
            let next = vs |> Seq.collect(fun v -> g.GetOutgoingVertices v) 
            seq{
                yield! vs |> Seq.map(fun v -> v, addr)
                if next.isEmpty() |> not then yield! getAddress g next (addr+1)
            }

        let result = getAddress rootGraph (rootGraph.GetInitialVertices()) 1 |> Seq.sortByDescending(snd) |> Seq.distinctBy(fst) 

        /// vertex, addr tuple을 가지고 vertex에 addr setting
        result |> Seq.iter(fun (v, addr) -> v.Address <- addr)

        /// child Edges가있으면 재귀
        rootGraph.Vertices |> Seq.collect(fun v -> v.DAGs |> Seq.map(fun dag -> dag.Edges.ToAdjacencyGraph())) |> Seq.iter(setVertexAddress)


    /// true : v1 >= v2
    /// false : v1 < v2
    let compareAddress (v1:IVertex) (v2:IVertex) : bool =
        let addr1 = v1.GetAllDepthAddress()
        let addr2 = v2.GetAllDepthAddress()
        let length = if addr1.Length > addr2.Length then addr2.Length else addr1.Length

        let rec result idx =
            if idx < (length-1) && addr1.[idx] = addr2.[idx] then 
                result (idx+1)
            else if addr1.[idx] >= addr2.[idx] then
                true
            else 
                false
        
        result 0

    /// 반복사용된 버텍스의 리셋 연관 관계를 순서에 따라 재설정
    let repairingVertexReset (rootGraph:AdjacencyGraph<IVertex, IEdge>) = 
        let repairing (all:IVertex seq) (dest:IVertex) = 
            let vs = all |> Seq.where(fun v -> v.Name = dest.Name) |> Seq.sortBy(fun v -> v.Address) |> List.ofSeq
            let resets = vs |> Seq.collect(fun v -> v.getResetVertices()) |> Seq.sortBy(fun v -> v.Address) |> List.ofSeq
            let getPairReset v =
                let section = vs |> Seq.pairwise
                let first = vs |> Seq.head
                let last = vs |> Seq.last
                let firstReset = resets |> Seq.head

                /// 버텍스가 리셋보다 먼저 일때 or 셀프 (인 -> `인 or self)
                if compareAddress first firstReset then
                   match section |> Seq.tryFind(fun (v1, _) -> v1 = v) with
                   | Some(v1, v2) ->  resets |> Seq.where(fun reset -> compareAddress reset v1 && compareAddress reset v2 |> not)
                   | None -> resets |> Seq.where(fun reset -> compareAddress reset last)
                /// 리셋이 버텍스보다 앞일때 (`인 -> 인)
                else 
                    match section |> Seq.tryFind(fun (_, v2) -> v2 = v) with
                    | Some(v1, v2) ->  resets |> Seq.where(fun reset -> compareAddress v1 reset |> not && compareAddress v2 reset)
                    | None -> resets |> Seq.where(fun reset -> compareAddress first reset)

            vs 
            |> Seq.iter(fun v -> 
                let resets = getPairReset v |> Seq.cast<IVertex> |> List.ofSeq
                v <||< resets
                )

        let vertices = getAllVertices rootGraph

        /// 중복으로 사용되는 버텍스만 골라서 repairing
        vertices 
        |> Seq.groupBy(fun v -> v.Name) 
        |> Seq.where(fun kv -> snd kv |> Seq.length > 1) 
        |> Seq.collect(snd) 
        |> Seq.distinctBy(fun v -> v.Name)
        |> Seq.iter(repairing vertices)

    let PreProcessModel (model:QgModel) =
        setVertexAddress model.DAG |> ignore
        repairingVertexReset model.DAG |> ignore
        vertexPostProcessing model.DAG |> ignore
        model

    let PreProcessVertex (opt:CodeGenerationOption) (segs:ISegment seq) (vs:IVertex list) =
        vs 
        |> List.iter(fun v ->
            v
            |> generatePort
            |> generatePortTag opt segs
            |> ignore
            )
