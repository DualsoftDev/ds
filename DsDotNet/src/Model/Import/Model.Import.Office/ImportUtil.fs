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

    let private createCallVertex(mySys:DsSystem, node:pptNode, parentReal:Real Option, parentFlow:Flow Option, dicSeg:Dictionary<string, Vertex>) =
        let sysName, apiName = GetSysNApi(node.PageTitle, node.Name)

        let call =
            match mySys.Jobs.TryFind(fun job -> job.Name = sysName+"_"+apiName) with
            |Some job ->
                if(parentReal.IsSome)
                then  Call.Create(job, DuParentReal (parentReal.Value))
                else  Call.Create(job, DuParentFlow (parentFlow.Value))
            |None -> node.Shape.ErrorName(ErrID._32, node.PageNum)

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


    let private getApiItems(sys:DsSystem, refSys:string, apiName:string) =
                let refSystem = sys.TryFindReferenceSystem(refSys).Value
                refSystem.TryFindExportApiItem([|refSystem.Name;apiName|]).Value

    let private getOtherFlowReal(flows:Flow seq, nodeEx:pptNode) =
                let flowName, nodeName = nodeEx.Name.Split('.')[0], nodeEx.Name.Split('.')[1]
                match flows.TryFind(fun f -> f.Name = flowName) with
                |Some flow ->
                    match flow.Graph.Vertices.TryFind(fun f->f.Name = nodeName) with
                    |Some real -> real
                    |None ->  nodeEx.Shape.ErrorName(ErrID._27, nodeEx.PageNum)
                |None ->
                    nodeEx.Shape.ErrorName(ErrID._26, nodeEx.PageNum)

    [<Extension>]
    type ImportUtil =


        //Job 만들기
        [<Extension>]
        static member MakeJobs  (doc:pptDoc, mySys:DsSystem) =
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType = COPY)
            |> Seq.collect(fun node -> node.JobInfos)
            |> Seq.iter(fun jobSet ->
                let jobBase = jobSet.Key
                let JobTargetSystems = jobSet.Value
                let refSystem = mySys.TryFindReferenceSystem(JobTargetSystems.First()).Value

                refSystem.ApiItems.ForEach(fun api->
                    let jobDefs =
                        JobTargetSystems
                            .Select(fun tgt -> getApiItems(mySys, tgt, api.Name))
                            .Select(fun api -> JobDef(api, "","", api.System.Name))

                    let job = Job(jobBase+"_"+api.Name, jobDefs)
                    mySys.Jobs.Add(job)
                    )
                )

        //Interface 만들기
        [<Extension>]
        static member MakeInterfaces (doc :pptDoc, sys:DsSystem) =
            let checkName = HashSet<string>()
            doc.Nodes
            |> Seq.filter(fun node -> node.NodeType = IF)
            |> Seq.iter(fun node ->

                    if checkName.Add(node.IfName) |> not
                    then node.Shape.ErrorName(ErrID._25, node.PageNum)

                    let apiName = node.IfName
                    ApiItem.Create(apiName, sys) |> ignore
            )

        //MFlow 리스트 만들기
        [<Extension>]
        static member MakeFlows (doc:pptDoc, sys:DsSystem) =
            let checkName = HashSet<string>()
            let dicFlow = doc.DicFlow
            doc.Pages
            |> Seq.filter(fun page -> page.IsUsing)
            |> Seq.iter  (fun page ->
                let pageNum  = page.PageNum
                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                if flowName.Contains(".")
                then Office.ErrorPPT(ErrorCase.Name, ErrID._19, page.Title, page.PageNum, "")
                if checkName.Add(flowName) |> not
                then Office.ErrorPPT(ErrorCase.Name, ErrID._25, page.Title, page.PageNum, "")

                dicFlow.Add(pageNum,  Flow.Create(flowName, sys) ) |> ignore
                )

        //EMG & Start & Auto 리스트 만들기
        [<Extension>]
        static member MakeButtons (doc:pptDoc, mySys:DsSystem) =
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.filter(fun node -> node.BtnType.IsSome)
            |> Seq.iter(fun node ->
                    let flow = dicFlow.[node.PageNum]

                    //Start, Reset, Auto, Emg 버튼
                    if(node.BtnType.Value = BtnType.DuStartBTN)
                    then mySys.AddButton(BtnType.DuStartBTN, node.Name, flow)
                    if(node.BtnType.Value = BtnType.DuResetBTN)
                    then mySys.AddButton(BtnType.DuResetBTN,node.Name, flow)
                    if(node.BtnType.Value = BtnType.DuAutoBTN)
                    then mySys.AddButton(BtnType.DuAutoBTN,node.Name, flow)
                    if(node.BtnType.Value = BtnType.DuEmergencyBTN)
                    then mySys.AddButton(BtnType.DuEmergencyBTN ,node.Name, flow)
                    )


        //real call alias  만들기
        [<Extension>]
        static member MakeSegment (doc:pptDoc, mySys:DsSystem) =
                let dicFlow = doc.DicFlow
                let dicVertex = doc.DicVertex

                let pptNodes = doc.Nodes
                let parents = doc.Parents
                let dicChildParent =
                    parents
                    |> Seq.collect(fun parentChildren ->
                        parentChildren.Value
                        |> Seq.map(fun child -> child, parentChildren.Key)) |> dict

                let createReal() =
                    pptNodes
                    |> Seq.filter(fun node -> node.Alias.IsNone)
                    |> Seq.filter(fun node -> node.NodeType.IsReal)
                    |> Seq.filter(fun node -> dicChildParent.ContainsKey(node)|>not)
                    |> Seq.sortBy(fun node -> node.NodeType = REALEx)  //real 부터 생성 후 realEx 처리
                    |> Seq.iter(fun node   ->
                            if node.NodeType = REALEx
                            then
                                let real = getOtherFlowReal(dicFlow.Values, node)
                                dicVertex.Add(node.Key, real)
                            else
                                let real = Real.Create(node.Name, dicFlow.[node.PageNum])
                                dicVertex.Add(node.Key, real)
                        )

                let createCall() =
                    pptNodes
                    |> Seq.filter(fun node -> node.Alias.IsNone)
                    |> Seq.filter(fun node -> node.NodeType.IsCall)
                    |> Seq.iter(fun node ->
                                let parentReal = if dicChildParent.ContainsKey(node)
                                                    then Some(dicVertex.[dicChildParent.[node].Key] :?> Real)
                                                    else None
                                let parentFlow = if dicChildParent.ContainsKey(node)
                                                    then None
                                                    else Some(dicFlow.[node.PageNum])

                                if parentReal.IsSome || parentFlow.IsSome
                                then
                                    createCallVertex(mySys, node, parentReal, parentFlow, dicVertex)
                            )

                let createAlias() =
                    pptNodes
                    |> Seq.filter(fun node -> node.Alias.IsSome)
                    |> Seq.iter(fun node ->
                            let segOrg = dicVertex.[node.Alias.Value.Key]
                            if dicChildParent.ContainsKey(node)
                            then
                                let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                                Alias.Create(node.Name, DuAliasTargetCall(segOrg:?>Call), DuParentReal(real)) |> ignore
                            else
                                let alias =
                                    let flow = dicFlow.[node.PageNum]
                                    match segOrg with
                                    | :? Real as rt -> Alias.Create(node.Name, DuAliasTargetReal(rt), DuParentFlow(flow))
                                    | :? Call as ct -> Alias.Create(node.Name, DuAliasTargetCall(ct), DuParentFlow(flow))
                                    |_ -> failwithf "Error type"

                                dicVertex.Add(node.Key, alias )
                        )

                //Real 부터
                createReal()
                //Call 처리
                createCall()
                //Alias Node 처리 마감
                createAlias()

        //pptEdge 변환 및 등록
        [<Extension>]
        static member MakeEdges (doc:pptDoc, mySys:DsSystem) =
                let dicVertex = doc.DicVertex
                let dicFlow = doc.DicFlow
                let pptEdges = doc.Edges
                let parents = doc.Parents
                let dummys = doc.Dummys

                let convertEdge(edge:pptEdge, flow:Flow, srcs:Vertex seq, tgts:Vertex seq) =

                    let mei = ModelingEdgeInfo<Vertex>(srcs, edge.Causal.ToText(), tgts)

                    match getParent(edge, parents, dicVertex) with
                    |Some(real) -> (real:?>Real).CreateEdge(mei)
                    |None ->       flow.CreateEdge(mei)

                pptEdges
                    |> Seq.iter(fun edge ->
                        let flow = dicFlow.[edge.PageNum]


                        if(edge.StartNode.NodeType = IF && edge.StartNode.NodeType = edge.EndNode.NodeType|>not)
                        then Office.ErrorConnect(edge.ConnectionShape, ErrID._37, edge.StartNode.Name, edge.EndNode.Name, edge.PageNum)

                        if(edge.StartNode.NodeType = IF || edge.EndNode.NodeType = IF)
                        then
                            //인터페이스 인과는 약 리셋 불가 //ahn
                            if (edge.Causal = InterlockWeak || edge.Causal = ResetEdge)
                            then Office.ErrorConnect(edge.ConnectionShape, ErrID._11, edge.StartNode.Name, edge.EndNode.Name, edge.PageNum)

                            mySys.ApiResetInfos.Add(ApiResetInfo.Create(mySys, edge.StartNode.Name, edge.Causal ,edge.EndNode.Name ))|>ignore

                        else
                            let srcDummy = dummys.TryFindDummy(edge.StartNode)
                            let tgtDummy = dummys.TryFindDummy(edge.EndNode)

                            if(srcDummy.IsNonNull())
                                then
                                    let tgt = if tgtDummy.IsNull() then edge.EndNode.Key else tgtDummy.DummyNodeKey
                                    srcDummy.AddOutEdge(edge.Causal, tgt)
                                else
                                    if(tgtDummy.IsNonNull())
                                    then
                                        let src = if srcDummy.IsNull() then edge.StartNode.Key else srcDummy.DummyNodeKey
                                        tgtDummy.AddInEdge(edge.Causal, src)


                            let getVertexs(pptNodes:pptNode seq) =
                                pptNodes.Select(fun s-> dicVertex.[s.Key])

                            let srcs = if(dummys.IsMember(edge.StartNode))
                                        then dummys.GetMembers(edge.StartNode) |> getVertexs
                                        else [dicVertex.[edge.StartNode.Key]]

                            let tgts = if(dummys.IsMember(edge.EndNode))
                                        then dummys.GetMembers(edge.EndNode)   |> getVertexs
                                        else [dicVertex.[edge.EndNode.Key]]

                            convertEdge(edge, flow, srcs, tgts) |> ignore

                            )


        //Safety 만들기
        [<Extension>]
        static member MakeSafeties (doc:pptDoc, mySys:DsSystem) =
            let dicVertex = doc.DicVertex
            let dicFlow = doc.DicFlow
            doc.Nodes
            |> Seq.iter(fun node ->
                    let flow = dicFlow.[node.PageNum]
                    let dicQualifiedNameSegs  = dicVertex.Values.Select(fun seg -> seg.QualifiedName, seg) |> dict
                    let getJobName(flowName:string, safety:string) =
                        $"{flowName}_{TrimSpace (safety.Split('$').[0])}_{TrimSpace(safety.Split('$').[1])}"

                    let safeName(safety:string) =
                        if safety.Contains("$") //call
                        then //call은 ppt 상에서는 같은 부모끼리만 가능
                            match dicVertex.[node.Key].Parent with
                            | DuParentFlow f -> sprintf "%s.%s.%s"    mySys.Name f.Name (getJobName(f.Name, safety))
                            | DuParentReal r -> sprintf "%s.%s.%s.%s" mySys.Name flow.Name r.Name (getJobName(flow.Name, safety))

                        elif safety.Contains(".") //RealEx
                        then sprintf "%s.%s" mySys.Name safety
                        else sprintf "%s.%s.%s" mySys.Name flow.Name safety //Real

                    node.Safeties   //세이프티 입력 미등록 이름오류 체크
                    |> Seq.map(fun safe ->  safeName(safe))
                    |> Seq.iter(fun safeFullName ->
                            if(dicQualifiedNameSegs.ContainsKey safeFullName|>not)
                            then Office.ErrorName(node.Shape, ErrID._28, node.PageNum))

                    node.Safeties
                    |> Seq.map(fun safe ->  safeName(safe))
                    |> Seq.map(fun safeFullName ->  dicQualifiedNameSegs.[safeFullName])
                    |> Seq.iter(fun safeCondV ->
                            match  dicVertex.[node.Key] |> box with
                            | :? ISafetyConditoinHolder as holder ->
                                    match safeCondV with
                                    | :? Real as r -> holder.SafetyConditions.Add( DuSafetyConditionReal (r)) |>ignore
                                    | :? RealEx as ex -> holder.SafetyConditions.Add(DuSafetyConditionRealEx (ex))  |>ignore
                                    | :? Call as c -> holder.SafetyConditions.Add(DuSafetyConditionCall (c)) |>ignore
                                    | _ -> failwith "Error"
                            | _ -> failwith "Error"
                        )
                    )

        [<Extension>]
        static member MakeApiTxRx (doc:pptDoc) =
                let dicFlow = doc.DicFlow
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

                                if dicFlow.Values.Where(fun w->w.Name = flowName).IsEmpty()
                                then Office.ErrorPPT(Name, ErrID._42, $"원인이름{flowName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]", node.PageNum)
                                let vertex = sys.TryFindGraphVertex([|sys.Name;flowName;realName|])
                                if vertex.IsNone
                                then Office.ErrorPPT(Name, ErrID._41, $"원인이름{realName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]", node.PageNum)
                                match vertex.Value with
                                | :? Real as r -> r
                                | _ -> failwith "Error "

                            let txs = node.IfTXs |> Seq.map(fun f-> findReal(f))
                            let rxs = node.IfRXs |> Seq.map(fun f-> findReal(f))
                            api.AddTXs(txs)|>ignore
                            api.AddRXs(rxs)|>ignore
                            )
