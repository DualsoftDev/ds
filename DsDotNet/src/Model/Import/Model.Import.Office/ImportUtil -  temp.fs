// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open PPTX
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Engine.Common.FS
open Model.Import.Office
open Engine.Core
open System.Runtime.CompilerServices

[<AutoOpen>]
module ImportUTemp =


        //flow.AliasSet 업데이트
        let UpdateAlias(seg:MSeg, flow:MFlow) = 
            let aliasName = seg.ValidName 
            let orginName = seg.AliasOrg.Value.ValidName 
            if(flow.AliasSet.Keys.Contains(orginName))  
                then flow.AliasSet.[orginName].Add(aliasName)|> ignore
                else let set = HashSet<string>()
                     set.Add(aliasName)|> ignore
                     flow.AliasSet.TryAdd(orginName, set ) |> ignore

        



        //ExSys 및 Flow 만들기
        let MakeExSys(doc:pptDoc, model:MModel) = 
            doc.Pages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    if sysName = TextMySys |>not && model.DicSystems.ContainsKey sysName|>not
                    then MSys.Create(sysName, false, model) |> ignore
                    )

            doc.Nodes 
            |> Seq.filter(fun node -> node.NodeType = COPY) 
            |> Seq.iter(fun node -> 
                    node.CopySys.ForEach(fun sysName -> MSys.Create(sysName.Key, false, model) |> ignore)
                    )

       
        
        //MFlow 리스트 만들기
        let MakeFlos(pptPages:pptPage seq, model:MModel) = 
             pptPages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let pageNum  = page.PageNum
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    let sys    = model.DicSystems.[sysName]
                    MFlow.Create(flowName, sys, pageNum) |> ignore
                    )

        //MFlow 리스트 만들기
        let MakeFlows(pptPages:pptPage seq, model:Model, dicFlow:Dictionary<int, Flow>) = 
             pptPages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let pageNum  = page.PageNum
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    let sys    = model.FindSystem(sysName)      //.[sysName]
                    dicFlow.Add(  pageNum,  Flow.Create(flowName, sys) ) |> ignore
                    )




        //Interface 만들기
        let MakeIf(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            pptNodes
            |> Seq.filter(fun node -> node.NodeType = IF) 
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                    let txs = node.IfTxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>
                    let rxs = node.IfRxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>

                    flow.System.AddInterface(node.IfName, txs, rxs) |> ignore
            )


        //parent 리스트 만들기
        let MakeParent(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
            let dicParent = parents 
                            |> Seq.filter(fun parentChildren -> parentChildren.Key.IsDummy|>not)
                            |> Seq.collect(fun parentChildren -> 
                                                       parentChildren.Value 
                                                       |> Seq.map(fun child -> child, parentChildren.Key)) |> dict
            pptNodes
            |> Seq.iter(fun node -> 
                if(dicParent.ContainsKey(node))
                then 
                    let child  = dicSeg.[node.Key]
                    let parent = dicSeg.[dicParent.[node].Key]
                
                    child.Parent <- Some(parent)
                )

      
                
        //segment 리스트 만들기
        let MakeSeg(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
            let mySys = model.ActiveSys
            pptNodes
            |> Seq.sortBy(fun node -> node.Alias.IsSome)
            |> Seq.iter(fun node -> 
                
                let flow  = model.GetFlow(node.PageNum)
                let sys   = if(node.NodeType.IsCall) 
                            then model.DicSystems.[node.CallName.Split('.').[0]]
                            else model.DicSystems.[TextMySys]

                let btn   = node.IsEmgBtn || node.IsStartBtn || node.IsAutoBtn || node.IsResetBtn 
                let bound = if(btn) then ExBtn
                            else if(node.NodeType.IsCall) then OtherFlow else ThisFlow

             
                if(node.Alias.IsSome)
                then
                    let segOrg = dicSeg.[node.Alias.Value.Key]
                    let aliasName = node.Name
                    let aliasSeg = MSeg(aliasName, mySys, ThisFlow, segOrg.NodeType, flow, segOrg.IsDummy)
                    aliasSeg.Update(node.Key, node.Id.Value, 0,0)
                    aliasSeg.AliasOrg <- Some(segOrg)
                    dicSeg.Add(node.Key, aliasSeg) |> ignore
                else 
                    let name =  if(node.NodeType.IsCall) then node.CallName else node.NameOrg
                    let seg = MSeg(name, sys,  bound, node.NodeType, flow, node.IsDummy)
                    seg.Update(node.Key, node.Id.Value, node.CntTX, node.CntRX)
                    dicSeg.Add(node.Key, seg) |> ignore
                )

        let MakeCopySys(doc:pptDoc, model:MModel) = 
            doc.Nodes 
                |> Seq.filter(fun node -> node.NodeType = COPY) 
                |> Seq.iter(fun node -> 
                        node.CopySys.ForEach(fun copy ->
                            let exSys = model.DicSystems.[copy.Key]
                            let libSys   = model.DicSystems.[node.Name]
                            libSys.Flows.ForEach(fun f ->  
                                let flow = MFlow.Create(f.Name, exSys, f.Page) 
                                let flowEdges = f.CopyMEdges()

                                flowEdges.ForEach(fun edge -> flow.AddEdge(edge) |> ignore ))

                            libSys.IFs.ForEach(fun f-> exSys.AddInterface(f.Name, f.TXs, f.RXs)|> ignore)
                            )
                )

        //Dummy child 처리
        let MakeDummy(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, MSeg>) = 
                parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key]
                    children 
                    |> Seq.iter(fun child -> pSeg.ChildFlow.AddSingleNode(dicSeg.[child.Key]) |> ignore  ) )
                
           //pptEdge 변환 및 등록
        let MakeEdge(doc:pptDoc, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            let pptEdges = doc.Edges
            let parents = doc.Parents
            let mySys = model.ActiveSys
            let convertEdge(edge:pptEdge, sysSeg, mFlow:MFlow,  dicSeg:Dictionary<string, MSeg>) = 
                let getParent(edge:pptEdge) = 
                    ImportCheck.SameParent(parents, edge)
                    let newParents = 
                        parents
                        |> Seq.filter(fun group -> group.Value.Contains(edge.StartNode)  && group.Value.Contains(edge.EndNode))
                        |> Seq.map(fun group -> 
                            Some(group.Key), dicSeg.[group.Key.Key]  )
        
                    if(newParents.Any())
                        then newParents |> Seq.head
                        else (None , sysSeg)

                let sSeg = dicSeg.[edge.StartNode.Key]
                let eSeg = dicSeg.[edge.EndNode.Key]
               
                let parentNode, parentSeg= getParent(edge) 
                let mEdge = MEdge(sSeg, eSeg, edge.Causal)

                if(mEdge.Causal = Interlock)
                then mFlow.AddInterlock(mEdge)

                if(parentNode.IsNone) 
                then mFlow.AddEdge(mEdge) |> ignore
                else parentSeg.ChildFlow.AddEdge(mEdge) |> ignore
                mEdge


            pptEdges
                |> Seq.iter(fun edge -> 
                    let flow = model.GetFlow(edge.PageNum)
                    let sSeg = dicSeg.[edge.StartNode.Key]
                    let eSeg = dicSeg.[edge.EndNode.Key]
                    if(sSeg.IsDummy || eSeg.IsDummy)
                    then 
                        convertEdge(edge,  mySys.SysSeg, flow, dicSeg).IsDummy <- true

                        let srcs = if(sSeg.Singles.Any()) then sSeg.Singles |> Seq.toList else [sSeg]
                        let tgts = if(eSeg.Singles.Any()) then eSeg.Singles |> Seq.toList else [eSeg]

                        srcs
                        |> Seq.iter(fun src ->
                                tgts
                                |> Seq.iter(fun tgt -> 
                                    let edge = pptEdge(edge.ConnectionShape, edge.Id, edge.PageNum, src.ShapeID, tgt.ShapeID , doc.DicNodes)
                                    convertEdge(edge,  mySys.SysSeg, flow, dicSeg).IsSkipUI <- true  ))
                    else 
                        convertEdge(edge, mySys.SysSeg, flow, dicSeg) |> ignore
                    )

        let MakeSingleNode(doc:pptDoc, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            let dicFlow = model.Flows |> Seq.map(fun flow -> flow.Page, flow) |> dict
            let nodes   = doc.Nodes   |> Seq.filter(fun node -> node.IsDummy|>not)
                                      |> Seq.filter(fun node -> node.NodeType.IsCall ||node.NodeType.IsReal)
            nodes   //root singleNode
                |> Seq.filter(fun node -> dicSeg.[node.Key].IsRoot)       
                |> Seq.filter(fun node -> dicFlow.[node.PageNum].MEdges.GetNodes().Contains(dicSeg.[node.Key])|>not)
                |> Seq.iter  (fun node -> dicFlow.[node.PageNum].AddSingleNode(dicSeg.[node.Key]) |> ignore)       
            nodes  //child singleNode
                |> Seq.filter(fun node -> dicSeg.[node.Key].IsRoot |> not)       
                |> Seq.filter(fun node -> dicSeg.[node.Key].Parent.IsSome)
                |> Seq.filter(fun node -> dicSeg.[node.Key].Parent.Value.MEdges.GetNodes().Contains(dicSeg.[node.Key])|>not)
                |> Seq.iter  (fun node -> dicSeg.[node.Key].Parent.Value.ChildFlow.AddSingleNode(dicSeg.[node.Key]) |> ignore)   


        let MakeLayouts(doc:pptDoc,  model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            let pptNodes =    doc.Nodes
            let visibleLast = doc.Pages.OrderByDescending(fun p -> p.PageNum) |> Seq.filter(fun p -> p.IsUsing) |> Seq.head
            let mySys = model.ActiveSys
            pptNodes
                |> Seq.filter(fun node -> node.PageNum = visibleLast.PageNum)
                |> Seq.filter(fun node -> node.Name = ""|>not)
                |> Seq.filter(fun node -> dicSeg.[node.Key].Bound = ThisFlow)
                |> Seq.filter(fun node -> node.NodeType = TX || node.NodeType = TR || node.NodeType = RX )
                |> Seq.distinctBy(fun node -> node.Name)
                |> Seq.iter(fun node -> 
                            if(node.Alias.IsSome)
                            then 
                                mySys.LocationSet.TryAdd((dicSeg.[node.Key].AliasOrg.Value).FullName, node.Rectangle) |> ignore
                            else
                                mySys.LocationSet.TryAdd(dicSeg.[node.Key].FullName, node.Rectangle) |> ignore
                                )

        //Safety 만들기
        let MakeSafety(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            let mySys = model.ActiveSys
            pptNodes
            |> Seq.filter(fun node -> node.IsDummy|>not)
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                    let mFlowName =  flow.Name
                    let dic = dicSeg.Values.Filter(fun w-> w.IsDummy|>not).Select(fun seg -> seg.Name, seg) |> dict
                   
                    //Safety
                    let safeSeg = 
                        node.Safeties   //세이프티 입력 미등록 이름오류 체크
                        |> Seq.map(fun safe ->  sprintf "%s_%s" mFlowName safe)
                        |> Seq.iter(fun safeFullName -> if(dic.ContainsKey safeFullName|>not) 
                                                        then Office.ErrorName(node.Shape, 28, node.PageNum))


                        node.Safeties   
                        |> Seq.map(fun safe ->  sprintf "%s_%s" mFlowName safe)
                        |> Seq.map(fun safeFullName ->  dic.[safeFullName])

                    if(safeSeg.Any())
                        then flow.AddSafety(dicSeg.[node.Key], safeSeg)
                    )
     
        //EMG & Start & Auto 리스트 만들기
        let MakeBtn(pptNodes:pptNode seq, model:MModel) = 
            let mySys = model.ActiveSys
            pptNodes
            |> Seq.filter(fun node -> node.IsDummy|>not)
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                               
                    //Start, Reset, Auto, Emg 버튼
                    if(node.IsStartBtn) then mySys.TryAddStartBTN(node.Name, flow)
                    if(node.IsResetBtn) then mySys.TryAddResetBTN(node.Name, flow)
                    if(node.IsAutoBtn)  then mySys.TryAddAutoBTN(node.Name,  flow)
                    if(node.IsEmgBtn)   then mySys.TryAddEmergBTN(node.Name, flow)
                    
                    ) 
     