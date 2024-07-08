namespace Engine.Core

open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module TimeModule =

    let rec dfs (graph: Graph<Vertex, Edge>, current: Vertex, target: Vertex, visited: HashSet<Vertex>, timeAcc: int, maxTime: int ref) =
        if current = target then
            maxTime.Value <- max maxTime.Value timeAcc
        else
            visited.Add(current) |> ignore
            for edge in graph.GetOutgoingEdges(current) do
                if not (visited.Contains(edge.Target)) && edge.Target.Time.IsSome then
                    dfs(graph, edge.Target, target, visited, timeAcc + edge.Target.Time.Value, maxTime)
            visited.Remove(current) |> ignore



    let find_max_path_time (graph: Graph<Vertex, Edge>, srcs: Vertex seq, tgts: Vertex seq) : option<int> =
        let find_time (graph: Graph<Vertex, Edge>, src: Vertex, tgt: Vertex) : option<int> =
            if src.Time.IsNone || tgt.Time.IsNone then
                None
            elif src = tgt then
                Some src.Time.Value
            else
                let maxTime = ref -1
                let visited = HashSet<Vertex>()

                dfs(graph, src, tgt, visited, src.Time.Value, maxTime)

                if maxTime.Value > -1 then Some maxTime.Value else None

        let getDummyAliasTarget name vertex =
            let aliasTarget = 
                match getPure(vertex) with
                | :? Call as c -> DuAliasTargetCall c 
                | :? Real as r -> DuAliasTargetReal r 
                | _ -> failWithLog "Invalid source vertex type"

            let alias = Alias.Create(name, aliasTarget, vertex.Parent, false)
            alias.Time <- Some 0
            alias

        let dummySrc = getDummyAliasTarget [|"dummySrc"|]  (srcs.First())
        let dummyTgt = getDummyAliasTarget [|"dummyTgt"|]  (tgts.First())
        let dummyEdges = HashSet<Edge>()

        for src in srcs do
            Edge.Create(graph, dummySrc, src, EdgeType.Start) |> dummyEdges.Add |> ignore
        for tgt in tgts do
            Edge.Create(graph, tgt, dummyTgt, EdgeType.Start) |> dummyEdges.Add |> ignore

        let time = find_time(graph, dummySrc :> Vertex, dummyTgt :> Vertex)

        graph.RemoveEdges dummyEdges |> ignore
        graph.RemoveVertex(dummySrc :> Vertex) |> ignore
        graph.RemoveVertex(dummyTgt :> Vertex) |> ignore

        time    

    [<Extension>]
    type TimeExt() =
        [<Extension>]
        static member GetDuration(g: Graph<Vertex, Edge>, src: Vertex, tgt: Vertex) : option<int> =
            find_max_path_time(g, [src], [tgt])

        [<Extension>]
        static member GetDuration(g: Graph<Vertex, Edge>, srcs: Vertex seq, tgts: Vertex seq) : option<int> =
            find_max_path_time (g, srcs, tgts)

        [<Extension>]
        static member GetDuration(api:ApiItem) : option<int> =
            let g = api.ApiSystem.Flows.Select(fun f -> f.Graph) |> mergeGraphs |> changeRealGraph
            g.GetDuration(api.TX, api.RX)
