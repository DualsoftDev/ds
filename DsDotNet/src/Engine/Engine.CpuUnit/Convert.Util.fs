[<AutoOpen>]
module Engine.Cpu.ConvertUtil

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System.Collections.Concurrent
 
[<AutoOpen>]
[<Extension>]
type ConvertUtilExt =
    
    [<Extension>] static member GetVertices(sys:DsSystem) =
                    sys.Flows.SelectMany(fun flow->
                        flow.Graph.Vertices 
                            |> Seq.collect(fun v ->
                                match v with
                                | :? Real as r -> r.Graph.Vertices.ToArray() @ [r]
                                | _ -> [|v|])
                           ) 

    [<Extension>] static member AppendSome(xs:Statement<'T> seq, xOpt:Statement<'T> option) =
                                if xOpt.IsSome 
                                then xs |> Seq.append [xOpt.Value]
                                else xs

    [<Extension>] static member FindEdgeSources(target:Vertex, graph:DsGraph, edgeType:ModelingEdgeType) = 
                    let edges = graph.GetIncomingEdges(target)
                    let findEdges = 
                        match edgeType with 
                        | StartEdge        -> edges.OfNotResetEdge().Where(fun e->e.EdgeType.HasFlag(EdgeType.Strong)|> not)
                        | StartPush        -> edges.OfNotResetEdge().Where(fun e->e.EdgeType.HasFlag(EdgeType.Strong))
                        | ResetEdge        -> edges.OfWeakResetEdge()
                        | ResetPush        -> edges.OfStrongResetEdge()
                        | StartReset       
                        | InterlockWeak    
                        | Interlock        -> failwith $"Do not use {edgeType} Error"
        
                    findEdges.Select(fun e->e.Source)
