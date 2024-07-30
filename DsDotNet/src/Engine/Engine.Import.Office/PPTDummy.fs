// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open PPTUtil
open Dual.Common.Core.FS
open Engine.Import.Office
open System.Collections.Generic
open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module PPTDummyModule =

    /// 방향성 없는 edge 로 연결된 node 의 표현????
    type pptDummy(shapeName: string, page: int) =
        let pptNodes = HashSet<pptNode>()
        let vertices = HashSet<Vertex>()
        let dummyEdges = HashSet<ModelingEdgeInfo<string>>()
        let mutable dicVertex = Dictionary<string, Vertex>()
        let dummyNode = $"{page}_{shapeName}"

        member x.Page = page
        member x.DummyNodeKey = dummyNode
        member x.DummyEdges = dummyEdges
        member x.Members = vertices

        member x.GetVertex(name: string) =
            dicVertex |> Seq.tryFind (fun kvp -> kvp.Key = name)

        member x.AddOutEdge(edgeType: ModelingEdgeType, target: string) =
            dummyEdges.Add(ModelingEdgeInfo<string>(dummyNode, edgeType.ToText(), target)) |> ignore

        member x.AddInEdge(edgeType: ModelingEdgeType, source: string) =
            dummyEdges.Add(ModelingEdgeInfo<string>(source, edgeType.ToText(), dummyNode)) |> ignore

        member x.GetParent() =
            vertices.First().Parent

        member val Items = pptNodes

        member x.Update(dic: Dictionary<string, Vertex>) =
            dicVertex <- dic
            pptNodes |> Seq.iter (fun f -> vertices.Add(dicVertex.[f.Key]) |> ignore)

[<Extension>]
type PPTDummyExt =

    [<Extension>]
    static member TryFindDummy(dummys: ISet<pptDummy>, pptNode: pptNode) =
        dummys |> Seq.tryFind (fun w -> w.Items.Contains(pptNode))

    [<Extension>]
    static member IsMember(dummys: ISet<pptDummy>, pptNode: pptNode) =
        dummys.TryFindDummy(pptNode).IsSome

    [<Extension>]
    static member GetMembers(dummys: ISet<pptDummy>, pptNode: pptNode) =
        match dummys.TryFindDummy(pptNode) with
        | Some(findDummy) -> findDummy.Items
        | None -> HashSet<pptNode>()

    [<Extension>]
    static member AddDummys(dummys: HashSet<pptDummy>, srcNode: pptNode, tgtNode: pptNode) =
        match dummys.TryFindDummy(srcNode), dummys.TryFindDummy(tgtNode) with
        | Some srcDummy, Some tgtDummy ->
            srcDummy.Items.UnionWith tgtDummy.Items
            dummys.Remove(tgtDummy) |> ignore
        | Some srcDummy, None ->
            srcDummy.Items.Add(tgtNode) |> ignore
        | None, Some tgtDummy ->
            tgtDummy.Items.Add(srcNode) |> ignore
        | None, None ->
            let newDummy = pptDummy(srcNode.Shape.ShapeName(), srcNode.PageNum)
            newDummy.Items.Add(srcNode) |> ignore
            newDummy.Items.Add(tgtNode) |> ignore
            dummys.Add(newDummy) |> ignore
