namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module CodeConvertUtil =

    let rec getCoinTags(v:Vertex, isInTag:bool) : Tag<bool> seq =
            match v with
            | :? Call as c ->
                [ for j in c.CallTarget.JobDefs do
                    let typ = if isInTag then "I" else "O"
                    PlcTag( $"{j.ApiName}_{typ}", "", false) :> Tag<bool>
                ]
            | :? Alias as a ->
                match a.TargetWrapper with
                | DuAliasTargetReal ar    -> getCoinTags( ar, isInTag)
                | DuAliasTargetCall ac    -> getCoinTags( ac, isInTag)
                | DuAliasTargetRealEx ao  -> getCoinTags( ao, isInTag)
            | _ -> failwith "Error"

    let getTxRxTags(v:Vertex, isTx:bool) : Tag<bool> seq =
        match v with
        | :? Call as c ->
            c.CallTarget.JobDefs
                .SelectMany(fun j->
                    if isTx then
                        j.ApiItem.TXs.Select(getVM).Select(fun f->f.ST.Expr)
                    else
                        j.ApiItem.RXs.Select(getVM).Select(fun f->f.ET.Expr)
                )
                .Cast<Tag<bool>>()
        | :? Alias as a ->
            match a.TargetWrapper with
            | DuAliasTargetReal ar    -> getCoinTags(ar, isTx)
            | DuAliasTargetCall ac    -> getCoinTags(ac, isTx)
            | DuAliasTargetRealEx ao  -> getCoinTags(ao, isTx)
        | _ -> failwith "Error"
    
    [<Extension>]
    type CodeConvertUtilExt =
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
    