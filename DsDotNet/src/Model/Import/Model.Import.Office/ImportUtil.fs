// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System.Collections.Concurrent
open PPTObjectModule
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

        if(parentReal.IsNone && parentFlow.IsNone)  then () //alias 지정후 다시 생성
        else
            if(node.NodeType.IsReal)
            then
                let real = Real.Create(node.Name, parentFlow.Value)
                dicSeg.Add(node.Key, real)
            else
                let sysName, ApiName = GetSysNApi(node.PageTitle, node.Name)
                let findApi = model.FindApiItem([|sysName;ApiName|])

                if findApi.IsNull()
                then Office.ErrorPPT(Name, ErrID._42, $"원인이름{ApiName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]", node.PageNum)

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
        [<Extension>] static member GetAcive(model:Model) =  model.Systems.Filter(fun s->s.Active).First()
        [<Extension>] static member MakeSystem (doc:pptDoc, model:Model) =
                        doc.Pages
                            |> Seq.filter(fun page -> page.IsUsing)
                            |> Seq.iter  (fun page ->
                                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                                if sysName = doc.Name|>not
                                then if model.TryFindSystem(sysName).IsNull()
                                     then dicSys.Add(page.PageNum, DsSystem.Create(sysName, "", model))
                                     else dicSys.Add(page.PageNum, model.FindSystem(sysName))
                                else dicSys.Add(page.PageNum, model.FindSystem(sysName))
                                )
        [<Extension>] static member MakeCopySystem (doc:pptDoc, model:Model) =
                        doc.Nodes
                        |> Seq.filter(fun node -> node.NodeType = COPY)
                        |> Seq.iter(fun node ->
                                node.CopySys.ForEach(fun copy ->
                                    let origSys  = model.TryFindSystem(copy.Value)

                                    if origSys.IsNull()
                                    then Office.ErrorPPT(Name, ErrID._43, $"원인이름{copy.Value}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]", node.PageNum)

                                    let copySys  = DsSystem.Create(copy.Key, "", model)
                                    dicCopy.Add(copySys, origSys) |>ignore
                                    )
                                )
        //Interface 만들기
        [<Extension>] static member MakeInterfaces (doc :pptDoc) =
                        doc.Nodes
                        |> Seq.filter(fun node -> node.NodeType = IF)
                        |> Seq.iter(fun node ->
                                let system = dicSys.[node.PageNum]
                                let apiName = node.IfName;
                                ApiItem.Create(apiName, system) |> ignore
                        )

        //MFlow 리스트 만들기
        [<Extension>] static member MakeFlows (doc:pptDoc, model:Model) =
                            doc.Pages
                            |> Seq.filter(fun page -> page.IsUsing)
                            |> Seq.iter  (fun page ->
                                let pageNum  = page.PageNum
                                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                                let sys    = model.FindSystem(sysName)
                                dicFlow.Add(pageNum,  Flow.Create(flowName, sys) ) |> ignore
                                )

                            //copy system ApiItems 동일 처리
                            dicCopy.ForEach(fun sysTwin->
                                let copySys = sysTwin.Key
                                let origSys = sysTwin.Value
                                origSys.ApiItems.ForEach(fun apiItem -> apiItem.ToCopy(copySys)|>ignore)
                                )

        //EMG & Start & Auto 리스트 만들기
        [<Extension>] static member MakeButtons (doc:pptDoc, model:Model) =
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

                        let createCall() =
                            pptNodes
                            |> Seq.filter(fun node -> node.Alias.IsNone)
                            |> Seq.filter(fun node -> node.NodeType.IsCall)
                            |> Seq.iter(fun node ->
                                        let parentReal = if  (dicChildParent.ContainsKey(node))
                                                            then
                                                                 Some(dicVertex.[dicChildParent.[node].Key] :?> Real)
                                                            else None
                                        let parentFlow = if  dicChildParent.ContainsKey(node)
                                                            then None
                                                            else Some(dicFlow.[node.PageNum])
                                        createVertex(model, node, parentReal, parentFlow, dicVertex))

                        let createReal() =
                            pptNodes
                                |> Seq.filter(fun node -> node.Alias.IsNone)
                                |> Seq.filter(fun node -> node.NodeType.IsReal)
                                |> Seq.filter(fun node -> dicChildParent.ContainsKey(node)|>not)
                                |> Seq.iter(fun node   -> createVertex(model, node, None, Some(dicFlow.[node.PageNum]), dicVertex))

                        let createAlias() =
                            pptNodes
                            |> Seq.filter(fun node -> node.Alias.IsSome)
                            |> Seq.iter(fun node ->
                                    let segOrg = dicVertex.[node.Alias.Value.Key]
                                    if dicChildParent.ContainsKey(node)
                                    then
                                        let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                                        let alias = Alias.Create(node.Name, CallTarget(segOrg:?>Call), Real(real), false)
                                        dicVertex.Add(node.Key, alias)
                                    else
                                        let alias =
                                            let flow = dicFlow.[node.PageNum]
                                            match segOrg with
                                            | :? Real as rt -> Alias.Create(node.Name, RealTarget(rt), Flow(flow), false)
                                            | :? Call as ct -> Alias.Create(node.Name, CallTarget(ct), Flow(flow), false)
                                            |_ -> failwithf "Error type"

                                        dicVertex.Add(node.Key, alias )
                                )


                        //Real 부터
                        createReal()
                        //Call 처리
                        createCall()
                        //Alias Node 처리 마감
                        createAlias()

                        //copy system Flows 동일 처리
                        dicCopy.ForEach(fun sysTwin->
                            let copySys = sysTwin.Key
                            let origSys = sysTwin.Value
                            origSys.Flows.ForEach(fun flow-> flow.ToCopy(copySys)|>ignore)
                            )

        //pptEdge 변환 및 등록
        [<Extension>] static member MakeEdges (doc:pptDoc, model:Model) =
                            let pptEdges = doc.Edges
                            let parents = doc.Parents

                            let convertEdge(edge:pptEdge, flow:Flow, src:Vertex, tgt:Vertex) =
                                let graph =
                                        match getParent(edge, parents, dicVertex) with
                                        |Some(real) -> (real:?>Real).Graph
                                        |None ->       flow.Graph

                                match edge.Causal with
                                | Interlock ->
                                    flow.AddModelEdge(src, Interlock, tgt)
                                    //let edge1 = Edge.Create(graph, src, tgt, toR ResetPush)
                                    //let edge2 = Edge.Create(graph, tgt, src, toR ResetPush)
                                    //edge1.EditorInfo <- ModelEdgeType.EditorInterlock
                                    //edge2.EditorInfo <- ModelEdgeType.EditorInterlock
                                | StartReset ->
                                    flow.AddModelEdge(src, StartReset, tgt)
                                    //let edge1 = Edge.Create(graph, src, tgt, toR StartEdge)
                                    //let edge2 = Edge.Create(graph, tgt, src, toR ResetEdge)
                                    //edge1.EditorInfo <- ModelEdgeType.EditorStartReset
                                    //edge2.EditorInfo <- ModelEdgeType.EditorStartReset
                                | _ ->
                                    flow.AddModelEdge(src, edge.Causal, tgt)



                            let dicDummys = getDummys(parents, dicVertex)

                            pptEdges
                                |> Seq.iter(fun edge ->
                                    let flow = dicFlow.[edge.PageNum]


                                    if(edge.StartNode.NodeType = IF && edge.StartNode.NodeType = edge.EndNode.NodeType|>not)
                                    then Office.ErrorConnect(edge.ConnectionShape,ErrID._37, edge.StartNode.Name, edge.EndNode.Name, edge.PageNum)

                                    if(edge.StartNode.NodeType = IF || edge.EndNode.NodeType = IF)
                                    then
                                        //인터페이스 인과는 약 리셋 불가 //ahn
                                        if (edge.Causal = InterlockWeak
                                            ||edge.Causal = ResetEdge)
                                        then Office.ErrorConnect(edge.ConnectionShape, ErrID._11, edge.StartNode.Name, edge.EndNode.Name, edge.PageNum)

                                        let sys = dicSys.[edge.PageNum]
                                        sys.ApiResetInfos.Add(ApiResetInfo.Create(sys, edge.StartNode.Name, edge.Causal ,edge.EndNode.Name ))|>ignore

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


        //Safety 만들기
        [<Extension>] static member MakeSafeties (doc:pptDoc, model:Model) =
                        doc.Nodes
                        |> Seq.filter(fun node -> node.IsDummy|>not)
                        |> Seq.iter(fun node ->
                                let flow = dicFlow.[node.PageNum]
                                let dicQualifiedNameSegs  = dicVertex.Values.Select(fun seg -> seg.QualifiedName, seg) |> dict
                                let safeName(safe) = sprintf "%s.%s.%s" flow.System.Name flow.Name safe

                                node.Safeties   //세이프티 입력 미등록 이름오류 체크
                                |> Seq.map(fun safe ->  safeName(safe))
                                |> Seq.iter(fun safeFullName -> if(dicQualifiedNameSegs.ContainsKey safeFullName|>not)
                                                                then Office.ErrorName(node.Shape, ErrID._28, node.PageNum))

                                node.Safeties
                                |> Seq.map(fun safe ->  safeName(safe))
                                |> Seq.map(fun safeFullName ->  dicQualifiedNameSegs.[safeFullName])
                                |> Seq.iter(fun safeConditionSeg ->
                                        let realTarget = dicVertex.[node.Key] :?> Real  //Target  call 은 안되나 ?  ahn
                                        realTarget.SafetyConditions.Add(safeConditionSeg :?> Real)|>ignore)  //safeCondition  call 은 안되나 ?
                                )

        [<Extension>] static member MakeApiTxRx (doc:pptDoc, model:Model) =
                            //1. 원본처리
                            doc.Nodes
                                |> Seq.filter(fun node -> node.NodeType = IF)
                                |> Seq.iter(fun node ->
                                        let flow = dicFlow.[node.PageNum]
                                        let sys =  dicFlow.[node.PageNum].System
                                        let api = sys.ApiItems.Where(fun w->w.Name = node.IfName).First()

                                        let findReal(trxName:string) =

                                                let flowName, realName =
                                                    if trxName.Contains(".")
                                                    then trxName.Split('.').[0], trxName.Split('.').[1]
                                                    else flow.Name, trxName
                                                let vertex = model.FindGraphVertex([|sys.Name;flowName;realName|]) 
                                                if vertex.IsNull()
                                                then Office.ErrorPPT(Name, ErrID._41, $"원인이름{realName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]", node.PageNum)

                                                vertex :?> Real
                                        let txs = node.IfTXs |> Seq.map(fun f-> findReal(f))
                                        let rxs = node.IfRXs |> Seq.map(fun f-> findReal(f))
                                        api.AddTXs(txs)|>ignore
                                        api.AddRXs(rxs)|>ignore
                                        )

                            //copy system flow 동일 처리
                            dicCopy.ForEach(fun sysTwin->
                                let copySys = sysTwin.Key
                                let origSys = sysTwin.Value
                                origSys.Flows.ForEach(fun flow->
                                            let copyFlow = copySys.FindFlow(flow.Name)
                                            flow.ToCopy(copyFlow)|>ignore
                                            dicFlow.Add((-dicFlow.Count),  copyFlow ) |> ignore ) //복사본 page는 음수 임의 표기
                                origSys.ApiItems.ForEach(fun apiItem ->
                                            let apiCopy = copySys.ApiItems.First(fun f->f.Name = apiItem.Name)
                                            apiItem.ToCopy(apiCopy)|>ignore)
                                origSys.ApiResetInfos.ForEach(fun apiResetInfo ->
                                            apiResetInfo.ToCopy(copySys)|>ignore)
                                )

