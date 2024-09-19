// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Common

open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Linq
open Dual.Common.Core.FS


[<AutoOpen>]
module internal GraphHelperModule =
    let dumpGraph(graph:TDsGraph<_, _>) =
        let g = graph
        let text =
            [   for e in g.Edges do
                    e.ToString()
                for i in g.Islands do
                    match box i with
                    | :? IQualifiedNamed as v -> v.QualifiedName
                    | _ -> i.Name
            ] |> String.concat "\r\n"

        if text.Any() then
            logDebug $"%s{text}"

        text

    /// 양방향 edge 검색.
    ///
    /// - A > B 와 B > A (혹은 A |> B) 등 복수개의 양방향 edge 가 존재하면
    /// duplexDic[(A, B)] == duplexDic[(B, A)] 값에 A <-> B 모든 edge 를 저장.
    ///
    /// - 단방향 simplex edge 는 값에 하나만 저장됨
    let groupDuplexEdges<'V, 'E
            when 'V :> INamed and 'V : equality
            and 'E :> IEdge<'V> and 'E: equality>
            (edges:'E seq) =

        let duplexEdgeComparer = {
            new IEqualityComparer<'V*'V> with
                member _.Equals(x:'V*'V, y:'V*'V) =
                    let s1, t1 = x
                    let s2, t2 = y
                    (s1 = s2 && t1 = t2) || (s1 = t2 && t1 = s2)
                member _.GetHashCode(x:'V*'V) =
                    let s, t = x
                    s.GetHashCode()/2 + t.GetHashCode()/2
        }

        let duplexDic = Dictionary<'V*'V, HashSet<'E>>(duplexEdgeComparer)

        for e in edges do
            let key = (e.Source, e.Target)
            if not <| duplexDic.ContainsKey(key) then
                duplexDic.Add(key, HashSet<'E>())
            duplexDic[key].Add(e) |> verify

        duplexDic






    (* https://blog.naver.com/ndb796/221236952158 *)
    /// function that returns strongly connected components from given edge lists of graph
    let findStronglyConnectedComponents(graph:TDsGraph<'V, 'E>) (edges:'E seq) =
        let g = graph
        let sccs =
            let sccs:ResizeArray<'V[]> = ResizeArray()
            let visited = new HashSet<'V>()
            let stack = new Stack<'V>()
            let edges = edges.ToHashSet();

            let rec visit(v:'V) =
                if visited.Contains(v) then
                    let mutable cond = stack.Contains(v)
                    if cond then
                        [|
                            while cond do
                                let s = stack.Pop()
                                s
                                cond <- s <> v
                        |] |> sccs.Add
                else
                    visited.Add(v) |> ignore
                    stack.Push(v)

                    let oges = g.GetOutgoingEdges(v).Where(fun e -> edges.Contains(e))
                    let ogvs = oges.Select(fun e -> e.Target).ToArray()
                    if ogvs.IsEmpty then
                        stack.Pop() |> ignore
                    else
                        for ogv in ogvs do
                            visit(ogv)
                        if stack.Any() then
                            stack.Pop() |> ignore

            let vs =
                edges.Collect(fun e -> [ e.Source; e.Target ])
                |> Seq.distinct

            for v in vs do
                visit(v)

            sccs

        sccs


    let validateCylce (graph:TDsGraph<'V, 'E>, allowCyclicGraph:bool) =
        let edges =
            graph.Edges
                .Where(fun e -> not <| e.EdgeType.HasFlag(EdgeType.Reset))
                .ToArray()
        let sccs = findStronglyConnectedComponents graph edges
        if sccs.Any() && not(allowCyclicGraph) then
            let msg =
                [ for vs in sccs do
                    vs.Select(fun v -> v.Name).JoinWith(", ").EncloseWith2("[", "]")
                ].JoinWith("\r\n")
            failwithlogf $"ERROR: Cyclic graph on {msg}"
        true

type GraphHelper =
    [<Extension>] static member Dump(graph:TDsGraph<_, _>) = dumpGraph(graph)
    [<Extension>] static member GetVertices(edge:IEdge<'V>) = [edge.Source; edge.Target]
    [<Extension>] static member ValidateCylce(graph:TDsGraph<'V, 'E>, allowCyclicGraph:bool) = validateCylce(graph, allowCyclicGraph)
    [<Extension>] static member TopologicalSort(graph:TDsGraph<_, _>) = GraphSortImpl.topologicalSort graph
    [<Extension>] static member TopologicalGroupSort(graph:TDsGraph<_, _>) = GraphSortImpl.topologicalGroupSort graph
    /// DAG graph 상의 임의의 두 vertex 가 ancestor-descendant 관계인지 검사하는 함수를 반환
    [<Extension>] static member BuildPairwiseComparer(graph:TDsGraph<_, _>) = GraphPairwiseOrderImpl.isAncestorDescendant (graph, EdgeType.Start)
