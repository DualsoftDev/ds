namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Engine.Common.FS


[<AutoOpen>]
module EdgeModule =


    let internal edgeTypeTuples =
        [
            EdgeType.Default, TextStartEdge
            EdgeType.Default ||| EdgeType.Strong, TextStartPush     
            EdgeType.Reset, TextResetEdge     
            EdgeType.Reset ||| EdgeType.Strong , TextResetPush     
        ] |> Tuple.toDictionary

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let createEdgesReArranged(source:'V, operator:string, target:'V) =
        [
            match operator with
            | TextInterlockWeak -> //"<|>" 
                yield source, EdgeType.Reset, target
                yield target, EdgeType.Reset, source

            | TextInterlock -> //"<||>" 
                yield source, EdgeType.Reset ||| EdgeType.Strong , target
                yield target, EdgeType.Reset ||| EdgeType.Strong , source
            
            | TextStartEdge  -> yield source, EdgeType.Default, target  //">" 
            | TextStartPush  -> yield source, EdgeType.Default ||| EdgeType.Strong, target //">>" 
            | TextResetEdge  -> yield source, EdgeType.Reset, target //"|>"
            | TextResetPush  -> yield source, EdgeType.Reset ||| EdgeType.Strong, target //"||>"

            | TextStartEdgeRev  -> yield target, EdgeType.Default, source   //"<"
            | TextStartPushRev  -> yield target, EdgeType.Default ||| EdgeType.Strong, source   //"<<"
            | TextResetEdgeRev  -> yield target, EdgeType.Reset, source //"<|"
            | TextResetPushRev  -> yield target, EdgeType.Reset ||| EdgeType.Strong, source //"<||"

            | _ ->
                failwithlogf $"Unknown causal operator [{operator}]."
        ]

    let createFlowEdges(flow:Flow, source:Vertex, target:Vertex, operator:string) =
        [|
            for src, op, tgt in createEdgesReArranged(source, operator, target) do
                yield Edge.Create(flow.Graph, src, tgt, op)
        |]

    let createChildEdges(segment:Real, source:Vertex, target:Vertex, operator:string) =
        [|
            for src, op, tgt in createEdgesReArranged(source, operator, target) do
                yield Edge.Create(segment.Graph, src, tgt, op)
        |]


    let ofResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset))
    let ofStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong ||| EdgeType.Reset))
    let ofWeakResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset) && not <| e.EdgeType.HasFlag(EdgeType.Strong))
            
    let ofNotStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Except(ofStrongResetEdge edges)
    let ofNotResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Except(ofResetEdge edges)

    let toText<'V, 'E when 'V :> INamed and 'E :> EdgeBase<'V>> (e:'E) = $"{e.Source.Name.QuoteOnDemand()} {e.EdgeType.ToText()} {e.Target.Name.QuoteOnDemand()}"

    /// 상호 reset 정보(Mutual Reset Info) 확장
    let internal createMRIEdgesTransitiveClosure4Graph(graph:Graph<'V, 'E>, edgeCreator:'V*'V*EdgeType -> IEdge<'V>) =
        // todo: system 의 flow 에 대해서 MRI 를 갖는 real 들의 MRI edge 생성
        // https://www.tutorialspoint.com/Transitive-closure-of-a-Graph
        (*
            Begin
               copy the adjacency matrix into another matrix named transMat
               for any vertex k in the graph, do
                  for each vertex i in the graph, do
                     for each vertex j in the graph, do
                        transMat[i, j] := transMat[i, j] OR (transMat[i, k]) AND transMat[k, j])
                     done
                  done
               done
               Display the transMat
            End
        *)
        let originalGraph = graph
        let es =
            originalGraph.Edges
                .OfType<EdgeBase<'V>>()
                .Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong ||| EdgeType.Reset))
                .ToArray()

        let gr = Graph(Seq.empty, es)
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
            if v && dic[(j, i)] && originalGraph.FindEdges(j, i) |> ofStrongResetEdge |> Seq.isEmpty then
                edgeCreator(j, i, EdgeType.Reset ||| EdgeType.Strong ||| EdgeType.AugmentedTransitiveClosure) |> ignore

    let createMRIEdgesTransitiveClosure(flow:Flow) =
        let edgeCreator = fun (s, t, edgeType) -> Edge.Create(flow.Graph, s, t, edgeType) :> IEdge<Vertex>
        createMRIEdgesTransitiveClosure4Graph(flow.Graph, edgeCreator)

    let createMRIEdgesTransitiveClosure4System(system:DsSystem) =
        for f in system.Flows do
            createMRIEdgesTransitiveClosure f

    let validateModel(model:Model) =
        let cores =
            model.Spit()
                .Select(fun sp -> sp.GetCore())

        for f in cores.OfType<Flow>() do
            try
                f.Graph.Validate() |> ignore
            with exn ->
                logWarn "%A" exn

        cores.OfType<Real>()
            .Select(fun r -> r.Graph.Validate())
            .All(id)


[<Extension>]
type EdgeExt =
    [<Extension>] static member ToText(edgeType:EdgeType) = edgeTypeTuples[edgeType]
    [<Extension>] static member IsStart(edgeType:EdgeType) = edgeType.HasFlag(EdgeType.Reset)|> not
    [<Extension>] static member IsReset(edgeType:EdgeType) = edgeType.HasFlag(EdgeType.Reset) 
    [<Extension>] static member GetEdgeType(causal:string) =    // EdgeCausalType
                    edgeTypeTuples.Where(fun kv -> kv.Value = causal).Select(fun kv -> kv.Key).First()

   
    [<Extension>] static member CreateEdges(flow:Flow, source:Vertex, target:Vertex, operator:string) =
                    createFlowEdges(flow, source, target, operator)
    [<Extension>] static member CreateEdges(segment:Real, source:Vertex, target:Vertex, operator:string) =
                    createChildEdges(segment, source, target, operator)

    [<Extension>] static member CreateMRIEdgesTransitiveClosure(model:Model) =
                    for sys in model.Systems do
                        createMRIEdgesTransitiveClosure4System sys

    [<Extension>] static member Validate(model:Model) = validateModel model
    
    [<Extension>] static member OfStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofStrongResetEdge edges
    [<Extension>] static member OfWeakResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofWeakResetEdge edges
    [<Extension>] static member OfNotResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotResetEdge edges
    [<Extension>] static member OfNotStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotStrongResetEdge edges
    [<Extension>] static member ToText<'V, 'E when 'V :> INamed and 'E :> EdgeBase<'V>> (edge:'E) = toText edge
    [<Extension>] static member GetVertices(edges:IEdge<'V> seq) = edges.Collect(fun e -> e.GetVertices())

