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

    //[<Extension>] static member GetVertices(sys:DsSystem) =
    //                sys.Flows.SelectMany(fun flow->
    //                    flow.Graph.Vertices
    //                        |> Seq.collect(fun v ->
    //                            match v with
    //                            | :? Real as r -> r.Graph.Vertices.ToArray() @ [r]
    //                            | _ -> [|v|])
    //                       )


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

                    
    [<Extension>]  static member GetCoinTags(coin:Vertex, memory:VertexM, isInTag:bool) =
                            match coin with
                            | :? Call as c -> c.CallTarget.JobDefs
                                                .Select(fun j-> 
                                                            if isInTag
                                                            then PlcTag(j.ApiName+"_I", false)
                                                            else PlcTag(j.ApiName+"_O", false)
                                                )
                                                .Cast<TagBase<bool>>()
                            | :? Alias as a -> 
                                        match a.TargetVertex with
                                        | AliasTargetReal ar    -> ar.GetCoinTags(memory, isInTag)
                                        | AliasTargetCall ac    -> ac.GetCoinTags(memory, isInTag)
                                        | AliasTargetRealEx ao  -> ao.GetCoinTags(memory, isInTag)
                            | _ -> failwith "Error"


    [<Extension>]  static member GetTxRxTags(coin:Vertex, isTx:bool, dicM:ConcurrentDictionary<Vertex, VertexM>) =
                            let memory = dicM[coin]
                            match coin with
                            | :? Call as c -> c.CallTarget.JobDefs
                                                .SelectMany(fun j-> 
                                                            if isTx
                                                            then j.ApiItem.TXs.Select(fun s-> dicM[s].StartTag)
                                                            else j.ApiItem.RXs.Select(fun s-> dicM[s].EndTag)
                                                )
                                                .Cast<TagBase<bool>>()
                            | :? Alias as a -> 
                                        match a.TargetVertex with
                                        | AliasTargetReal ar    -> ar.GetCoinTags(memory, isTx)
                                        | AliasTargetCall ac    -> ac.GetCoinTags(memory, isTx)
                                        | AliasTargetRealEx ao  -> ao.GetCoinTags(memory, isTx)
                            | _ -> failwith "Error"
