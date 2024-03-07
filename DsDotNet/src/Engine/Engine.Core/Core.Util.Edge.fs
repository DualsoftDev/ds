namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Dual.Common.Core.FS


[<AutoOpen>]
module EdgeModule =
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
        let invalidEdge =  (mei.Sources @ mei.Targets).OfType<Alias>()
                             .Where(fun a->a.TargetWrapper.RealTarget().IsSome
                                        || a.TargetWrapper.RealExFlowTarget().IsSome)

        if invalidEdge.any() then failwith $"Vertex {invalidEdge.First().Name} children type error"

    let createFlowEdge(flow:Flow) (modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =
        let mei = modelingEdgeInfo
        validateParentOfEdgeVertices mei flow
        flow.ModelingEdges.Add(mei) |> verifyM $"Duplicated edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
        createEdge flow.Graph mei

    let createChildEdge(segment:Real) (modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =
        let mei = modelingEdgeInfo
        validateParentOfEdgeVertices mei segment
        segment.ModelingEdges.Add(mei) |> verifyM $"Duplicated edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
        validateChildrenVertexType mei

        createEdge segment.Graph modelingEdgeInfo

    let toText<'V, 'E when 'V :> INamed and 'E :> EdgeBase<'V>> (e:'E) = $"{e.Source.Name.QuoteOnDemand()} {e.EdgeType.ToText()} {e.Target.Name.QuoteOnDemand()}"

    /// edges 에서 strong reset edge type 만 추려 냄
    let ofStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong ||| EdgeType.Reset))

    let ofResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset))

    let ofWeakResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Where(fun e -> e.EdgeType.HasFlag(EdgeType.Reset) && not <| e.EdgeType.HasFlag(EdgeType.Strong))

    let ofNotStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Except(ofStrongResetEdge edges)
    let ofNotResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) =
            edges.Except(ofResetEdge edges)


    let ofAliasForCallVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.CallTarget().IsSome)

    let ofAliasForRealVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.RealTarget().IsSome)

    let ofAliasForRealExVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.RealExFlowTarget().IsSome)


    /// 상호 reset 정보(Mutual Reset Info) 확장
    let internal createMRIEdgesTransitiveClosure4Graph(graph:Graph<'V, 'E>, edgeCreator:'V*'V*EdgeType -> IEdge<'V>) =
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

    

    let getVerticesOfSystem(system:DsSystem) =
        let realVertices = system.Flows.SelectMany(fun f ->
                                    f.Graph.Vertices.OfType<Real>()
                                        .SelectMany(fun r -> r.Graph.Vertices.Cast<Vertex>()))

        let flowVertices = system.Flows.SelectMany(fun f -> f.Graph.Vertices.Cast<Vertex>())
        realVertices @ flowVertices

    let getVerticesOfFlow(flow:Flow) =
        let realVertices =
            flow.Graph.Vertices.OfType<Real>()
                .SelectMany(fun r -> r.Graph.Vertices.Cast<Vertex>())

        let flowVertices =  flow.Graph.Vertices.Cast<Vertex>()
        realVertices @ flowVertices

    let getDevicesOfFlow(flow:Flow) =
        let devNames = getVerticesOfFlow(flow).OfType<Call>()   
                             .SelectMany(fun c->c.TargetJob.DeviceDefs.Select(fun d->d.DeviceName))

        flow.System.Devices.Where(fun d -> devNames.Contains d.Name)

    let getDistinctApis(x:DsSystem) =
        getVerticesOfSystem(x).OfType<Call>()   
                            .SelectMany(fun c-> c.TargetJob.ApiDefs)
                            .Distinct()

    type DsSystem with
        member x.CreateMRIEdgesTransitiveClosure() = createMRIEdgesTransitiveClosure4System x
                                   

    type Flow with
        member x.CreateEdge(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =
            createFlowEdge x modelingEdgeInfo
    type Real with
        member x.CreateEdge(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =
            createChildEdge x modelingEdgeInfo

[<Extension>]
type EdgeExt =
    [<Extension>] static member ToText<'V, 'E when 'V :> INamed and 'E :> EdgeBase<'V>> (edge:'E) = toText edge
    [<Extension>] static member GetVertices(edges:IEdge<'V> seq) = edges.Collect(fun e -> e.GetVertices())
    [<Extension>] static member GetVertices(x:DsSystem) =  getVerticesOfSystem x
    [<Extension>] static member GetVerticesOfFlow(x:Flow) =  getVerticesOfFlow x
    [<Extension>] static member GetVerticesOfCoins(x:DsSystem) = 
                    let vs = x.GetVertices()
                    let calls = vs.OfType<Call>().Cast<Vertex>()
                    let aliases = vs.OfType<Alias>().Cast<Vertex>()
                    (calls@aliases)
                        .Where(fun c->c.Parent.GetCore() :? Real)     

    [<Extension>] static member GetVerticesOfCoinCalls(x:DsSystem) = 
                    x.GetVertices().OfType<Call>().Where(fun c->c.Parent.GetCore() :? Real)    
    [<Extension>] static member GetDevicesOfFlow(x:Flow) =  getDevicesOfFlow x
    [<Extension>] static member GetDistinctApis(x:DsSystem) =  getDistinctApis x
    
    [<Extension>] static member GetAliasTypeReals(xs:Vertex seq)   = ofAliasForRealVertex xs
    [<Extension>] static member GetAliasTypeRealExs(xs:Vertex seq) = ofAliasForRealExVertex xs
    [<Extension>] static member GetAliasTypeCalls(xs:Vertex seq)   = ofAliasForCallVertex xs

    [<Extension>] static member OfStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofStrongResetEdge edges
    [<Extension>] static member OfWeakResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofWeakResetEdge edges
    [<Extension>] static member OfResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofResetEdge edges
    [<Extension>] static member OfNotResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotResetEdge edges
    [<Extension>] static member OfNotStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotStrongResetEdge edges

