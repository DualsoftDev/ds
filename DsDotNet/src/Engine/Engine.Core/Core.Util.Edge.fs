namespace Engine.Core

open System.Linq
open System.Runtime.CompilerServices
open System.Collections.Generic
open Dual.Common.Core.FS


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
    let private getWeakEdgeTargets(graph:DsGraph, source:Vertex, bStartEdge:bool) =
        let edges = graph.GetOutgoingEdges(source) |> List.ofSeq
        let mask  = if bStartEdge then EdgeType.Start else EdgeType.Reset

        let srcsWeek   = edges |> filter(fun e -> e.EdgeType = mask )
        srcsWeek |> map (fun e->e.Target)

    /// returns [weak] start incoming/outgoing edges for target
    let getStartWeakEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, true) with
        | DuEssWeak ws when ws.Any() -> ws
        | _ -> []
    /// returns [strong] start incoming/outgoing edges for target
    let getStartStrongEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, true) with
        | DuEssStrong ss when ss.Any() -> ss
        | _ -> []
    /// returns [weak] reset incoming/outgoing edges for target
    let getResetWeakEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, false) with
        | DuEssWeak wr when wr.Any() -> wr
        | _ -> []
    /// returns [strong] reset incoming/outgoing edges for target
    let getResetStrongEdgeSources(target:Vertex) =
        match getEdgeSources (target.Parent.GetGraph(), target, false) with
        | DuEssStrong sr when sr.Any() -> sr
        | _ -> []

    /// returns  reset outgoing edges for target
    let getResetWeakEdgeTargets(source:Vertex) =
        getWeakEdgeTargets (source.Parent.GetGraph(), source, false) 
    /// returns  Start outgoing edges for target
    let getStartWeakEdgeTargets(source:Vertex) =
        getWeakEdgeTargets (source.Parent.GetGraph(), source, true) 



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
        flow.ModelingEdges.Add(mei) |> verifyM $"중복 edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
        createEdge flow.Graph mei

    let createChildEdge(segment:Real) (modelingEdgeInfo:ModelingEdgeInfo<Vertex>) =
        let mei = modelingEdgeInfo
        validateParentOfEdgeVertices mei segment
        segment.ModelingEdges.Add(mei) |> verifyM $"중복 edge {mei.Sources[0].Name}{mei.EdgeSymbol}{mei.Targets[0].Name}"
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





    /// 상호 reset 정보(Mutual Reset Info) 확장
    let internal createMRIEdgesTransitiveClosure4Graph(graph:Graph<'V, 'E>, edgeCreator:'V*'V*EdgeType -> IEdge<'V>) =
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
    
    let getResetRootEdges (v:Vertex) =
        let es = getResetStrongEdgeSources(v)
        let ew = getResetWeakEdgeSources(v)
        es @ ew   

    let getStartRootEdges (v:Vertex) =
        let es = getStartStrongEdgeSources(v)
        let ew = getStartWeakEdgeSources(v)
        es @ ew
            
    
    let checkRealEdgeErrExist (sys:DsSystem) (bStart:bool)  =
        let vs = sys.GetVertices()
        let checkReals = vs.OfType<Real>()
        let realAlias = vs.GetAliasTypeReals()
        let realExs = vs.OfType<RealExF>()
        let realExAlias = vs.GetAliasTypeRealExs()
        let errors = System.Collections.Generic.List<Real>()

        for real in checkReals do
            let realAlias_ = realAlias.Where(fun f -> f.GetPure() = real).OfType<Vertex>()
            let realExs_ = realExs.Where(fun f -> f.GetPure() = real).OfType<Vertex>()
            let realExAlias_ = realExAlias.Where(fun f -> f.GetPure() = real).OfType<Vertex>()
            let checkList = ([real:>Vertex] @ realAlias_ @ realExs_ @ realExAlias_)

            let checks = if bStart then checkList |> Seq.collect(fun f -> getStartRootEdges(f))
                                    else checkList |> Seq.collect(fun f -> getResetRootEdges(f))
            if checks.IsEmpty() then
                errors.Add(real)

        errors

   
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
    
    [<Extension>] static member OfStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofStrongResetEdge edges
    [<Extension>] static member OfWeakResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofWeakResetEdge edges
    [<Extension>] static member OfResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofResetEdge edges
    [<Extension>] static member OfNotResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotResetEdge edges
    [<Extension>] static member OfNotStrongResetEdge<'V, 'E when 'E :> EdgeBase<'V>> (edges:'E seq) = ofNotStrongResetEdge edges

