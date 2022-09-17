// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open Engine.Core
open System.Collections.Concurrent

 
[<AutoOpen>]
module DsBase =
  
    /// Seg Container
    [<AbstractClass>]
    type SystemBase(name)  =
        interface ISystem with
            member this.Name: string = name

        member x.Name:string = name
        member x.ToText() = $"{name}"
       
    /// Seg Vertex
    [<AbstractClass>]
    type SegBase(name,  baseSystem:SystemBase) =
        let noEdgeBaseSegs  = ConcurrentDictionary<SegBase, SegBase>()
        interface IVertex with
            member x.Name: string = x.Name

        member x.Name: string = name
        ///ppt에서 불러온 pptShape.Key와 같음
        member x.BaseSys = baseSystem
        member x.NoEdgeBaseSegs = noEdgeBaseSegs.Values |> Seq.sortBy(fun seg -> seg.Name)
        member x.AddSegNoEdge(seg) = noEdgeBaseSegs.TryAdd(seg, seg) |> ignore 
        member x.RemoveSegNoEdge(seg) = noEdgeBaseSegs.TryRemove(seg) |> ignore 

    /// Seg edge
    [<AbstractClass>]
    type EdgeBase(src:SegBase, tgt:SegBase, edgeCausal:EdgeCausal) =
        do
            if(src = tgt && edgeCausal = SEdge)
            then failwith $"SourceVertex [{src.Name}] = TargetVertex [{tgt.Name}]"

        interface IEdge with
            member this.SourceVertex: IVertex = src :> IVertex
            member this.TargetVertex: IVertex = tgt :> IVertex

        member x.Causal = edgeCausal
        member x.Nodes = [src;tgt]
        member x.SourceVertex = src
        member x.TargetVertex = tgt
        