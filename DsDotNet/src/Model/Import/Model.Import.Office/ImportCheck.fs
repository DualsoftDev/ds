// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTObjectModule
open System
open System.Collections.Concurrent
open Engine.Common.FS
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
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextStartEdge, v("상호행위간섭")))|>ignore
            flow.ModelingEdges.Add(ModelingEdgeInfo(v("ETC"), TextStartReset, v("시작후행리셋")))|>ignore

            sys

        let SameParent(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, edge:pptEdge) =
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
                            |> Seq.filter(fun group ->group.Key.IsDummy |>not)
                            |> Seq.map (fun group -> group.Key)
            let tgtParents = parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.EndNode))
                            |> Seq.filter(fun group ->group.Key.IsDummy |>not)
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
            let dicSameCheck = ConcurrentDictionary<string, string>()
            pptEdges |> Seq.iter(fun edge ->
                if(dicSameCheck.TryAdd(edge.Text,edge.Text)|>not)
                    then
                        edge.ConnectionShape.ErrorConnect(ErrID._20, edge.Text, edge.PageNum)
            )

        //page 타이틀 중복체크
        let CheckMakeSystem(doc:pptDoc) =
            let dicPage = ConcurrentDictionary<string, int>()
            doc.Pages.Filter(fun page  ->  page.IsUsing && page.Title = ""|> not)
                    .ForEach(fun page->
                                if(dicPage.TryAdd(page.Title, page.PageNum)|>not)
                                then Office.ErrorPPT(Page, ErrID._21, $"{page.Title},  Same Page({dicPage.[page.Title]})",  page.PageNum)
                                )

            let dicSys = ConcurrentDictionary<string, string>()
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType = COPY)
            |> Seq.iter(fun node ->
                    node.CopySys.ForEach(fun copy ->
                        if dicSys.TryAdd(copy.Key, copy.Value)|>not
                        then Office.ErrorName(node.Shape, ErrID._34, node.PageNum)
                        )
                    )


        //page 타이틀 중복체크
        let SameSysFlowName(systems:DsSystem seq, dicFlow: Dictionary<int, Flow>) =
            let sysNames = systems.Select(fun s->s.Name)
            systems.ForEach(fun sys->
                sys.Flows.ForEach(fun flow ->
                    if sysNames.Contains(flow.Name)
                    then
                        let page = dicFlow.Where(fun w-> w.Value = flow).First().Key
                        Office.ErrorPPT(ErrorCase.Name, ErrID._31, $"시스템이름 : {flow.System.Name}",page, $"중복페이지 : {page}")  )
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

