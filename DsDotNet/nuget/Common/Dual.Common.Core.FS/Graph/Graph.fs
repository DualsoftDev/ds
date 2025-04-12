// Copyright (c) Dualsoft  All Rights Reserved.
namespace Dual.Common.Core.FS

open System.Collections.Generic
open System.Linq
open Dual.Common.Base.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module GraphModule =
    /// Graph<'V, 'E> Vertex 의 interface
    [<AllowNullLiteral>]
    type IVertex = interface end

    /// 기존 INamed 기반의 vertex 대체용.
    type IVertexKey =
        inherit IVertex
        abstract VertexKey: string with get, set

    /// Graph<'V, 'E> Edge 의 interface
    type IEdge<'V> =
        abstract Source :'V    //방향을 고려안한 위치상 왼쪽   Vertex
        abstract Target :'V    //방향을 고려안한 위치상 오른쪽 Vertex
        abstract Edge: obj

    /// Graph<'V, 'E> 구성하는 기본 Edge
    [<AbstractClass>]
    type EdgeBase<'V, 'E>(source:'V, target:'V, edge:'E) =
        interface IEdge<'V> with
            member x.Source = x.Source
            member x.Target = x.Target
            member x.Edge = x.Edge

        member _.Source = source
        member _.Target = target
        member _.Edge = edge

    // [NOTE] GraphVertex
    /// Graph 상의 vertex 가 추가/삭제 될 때의 event handlers
    type GraphVertexAddRemoveHandlers(onVertexAdded, onVertexRemoved) =
        member _.OnVertexAdded: IVertexKey -> bool = onVertexAdded
        member _.OnVertexRemoved: IVertexKey -> bool = onVertexRemoved

    /// 범용 Graph<'V, 'E>.  DS 그래프는 TDsGraph 참조 (Engine.Common)
    type Graph<'V, 'E
            when 'V :> IVertexKey and 'V : equality
            and 'E :> IEdge<'V> and 'E: equality> (
            vertices_:'V seq,
            edges_:'E seq,
            vertexHandlers:GraphVertexAddRemoveHandlers option
    ) =
        //do
            //tracefn $"Creating Graph with #vs = {vertices_.Count()}, #es = {edges_.Count()}"

        let edgeComparer = {
            new IEqualityComparer<'E> with
                member _.Equals(x:'E, y:'E) = x.Source = y.Source && x.Target = y.Target && x.Edge = y.Edge
                member _.GetHashCode(x) = x.Source.GetHashCode()/2 + x.Target.GetHashCode()/2
        }

        let vertexComparer = {
            new IEqualityComparer<'V> with
                member _.Equals(x:'V, y:'V) = x.VertexKey = y.VertexKey
                member _.GetHashCode(x) = x.GetHashCode()
         }

        let vertices = (vertices_ @ edges_.Collect(fun e -> [e.Source; e.Target])) |> distinct
        let vs = new HashSet<'V>(vertices, vertexComparer)
        let es = new HashSet<'E>(edges_, edgeComparer)

        // [NOTE] GraphVertex
        let addVertex(vertex:'V) =
#if DEBUG
            let result1 = vs.Add vertex
            let result2 = vertexHandlers |> map (fun h -> h.OnVertexAdded (vertex :> IVertexKey)) |? true
            result1 && result2
#else
            vs.Add vertex && vertexHandlers |> map (fun h -> h.OnVertexAdded (vertex :> IVertexKey)) |? true
#endif

        let removeVertex(vertex:'V) =
            vs.Remove vertex && vertexHandlers |> map (fun h -> h.OnVertexRemoved (vertex :> IVertexKey)) |? true

        new () = Graph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>, None)
        new (vs, es) = Graph<'V, 'E>(vs, es, None)
        new (vertexHandlers:GraphVertexAddRemoveHandlers option) = Graph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>, vertexHandlers)

        member _.Vertices = vs
        member _.Edges = es
        /// 중복 edge 삽입은 허용되지 않으나, 중복된 항목이 존재하면 이를 무시하고 false 를 반환.  중복이 없으면 true 반환
        member _.AddEdges(edges:'E seq) =
            edges
            |> Seq.forall(fun e ->
                [ e.Source; e.Target ]
                |> Seq.filter(fun v -> not <| vs.Contains(v))
                |> Seq.iter(fun v -> addVertex v |> ignore)
                es.Add(e))  // |> verifyMessage $"Duplicated edge [{e.Source.Name} -> {e.Target.Name}]"

        member _.RemoveEdges(edges:'E seq)       = edges    |> Seq.forall es.Remove

        member _.AddVertices(vertices:'V seq)    = vertices |> Seq.forall addVertex
        member _.RemoveVertices(vertices:'V seq) = vertices |> Seq.forall removeVertex


        member x.AddEdge(edge:'E)        = x.AddEdges([edge])
        member x.RemoveEdge(edge:'E)     = x.RemoveEdges([edge])
        member x.AddVertex(vertex:'V)    = x.AddVertices([vertex])
        member x.RemoveVertex(vertex:'V) = x.RemoveVertices([vertex])
        member _.TryFindVertex(name:string) = vs |> Seq.tryFind(fun v ->v.VertexKey = name)
        member x.TryFindVertex<'T>(name:string) = x.TryFindVertex(name).Filter(isType<'T>).Map(forceCast<'T>)
        member _.FindVertex(name:string) = vs.FirstOrDefault(fun v -> v.VertexKey = name)
        member _.FindEdges(source:string, target:string) = es.Where(fun e -> e.Source.VertexKey = source && e.Target.VertexKey = target)
        member _.FindEdges(source:'V, target:'V) = es.Where(fun e -> e.Source = source && e.Target = target)

        /// Island 가 아닌 vertices.  즉 edge 와 연결을 갖는 vertices
        member private x.ConnectedVertices = x.Edges |> Seq.collect(fun e -> [e.Source; e.Target]) |> Seq.distinct
        member x.Islands = x.Vertices.Except(x.ConnectedVertices)
        /// Island 가 아닌 연결 처음 시작 vertices. 
        member x.HeadConnectedOrSingleVertex = if x.Vertices.Count > 1 then x.Inits.Except(x.Islands) else  x.Vertices 
        member x.GetIncomingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Target = vertex)
        member x.GetOutgoingEdges(vertex:'V) = x.Edges.Where(fun e -> e.Source = vertex)
        member x.GetEdges(vertex:'V) = x.GetIncomingEdges(vertex).Concat(x.GetOutgoingEdges(vertex))
        member x.GetIncomingVertices(vertex:'V) = x.GetIncomingEdges(vertex).Select(fun e -> e.Source)

        member x.GetIncomingVerticesWithEdgeFilter(vertex:'V, f:'E->bool) =
            x.GetIncomingEdges(vertex).Where(f)
                .Select(fun e -> e.Source)
        member x.GetOutgoingVerticesWithEdgeType(vertex:'V, f:'E->bool) =
            x.GetOutgoingEdges(vertex).Where(f)
                .Select(fun e -> e.Target)

        member x.GetOutgoingVertices(vertex:'V) = x.GetOutgoingEdges(vertex).Select(fun e -> e.Target)


        abstract member Inits: 'V seq
        abstract member Lasts: 'V seq
        default x.Inits =
            let inits = x.Vertices.Where(fun v -> x.GetIncomingEdges(v) |> Seq.isEmpty)
            x.Islands @ inits
        default x.Lasts =
            let lasts = x.Vertices.Where(fun v -> x.GetOutgoingEdges(v) |> Seq.isEmpty)
            x.Islands @ lasts


        /// Graph 상의 connected components 들을 찾아서 각 connected components 를 구성하는 edge 들('E[]) 의 목록('E[][])을 반환
        member x.GetEdgesOfConnectedComponents(edgeFilter:'E -> bool) : 'E[][] =
            let visitedEdges = HashSet<'E>()  // 방문한 엣지 기록
            let ess = ResizeArray<'E list>()  // 각 성분을 엣지 리스트로 저장

            // 깊이 우선 탐색 함수 (엣지 기반 탐색)
            let rec dfsEdges (vertex: 'V) (currentComponent: ResizeArray<'E>) =
                // vertex 에서 나가는 모든 엣지들에 대해
                x.GetOutgoingEdges(vertex).Filter(edgeFilter) >>: fun edge ->
                    // 아직 방문하지 않은 엣지에 대해서만
                    if !! visitedEdges.Contains(edge) then
                        visitedEdges.Add(edge) |> ignore  // 엣지를 방문한 것으로 표시
                        currentComponent.Add(edge)        // 현재 성분에 엣지를 추가
                        dfsEdges edge.Target currentComponent  // 엣지의 타겟 정점으로 이동하여 재귀 탐색

            // 모든 정점에 대해 탐색 시작
            x.Vertices >>: fun vertex ->
                // 방문하지 않은 엣지가 있으면 새로운 연결 성분을 시작
                if x.GetOutgoingEdges(vertex).Filter(edgeFilter) |> exists (visitedEdges.Contains >> not) then
                    let es = ResizeArray<'E>() // 새로운 성분 생성
                    dfsEdges vertex es                            // 해당 성분을 탐색
                    ess.Add(es |> List.ofSeq)               // 탐색된 성분을 추가

            ess |> map toArray |> toArray  // 'E[][] 형태로 변환하여 반환

        /// Graph 상의 connected components 들을 찾아서 각 connected components 를 구성하는 edge 들('E[]) 의 목록('E[][])을 반환
        member x.GetEdgesOfConnectedComponents() : 'E[][] = x.GetEdgesOfConnectedComponents(konst true)

