[<AutoOpen>]
module Engine.Cpu.ConvertUtil

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System.Collections.Concurrent
 
[<AutoOpen>]
[<Extension>]
type ConvertUtil =
    
    [<Extension>] static member GetVertices(sys:DsSystem) =
                    sys.Flows.SelectMany(fun flow->
                        flow.Graph.Vertices 
                            |> Seq.collect(fun v ->
                                match v with
                                | :? Real as r -> r.Graph.Vertices |> Seq.append [r]
                                | _ -> [|v|])
                           ) 

    [<Extension>] static member IncomingReset(target:Vertex, graph:DsGraph, dicM:ConcurrentDictionary<Vertex, DsTag>) =
                     graph.GetIncomingEdges(target)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Reset))
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Strong)|>not)
                        .Select(fun e->dicM[e.Source])
    
    [<Extension>] static member IncomingStart(target:Vertex, graph:DsGraph, dicM:ConcurrentDictionary<Vertex, DsTag>) =
                     graph.GetIncomingEdges(target)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Reset)|>not)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Strong)|>not)
                        .Select(fun e->dicM[e.Source])
    
    [<Extension>] static member IncomingStartStrong(target:Vertex, graph:DsGraph, dicM:ConcurrentDictionary<Vertex, DsTag>) =
                     graph.GetIncomingEdges(target)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Reset)|>not)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Strong))
                        .Select(fun e->dicM[e.Source])

      
    [<Extension>] static member IncomingResetStrong(target:Vertex, graph:DsGraph, dicM:ConcurrentDictionary<Vertex, DsTag>) =
                     graph.GetIncomingEdges(target)
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Reset))
                        .Where(fun e-> e.EdgeType.HasFlag(EdgeType.Strong))
                        .Select(fun e->dicM[e.Source])
    