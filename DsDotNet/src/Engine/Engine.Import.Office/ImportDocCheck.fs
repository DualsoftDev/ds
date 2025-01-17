// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open PptConnectionModule
open System
open System.Collections.Concurrent
open Dual.Common.Core.FS
open Engine.Import.Office
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module ImportDocCheck =

    let GetDemoModel (sysName: string) =
        let sys = DsSystem.Create(sysName)
        let flow = sys.CreateFlow("P0")
        let vertexs = HashSet<Real>()
        let find (name: string) = vertexs.First(fun f -> f.Name = name)

        for v in [ "START"; "시작인과"; "시작유지"; "RESET"; "복귀인과"; "복귀유지"; "ETC"; "상호행위간섭"; "시작후행리셋" ] do
            vertexs.Add(flow.CreateReal(v)) |> ignore

        let fg = flow.Graph
        fg.AddVertices(vertexs.Cast<Vertex>()) |> ignore

        let v (name: string) =
            fg.Vertices.Find(fun f -> f.Name = name)

        flow.ModelingEdges.Add(ModelingEdgeInfo(v ("START"), TextStartEdge, v ("시작인과")))
        |> ignore


        flow.ModelingEdges.Add(ModelingEdgeInfo(v ("RESET"), TextResetEdge, v ("복귀인과")))
        |> ignore


        flow.ModelingEdges.Add(ModelingEdgeInfo(v ("ETC"), TextStartReset, v ("시작후행리셋")))
        |> ignore
        flow.ModelingEdges.Add(ModelingEdgeInfo(v ("ETC2"), TextSelfReset, v ("셀프리셋")))
        |> ignore

        sys

    let SameParent (parents: IDictionary<PptNode, seq<PptNode>>, edge: PptEdge) =
        let failError (parents: PptNode seq, node: PptNode) =
            let error =
                seq {
                    yield "그룹오류 : 자식은 한부모에만 존재 가능합니다."

                    for parent in parents do
                        yield $"[Page{parent.PageNum}:parent-{parent.PageNum}~child-{edge.StartNode.Name}]"
                }
                |> String.concat "\r\n"

            failwithf $"{error}"

        let srcParents =
            parents
            |> Seq.filter (fun group -> group.Value.Contains(edge.StartNode))
            |> Seq.map (fun group -> group.Key)

        let tgtParents =
            parents
            |> Seq.filter (fun group -> group.Value.Contains(edge.EndNode))
            |> Seq.map (fun group -> group.Key)

        if (srcParents.Count() > 1) then
            failError (srcParents, edge.StartNode)

        if (tgtParents.Count() > 1) then
            failError (tgtParents, edge.EndNode)



    let SameEdgeErr (pptEdges: PptEdge seq) =
        let dicSameCheck = Dictionary<string, string>()

        pptEdges
        |> Seq.iter (fun edge ->
            if (dicSameCheck.TryAdd(edge.Text, edge.Text) |> not) then
                edge.ConnectionShape.ErrorConnect(ErrID._22, edge.Text, edge.PageNum))

    let CheckSameCopy (doc: PptDoc) =
        let dicSame = Dictionary<string, PptNode>()

        doc.Nodes
        |> Seq.filter (fun node -> node.NodeType.IsLoadSys)
        |> Seq.iter (fun node ->
            node.CopySys.ForEach(fun loadsys ->
                let key = $"{node.PageNum}_{loadsys.Key}"

                if dicSame.ContainsKey(key) then
                    Office.ErrorName(node.Shape, ErrID._33, node.PageNum)
                else
                    dicSame.Add(key, node)))



    //page 타이틀 중복체크
    let SameFlowName (doc: PptDoc) =
        let duplicatePages =
            doc.Pages
                .Where(fun f -> f.PageNum <> pptHeadPage && f.IsUsing && not <| f.Title.IsNullOrEmpty())
                .GroupBy(fun f -> f.Title)
                .SelectMany(fun f -> f.Skip(1))

        duplicatePages.Iter(fun page ->
            Office.ErrorPpt(ErrorCase.Name, ErrID._2, $"중복이름 : {page.Title}", page.PageNum, 0u, $"중복페이지"))
