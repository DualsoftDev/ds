// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open PPTX
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module ImportUtil =
        

        //flow.AliasSet 업데이트
        let UpdateAlias(seg:MSeg, flow:MFlow) = 
            let aliasName = seg.ValidName 
            let orginName = seg.Alias.Value.ValidName 
            if(flow.AliasSet.Keys.Contains(orginName))  
                then flow.AliasSet.[orginName].Add(aliasName)|> ignore
                else let set = HashSet<string>()
                     set.Add(aliasName)|> ignore
                     flow.AliasSet.TryAdd(orginName, set ) |> ignore

        


        //ExSys 및 Flow 만들기
        let MakeExSys(pptPages:pptPage seq, model:MModel) = 
             pptPages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    if sysName = TextMySys |>not && model.DicSystems.ContainsKey sysName|>not
                    then MSys.Create(sysName, false, model) |> ignore
                    )

        
        //MFlow 리스트 만들기
        let MakeFlows(pptPages:pptPage seq, model:MModel) = 
             pptPages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let pageNum  = page.PageNum
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    let sys    = model.DicSystems.[sysName]
                    MFlow.Create(flowName, sys, pageNum) |> ignore
                    )


        //alias Setting
        let MakeAlias(pptNodes:pptNode seq, model:MModel, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
            let settingAlias(nodes:pptNode seq) = 
                let names  = nodes|> Seq.map(fun f->f.Name)
                (nodes, GetAliasName(names))
                ||> Seq.map2(fun node  nameSet -> node,  nameSet)
                |> Seq.iter(fun (node, (name, newName)) -> 
                                if(name  = newName |> not)
                                then    let orgNode = nodes |> Seq.filter(fun f->f.Name = name) |> Seq.head
                                        node.Alias <- Some(newName) 
                                        node.AliasKey <- orgNode.Key)
                
            let dicFlowNodes = model.Flows 
                                |> Seq.map(fun flow -> flow.Page, pptNodes |> Seq.filter(fun node -> node.PageNum = flow.Page)
                                ) |> dict

            let realSet = dicFlowNodes
                          |> Seq.map(fun flowNodes -> 
                                         flowNodes.Value |> Seq.filter(fun node -> node.NodeType.IsReal))

            let callSet = realSet 
                          |> Seq.collect(fun reals -> reals)
                          |> Seq.filter(fun real -> parents.ContainsKey(real))
                          |> Seq.map(fun real -> 
                                        dicFlowNodes.[real.PageNum] 
                                        |> Seq.filter(fun node -> node.NodeType.IsCall)
                                        |> Seq.filter(fun node -> parents.[real].Contains(node) ))
            
            realSet |> Seq.iter settingAlias
            callSet |> Seq.iter settingAlias
           

        //Interface 만들기
        let MakeInterface(pptNodes:pptNode seq, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>) = 
            pptNodes
            |> Seq.filter(fun node -> node.NodeType = IF) 
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                    let txs = node.IfTxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>
                    let rxs = node.IfRxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>

                    flow.System.AddInterface(node.IfName, txs, rxs) |> ignore
            )
        //parent 리스트 만들기
        let MakeParent(pptNodes:pptNode seq, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
            let dicParent = parents |> Seq.collect(fun parentChilren -> 
                                                       parentChilren.Value |> Seq.map(fun child -> child, parentChilren.Key)) |> dict
            pptNodes
            |> Seq.iter(fun node -> 
                if(dicParent.ContainsKey(node))
                then 
                    let child  = dicSeg.[node.Key]
                    let parent = dicSeg.[dicParent.[node].Key]
                
                    child.Parent <- Some(parent)
                )

        //segment 리스트 만들기
        let MakeSegment(pptNodes:pptNode seq, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
            let mySys = model.ActiveSys
            pptNodes
            |> Seq.sortBy(fun node -> node.Alias.IsSome)
            |> Seq.iter(fun node -> 
                let flow = model.GetFlow(node.PageNum)
                let realName, bMyMFlow  = 
                    if(node.NodeType.IsCall) 
                    then node.CallName, flow.System.Active
                    else node.NameOrg,  flow.System.Active

                let sys    = model.DicSystems.[flow.System.Name]
                let btn = node.IsEmgBtn || node.IsStartBtn || node.IsAutoBtn || node.IsResetBtn 
                let bound = if(btn) then ExBtn
                            else if(bMyMFlow) then ThisFlow else OtherFlow

             
                if(node.Alias.IsSome)
                then
                    let segOrg = dicSeg.[node.AliasKey]
                    let aliasName = node.Alias.Value
                    let aliasSeg = MSeg(aliasName, mySys, ThisFlow, segOrg.NodeType, flow, segOrg.IsDummy)
                    aliasSeg.Update(node.Key, node.Id.Value, 0,0)
                    aliasSeg.Alias <- Some(segOrg)
                    dicSeg.TryAdd(node.Key, aliasSeg) |> ignore
                else 
                    let seg = MSeg(realName, sys,  bound, node.NodeType, flow, node.IsDummy)
                    seg.Update(node.Key, node.Id.Value, node.CntTX, node.CntRX)
                    dicSeg.TryAdd(node.Key, seg) |> ignore
                )

        let MakeCopySystem(doc:pptDoc, model:MModel) = 
            doc.Nodes 
                |> Seq.filter(fun node -> node.NodeType = COPY) 
                |> Seq.iter(fun node -> 
                        let flow  = model.GetFlow(node.PageNum)
                        
                        node.CopySys.foreach(fun sysName ->
                            let exSys = MSys.Create(sysName, false, model) 
                            let sys   = model.DicSystems.[node.Name]
                            sys.Flows.foreach(fun f ->  
                                let flow = MFlow.Create(f.Name, exSys, f.Page) 
                                let flowEdges = f.CopyMEdges()

                                flowEdges.foreach(fun edge -> flow.AddEdge(edge) |> ignore ))

                            sys.IFs.foreach(fun f-> exSys.AddInterface(f.Name, f.TXs, f.RXs)|> ignore)
                            )
                )

        let MakeSingleNode(doc:pptDoc, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>) = 
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

        //pptEdge 변환 및 등록
        let MakeEdges(doc:pptDoc, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>) = 
            let pptEdges = doc.Edges
            let parents = doc.Parents
            let mySys = model.ActiveSys
            let convertEdge(edge:pptEdge, sysSeg, mFlow:MFlow,  dicSeg:ConcurrentDictionary<string, MSeg>) = 
                let getParent(edge:pptEdge) = 
                    ImportCheck.SameParent(parents, edge)
                    let newParents = 
                        parents
                        |> Seq.filter(fun group -> group.Value.Contains(edge.StartNode)  && group.Value.Contains(edge.EndNode))
                        |> Seq.map(fun group -> 
                            Some(group.Key), dicSeg.[group.Key.Key]  )
        
                    if(newParents.Any())
                        then newParents |> Seq.toArray
                        else [(None , sysSeg)] |> Seq.toArray

                let sSeg = dicSeg.[edge.StartNode.Key]
                let eSeg = dicSeg.[edge.EndNode.Key]
               
                getParent(edge) |> Seq.iter(fun (parentNode, parentSeg) ->
                        
                               let mEdge = MEdge(sSeg, eSeg, edge.Causal)

                               if(mEdge.Causal = Interlock)
                               then mFlow.AddInterlock(mEdge)

                               if(parentNode.IsNone) 
                               then mFlow.AddEdge(mEdge) |> ignore
                               else parentSeg.ChildFlow.AddEdge(mEdge) |> ignore
                                  
                                    
                              )
            pptEdges
                |> Seq.iter(fun edge -> 
                    let flow = model.GetFlow(edge.PageNum)
                    let sSeg = dicSeg.[edge.StartNode.Key]
                    let eSeg = dicSeg.[edge.EndNode.Key]
                    if(sSeg.IsDummy || eSeg.IsDummy)
                    then 
                        let srcs = if(sSeg.Singles.Any()) then sSeg.Singles |> Seq.toList else [sSeg]
                        let tgts = if(eSeg.Singles.Any()) then eSeg.Singles |> Seq.toList else [eSeg]

                        srcs
                        |> Seq.iter(fun src ->
                                tgts
                                |> Seq.iter(fun tgt -> 
                                    let edge = pptEdge(edge.ConnectionShape, edge.Id, edge.PageNum, src.ShapeID, tgt.ShapeID , doc.DicNodes)
                                    convertEdge(edge,  mySys.SysSeg, flow, dicSeg)  ))
                    else 
                        convertEdge(edge, mySys.SysSeg, flow, dicSeg)
                    )

        //Dummy child 처리
        let MakeDummy(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:ConcurrentDictionary<string, MSeg>) = 
                parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key]
                    children 
                    |> Seq.iter(fun child -> pSeg.ChildFlow.AddSingleNode(dicSeg.[child.Key]) |> ignore  ) )
                

        let MakeLayouts(doc:pptDoc,  model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>) = 
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
                                mySys.LocationSet.TryAdd((dicSeg.[node.Key].Alias.Value).FullName, node.Rectangle) |> ignore
                            else
                                mySys.LocationSet.TryAdd(dicSeg.[node.Key].FullName, node.Rectangle) |> ignore
                                )

        //Safety 만들기
        let MakeSafety(pptNodes:pptNode seq, model:MModel, dicSeg:ConcurrentDictionary<string, MSeg>) = 
            let mySys = model.ActiveSys
            pptNodes
            |> Seq.filter(fun node -> node.IsDummy|>not)
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                    let mFlowName =  flow.Name
                    let dic = dicSeg.Values.where(fun w-> w.IsDummy|>not).Select(fun seg -> seg.FullName, seg) |> dict
                   
                    //Safety
                    let safeSeg = 
                        node.Safeties   //세이프티 입력 미등록 이름오류 체크
                        |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name mFlowName safe)
                        |> Seq.iter(fun safeFullName -> if(dic.ContainsKey safeFullName|>not) 
                                                        then Office.ErrorName(node.Shape, 28, node.PageNum))


                        node.Safeties   
                        |> Seq.map(fun safe ->  sprintf "%s.%s.%s" mySys.Name mFlowName safe)
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
                                