namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core

[<AutoOpen>]
module ConvertUtil =
    [<Extension>]
    type ConvertUtilExt =

        [<Extension>]
        static member FindEdgeSources(graph:DsGraph, target:Vertex, edgeType:ModelingEdgeType): Vertex seq =
            let edges = graph.GetIncomingEdges(target)
            let foundEdges =
                match edgeType with
                | StartPush -> edges.OfNotResetEdge().Where(fun e -> e.EdgeType.HasFlag(EdgeType.Strong))
                | StartEdge -> edges.OfNotResetEdge().Where(fun e -> not <| e.EdgeType.HasFlag(EdgeType.Strong))
                | ResetEdge -> edges.OfWeakResetEdge()
                | ResetPush -> edges.OfStrongResetEdge()
                | ( StartReset | InterlockWeak | Interlock )
                    -> failwith $"Do not use {edgeType} Error"

            foundEdges.Select(fun e->e.Source)


    // vertex 를 coin 입장에서 봤을 때의 extension methods
    type Vertex with
        member coin.GetCoinTags(memory:VertexMemoryManager, isInTag:bool) : Tag<bool> seq =
            match coin with
            | :? Call as c ->
                [ for j in c.CallTarget.JobDefs do
                    let typ = if isInTag then "I" else "O"
                    PlcTag( $"{j.ApiName}_{typ}", "", false) :> Tag<bool>
                ]
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> ar.GetCoinTags(memory, isInTag)
                | DuAliasTargetCall ac    -> ac.GetCoinTags(memory, isInTag)
                | DuAliasTargetRealEx ao  -> ao.GetCoinTags(memory, isInTag)
            | _ -> failwith "Error"


        member coin.GetTxRxTags(isTx:bool, memory:VertexMemoryManager) : Tag<bool> seq =
            let getVertexManager(v:Vertex) = v.VertexMemoryManager :?> VertexMemoryManager

            match coin with
            | :? Call as c ->
                c.CallTarget.JobDefs
                    .SelectMany(fun j->
                        if isTx then
                            j.ApiItem.TXs.Select(fun s -> (getVertexManager s).StartTag)
                        else
                            j.ApiItem.RXs.Select(fun s -> (getVertexManager s).EndTag)
                    )
                    .Cast<Tag<bool>>()
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> ar.GetCoinTags(memory, isTx)
                | DuAliasTargetCall ac    -> ac.GetCoinTags(memory, isTx)
                | DuAliasTargetRealEx ao  -> ao.GetCoinTags(memory, isTx)
            | _ -> failwith "Error"
