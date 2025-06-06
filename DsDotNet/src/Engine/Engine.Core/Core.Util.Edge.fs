namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic

open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Engine.Common

[<AutoOpen>]
module EdgeModule =

    /// Edge source 검색 결과 정보 : target 으로 들어오는 source vertices list 와 그것들이 약연결로 들어오는지, 강연결로 들어오는지 정보
    type EdgeSourcesWithStrength =
        | DuEssWeak of Vertex list
        | DuEssStrong of Vertex list
        | DuEssNone

    /// returns [week] * [strong] incoming edges
    let private getEdgeSources(graph:DsGraph, target:Vertex, bStartEdge:bool) =
        let edges = graph.GetIncomingEdges(target) |> List.ofSeq
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges |> filter(fun e -> e.EdgeType = mask )
        let srcsStrong = edges |> filter(fun e -> e.EdgeType = (mask ||| EdgeType.Strong))

        match srcsWeek.Any(), srcsStrong.Any() with
        | true, true -> failwithlog "인터락 리셋과 후행리셋은 동시에 연결불가능 합니다."
        | true, false -> srcsWeek   |> map (fun e->e.Source) |> DuEssWeak
        | false, true -> srcsStrong |> map (fun e->e.Source) |> DuEssStrong
        | false, false -> DuEssNone

     /// returns Weak outgoing edges
    let private getEdgeTargets(graph:DsGraph, source:Vertex, bStartEdge:bool) =
        let edges = graph.GetOutgoingEdges(source) |> List.ofSeq
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges |> filter(fun e -> e.EdgeType = mask )
        srcsWeek |> map (fun e->e.Target)

    /// returns start incoming/outgoing edges for target
    let getStartEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, true) with
        | DuEssWeak ws when ws.Any() -> ws
        | _ -> []

    /// returns reset incoming/outgoing edges for target
    let getResetEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, false) with
        | DuEssWeak wr when wr.Any() -> wr
        | _ -> []

    /// returns  reset outgoing edges for target
    let getResetEdgeTargets(source:Vertex) =
        getEdgeTargets (source.Parent.GetGraph(), source, false)
    /// returns  Start outgoing edges for target
    let getStartEdgeTargets(source:Vertex) =
        getEdgeTargets (source.Parent.GetGraph(), source, true)



    let private createEdge (graph:DsGraph) (modelingEdgeInfo:ModelingEdgeInfo<'v>) =
         [|
            for src, op, tgt in expandModelingEdge modelingEdgeInfo do
                let edge = Edge.Create(graph, src, tgt, op)
                yield edge
         |]


    let validateParentOfEdgeVertices (mei:ModelingEdgeInfo<Vertex>) (parent:FqdnObject) =
        let invalid = (mei.Sources @ mei.Targets).Select(fun v -> v.Parent.GetCore()).TryFind(fun c -> c <> parent)
        match invalid with
        | Some v -> failwith $"Vertex {v.Name} is not child of flow {parent.Name}"
        | None -> ()

    let private validateChildrenVertexType (mei:ModelingEdgeInfo<Vertex>) =
        let invalidEdge =
            (mei.Sources @ mei.Targets).OfType<Alias>()
                .Where(fun a->a.TargetWrapper.RealTarget().IsSome)

        if invalidEdge.Any() then
            failwith $"Vertex {invalidEdge.First().Name} children type error"


    let toText<'V, 'E when 'V :> INamed and 'E :> DsEdgeBase<'V>> (e:'E) =
        $"{e.Source.Name} {e.EdgeType.ToText()} {e.Target.Name}"

    let toMermaidText<'V, 'E when 'V :> INamed and 'E :> DsEdgeBase<'V>> (e:'E) =
        $"{e.Source.Name} --> {e.Target.Name}"

    let ofResetEdge<'V, 'E when 'E :> DsEdgeBase<'V>> (edges:'E seq) =
        edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset))


    let ofNotResetEdge<'V, 'E when 'E :> DsEdgeBase<'V>> (edges:'E seq) =
        edges.Except(ofResetEdge edges)





    /// 상호 reset 정보(Mutual Reset Info) 확장
    let internal createMRIEdgesTransitiveClosure4Graph(graph:TDsGraph<'V, 'E>, edgeCreator:'V*'V*EdgeType -> IEdge<'V>) =
        // https://www.tutorialspoint.com/Transitive-closure-of-a-Graph
        (*
            Begin
               copy the adjacency matrix into another matrix named transMat
               for any vertex k in the graph, do
                  for each vertex i in the graph, do
                     for each vertex j in the graph, do
                        transMat[i, j] = transMat[i, j] OR (transMat[i, k]) AND transMat[k, j])
                     done
                  done
               done
               Display the transMat
            End
        *)
        let originalGraph = graph
        let es =
            originalGraph.Edges
                .OfType<DsEdgeBase<'V>>()
                .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong ||| EdgeType.Reset))
                .ToArray()

        let gr = TDsGraph(Seq.empty, es, None)
        let vs = gr.Vertices.ToArray()
        let dic = Dictionary<'V*'V, bool>()     // v1 -> v2 : reachable?
        for i in vs do
            for j in vs do
            if i <> j then
                dic.Add((i, j), false)
        for e in es do
            dic[(e.Source, e.Target)] <- true


        for i in vs do
            for j in vs do
            for k in vs do
            if i <> j then
                dic[(i, j)] <- dic[(i, j)] || (i <> k && dic[(i, k)] && dic[(k, j)])

        for KeyValue( (i, j), v) in dic do
            // i -> j 의 reset edge 가 존재하고, j -> i 로도 reset edge 가 존재해야 하지만, 실제 j -> i reset edge 가 없는 경우
            if v && dic[(j, i)] && originalGraph.FindEdges(j, i) |> ofResetEdge |> Seq.isEmpty then
                edgeCreator(j, i, EdgeType.Reset ||| EdgeType.Strong ||| EdgeType.AugmentedTransitiveClosure) |> ignore

    let createMRIEdgesTransitiveClosure(flow:Flow) =
        let edgeCreator = fun (s, t, edgeType) -> Edge.Create(flow.Graph, s, t, edgeType) :> IEdge<Vertex>
        createMRIEdgesTransitiveClosure4Graph(flow.Graph, edgeCreator)

    let createMRIEdgesTransitiveClosure4System(system:DsSystem) =
        for f in system.Flows do
            createMRIEdgesTransitiveClosure f


    let isResetEdge (edge: Edge) = edge.EdgeType.HasFlag(EdgeType.Reset)
    let isStartEdge (edge: Edge) = edge.EdgeType.HasFlag(EdgeType.Start)


    let mergeGraphs (graphs:  TDsGraph<Vertex, Edge> seq) : TDsGraph<Vertex, Edge>  =

        let g = TDsGraph<Vertex, Edge>(None)

        for graph in graphs do
            for vertex in graph.Islands do
                g.Vertices.Add (vertex) |>ignore

            for edge in graph.Edges do
                Edge.Create(g, edge.Source, edge.Target, edge.EdgeType) |> ignore
        g


    let changeRealGraph (graph: TDsGraph<Vertex, Edge>) : TDsGraph<Vertex, Edge>  =

        let g = TDsGraph<Vertex, Edge>(None)

        for vertex in graph.Islands do
            if vertex.TryGetPureCall().IsNone then  //flow에서 조건으로 Call은 제외
                g.Vertices.Add (vertex.GetPureReal():>Vertex) |>ignore

        for edge in graph.Edges do
            if edge.Source.TryGetPureCall().IsNone then //flow에서 조건으로 Call은 제외
                let isEdgeMatch =
                    g.Edges.Any(fun (e:Edge) ->
                        e.EdgeType = edge.EdgeType
                        && e.Source = edge.Source.GetPureReal()
                        && e.Target = edge.Target.GetPureReal())
                if not isEdgeMatch then
                    Edge.Create(g, edge.Source.GetPureReal(), edge.Target.GetPureReal(), edge.EdgeType) |> ignore

        g


    // Recursive function to find all Real objects connected via Start edges
    let getPathReals (graph: TDsGraph<Vertex,Edge>) (srcs: Vertex seq) : Real seq =
        let graphOrder = graph.BuildPairwiseComparer()

        let getStartReals (src: Vertex, visited: HashSet<Real>) : Real seq =
            graph.Edges
            |> filter isStartEdge
            |> collect (fun e -> [e.Source; e.Target])
            |> filter (fun n -> graphOrder src n = Some true)
            |> choose (fun r ->
                match r with
                | :? Real as real ->
                    if not (visited.Contains real)
                    then
                        visited.Add real |> ignore
                        Some(real)
                    else
                        None
                | _ -> failwithlog $"{r.QualifiedName} is not real vertex"
                )

        // Create a HashSet to keep track of visited Real objects
        let allVisited = HashSet<Real>()

        srcs |> Seq.collect (fun f-> getStartReals(f, allVisited))

    /// srcs로 인해서 시작가능한 reals 구하고 구해진 reals에 리셋으로 연결된 target reals 구한다
    let private appendInterfaceReset (graph: TDsGraph<Vertex,Edge>) (srcs: Vertex seq) : Real seq =
        // Find all Real objects connected by Start edges to the source Reals
        let startLinkReals =
            (srcs @ (getPathReals graph srcs).OfType<Vertex>())
            |> distinct

        // Find all target Reals connected by a reset edge to the startLinkReals
        startLinkReals
        |> collect (fun link ->
            graph.Edges
            |> filter isResetEdge
            |> filter (fun e -> e.Source.GetPure() = link.GetPure())
            |> map (fun e -> e.Target.GetPureReal())
        )
        |> Seq.distinct

    /// Automatically append mutual reset information
    let autoAppendInterfaceReset (sys: DsSystem) =
        let graph = sys.Flows.Select(fun f->f.Graph) |>  mergeGraphs |> changeRealGraph
        let apiNResetNodes =
            sys.ApiItems
            |> map (fun api ->
                let resetAbleReals = appendInterfaceReset  graph [api.TX]
                api, resetAbleReals
            )
        apiNResetNodes
        |> iter (fun (api, resetAbleReals) ->
            sys.ApiItems
                .Where(fun f -> f <> api && resetAbleReals.Contains(f.RX))
                .Iter (fun f ->
                        sys.CreateApiResetInfo(api.Name, "|>"|> toModelEdge ,f.Name, true) |> ignore
            )
        )


    let updateDeviceRootInfo (x: DsSystem) =
        let calls = x.GetVerticesHasJob()
        calls.SelectMany(fun c-> c.TaskDefs.Select(fun dev-> dev, c))
             |> groupBy fst
             |> iter(fun (k, vs) ->
                    k.IsRootOnlyDevice <- not(vs.Any(fun (_, c)->c.Parent.GetCore() :? Real))
                )

    type Flow with
        member internal x.CreateEdgeImpl(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =   // see Flow.CreateEdge
            let flow:Flow = x
            let mei = modelingEdgeInfo
            validateParentOfEdgeVertices mei flow
            flow.ModelingEdges.Add(mei) |> verifyM $"중복 edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
            createEdge flow.Graph mei

    type Real with
        member internal x.CreateEdgeImpl(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =   // see Real.CreateEdge
            let segment:Real = x
            let mei = modelingEdgeInfo
            validateParentOfEdgeVertices mei segment
            segment.ModelingEdges.Add(mei) |> verifyM $"중복 edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
            validateChildrenVertexType mei

            createEdge segment.Graph modelingEdgeInfo


[<Extension>]
type EdgeExt =
    [<Extension>] static member ToText<'V, 'E when 'V :> INamed and 'E :> DsEdgeBase<'V>> (edge:'E) = toText edge
    [<Extension>] static member ToMermaidText<'V, 'E when 'V :> INamed and 'E :> DsEdgeBase<'V>> (edge:'E) = toMermaidText edge

    [<Extension>] static member OfResetEdge<'V, 'E when 'E :> DsEdgeBase<'V>> (edges:'E seq) = ofResetEdge edges
    [<Extension>] static member OfNotResetEdge<'V, 'E when 'E :> DsEdgeBase<'V>> (edges:'E seq) = ofNotResetEdge edges


    [<Extension>] static member GetPathReals(inits:Real seq, g:TDsGraph<Vertex,Edge>) : Real seq = getPathReals g (inits.OfType<Vertex>())
