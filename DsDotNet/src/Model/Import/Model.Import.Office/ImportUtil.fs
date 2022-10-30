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

    let dicSys  = Dictionary<int, DsSystem>()  //0 페이지 기본 나의 시스템 (각페이지별 해당시스템으로 구성)
    let dicCopy = Dictionary<DsSystem, DsSystem>()  //Dic<copySys, orgiSys> 원본 구조 생성시 계속 같이 만듬
    let dicFlow = Dictionary<int, Flow>() // page , flow
    let dicVertex = Dictionary<string, Vertex>()

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

    
   
   


    [<Extension>]
    type ImportUtil =
        [<Extension>]  static member GetAcive(model:Model) =  model.Systems.Filter(fun s->s.Active).First()
        [<Extension>]  static member MakeSystem (doc:pptDoc, model:Model) = 
                        doc.Pages
                            |> Seq.filter(fun page -> page.IsUsing)
                            |> Seq.iter  (fun page -> 
                                let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                                if sysName = TextMySys |>not
                                then dicSys.Add(page.PageNum, DsSystem.Create(sysName, "", model))
                                else dicSys.Add(page.PageNum, model.FindSystem(TextMySys))
                                )
          
        [<Extension>]  static member MakeCopySystem (doc:pptDoc, model:Model) = 
                        doc.Nodes 
                        |> Seq.filter(fun node -> node.NodeType = COPY) 
                        |> Seq.iter(fun node -> 
                                node.CopySys.ForEach(fun copy ->
                                    let orgSys  = model.FindSystem(copy.Value) 
                                    let copySys = DsSystem.Create(copy.Key, "", model) 
                                    dicCopy.Add(copySys, orgSys) |>ignore
                                    )
                                )

        //Interface 만들기
        [<Extension>]  static member  MakeInterfaces (doc :pptDoc) = 
                        doc.Nodes
                        |> Seq.filter(fun node -> node.NodeType = IF) 
                        |> Seq.iter(fun node ->  
                                let system = dicSys.[node.PageNum]
                                let apiName = node.IfName;
                                ApiItem.Create(apiName, system) |> ignore
                        )

        [<Extension>]  static member MakeCopyApi (doc:pptDoc, model:Model) = 
                            doc.Nodes 
                                |> Seq.filter(fun node -> node.NodeType = COPY) 
                                |> Seq.iter(fun node -> 
                                        node.CopySys.ForEach(fun copy -> 
                                            let orgSys = model.FindSystem(copy.Value) 
                                            let copySys = model.FindSystem(copy.Key) 

                                            orgSys.ApiItems
                                            |>Seq.iter(fun api-> ApiItem.Create(api.Name, copySys) |>ignore)
                                        )
                                )

                            
        //MFlow 리스트 만들기
        [<Extension>]  static member MakeFlows (doc:pptDoc, model:Model) = 
                            doc.Pages
                            |> Seq.filter(fun page -> page.IsUsing)
                            |> Seq.iter  (fun page -> 
                                let pageNum  = page.PageNum
                                let sysName, flowName = GetSysNFlow(page.Title, page.PageNum)
                                let sys    = model.FindSystem(sysName)     
                                dicFlow.Add(pageNum,  Flow.Create(flowName, sys) ) |> ignore
                                )

                            //copy system flow 동일 처리
                            dicCopy.ForEach(fun sysTwin->
                                let copySys = sysTwin.Key
                                let origSys = sysTwin.Value
                                origSys.Flows.ForEach(fun flow->Flow.Create(flow.Name, copySys)|>ignore)
                                )


                     //EMG & Start & Auto 리스트 만들기
        [<Extension>]  static member  MakeButtons (doc:pptDoc, model:Model) = 
                        let mySys = model.GetAcive() 
                        doc.Nodes
                        |> Seq.filter(fun node -> node.IsDummy|>not)
                        |> Seq.iter(fun node -> 
                                let flow = dicFlow.[node.PageNum]
                                //Start, Reset, Auto, Emg 버튼
                                if(node.IsStartBtn) then mySys.AddButton(BtnType.StartBTN, node.Name, flow)
                                if(node.IsResetBtn) then mySys.AddButton(BtnType.ResetBTN,node.Name, flow)
                                if(node.IsAutoBtn)  then mySys.AddButton(BtnType.AutoBTN,node.Name, flow)
                                if(node.IsEmgBtn)   then mySys.AddButton(BtnType.EmergencyBTN ,node.Name, flow)
                                )

                         //copy system flow 동일 처리
                        dicCopy.ForEach(fun sysTwin ->
                            let copySys = sysTwin.Key
                            let origSys = sysTwin.Value
                            
                            origSys.StartButtons.ForEach(fun btn ->
                                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(StartBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
                            origSys.ResetButtons.ForEach(fun btn ->
                                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(ResetBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
                            origSys.AutoButtons.ForEach(fun btn ->
                                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(AutoBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
                            origSys.EmergencyButtons.ForEach(fun btn ->
                                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(EmergencyBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
                            
                            )
        
        //real call alias  만들기
        [<Extension>] static member MakeSegment (doc:pptDoc, model:Model) = 
                        let pptNodes = doc.Nodes
                        let parents = doc.Parents
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
                            |> Seq.iter(fun node   -> createVertex(model, node, None, Some(dicFlow.[node.PageNum]), dicVertex))

                        //Call 처리
                        pptNodes
                            |> Seq.filter(fun node -> node.Alias.IsNone) 
                            |> Seq.filter(fun node -> node.NodeType.IsCall) 
                            |> Seq.iter(fun node -> 
                                        let parentReal = if  dicChildParent.ContainsKey(node) 
                                                            then Some(dicVertex.[dicChildParent.[node].Key] :?> Real)
                                                            else None
                                        let parentFlow = if  dicChildParent.ContainsKey(node) 
                                                            then None
                                                            else Some(dicFlow.[node.PageNum])
                                        createVertex(model, node, parentReal, parentFlow, dicVertex))
          
                        //Alias Node 처리 마감
                        pptNodes
                        |> Seq.filter(fun node -> node.Alias.IsSome)
                        |> Seq.iter(fun node -> 
                                let segOrg = dicVertex.[node.Alias.Value.Key]
                                if dicChildParent.ContainsKey(node) 
                                then 
                                    let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                                    let alias = Alias.CreateInReal(node.Name, segOrg:?>Call, real)
                                    dicVertex.Add(node.Key, alias)
                                else 
                                    let flow = dicFlow.[node.PageNum]
                                    let alias = Alias.CreateInFlow(node.Name, segOrg.NameComponents, flow)
                                    dicVertex.Add(node.Key, alias )
                            )

                  
                        //copy system  동일 처리
                        dicCopy.ForEach(fun sysTwin ->
                            let copySys = sysTwin.Key
                            let origSys = sysTwin.Value
                            let findReal (flow:Flow, realName:string)  = model.FindGraphVertex([|copySys.Name;flow.Name;realName|]) :?> Vertex
                            let findCall (flow:Flow, callName:string)  = model.FindGraphVertex([|copySys.Name;flow.Name;callName|]) :?> Vertex
                            let findAlias(flow:Flow, aliasName:string) = model.FindGraphVertex([|copySys.Name;flow.Name;aliasName|]) :?> Vertex
                            let findCopyApi  (orgApi:ApiItem)   = model.FindSystem(orgApi.System.Name).ApiItems.First(fun f->f.Name = orgApi.Name)



                            if origSys.Name.StartsWith("Ex") 
                            then ()

                            //Real, Call 처리 부터
                            origSys.Flows.ForEach(fun flow->
                                    let copyFlow = copySys.FindFlow(flow.Name)
                                    flow.Graph.Vertices.ForEach(fun vInFlow ->
                                        match vInFlow  with
                                        | :? Real as orgiReal   
                                            -> Real.Create(orgiReal.Name, copyFlow) |> ignore
                                               orgiReal.Graph.Vertices.ForEach(fun vInReal->
                                                match vInReal  with
                                                | :? Call as orgiCall -> Call.CreateInFlow(findCopyApi(orgiCall.ApiItem), copyFlow) |> ignore
                                                | _ -> () )

                                        | :? Call as orgiCall   -> Call.CreateInFlow(findCopyApi(orgiCall.ApiItem), copyFlow) |> ignore
                                        | _ -> () )
                                 )

                            //Alias Node 처리 마감
                            origSys.Flows.ForEach(fun flow->
                                    let copyFlow = copySys.FindFlow(flow.Name)
                                    flow.Graph.Vertices.ForEach(fun vInFlow ->
                                        match vInFlow  with
                                        | :? Real as orgiReal   
                                            -> 
                                               orgiReal.Graph.Vertices.ForEach(fun vInReal->
                                                match vInReal  with
                                                | :? Alias as orgiAlias -> Alias.CreateInFlow(vInReal.Name,  findAlias(copyFlow, vInReal.Name).NameComponents, copyFlow) |> ignore
                                                | _ -> () )

                                        | :? Alias as orgiAlias -> Alias.CreateInFlow(vInFlow.Name,  findAlias(copyFlow, vInFlow.Name).NameComponents, copyFlow) |> ignore
                                        | _ -> () )
                                 )

                            )

        


                            //pptEdge 변환 및 등록
        [<Extension>] static member  MakeEdges (doc:pptDoc, model:Model) = 
                            let pptEdges = doc.Edges
                            let parents = doc.Parents

                            let convertEdge(edge:pptEdge, flow:Flow, src:Vertex, tgt:Vertex) = 
                                let graph = 
                                        match getParent(edge, parents, dicVertex) with
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

                            let dicDummys = getDummys(parents, dicVertex)

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

                                        let srcs = if(edge.StartNode.IsDummy) then dicDummys.[edge.StartNode] else [dicVertex.[edge.StartNode.Key]]
                                        let tgts = if(edge.EndNode.IsDummy)   then dicDummys.[edge.EndNode]   else [dicVertex.[edge.EndNode.Key]]

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

                                //copy system  동일 처리
                            dicCopy.ForEach(fun sysTwin ->
                                let copySys = sysTwin.Key
                                let origSys = sysTwin.Value
                                origSys.Flows
                                    .ForEach(fun flow->
                                        let copyFlow = copySys.FindFlow(flow.Name)
                                        let findReal(realName:string) = model.FindGraphVertex([|copySys.Name;copyFlow.Name;realName|]) :?> Vertex
                                        flow.Graph.Edges.ForEach(fun e->
                                            Edge.Create(
                                                copyFlow.Graph
                                                , findReal(e.Source.Name)
                                                , findReal(e.Target.Name)
                                                , e.EdgeType) |> ignore
                                            )
                                        )
                                )

             //Safety 만들기
        [<Extension>] static member MakeSafeties (doc:pptDoc, model:Model) = 
                        doc.Nodes
                        |> Seq.filter(fun node -> node.IsDummy|>not)
                        |> Seq.iter(fun node -> 
                                let flow = dicFlow.[node.PageNum]
                                let dicQualifiedNameSegs  = dicVertex.Values.Select(fun seg -> seg.QualifiedName, seg) |> dict
                                let safeName(safe) = sprintf "%s.%s.%s_%s" flow.System.Name flow.Name flow.Name safe
                        
                                node.Safeties   //세이프티 입력 미등록 이름오류 체크
                                |> Seq.map(fun safe ->  safeName(safe))
                                |> Seq.iter(fun safeFullName -> if(dicQualifiedNameSegs.ContainsKey safeFullName|>not) 
                                                                then Office.ErrorName(node.Shape, 28, node.PageNum))

                                node.Safeties   
                                |> Seq.map(fun safe ->  safeName(safe))
                                |> Seq.map(fun safeFullName ->  dicQualifiedNameSegs.[safeFullName])
                                |> Seq.iter(fun safeConditionSeg ->
                                        let realTarget = dicVertex.[node.Key] :?> Real  //Target  call 은 안되나 ?  ahn
                                        realTarget.SafetyConditions.Add(safeConditionSeg :?> Real)|>ignore)  //safeCondition  call 은 안되나 ?
                                )

                        //copy system  동일 처리
                        dicCopy.ForEach(fun sysTwin ->
                            let copySys = sysTwin.Key
                            let origSys = sysTwin.Value
                            
                            origSys.Flows
                                .ForEach(fun flow->
                                    let copyFlow = copySys.FindFlow(flow.Name)
                                    let findReal(realName:string) = model.FindGraphVertex([|copySys.Name;copyFlow.Name;realName|]) :?> Real
                                    flow.Graph.Vertices.Where(fun w->w :? Real).Cast<Real>()
                                        .ForEach(fun real->
                                                let copyReal = findReal(real.Name)
                                                real.SafetyConditions.ForEach(fun safety ->
                                                    copyReal.SafetyConditions.Add(findReal(safety.Name)) |>ignore
                                                    )
                                            )
                                    )
                            )

             [<Extension>] static member  MakeApiTxRx (doc:pptDoc, model:Model) = 
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

                            //copy system  동일 처리
                            dicCopy.ForEach(fun sysTwin ->
                                let copySys = sysTwin.Key
                                let origSys = sysTwin.Value
                            
                                origSys.ApiItems
                                    .ForEach(fun api ->
                                        let findReal(realName:string) = model.FindGraphVertex([|copySys.Name;TextExFlow;realName|]) :?> Real
                                        copySys.ApiItems
                                            |>Seq.iter(fun api-> 
                                                //외부자신의 리얼을 찾아서 넣음
                                                let txs = api.TXs |> Seq.map(fun f-> findReal(f.Name)) 
                                                let rxs = api.RXs |> Seq.map(fun f-> findReal(f.Name)) 

                                                api.AddTXs(txs)  |> ignore
                                                api.AddRXs(rxs)  |> ignore 
                                                )
                                                )
                                        )

