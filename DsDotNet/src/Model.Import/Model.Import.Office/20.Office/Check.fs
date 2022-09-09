// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open PPTX
open System
open System.Collections.Concurrent

[<AutoOpen>]
module Check =

        let GetDemoModel(sysName:string) = 
            let sys = DsSystem(sysName, true)
            let flow = Flo("P0",  Int32.MaxValue, sys)
            sys.Flos.TryAdd(flow.Page, flow) |> ignore
            flow.AddEdge( MEdge(Seg("START", sys, EX), Seg("시작인과", sys, MY), EdgeCausal.SEdge))
            flow.AddEdge( MEdge(Seg("RESET", sys, EX), Seg("복귀인과", sys, MY), EdgeCausal.REdge))
            flow.AddEdge( MEdge(Seg("START", sys, EX), Seg("시작유지", sys, MY), EdgeCausal.SPush))
            flow.AddEdge( MEdge(Seg("RESET", sys, EX), Seg("복귀유지", sys, MY), EdgeCausal.RPush))
            flow.AddEdge( MEdge(Seg("START", sys, EX), Seg("시작조건", sys, MY), EdgeCausal.SSTATE))
            flow.AddEdge( MEdge(Seg("RESET", sys, EX), Seg("복귀조건", sys, MY), EdgeCausal.RSTATE))
            flow.AddEdge( MEdge(Seg("ETC"  , sys, EX), Seg("상호행위간섭", sys, MY), EdgeCausal.Interlock))
            flow.AddEdge( MEdge(Seg("ETC"  , sys, EX), Seg("시작후행리셋", sys, MY), EdgeCausal.SReset))

            //모델만들기 및 시스템 등록
            let model = DsModel("testModel");
            model.Add(sys) |> ignore
            model.AddEdges(flow.Edges, sys.SysSeg)
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
                            |> Seq.filter(fun group ->group.Key.NodeCausal = DUMMY |>not) 
                            |> Seq.map (fun group -> group.Key)
            let tgtParents = doc.Parents
                            |> Seq.filter(fun group ->group.Value.Contains(edge.EndNode)) 
                            |> Seq.filter(fun group ->group.Key.NodeCausal = DUMMY |>not) 
                            |> Seq.map (fun group -> group.Key)
            if(srcParents.Count() > 1) then failError (srcParents, edge.StartNode)  
            if(tgtParents.Count() > 1) then failError (tgtParents, edge.EndNode)  

        let ValidFloPath(node:pptNode, dicFloName:ConcurrentDictionary<int, string>) =
            if(node.Name.Contains('.'))
            then 
                let paths = node.Name.Split('.')
                if(dicFloName.Values.Contains(paths.[0])|> not)
                then Office.ErrorName(node.Shape, 27, node.PageNum)
                else if(node.Name.Split('.').Length > 2)
                then Office.ErrorName(node.Shape, 26, node.PageNum)


        let SameNode(seg:Seg, node:pptNode, dicSegCheckSame:ConcurrentDictionary<string, Seg>) =
            if(dicSegCheckSame.ContainsKey(seg.Name)|>not)
            then dicSegCheckSame.TryAdd(seg.Name, seg)|> ignore

            let oldSeg = dicSegCheckSame.[seg.Name]
            if((seg.NodeCausal = oldSeg.NodeCausal)|>not) 
            then 
                Event.MSGError($"도형오류 :타입이 다른 같은이름이 존재합니다 \t[Page{node.PageNum}: {seg.Name}({seg.NodeCausal}) != ({oldSeg.NodeCausal}) ({node.Shape.ShapeName()})]")
        
        let SameEdgeErr(parentNode:pptNode option, pptEdge:pptEdge, mEdge:MEdge, dicSameCheck:ConcurrentDictionary<string, MEdge>) = 
            let parentName = if(parentNode.IsSome) 
                             then sprintf "%s.%s"  (mEdge.Source.OwnerFlo) (parentNode.Value.Name) 
                             else ""
            if(dicSameCheck.TryAdd(mEdge.ToCheckText(parentName), mEdge)|>not)
                then
                    let mEdge = dicSameCheck.[mEdge.ToCheckText(parentName)]
                    Office.ErrorConnect(pptEdge.ConnectionShape, 20, $"{mEdge.Source.Name}", $"{mEdge.Target.Name}", pptEdge.PageNum)
            
        let SamePage(page:pptPage, dicPage:ConcurrentDictionary<string, pptPage>) =
                    if(page.Title = ""|>not && page.IsUsing)
                    then if(dicPage.TryAdd(page.Title, page)|>not)
                         then Office.ErrorPPT(Page, 21, $"{page.Title},  Same Page->{dicPage.[page.Title].PageNum}",  page.PageNum)
