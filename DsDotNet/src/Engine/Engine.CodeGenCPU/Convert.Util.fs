[<AutoOpen>]
module Engine.CodeGenCPU.ConvertUtil

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


    [<Extension>]  static member GetCoinTags(coin:Vertex, memory:DsMemory, isInTag:bool) =
                            match coin with
                            | :? Call as c -> c.CallTarget.JobDefs
                                                .Select(fun j-> 
                                                            if isInTag
                                                            then PlcTag.Create(j.ApiName+"_I", false)
                                                            else PlcTag.Create(j.ApiName+"_O", false)
                                                )
                                                .Cast<Tag<bool>>()
                            | :? Real | :? RealEx ->   //가상부모에 의해 Coin이 Real으로 올 수 있음
                                                if isInTag
                                                then [memory.End].Cast<Tag<bool>>()   
                                                else [memory.Start].Cast<Tag<bool>>() 
                            | :? Alias as a -> 
                                        match a.TargetVertex with
                                        | AliasTargetReal ar    -> ar.GetCoinTags(memory, isInTag)
                                        | AliasTargetCall ac    -> ac.GetCoinTags(memory, isInTag)
                                        | AliasTargetRealEx ao  -> ao.GetCoinTags(memory, isInTag)
                            | _ -> failwith "Error"
