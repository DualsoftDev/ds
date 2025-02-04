// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Common

open System.Linq
open Dual.Common.Base.FS
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module GraphModule =
    // Extended interfaces
    type IQualifiedNamed =
        inherit INamed
        abstract QualifiedName: string with get
        abstract DequotedQualifiedName: string with get
        abstract NameComponents: string[] with get

    type INamedVertex =
        inherit IVertex
        inherit INamed

    /// Runtime Edge Types
    [<Flags>]
    type EdgeType =
        | None                       = 0b00000000    // Invalid state
        | Start                      = 0b00000001    // Start, Weak
        | Reset                      = 0b00000010    // else start
        | Strong                     = 0b00000100    // else weak
        | AugmentedTransitiveClosure = 0b00001000    // 강한 상호 reset 관계 확장 edge


    [<AbstractClass>]
    type DsEdgeBase<'V>(source:'V, target:'V, edgeType:EdgeType) =
        inherit EdgeBase<'V, EdgeType>(source, target, edgeType)
        member _.EdgeType = edgeType


    /// Template class for DS Graph<'V, 'E>
    type TDsGraph<'V, 'E
            when 'V :> IVertexKey and 'V : equality
            and 'E :> DsEdgeBase<'V> and 'E: equality> (
            vertices_:'V seq,
            edges_:'E seq,
            vertexHandlers:GraphVertexAddRemoveHandlers option) =
        inherit Graph<'V, 'E>(vertices_, edges_, vertexHandlers)

        let isStartEdge (e:'E) = e.Edge = EdgeType.Start

        new () = TDsGraph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>, None)
        new (vs, es) = TDsGraph<'V, 'E>(vs, es, None)
        new (vertexHandlers:GraphVertexAddRemoveHandlers option) = TDsGraph<'V, 'E>(Seq.empty<'V>, Seq.empty<'E>, vertexHandlers)

        member x.GetIncomingVerticesWithEdgeType(vertex:'V, f: 'E -> bool) =
                                    x.GetIncomingEdges(vertex).Where(f)
                                     .Select(fun e -> e.Source)

        member x.GetOutgoingVertices(vertex:'V) = x.GetOutgoingEdges(vertex).Select(fun e -> e.Target)
        member x.GetOutgoingVerticesWithEdgeType(vertex:'V, f: 'E -> bool) =
                                    x.GetOutgoingEdges(vertex).Where(f)
                                     .Select(fun e -> e.Target)
        override x.Inits =
            let inits =
                x.Edges
                    .Select(fun e -> e.Source)
                    .Where(fun src -> not <| x.GetIncomingVerticesWithEdgeType(src, isStartEdge).Any())
                    .Distinct()
            x.Islands @ inits
        override x.Lasts =
            let lasts =
                x.Edges
                    .Select(fun e -> e.Target)
                    .Where(fun tgt -> not <| x.GetOutgoingVerticesWithEdgeType(tgt, isStartEdge).Any())
                    .Distinct()
            x.Islands @ lasts


