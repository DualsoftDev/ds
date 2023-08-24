// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System
open System.Collections.Concurrent
open Dual.Common.Core.FS
open Model.Import.Office
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module ImportCheck =

        let GetDemoModel(sysName:string) =
            let sys   = DsSystem(sysName, "top")
            let flow = Flow.Create("P0", sys)
            let vertexs = HashSet<Real>()
            let find(name:string) = vertexs.First(fun f->f.Name = name)
            for v in [
                        "START"; "시작인과"; "시작유지"; "RESET"; "복귀인과";
                        "복귀유지"; "ETC"; "상호행위간섭"; "시작후행리셋";
                ] do
                    vertexs.Add(Real.Create(v, flow)) |>ignore

            let fg = flow.Graph
            fg.AddVertices(vertexs.Cast<Vertex>())|>ignore
            let v(name:string) = fg.Vertices.Find(fun f->f.Name = name)

            flow.ModelingEdges.Add(ModelingEdgeInfo(v("START"), TextStartEdge, v("시작인과")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("START"), TextStartPush, v("시작유지")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("RESET"), TextResetEdge, v("복귀인과")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("RESET"), TextResetPush, v("복귀유지")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextInterlock, v("상호행위간섭")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextStartReset, v("시작후행리셋")))|>ignore

            sys

        let SameParent(parents:Dictionary<pptNode, seq<pptNode>>, edge:pptEdge) =
            let failError (parents:pptNode seq, node:pptNode)=
                let error=
                    seq {
                           yield "그룹오류 : 자식은 한부모에만 존재 가능합니다."
                           for parent in  parents do
                           yield $"[Page{parent.PageNum}:parent-{parent.PageNum}~child-{edge.StartNode.Name}]"
                                }  |> String.concat "\r\n"
                failwithf  $"{error}"

            let srcParents = parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.StartNode))
                            |> Seq.map (fun group -> group.Key)
            let tgtParents = parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.EndNode))
                            |> Seq.map (fun group -> group.Key)
            if(srcParents.Count() > 1) then failError (srcParents, edge.StartNode)
            if(tgtParents.Count() > 1) then failError (tgtParents, edge.EndNode)



        //let CheckMakeCopyApi(nodes:pptNode seq, dicSys:Dictionary<int, DsSystem>) =
        //    let dicName = ConcurrentDictionary<string, string>()
        //    let sysNames = dicSys.Values.Select(fun s->s.Name)
        //    nodes
        //        |> Seq.filter(fun node -> node.NodeType = COPY)
        //        |> Seq.iter(fun node ->

        //            if(sysNames.Contains(node.Name)|> not)
        //            then Office.ErrorPPT(Name, ErrID._32,  node.Shape.InnerText, node.PageNum, $"확인 시스템 이름 : {node.Name}")
        //            )


        let SameEdgeErr(pptEdges:pptEdge seq) =
            let dicSameCheck = Dictionary<string, string>()
            pptEdges |> Seq.iter(fun edge ->
                if(dicSameCheck.TryAdd(edge.Text,edge.Text)|>not) then
                    edge.ConnectionShape.ErrorConnect(ErrID._22, edge.Text, edge.PageNum)
            )

        let CheckSameCopy(doc:pptDoc) =
            let dicSame = Dictionary<string, pptNode>()

            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType.IsLoadSys)
            |> Seq.iter(fun node ->
                    node.CopySys.ForEach(fun loadsys ->
                        let key = $"{node.PageNum}_{loadsys.Key}"
                        if dicSame.ContainsKey(key)
                        then Office.ErrorName(node.Shape, ErrID._33, node.PageNum)
                        else dicSame.Add(key, node)
                    )
                    )



        //page 타이틀 중복체크
        let SameFlowName(doc:pptDoc) =
            let duplicatePages = doc.Pages
                                    .Where(fun f->f.IsUsing && not<|f.Title.IsNullOrEmpty())
                                    .GroupBy(fun f->f.Title)
                                    .SelectMany(fun f->f.Skip(1));

            duplicatePages.Iter  (fun page-> 
                 Office.ErrorPPT(ErrorCase.Name, ErrID._2, $"중복이름 : {page.Title}",page.PageNum, $"중복페이지") 
            )
            

        //let ValidPath(nodes:pptNode seq, model:MModel) =
        //    let checkNodeName(nodes:pptNode seq) =
        //        nodes.Filter(fun node -> node.NodeType.IsCall || node.NodeType.IsReal)
        //             .ForEach(fun node -> if node.Name.Contains(";") then node.Shape.ErrorName(29, node.PageNum))

        //    let checkSameNodeType(nodes:pptNode seq, model:MModel) =
        //        let dicSame = ConcurrentDictionary<string, pptNode>()
        //        nodes.ForEach(fun node ->
        //            let flow = model.GetFlow(node.PageNum)

        //            let nodekey = sprintf "%s;%s" flow.Name node.Name
        //            if(dicSame.ContainsKey(nodekey)|>not)
        //            then dicSame.TryAdd(nodekey, node)|> ignore

        //            let oldNode = dicSame.[nodekey]
        //            if((node.NodeType = oldNode.NodeType)|>not)
        //            then
        //                MSGError($"도형오류 :타입이 다른 같은이름이 존재합니다 \t[Page{node.PageNum}: {nodekey}({node.NodeType}) != ({oldNode.NodeType}) ({node.Shape.ShapeName()})]")

        //       )

        //    let myFlowNames  = model.Flows.Filter(fun flow -> flow.System.Name = TextMySys).Map(fun s->s.Name)
        //    let exSysNamesDic  = model.Systems.Filter(fun sys->sys.Name = TextMySys|>not).Map(fun sys -> sys.Name, sys) |> dict

        //    nodes.ForEach(fun node ->
        //        if(node.Name.Contains('.'))
        //        then
        //            if(node.Name.Split('.').Length > 2)
        //            then Office.ErrorName(node.Shape, 26, node.PageNum)

        //            if node.NodeType.IsReal
        //            then
        //                if(myFlowNames.Contains(node.Name.Split('.').[0])|> not)
        //                then Office.ErrorName(node.Shape, 27, node.PageNum)
        //            elif node.NodeType.IsCall
        //            then

        //                let callSys, callIf = node.CallName.Split('.').[0], node.CallName.Split('.').[1]

        //                if(exSysNamesDic.ContainsKey(callSys)|> not)
        //                then
        //                     let exSysNamesText = exSysNamesDic.Keys |> Seq.sort |> String.concat ";\n"
        //                     let errText = $"\n{callSys} 시스템은 \n[{exSysNamesText}]에 없습니다."
        //                     Office.ErrorPPT(ErrorCase.Name, 32, Office.ShapeName(node.Shape), node.PageNum, errText)
        //                else
        //                     let libSys = exSysNamesDic.[callSys]
        //                     if (libSys.IFNames.Contains(callIf)|> not)
        //                     then
        //                         let libSysIFText = libSys.IFNames |> String.concat "; "
        //                         let errText = $"{callIf} 행위는  {libSys.Name} = [{libSysIFText}]에 없습니다."
        //                         Office.ErrorPPT(ErrorCase.Name, 33, Office.ShapeName(node.Shape), node.PageNum, errText)
        //    )
        //    checkNodeName(nodes)
        //    checkSameNodeType(nodes, model)

