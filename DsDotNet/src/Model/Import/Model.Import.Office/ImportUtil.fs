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
module ImportU =

        type ImportUtil =
            [<Extension>]  static member GetAcive(model:#Model) =  model.Systems.Filter(fun s->s.Active).First()


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

        let MakeCopySystem(doc:pptDoc, model:CoreModule.Model, dicSys:Dictionary<int, DsSystem>) = 
            doc.Pages
                |> Seq.filter(fun page -> page.IsUsing)
                |> Seq.iter  (fun page -> 
                    let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                    if sysName = TextMySys |>not
                    then dicSys.Add(page.PageNum, DsSystem.Create(sysName, "", model))
                    else dicSys.Add(page.PageNum, model.FindSystem(TextMySys))
                    )

            doc.Nodes 
            |> Seq.filter(fun node -> node.NodeType = COPY) 
            |> Seq.iter(fun node -> 
                    node.CopySys.ForEach(fun copy -> 
                        let copySys = DsSystem.Create(copy.Key, "", model) 
                        dicSys.Add(-dicSys.Count, copySys)  //복사 시스템음 순서대로 음수 페이지로 저장
                        
                        )
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
        let MakeAlias(doc:pptDoc,  dicFlow:Dictionary<int, Flow>) = 

            let pptNodes = doc.Nodes
            let parents = doc.Parents

            let settingAlias(nodes:pptNode seq) = 
                let names  = nodes|> Seq.map(fun f->f.Name)
                (nodes, GetAliasName(names))
                ||> Seq.map2(fun node  nameSet -> node,  nameSet)
                |> Seq.iter(fun (node, (name, newName)) -> 
                                if(name  = newName |> not)
                                then    let orgNode = nodes |> Seq.filter(fun f->f.Name = name) |> Seq.head
                                        node.Alias <- Some(orgNode) 
                                        node.Name  <- newName
                                        )
                
            let dicFlowNodes = dicFlow
                                |> Seq.sortBy(fun flow -> flow.Key)
                                |> Seq.map(fun flow -> flow.Key, pptNodes |> Seq.filter(fun node -> node.PageNum = flow.Key)
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
        let MakeIf(pptNodes:pptNode seq, model:MModel, dicSeg:Dictionary<string, MSeg>) = 
            pptNodes
            |> Seq.filter(fun node -> node.NodeType = IF) 
            |> Seq.iter(fun node -> 
                    let flow = model.GetFlow(node.PageNum)
                    let txs = node.IfTxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>
                    let rxs = node.IfRxs |> Seq.map(fun f-> dicSeg.[f]) |> Seq.cast<IVertex>

                    flow.System.AddInterface(node.IfName, txs, rxs) |> ignore
            )


        //Interface 만들기
        let MakeInterfaces(doc :pptDoc, model:Model, dicSys:Dictionary<int, DsSystem>) = 
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType = IF) 
            |> Seq.iter(fun node ->  
                    let system = dicSys.[node.PageNum]
                    let apiName = node.Name;
                    ApiItem.Create(apiName, system) |> ignore
            )
            
        let MakeCopySystemAddApi(doc:pptDoc, model:Model, dicSys:Dictionary<int, DsSystem>) = 
            doc.Nodes 
                |> Seq.filter(fun node -> node.NodeType = COPY) 
                |> Seq.iter(fun node -> 
                        node.CopySys.ForEach(fun copy -> 
                            let orgiSys = dicSys.Values.Where(fun w->w.Name = copy.Value).First()
                            let copySys = dicSys.Values.Where(fun w->w.Name = copy.Key).First()

                            orgiSys.ApiItems
                            |>Seq.iter(fun api->copySys.ApiItems.Add(ApiItem.Create(api.Name, copySys)) |>ignore)
                        )
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


        let private createVertex(model:Model, node:pptNode, parentReal:Real Option, parentFlow:Flow Option, dicFlow:Dictionary<int, Flow>, dicSeg:Dictionary<string, Vertex>) = 
            if node.Alias.IsSome 
            then
                 let vertex = dicSeg.[node.Alias.Value.Key] 
                 Alias.CreateInFlow(node.Name, vertex.NameComponents , dicFlow.[node.PageNum]) :> Vertex
                 //if parentReal.IsSome
                 //then 
                 //    let v = dicSeg.[node.Alias.Value.Key] 
                 //    Alias.CreateInFlow(node.Name, v.NameComponents , dicFlow.[node.PageNum]) :> Vertex
                 //else
                 //    Alias.CreateInReal(node.Name, dicSeg.[node.Alias.Value.Key] , dicFlow.[node.PageNum]) :> Vertex  //ahn Call 규격시 처리
            else

                let name =   if(node.NodeType.IsCall) then node.CallName else node.NameOrg
                let vertex = 
                            if(node.NodeType.IsReal) 
                            then  Real.Create(name, parentFlow.Value) :> Vertex
                            else 
                                    let system =model.FindSystem(node.CallName.Split('.').[0])
                                    let ifName = node.Name.Split('.').[1];
                                    let findApi = model.FindApiItem([|system.Name;ifName|]) 
                                    let api = if findApi.IsNull() then ApiItem.Create(ifName, system) else findApi


                                    if(parentReal.IsSome)
                                    then  Call.CreateInReal(api, parentReal.Value)  :> Vertex  
                                    else  Call.CreateInFlow(api, parentFlow.Value)  :> Vertex  
                vertex


        //segment 리스트 만들기
        let MakeSegment(pptNodes:pptNode seq, model:Model
            , parents:ConcurrentDictionary<pptNode, seq<pptNode>>
            , dicFlow:Dictionary<int, Flow>
            , dicSeg:Dictionary<string, Vertex>) = 

            let dicChildParent = 
                parents 
                |> Seq.filter(fun parent -> parent.Key.IsDummy|>not)
                |> Seq.collect(fun parentChildren -> 
                    parentChildren.Value 
                    |> Seq.map(fun child -> child, parentChildren.Key)) |> dict

            //Real 부터
            pptNodes
                |> Seq.filter(fun node -> node.Alias.IsNone) 
                |> Seq.filter(fun node -> node.NodeType.IsReal) 
                |> Seq.filter(fun node -> dicChildParent.ContainsKey(node)|>not) 
                |> Seq.iter(fun node   -> dicSeg.Add(node.Key, createVertex(model, node, None, Some(dicFlow.[node.PageNum]), dicFlow, dicSeg))|>ignore)
            //Call 처리
            pptNodes
                |> Seq.filter(fun node -> node.Alias.IsNone) 
                |> Seq.filter(fun node -> node.NodeType.IsCall) 
                |> Seq.iter(fun node -> 
                            let parentReal = if  dicChildParent.ContainsKey(node) 
                                             then Some(dicSeg.[dicChildParent.[node].Key] :?> Real)
                                             else None
                            let parentFlow = if  dicChildParent.ContainsKey(node) 
                                             then None
                                             else Some(dicFlow.[node.PageNum])
                            dicSeg.Add(node.Key, createVertex(model, node, parentReal, parentFlow, dicFlow, dicSeg)))
          
            //Alias Node 처리 마감
            pptNodes
            |> Seq.filter(fun node -> node.Alias.IsSome)
            |> Seq.iter(fun node -> 
                    let segOrg = dicSeg.[node.Alias.Value.Key]
                    if dicChildParent.ContainsKey(node) 
                    then 
                        let real = dicSeg.[dicChildParent.[node].Key] :?> Real
                        dicSeg.Add(node.Key, Alias.CreateInReal(node.Name, segOrg:?>Call, real) )
                    else 
                        dicSeg.Add(node.Key, Alias.CreateInFlow(node.Name, segOrg.NameComponents, dicFlow.[node.PageNum]) )

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

        let private getParent(edge:pptEdge, parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, Vertex>) = 
                ImportCheck.SameParent(parents, edge)
                let newParents = 
                    parents
                    |> Seq.filter(fun group -> group.Value.Contains(edge.StartNode) && group.Value.Contains(edge.EndNode))
                    |> Seq.map(fun group ->   dicSeg.[group.Key.Key])
                    
                if(newParents.Any() && newParents.length() > 1) then failwith "중복부모"
                if(newParents.Any())
                    then Some( newParents |> Seq.head)
                    else None 

        let private getDummys(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, Vertex>) = 
                    parents
                    |> Seq.filter(fun group -> group.Key.IsDummy)
                    |> Seq.map(fun group -> group.Key, group.Value |> Seq.map(fun node-> dicSeg.[node.Key])) |> dict

        let MakeVetexEdges(doc:pptDoc, model:Model
            , dicFlow:Dictionary<int, Flow>
            , dicSeg:Dictionary<string, Vertex> ) = 

            let pptEdges = doc.Edges
            let parents = doc.Parents
            let dicDummys = getDummys(parents, dicSeg)
            
            pptEdges
                |> Seq.iter(fun edge -> 
                    let flow = dicFlow.[edge.PageNum]
                    let realParentOption = match getParent(edge, parents, dicSeg) with
                                            |Some(real) -> Some(real:?>Real)
                                            |None ->       None

                    let flowParentOption = if realParentOption.IsSome then None else Some(flow)
                    let sSeg = if dicSeg.ContainsKey(edge.StartNode.Key)
                                then dicSeg.[edge.StartNode.Key]  
                                else createVertex(model, edge.StartNode,  realParentOption, flowParentOption, dicFlow, dicSeg)
                                    
                    let eSeg = if dicSeg.ContainsKey(edge.EndNode.Key)
                                then dicSeg.[edge.EndNode.Key]  
                                else createVertex(model, edge.EndNode,  realParentOption, flowParentOption, dicFlow, dicSeg)

                    let graph = if(realParentOption.IsSome) then realParentOption.Value.Graph else flow.Graph
                    if(edge.StartNode.IsDummy || edge.EndNode.IsDummy)
                    then 
                        
                        let srcs = if(edge.StartNode.IsDummy) then dicDummys.[edge.StartNode] else [dicSeg.[edge.StartNode.Key]]
                        let tgts = if(edge.EndNode.IsDummy)   then dicDummys.[edge.EndNode]   else [dicSeg.[edge.EndNode.Key]]

                        srcs
                        |> Seq.iter(fun src -> graph.AddVertex(src) |> ignore
                                               tgts
                                               |> Seq.iter(fun tgt -> graph.AddVertex(tgt) |> ignore))
                                       
                    else
                        graph.AddVertex(sSeg) |>ignore
                        graph.AddVertex(eSeg) |>ignore
                    )

        //pptEdge 변환 및 등록
        let MakeEdges(doc:pptDoc, model:Model
            , dicFlow:Dictionary<int, Flow>
            , dicSys:Dictionary<int, DsSystem>
            , dicSeg:Dictionary<string, Vertex>) = 

            let pptEdges = doc.Edges
            let parents = doc.Parents

            let convertEdge(edge:pptEdge, flow:Flow, src:Vertex, tgt:Vertex) = 
                let graph = 
                      match getParent(edge, parents, dicSeg) with
                      |Some(real) -> (real:?>Real).Graph
                      |None ->       flow.Graph
                        
                if(edge.Causal = Interlock)
                then  
                    Edge.Create(graph, src, tgt, ResetPush ) |> ignore
                    Edge.Create(graph, tgt, src, ResetPush) |> ignore  //vertex 미리 추가 ?
                elif (edge.Causal = StartReset)
                then
                    Edge.Create(graph, src, tgt, StartEdge) |> ignore
                    Edge.Create(graph, tgt, src, ResetEdge) |> ignore
                else
                    Edge.Create(graph, src, tgt, edge.Causal) |> ignore

            let dicDummys = getDummys(parents, dicSeg)

            pptEdges
                |> Seq.iter(fun edge -> 
                    let flow = dicFlow.[edge.PageNum]
                    
                    if(edge.StartNode.NodeType = IF && edge.StartNode.NodeType = edge.EndNode.NodeType|>not)
                    then Office.ErrorConnect(edge.ConnectionShape,37, edge.StartNode.Name, edge.EndNode.Name, edge.PageNum)



                    if(edge.StartNode.NodeType = IF || edge.EndNode.NodeType = IF)
                    then
                        let sys = dicSys.[edge.PageNum]
                        sys.ApiResetInfos.Add(ApiResetInfo.Create(sys, edge.StartNode.Name, edge.EndNode.Name, edge.Causal.ToText()))|>ignore
                        
                    else 

                        let srcs = if(edge.StartNode.IsDummy) then dicDummys.[edge.StartNode] else [dicSeg.[edge.StartNode.Key]]
                        let tgts = if(edge.EndNode.IsDummy)   then dicDummys.[edge.EndNode]   else [dicSeg.[edge.EndNode.Key]]

                        if(edge.StartNode.IsDummy || edge.EndNode.IsDummy)
                        then 
                       
                            srcs
                            |> Seq.iter(fun src -> tgts
                                                   |> Seq.iter(fun tgt -> convertEdge(edge, flow, src, tgt) |> ignore))
                                       
                        else 
                            let sSeg = srcs.First()
                            let eSeg = tgts.First()
                            convertEdge(edge, flow, sSeg, eSeg) |> ignore
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
                

        let MakeDummys(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, Vertex>) = 
                parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key] :?> Real
                    children 
                    |> Seq.iter(fun child -> pSeg.Graph.AddVertex(dicSeg.[child.Key]) |> ignore  ) )


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
        //Safety 만들기
        let MakeSafeties(pptNodes:pptNode seq, model:Model, dicFlow:Dictionary<int, Flow>, dicNodeKeySegs:Dictionary<string, Vertex>) = 
            pptNodes
            |> Seq.filter(fun node -> node.IsDummy|>not)
            |> Seq.iter(fun node -> 
                    let flow = dicFlow.[node.PageNum]
                    let dicQualifiedNameSegs  = dicNodeKeySegs.Values.Select(fun seg -> seg.QualifiedName, seg) |> dict
                    let safeName(safe) = sprintf "%s.%s.%s_%s" flow.System.Name flow.Name flow.Name safe
                    
                        
                    node.Safeties   //세이프티 입력 미등록 이름오류 체크
                    |> Seq.map(fun safe ->  safeName(safe))
                    |> Seq.iter(fun safeFullName -> if(dicQualifiedNameSegs.ContainsKey safeFullName|>not) 
                                                    then Office.ErrorName(node.Shape, 28, node.PageNum))

                    node.Safeties   
                    |> Seq.map(fun safe ->  safeName(safe))
                    |> Seq.map(fun safeFullName ->  dicQualifiedNameSegs.[safeFullName])
                    |> Seq.iter(fun safeConditionSeg ->
                            let realTarget = dicNodeKeySegs.[node.Key] :?> Real  //Target  call 은 안되나 ?  ahn
                            realTarget.SafetyConditions.Add(safeConditionSeg :?> Real)|>ignore)  //safeCondition  call 은 안되나 ?
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
            let mySys = ImportUtil.GetAcive(model) 
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

