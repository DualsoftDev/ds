namespace Engine.Core

open System
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Linq
open Engine.Common

[<AutoOpen>]
module TimeModule =

    let rec dfs (graph: TDsGraph<Vertex, Edge>, current: Vertex, target: Vertex, visited: HashSet<Vertex>, timeAcc: float, maxTime: float ref) =
        if current = target then
            maxTime.Value <- max maxTime.Value timeAcc
        else
            visited.Add(current) |> ignore
            for edge in graph.GetOutgoingEdges(current) do
                if not (visited.Contains(edge.Target)) && edge.Target.GetPureReal().Time.IsSome then
                    dfs(graph, edge.Target, target, visited, timeAcc + edge.Target.GetPureReal().Time.Value, maxTime)
            visited.Remove(current) |> ignore

    let find_max_path_time (graph: TDsGraph<Vertex, Edge>, srcs: Vertex seq, tgts: Vertex seq) : option<float> =
        let find_time (graph: TDsGraph<Vertex, Edge>, src: Vertex, tgt: Vertex) : option<float> =
            if src.GetPureReal().Time.IsNone || tgt.GetPureReal().Time.IsNone then
                None
            elif src = tgt then
                Some (src.GetPureReal().Time.Value)
            else
                let maxTime = ref -1.0
                let visited = HashSet<Vertex>()

                dfs(graph, src, tgt, visited, src.GetPureReal().Time.Value, maxTime)

                if maxTime.Value > -1.0 then Some maxTime.Value else None

        let getDummyTarget (vertex:Vertex) =
            let real = Real.Create(Guid.NewGuid().ToString(), vertex.Parent.GetFlow())
            real.Time <- Some 0.0
            real

        let dummySrc = getDummyTarget (srcs.First())
        let dummyTgt = getDummyTarget (tgts.First())
        let dummyEdges = HashSet<Edge>()

        for src in srcs do
            Edge.Create(graph, dummySrc, src, EdgeType.Start) |> dummyEdges.Add |> ignore
        for tgt in tgts do
            Edge.Create(graph, tgt, dummyTgt, EdgeType.Start) |> dummyEdges.Add |> ignore

        let time = find_time(graph, dummySrc :> Vertex, dummyTgt :> Vertex)

        graph.RemoveEdges dummyEdges |> ignore
        graph.RemoveVertex(dummySrc :> Vertex) |> ignore
        graph.RemoveVertex(dummyTgt :> Vertex) |> ignore
        //flow Graph에서도 삭제해야함
        dummySrc.Parent.GetGraph().RemoveVertex (dummySrc :> Vertex) |> ignore
        dummyTgt.Parent.GetGraph().RemoveVertex (dummyTgt :> Vertex) |> ignore

        time 

    [<Extension>]
    type TimeExt() =
        [<Extension>]
        static member GetDuration(g: TDsGraph<Vertex, Edge>, src: Vertex, tgt: Vertex) : option<float> =
            find_max_path_time(g, [src], [tgt])

        [<Extension>]
        static member GetDuration(g: TDsGraph<Vertex, Edge>, srcs: Vertex seq, tgts: Vertex seq) : option<float> =
            find_max_path_time (g, srcs, tgts)

        [<Extension>]
        static member GetDuration(api:ApiItem) : option<float> =
            let g = api.ApiSystem.Flows.Select(fun f -> f.Graph) |> mergeGraphs |> changeRealGraph
            g.GetDuration(api.TX, api.RX)
