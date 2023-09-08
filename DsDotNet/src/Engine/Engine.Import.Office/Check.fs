// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTX
open System
open System.Collections.Concurrent
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module Check =

        let GetDemoModel(sysName:string) =
            let model = ImportModel("testModel");
            let sys = MSys(sysName, true, model)
            let mFlow = MFlow("P0", sys, Int32.MaxValue)
            sys.AddFlow(mFlow) |> ignore
            mFlow.AddEdge( MEdge(MSeg("START", sys,mFlow, TR), MSeg("시작인과", sys,mFlow, MY), EdgeCausal.SEdge))         |> ignore
            mFlow.AddEdge( MEdge(MSeg("RESET", sys,mFlow, TR), MSeg("복귀인과", sys,mFlow, MY), EdgeCausal.REdge))         |> ignore
            mFlow.AddEdge( MEdge(MSeg("START", sys,mFlow, TR), MSeg("시작유지", sys,mFlow, MY), EdgeCausal.SPush))         |> ignore
            mFlow.AddEdge( MEdge(MSeg("RESET", sys,mFlow, TR), MSeg("복귀유지", sys,mFlow, MY), EdgeCausal.RPush))         |> ignore
            mFlow.AddEdge( MEdge(MSeg("ETC"  , sys,mFlow, TR), MSeg("상호행위간섭", sys,mFlow, MY), EdgeCausal.Interlock)) |> ignore
            mFlow.AddEdge( MEdge(MSeg("ETC"  , sys,mFlow, TR), MSeg("시작후행리셋", sys,mFlow, MY), EdgeCausal.SReset))    |> ignore

            //모델만들기 및 시스템 등록
            model.Add(sys) |> ignore
            model.AddEdges(mFlow.MEdges, mFlow)
            model

        let SameParent(doc:pptDoc, edge:pptEdge) =
            let failError (parents:pptNode seq, node:pptNode)=
                let error=
                    seq {
                           yield "그룹오류 : 자식은 한부모에만 존재 가능합니다."
                           for parent in  parents do
                           yield $"[Page{parent.PageNum}:parent-{parent.PageNum}~child-{edge.StartNode.Name}]"
                                }  |> String.concat "\r\n"
                failwithf  $"{error}"

            let srcParents = doc.Parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.StartNode))
                            |> Seq.filter(fun group ->group.Key.IsDummy |>not)
                            |> Seq.map (fun group -> group.Key)
            let tgtParents = doc.Parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.EndNode))
                            |> Seq.filter(fun group ->group.Key.IsDummy |>not)
                            |> Seq.map (fun group -> group.Key)
            if(srcParents.Count() > 1) then failError (srcParents, edge.StartNode)
            if(tgtParents.Count() > 1) then failError (tgtParents, edge.EndNode)

        let ValidMFlowPath(node:pptNode, dicMFlowName:ConcurrentDictionary<int, string>) =
            if(node.Name.Contains('.'))
            then
                let paths = node.Name.Split('.')
                if(dicMFlowName.Values.Contains(paths.[0])|> not)
                then Office.ErrorName(node.Shape, 27, node.PageNum)
                else if(node.Name.Split('.').Length > 2)
                then Office.ErrorName(node.Shape, 26, node.PageNum)


        let CheckSameNodeType(node:pptNode, dicSegCheckSame:ConcurrentDictionary<string, pptNode>, dicMFlowName:ConcurrentDictionary<int, string>) =

            let nodekey = sprintf "%s;%s" dicMFlowName.[node.PageNum] node.Name
            if(dicSegCheckSame.ContainsKey(nodekey)|>not)
            then dicSegCheckSame.TryAdd(nodekey, node)|> ignore

            let oldNode = dicSegCheckSame.[nodekey]
            if((node.NodeType = oldNode.NodeType)|>not)
            then
                MSGError($"도형오류 :타입이 다른 같은이름이 존재합니다 \t[Page{node.PageNum}: {nodekey}({node.NodeType}) != ({oldNode.NodeType}) ({node.Shape.ShapeName()})]")

        let SameEdgeErr(parentNode:pptNode option, pptEdge:pptEdge, mEdge:MEdge, dicSameCheck:ConcurrentDictionary<string, MEdge>) =
            let parentName = if(parentNode.IsSome)
                             then sprintf "%s.%s"  (mEdge.Source.OwnerMFlow) (parentNode.Value.Name)
                             else ""
            if(dicSameCheck.TryAdd(mEdge.ToCheckText(parentName), mEdge)|>not)
                then
                    let mEdge = dicSameCheck.[mEdge.ToCheckText(parentName)]
                    Office.ErrorConnect(pptEdge.ConnectionShape, 20, $"{mEdge.Source.Name}", $"{mEdge.Target.Name}", pptEdge.PageNum)

        let SamePage(page:pptPage, dicPage:ConcurrentDictionary<string, pptPage>) =
                    if(page.Title = ""|>not && page.IsUsing)
                    then if(dicPage.TryAdd(page.Title, page)|>not)
                         then Office.ErrorPPT(Page, 21, $"{page.Title},  Same Page->{dicPage.[page.Title].PageNum}",  page.PageNum)
