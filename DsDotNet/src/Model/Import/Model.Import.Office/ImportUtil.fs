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

[<AutoOpen>]
module ImportUtil =
        

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
                    node.CopySys.ForEach(fun sysName -> MSys.Create(sysName, false, model) |> ignore)
                    )

        let MakeExSystem(doc:pptDoc, model:CoreModule.Model) = 
            doc.Pages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    if sysName = TextMySys |>not
                    then DsSystem.Create(sysName, "", model) |> ignore
                    )

            doc.Nodes 
            |> Seq.filter(fun node -> node.NodeType = COPY) 
            |> Seq.iter(fun node -> 
                    node.CopySys.ForEach(fun sysName -> DsSystem.Create(sysName, "", model) |> ignore)
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
                                |> Seq.sortBy(fun flow -> flow.Page)
                                |> Seq.map(fun flow -> flow.Page, pptNodes |> Seq.filter(fun node -> node.PageNum = flow.Page)
                                ) |> dict
            

            let children = parents |> Seq.collect(fun parentSet -> parentSet.Value)
            let callInFlowSet = dicFlowNodes
                                |> Seq.map(fun flowNodes -> 
                                         flowNodes.Value 
                                         |> Seq.filter(fun node -> node.NodeType.IsCall && children.Contains(node)|>not)
                                         )

            let realSet = dicFlowNodes
                                |> Seq.map(fun flowNodes -> 
                                         flowNodes.Value |> Seq.filter(fun node -> node.NodeType.IsReal))
            
           
            let callInRealSet = realSet 
                                  |> Seq.collect(fun reals -> reals)
                                  |> Seq.filter(fun real -> parents.ContainsKey(real))
                                  |> Seq.map(fun real -> 
                                                dicFlowNodes.[real.PageNum] 
                                                |> Seq.filter(fun node -> node.NodeType.IsCall)
                                                |> Seq.filter(fun node -> parents.[real].Contains(node) ))
            
            realSet |> Seq.iter settingAlias
            callInFlowSet |> Seq.iter settingAlias
            callInRealSet |> Seq.iter settingAlias
           

        //Interface 만들기
        let MakeInterface(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
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
                    let segOrg = dicSeg.[node.AliasKey]
                    let aliasName = node.Alias.Value
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

        let MakeParents(pptNodes:pptNode seq, model:Model, dicSeg:Dictionary<string, Vertex>, parents:ConcurrentDictionary<pptNode, seq<pptNode>>) = 
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
                        ()
                       // child.Parent <- Some(parent)
                    )

      //segment 리스트 만들기
        let MakeSegment(pptNodes:pptNode seq, model:Model, parents:ConcurrentDictionary<pptNode, seq<pptNode>>,  dicFlow:Dictionary<int, Flow>) = 
            
            let dicSeg = Dictionary<string, Vertex>()
            let dicChildParent = parents 
                                |> Seq.filter(fun parentChildren -> parentChildren.Key.IsDummy|>not)
                                |> Seq.collect(fun parentChildren -> 
                                                           parentChildren.Value 
                                                           |> Seq.map(fun child -> child, parentChildren.Key)) |> dict

            let mySys = model.Systems.Where(fun w->w.Active).First(); // 일단 ppt는 단일 Active system만 지원 
            let getVertex(node:pptNode, parent:Vertex Option) = 
                let name =   if(node.NodeType.IsCall) then node.CallName else node.NameOrg
                let vertex = 
                             let flow  = dicFlow.[node.PageNum]
                             if(node.NodeType.IsCall) 
                                then  Real.Create(name, flow) :> Vertex
                                else  let newApi= ApiItem.Create(node.Name.Split('.').[1], model.FindSystem(node.Name.Split('.').[0]))
                                      Call.CreateInFlow(newApi, flow)  :> Vertex   //부모위치 설정 필요 ahn
                                                //   Call.CreateInReal(newApi, flow)  :> Vertex   //부모위치 설정 필요 ahn
                vertex
                    
            // let sys   = if(node.NodeType.IsCall) 
            //                then model.FindSystem(node.CallName.Split('.').[0])
            //                else mySys

            //let btn   = node.IsEmgBtn || node.IsStartBtn || node.IsAutoBtn || node.IsResetBtn 
            //let bound = if(btn) then ExBtn
            //            else if(node.NodeType.IsCall) then OtherFlow else ThisFlow

            //Stem  Node 줄기 부터
            pptNodes
                |> Seq.filter(fun node -> node.Alias.IsNone) 
                |> Seq.filter(fun node -> dicChildParent.ContainsKey(node)|>not) 
                |> Seq.iter(fun node -> dicSeg.Add(node.Key, getVertex(node, None)))
            //Leaf  Node 끝단 처리
            pptNodes
                |> Seq.filter(fun node -> node.Alias.IsNone) 
                |> Seq.filter(fun node -> dicChildParent.ContainsKey(node)) 
                |> Seq.iter(fun node -> 
                            let parent = dicChildParent.[node]
                            dicSeg.Add(node.Key, getVertex(node, Some(getVertex(parent, None))))
                            )
          
            //Alias Node 처리 마감
            pptNodes
            |> Seq.sortBy(fun node -> node.Alias.IsSome)
            |> Seq.iter(fun node -> 
             
                if(node.Alias.IsSome)
                then
                    let segOrg = dicSeg.[node.AliasKey]
                    let aliasName = node.Alias.Value
                    Alias.CreateInFlow(aliasName, segOrg.NameComponents, dicFlow.[node.PageNum]) |> ignore
                    //let aliasSeg = MSeg(aliasName, mySys, ThisFlow, segOrg.NodeType, flow, segOrg.IsDummy)
                    //aliasSeg.Update(node.Key, node.Id.Value, 0,0)
                    //aliasSeg.AliasOrg <- Some(segOrg)
                else 
                   
                    let vertex = getVertex(node, None)
                    if dicChildParent.ContainsKey(node)
                    then ()
                    else ()

                    dicSeg.Add(node.Key, vertex)

                    //let seg = MSeg(name, sys,  bound, node.NodeType, flow, node.IsDummy)
                    //seg.Update(node.Key, node.Id.Value, node.CntTX, node.CntRX)
                )

     
      
        let MakeCopySystem(doc:pptDoc, model:MModel) = 
            doc.Nodes 
                |> Seq.filter(fun node -> node.NodeType = COPY) 
                |> Seq.iter(fun node -> 
                        node.CopySys.ForEach(fun sysName ->
                            let exSys = model.DicSystems.[sysName]
                            let libSys   = model.DicSystems.[node.Name]
                            libSys.Flows.ForEach(fun f ->  
                                let flow = MFlow.Create(f.Name, exSys, f.Page) 
                                let flowEdges = f.CopyMEdges()

                                flowEdges.ForEach(fun edge -> flow.AddEdge(edge) |> ignore ))

                            libSys.IFs.ForEach(fun f-> exSys.AddInterface(f.Name, f.TXs, f.RXs)|> ignore)
                            )
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

        //pptEdge 변환 및 등록
        let MakeEdges(doc:pptDoc, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
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

        //Dummy child 처리
        let MakeDummy(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, MSeg>) = 
                parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key]
                    children 
                    |> Seq.iter(fun child -> pSeg.ChildFlow.AddSingleNode(dicSeg.[child.Key]) |> ignore  ) )
                

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
        //EMG & Start & Auto 리스트 만들기
        let MakeButtons(pptNodes:pptNode seq, model:Model, dicFlow: Dictionary<int, Flow>) = 
            let mySys = model.Systems.Where(fun w->w.Active).First();
            pptNodes
            |> Seq.filter(fun node -> node.IsDummy|>not)
            |> Seq.iter(fun node -> 
                    let flow = dicFlow.[node.PageNum]
                               
                    //Start, Reset, Auto, Emg 버튼
                    if(node.IsStartBtn) then mySys.AddButton(BtnType.AutoBTN, node.Name, flow)
                    if(node.IsResetBtn) then mySys.AddButton(BtnType.ResetBTN,node.Name, flow)
                    if(node.IsAutoBtn) then mySys.AddButton(BtnType.AutoBTN,node.Name, flow)
                    if(node.IsEmgBtn) then mySys.AddButton(BtnType.EmergencyBTN ,node.Name, flow)
                    )
        
                                