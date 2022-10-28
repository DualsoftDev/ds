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


        let MakeSystem(doc:pptDoc, model:CoreModule.Model, dicSys:Dictionary<int, DsSystem>) = 
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
        let MakeInterfaces(doc :pptDoc, model:Model, dicSys:Dictionary<int, DsSystem>) = 
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType = IF) 
            |> Seq.iter(fun node ->  
                    let system = dicSys.[node.PageNum]
                    let apiName = node.IfName;
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
                            |>Seq.iter(fun api-> ApiItem.Create(api.Name, copySys) |>ignore)
                        )
                )

        let MakeApiTxRx(doc:pptDoc, model:Model, dicFlow:Dictionary<int, Flow>) = 
            //1. 원본처리
            doc.Nodes 
                |> Seq.filter(fun node -> node.NodeType = IF) 
                |> Seq.iter(fun node -> 
                        let flow = dicFlow.[node.PageNum]
                        let sys =  dicFlow.[node.PageNum].System
                        let api = sys.ApiItems.Where(fun w->w.Name = node.IfName).First() 

                        let findReal(trxName:string) = model.FindGraphVertex([|sys.Name;flow.Name;trxName|]) :?> Real
                        let txs = node.IfTxs |> Seq.map(fun f-> findReal(f)) 
                        let rxs = node.IfRxs |> Seq.map(fun f-> findReal(f))
                        api.AddTXs(txs)|>ignore
                        api.AddRXs(rxs)|>ignore
                        )

            //1. CopySystem 처리
            doc.Nodes 
            |> Seq.filter(fun node -> node.NodeType = COPY) 
            |> Seq.iter(fun node -> 
                    node.CopySys.ForEach(fun copy -> 
                        let orgiSys = model.FindSystem(copy.Value)
                        let copySys = model.FindSystem(copy.Key)
                        let findApi name = orgiSys.ApiItems.Where(fun w->w.Name = name).First()
                        copySys.ApiItems
                            |>Seq.iter(fun api-> 
                                ()
                                ////복사원본이 아닌 외부자신의 리얼을 찾아서 넣어야함
                                //let findReal(trxName:string) = model.FindGraphVertex([|copySys.Name;TextExFlow;trxName|]) :?> Real
                                //let txs = (findApi (api.Name)).TXs |> Seq.map(fun f-> findReal(f.Name)) 
                                //let rxs = (findApi (api.Name)).RXs |> Seq.map(fun f-> findReal(f.Name)) 

                                //api.AddTXs(txs)  |> ignore
                                //api.AddRXs(rxs)  |> ignore 
                                
                                )
                                )
                    )

        let private createVertex(model:Model, node:pptNode, parentReal:Real Option, parentFlow:Flow Option, dicSeg:Dictionary<string, Vertex>) = 
            let name =   if(node.NodeType.IsCall) then node.CallName else node.NameOrg
            if(node.NodeType.IsReal) 
            then 
                let real = Real.Create(name, parentFlow.Value) 
                dicSeg.Add(node.Key, real)
            else 
                let system =model.FindSystem(node.CallName.Split('.').[0])
                let ifName = node.Name.Split('.').[1];
                let findApi = model.FindApiItem([|system.Name;ifName|]) 
                //Api 은 CopySystem 에서 미리 만들어야함 
                // let api = if findApi.IsNull() then ApiItem.Create(ifName, system) else findApi
                let call = 
                    if(parentReal.IsSome)
                    then  Call.CreateInReal(findApi, parentReal.Value)  
                    else  Call.CreateInFlow(findApi, parentFlow.Value)  

                dicSeg.Add(node.Key, call)


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
                |> Seq.iter(fun node   -> createVertex(model, node, None, Some(dicFlow.[node.PageNum]), dicSeg))

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
                            createVertex(model, node, parentReal, parentFlow, dicSeg))
          
            //Alias Node 처리 마감
            pptNodes
            |> Seq.filter(fun node -> node.Alias.IsSome)
            |> Seq.iter(fun node -> 
                    let segOrg = dicSeg.[node.Alias.Value.Key]
                    if dicChildParent.ContainsKey(node) 
                    then 
                        let real = dicSeg.[dicChildParent.[node].Key] :?> Real
                        let alias = Alias.CreateInReal(node.Name, segOrg:?>Call, real)
                        dicSeg.Add(node.Key, alias)
                    else 
                        let flow = dicFlow.[node.PageNum]
                        let alias = Alias.CreateInFlow(node.Name, segOrg.NameComponents, flow)
                        dicSeg.Add(node.Key, alias )
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
                    
                    if dicSeg.ContainsKey(edge.StartNode.Key) |> not
                        then createVertex(model, edge.StartNode,  realParentOption, flowParentOption,  dicSeg)
                                    
                    if dicSeg.ContainsKey(edge.EndNode.Key)
                        then createVertex(model, edge.EndNode,  realParentOption, flowParentOption,  dicSeg)

                    let graph = if(realParentOption.IsSome) then realParentOption.Value.Graph else flow.Graph
                    if(edge.StartNode.IsDummy || edge.EndNode.IsDummy)
                    then 
                        
                        let srcs = if(edge.StartNode.IsDummy) then dicDummys.[edge.StartNode] else [dicSeg.[edge.StartNode.Key]]
                        let tgts = if(edge.EndNode.IsDummy)   then dicDummys.[edge.EndNode]   else [dicSeg.[edge.EndNode.Key]]

                        srcs
                        |> Seq.iter(fun src -> graph.AddVertex(src) |> ignore
                                               tgts
                                               |> Seq.iter(fun tgt -> graph.AddVertex(tgt) |> ignore))
                                       
                    //else
                    //    graph.AddVertex(sSeg) |>ignore
                    //    graph.AddVertex(eSeg) |>ignore
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
                        sys.ApiResetInfos.Add(ApiResetInfo.Create(sys, edge.StartNode.Name, edge.Causal.ToText() ,edge.EndNode.Name ))|>ignore
                        
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


        let MakeDummys(parents:ConcurrentDictionary<pptNode, seq<pptNode>>, dicSeg:Dictionary<string, Vertex>) = 
                parents
                |> Seq.filter(fun group -> group.Key.IsDummy)
                |> Seq.map(fun group -> group.Key, group.Value)
                |> Seq.iter(fun (parent, children) -> 
                    let pSeg = dicSeg.[parent.Key] :?> Real
                    children 
                    |> Seq.iter(fun child -> pSeg.Graph.AddVertex(dicSeg.[child.Key]) |> ignore  ) )


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

