// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Concurrent
open PPTObjectModule
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Dual.Common.Core.FS
open Engine.Import.Office
open Engine.Core
open System.Runtime.CompilerServices
open System
open System.Data

[<AutoOpen>]
module ImportU =




    let private getApiItems (sys: DsSystem, refSys: string, apiName: string) =
        let refSystem = sys.TryFindLoadedSystem(refSys).Value.ReferenceSystem
        refSystem.TryFindExportApiItem([| refSystem.Name; apiName |]).Value



    let private createCallVertex
        (
            mySys: DsSystem,
            node: pptNode,
            parentReal: Real Option,
            parentFlow: Flow Option,
            dicSeg: Dictionary<string, Vertex>,
            jobCallNames: string seq
        ) =
        let sysName, apiName = GetSysNApi(node.PageTitle, node.Name)

        let call =
            if jobCallNames.Contains sysName
            then 

            
                let jobName = sysName + "_" + apiName
            
                match mySys.Jobs.TryFind(fun job -> job.Name = jobName) with
                | Some job ->
                    if job.DeviceDefs.any () then
                        let call =
                            if (parentReal.IsSome) then
                                Call.Create(job, DuParentReal(parentReal.Value))
                            else
                                Call.Create(job, DuParentFlow(parentFlow.Value))

                        
                        call
                    else
                        node.Shape.ErrorName(ErrID._52, node.PageNum)
                | None ->
                        node.Shape.ErrorName(ErrID._48, node.PageNum)

            else
                let apiName = node.CallApiName
                let loadedName = node.CallName

                addLoadedLibSystemNCall (loadedName, apiName, mySys, parentFlow, parentReal, node)


        dicSeg.Add(node.Key, call)


    //let private createExSystemReal (mySys: DsSystem, node: pptNode, parentFlow: Flow) =
    //    let sysName, apiName = GetSysNApi(node.PageTitle, node.Name)

    //    if mySys.TryFindExternalSystem(sysName).IsNone then
    //        node.Shape.ErrorName(ErrID._50, node.PageNum)

    //    let realExS =
    //        match mySys.Jobs.TryFind(fun job -> job.Name = sysName + "_" + apiName) with
    //        | Some job ->
    //            if job.LinkDefs.any () then
    //                CallSys.Create(job, DuParentFlow(parentFlow))
    //            else
    //                node.Shape.ErrorName(ErrID._51, node.PageNum)
    //        | None -> node.Shape.ErrorName(ErrID._49, node.PageNum)

    //    realExS

    let private getParent
        (
            edge: pptEdge,
            parents: Dictionary<pptNode, seq<pptNode>>,
            dicSeg: Dictionary<string, Vertex>
        ) =
        ImportDocCheck.SameParent(parents, edge)

        let newParents =
            parents
            |> Seq.filter (fun group -> group.Value.Contains(edge.StartNode) && group.Value.Contains(edge.EndNode))
            |> Seq.map (fun group -> dicSeg.[group.Key.Key])

        if (newParents.Any() && newParents.length () > 1) then
            failwithlog "중복부모"

        if (newParents.Any()) then
            Some(newParents |> Seq.head)
        else
            None



    let private getOtherFlowReal (flows: Flow seq, nodeEx: pptNode) =
        let flowName, nodeName = nodeEx.Name.Split('.')[0], nodeEx.Name.Split('.')[1]

        match flows.TryFind(fun f -> f.Name = flowName) with
        | Some flow ->
            match flow.Graph.Vertices.TryFind(fun f -> f.Name = nodeName) with
            | Some real -> real
            | None -> nodeEx.Shape.ErrorName($"{ErrID._27} Error Name : [{nodeName}]", nodeEx.PageNum)
        | None -> nodeEx.Shape.ErrorName($"{ErrID._26} Error Name : [{flowName}]", nodeEx.PageNum)


    [<Extension>]
    type ImportUtil =
        //Job 만들기
        [<Extension>]
        static member MakeJobs(doc: pptDoc, mySys: DsSystem) =
            let dicJobName = Dictionary<string, Job>()

            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType.IsLoadSys)
            |> Seq.iter (fun node ->
                node.JobInfos
                |> Seq.iter (fun jobSet ->
                    let jobBase = jobSet.Key
                    let JobTargetSystem = jobSet.Value.First()
                    //ppt에서는 동일한 디바이스만 동시 Job구성 가능하여  아무시스템이나 찾아도 API는 같음
                    let refSystem = mySys.TryFindLoadedSystem(JobTargetSystem).Value.ReferenceSystem

                    refSystem.ApiItems.ForEach(fun api ->
                        let devs =
                            jobSet.Value
                                .Select(fun tgt -> getApiItems (mySys, tgt, api.Name), tgt)
                                .Select(fun (api, tgt) ->
                                    match node.NodeType with
                                    | OPEN_EXSYS_CALL
                                    | COPY_DEV -> TaskDev(api, "", "", tgt) 
                                    | _ -> failwithlog "Error MakeJobs")


                        let job = Job(jobBase + "_" + api.Name, devs |> Seq.toList, None)

                        if dicJobName.ContainsKey(job.Name) then
                            Office.ErrorName(node.Shape, ErrID._33, node.PageNum)
                        else
                            dicJobName.Add(job.Name, job)

                        mySys.Jobs.Add(job))))



        //Interface Reset 정보 만들기
        [<Extension>]
        static member MakeInterfaceResets(doc: pptDoc, sys: DsSystem) =
            let resets = HashSet<string * string>()

            doc.Edges
            |> Seq.filter (fun edge -> edge.IsInterfaceEdge)
            |> Seq.iter (fun edge ->
                let src = edge.StartNode
                let tgt = edge.EndNode
                //인터페이스는 인터페이스끼리 인과가능
                if (src.NodeType.IsIF && src.NodeType = tgt.NodeType |> not) then
                    Office.ErrorConnect(edge.ConnectionShape, ErrID._37, src.Name, tgt.Name, edge.PageNum)
                //인터페이스 인과는 약 리셋 불가
                if (edge.Causal = InterlockWeak || edge.Causal = ResetEdge) then
                    Office.ErrorConnect(edge.ConnectionShape, ErrID._11, src.Name, tgt.Name, edge.PageNum)
                //인터페이스 Link는 인과정보 정의 불가
                if (src.NodeType = IF_LINK || tgt.NodeType = IF_LINK) then
                    Office.ErrorConnect(edge.ConnectionShape, ErrID._32, src.Name, tgt.Name, edge.PageNum)

                if
                    (edge.Causal = Interlock) //인터락 AugmentedTransitiveClosure 타입 만들기 재료
                then
                    resets.Add(src.Name, tgt.Name) |> ignore
                else
                    sys.ApiResetInfos.Add(
                        ApiResetInfo.Create(sys, edge.StartNode.Name, edge.Causal, edge.EndNode.Name)
                    )
                    |> ignore)

            let dicIL = Dictionary<int, HashSet<string>>()

            let updateILInfo (src, tgt) =
                match dicIL.TryFind(fun dic -> dic.Value.Contains(src) || dic.Value.Contains(tgt)) with
                | Some dic ->
                    dic.Value.Add(src) |> ignore
                    dic.Value.Add(tgt) |> ignore
                | None -> dicIL.Add(dicIL.length (), [ src; tgt ] |> HashSet)

            let createInterlockInfos (src, tgt) =
                let mei = ApiResetInfo.Create(sys, src, Interlock, tgt)
                sys.ApiResetInfos.Add(mei) |> ignore

            resets.ForEach updateILInfo

            dicIL.ForEach(fun dic ->
                dic.Value
                |> Seq.pairwiseWindingFull //2개식 조합
                |> Seq.iter createInterlockInfos)

        //Interface 만들기
        [<Extension>]
        static member MakeInterfaces(doc: pptDoc, sys: DsSystem) =
            let checkName = HashSet<string>()

            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType.IsIF)
            |> Seq.iter (fun node ->

                if checkName.Add(node.IfName) |> not then
                    node.Shape.ErrorName(ErrID._25, node.PageNum)

                let apiName = node.IfName
                ApiItem.Create(apiName, sys) |> ignore)

            doc.MakeInterfaceResets sys

        //MFlow 리스트 만들기
        [<Extension>]
        static member MakeFlows(doc: pptDoc, sys: DsSystem) =
            let checkName = HashSet<string>()
            let dicFlow = doc.DicFlow

            doc.Pages
            |> Seq.filter (fun page -> page.PageNum <> pptHeadPage)
            |> Seq.filter (fun page -> page.IsUsing)
            |> Seq.iter (fun page ->
                let pageNum = page.PageNum

                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                let flowName = if page.PageNum = pptHeadPage then $"{sysName}_Page1" else flowName
                if flowName.Contains(".") then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._20, page.Title, page.PageNum, 0u, "")

                if checkName.Add(flowName) |> not then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._25, page.Title, page.PageNum, 0u, "")

                dicFlow.Add(pageNum, Flow.Create(flowName, sys)) |> ignore)


         //RuntimePackage.LightPC 를 위한 버튼 램프 만들기
        [<Extension>]
        static member MakeAutoGenBtnLamp(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            dicFlow.Iter(fun flow ->
                mySys.AddButton(BtnType.DuAutoBTN, "AutoSelect", "", "-", flow.Value, None)
                mySys.AddButton(BtnType.DuManualBTN, "ManualSelect", "", "-", flow.Value, None)
                mySys.AddButton(BtnType.DuDriveBTN, "DrivePushBtn", "", "-", flow.Value, None)
                mySys.AddButton(BtnType.DuPauseBTN, "PausePushBtn", "", "-", flow.Value, None)
                mySys.AddButton(BtnType.DuClearBTN, "ClearPushBtn", "", "-", flow.Value, None)
                mySys.AddButton(BtnType.DuEmergencyBTN, "EmergencyBtn", "", "-", flow.Value, None)
                )

            mySys.AddLamp(LampType.DuAutoModeLamp   , "AutoModeLamp", "", "", None, None)
            mySys.AddLamp(LampType.DuManualModeLamp , "ManualModeLamp", "", "", None, None)
            mySys.AddLamp(LampType.DuIdleModeLamp   , "IdleModeLamp", "", "", None, None)

            mySys.AddLamp(LampType.DuReadyStateLamp , "ReadyStateLamp", "", "", None, None)
            mySys.AddLamp(LampType.DuDriveStateLamp, "DriveLamp", "", "", None, None)
            mySys.AddLamp(LampType.DuOriginStateLamp, "OriginStateLamp", "", "", None, None)
            mySys.AddLamp(LampType.DuErrorStateLamp, "ErrorLamp", "", "", None, None)


        //MakeButtons 리스트 만들기
        [<Extension>]
        static member MakeButtons(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.filter (fun node -> node.ButtonDefs.any ())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.ButtonDefs.ForEach(fun b -> mySys.AddButton(b.Value, $"{flow.Name}_{b.Key}", "", "", flow, None)))

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.ButtonHeadPageDefs.any())
            |> Seq.iter (fun node ->
                        
                if dicFlow.length() = 0 then Office.ErrorShape(node.Shape, ErrID._60, node.PageNum)
                else 
                    dicFlow.Iter(fun flow ->
                        node.ButtonHeadPageDefs.ForEach(fun b -> mySys.AddButton(b.Value, b.Key, "", "", flow.Value, None)))
                )        
                
        //MakeLamps 리스트 만들기
        [<Extension>]
        static member MakeLamps(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            let headPageLamps=  doc.NodesHeadPage |> Seq.filter (fun node -> node.LampHeadPageDefs.any())
            let flowPageLamps=  doc.Nodes |> Seq.filter (fun node -> node.LampDefs.any())
            let allLampNodes = headPageLamps @ flowPageLamps

            let duplicateNames =
                allLampNodes
                |> Seq.groupBy (fun node -> node.Name)
                |> Seq.filter (fun (_, nodes) -> Seq.length nodes > 1)
                |> Seq.map fst

            if Seq.length duplicateNames > 0 then
                let duplicateName = duplicateNames.First()
                let duplicateNode = allLampNodes.First(fun f->f.Name = duplicateName)
                Office.ErrorName(duplicateNode.Shape, ErrID._64, duplicateNode.PageNum)

            flowPageLamps
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.LampDefs.Iter(fun l -> mySys.AddLamp(l.Value, $"{flow.Name}_{l.Key}", "", "", Some flow, None)))
            
            headPageLamps
            |> Seq.iter (fun node ->
                node.LampHeadPageDefs.Iter(fun l -> mySys.AddLamp(l.Value, l.Key, "", "", None, None)))
                
        //MakeReadyConditions 리스트 만들기
        [<Extension>]
        static member MakeConditions(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            doc.Nodes
            |> Seq.filter (fun node -> node.CondiDefs.any())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.CondiDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, $"{flow.Name}_{c.Key}", "", "", flow, None)))

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.CondiHeadPageDefs.any())
            |> Seq.iter (fun node ->
                        
                if dicFlow.length() = 0 then Office.ErrorShape(node.Shape, ErrID._67, node.PageNum)
                else 
                    dicFlow.Iter(fun flow ->
                        node.CondiHeadPageDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, c.Key, "", "", flow.Value, None)))
                )     
                
        [<Extension>]
        static member MakeAnimationPoint(doc: pptDoc, mySys: DsSystem) =

            let addChannelPoints (dev:Device) (node:pptNode) = 
                if dev.IsNull()
                    then node.Shape.ErrorName(ErrID._61, node.PageNum)
                    else
                        if node.Position.X >=0 && node.Position.Y >= 0
                        then 
                             let xywh = Xywh(node.Position.X, node.Position.Y
                                           , node.Position.Width, node.Position.Height) 
                             dev.ChannelPoints[TextEmtpyChannel] <-xywh

            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType = CALL)
            |> Seq.iter (fun node ->
                match getJobActionType node.CallApiName with
                | MultiAction cnt -> 
                    for i in [1..cnt] do 
                        let dev = mySys.Devices.FirstOrDefault(fun f->f.Name = (getDummyDeviceName node.CallName i))
                        addChannelPoints dev node
                | _ ->
                    let dev = mySys.Devices.FirstOrDefault(fun f->f.Name = node.CallName)
                    addChannelPoints dev node
                    )

        //real call alias  만들기
        [<Extension>]
        static member MakeSegment(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            let dicVertex = doc.DicVertex

            let pptNodes = doc.Nodes
            let parents = doc.Parents

            let dicChildParent =
                parents
                |> Seq.collect (fun parentChildren ->
                    parentChildren.Value |> Seq.map (fun child -> child, parentChildren.Key))
                |> dict

            let createReal () =
                pptNodes
                |> Seq.filter (fun node -> node.Alias.IsNone)
                |> Seq.filter (fun node -> node.NodeType.IsReal)
                |> Seq.filter (fun node -> dicChildParent.ContainsKey(node) |> not)
                |> Seq.sortBy (fun node -> node.NodeType = REALExF) //real 부터 생성 후 realExF 처리
                |> Seq.iter (fun node ->
                    match node.NodeType with
                    | REALExF ->
                        let real = getOtherFlowReal (dicFlow.Values, node) :?> Real
                        dicVertex.Add(node.Key, RealExF.Create(real, DuParentFlow dicFlow.[node.PageNum]))
                    //| REALExS ->
                    //    let realExS = createExSystemReal (mySys, node, dicFlow.[node.PageNum])
                    //    dicVertex.Add(node.Key, realExS)
                    | _ ->
                        let real = Real.Create(node.Name, dicFlow.[node.PageNum])
                        real.Finished <- node.RealFinished
                        dicVertex.Add(node.Key, real))

            let calls =
                pptNodes
                |> Seq.filter (fun node -> node.Alias.IsNone)
                |> Seq.filter (fun node -> node.NodeType.IsCall)

            let jobCallNames =
                    pptNodes.Where(fun node -> node.NodeType.IsLoadSys)
                    |> Seq.collect (fun node -> node.JobCallNames)
                    
            let createCall () =
                calls
                |> Seq.iter (fun node ->
                    let parentReal =
                        if dicChildParent.ContainsKey(node) then
                            Some(dicVertex.[dicChildParent.[node].Key] :?> Real)
                        else
                            None

                    let parentFlow =
                        if dicChildParent.ContainsKey(node) then
                            None
                        else
                            Some(dicFlow.[node.PageNum])

                    if parentReal.IsSome || parentFlow.IsSome then
                        createCallVertex (mySys, node, parentReal, parentFlow, dicVertex, jobCallNames))

            let createAlias () =
                pptNodes
                |> Seq.filter (fun node -> node.IsAlias)
                |> Seq.iter (fun node ->
                    let segOrg = dicVertex.[node.Alias.Value.Key]

                    let alias =
                        if dicChildParent.ContainsKey(node) then
                            let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                            let call = dicVertex.[node.Alias.Value.Key] :?> Call

                            Alias.Create(
                                $"{call.Name}_{node.AliasNumber}",
                                DuAliasTargetCall(segOrg :?> Call),
                                DuParentReal(real)
                            )
                        else
                            let flow = dicFlow.[node.PageNum]

                            match segOrg with
                            | :? RealExF as ex ->
                                Alias.Create(
                                    $"{ex.Name}_{node.AliasNumber}",
                                    DuAliasTargetRealExFlow(ex),
                                    DuParentFlow(flow)
                                )
                            | :? Real as rt ->
                                Alias.Create(
                                    $"{rt.Name}_{node.AliasNumber}",
                                    DuAliasTargetReal(rt),
                                    DuParentFlow(flow)
                                )
                            | :? Call as ct ->
                                Alias.Create(
                                    $"{ct.Name}_{node.AliasNumber}",
                                    DuAliasTargetCall(ct),
                                    DuParentFlow(flow)
                                )
                            | _ -> failwithf "Error type"

                    dicVertex.Add(node.Key, alias))

            //Real 부터
            createReal ()
            //Call 처리
            createCall ()
            //Alias Node 처리 마감
            createAlias ()

        //pptEdge 변환 및 등록
        [<Extension>]
        static member MakeEdges(doc: pptDoc, mySys: DsSystem) =
            let dicVertex = doc.DicVertex
            let dicFlow = doc.DicFlow
            let pptEdges = doc.Edges
            let parents = doc.Parents
            let dummys = doc.Dummys

            let convertEdge (edge: pptEdge, flow: Flow, srcs: Vertex seq, tgts: Vertex seq) =

                let mei = ModelingEdgeInfo<Vertex>(srcs, edge.Causal.ToText(), tgts)

                match getParent (edge, parents, dicVertex) with
                | Some(real) -> (real :?> Real).CreateEdge(mei)
                | None ->
                    if (tgts.OfType<Call>().any ()) then
                        edge.ConnectionShape.ErrorConnect(ErrID._44, srcs.First().Name, tgts.First().Name, edge.PageNum)

                    flow.CreateEdge(mei)

            let edges = pptEdges |> Seq.filter (fun edge -> not <| edge.IsInterfaceEdge)

            let dicEdges =
                edges
                |> Seq.map (fun edge -> edge.EndNode)
                |> distinct
                |> Seq.map (fun endNode -> endNode, edges.Where(fun e -> e.EndNode = endNode))
                |> dict




            //dummy edge 연결정보 업데이트
            edges
            |> Seq.iter (fun edge ->
                let srcDummy = dummys.TryFindDummy(edge.StartNode)
                let tgtDummy = dummys.TryFindDummy(edge.EndNode)

                if (srcDummy.IsSome) then
                    let tgt =
                        if tgtDummy.IsNone then
                            edge.EndNode.Key
                        else
                            tgtDummy.Value.DummyNodeKey

                    srcDummy.Value.AddOutEdge(edge.Causal, tgt)

                if (tgtDummy.IsSome) then
                    let src =
                        if srcDummy.IsNone then
                            edge.StartNode.Key
                        else
                            srcDummy.Value.DummyNodeKey

                    tgtDummy.Value.AddInEdge(edge.Causal, src))
        
            dicEdges
            |> Seq.iter (fun dic ->
                let tgtNode = dic.Key
                let tgtEdges = dic.Value


                tgtEdges
                    .GroupBy(fun g-> g.Causal)
                    .Select(fun group -> group.Key, group.Select(id))
                    .Iter(fun (causal, edges) ->
                        edges.Iter(fun edge ->
                            
                            let flow = dicFlow.[edge.PageNum]

                            let getVertexs (pptNodes: pptNode seq) =
                                pptNodes.Select(fun s -> dicVertex.[s.Key])

                            let srcs =
                                if (dummys.IsMember(edge.StartNode)) then
                                    dummys.GetMembers(edge.StartNode) |> getVertexs
                                else
                                    [dicVertex[edge.StartNode.Key]]

                            let tgts =
                                if (dummys.IsMember(edge.EndNode)) then
                                    dummys.GetMembers(edge.EndNode) |> getVertexs
                                else
                                    [ dicVertex.[edge.EndNode.Key] ]

                            try
                                convertEdge (edge, flow, srcs, tgts) |> ignore
                            with ex ->
                                if
                                    (ex.Source = "Dual.Common.Core.FS") //관리되는 예외 failwithf 사용 : 추후 예외 타입 작성
                                then
                                    raise ex
                                else
                                    edge.ConnectionShape.ErrorConnect(
                                        ex.Message,
                                        srcs.First().Name,
                                        tgts.First().Name,
                                        edge.PageNum
                                    )))

                                )


        //Safety 만들기
        [<Extension>]
        static member MakeSafeties(doc: pptDoc, mySys: DsSystem) =
            let dicVertex = doc.DicVertex
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.iter   (fun node ->
                let flow = dicFlow.[node.PageNum]

                let dicQualifiedNameSegs =
                    dicVertex.Values
                        .OfType<Call>()
                        .Select(fun call -> call.TargetJob.Name, call)
                    |> dict

                let safeName (safety: string) =
                    $"{flow.Name}_{safety.Split('.')[0]}_{safety.Split('.')[1]}"

                let safeties = node.Safeties |> map safeName |> toArray

                safeties //세이프티 입력 미등록 이름오류 체크
                |> iter (fun safeFullName ->
                    if not (mySys.Jobs.Select(fun f -> f.Name).Contains safeFullName) then
                        Office.ErrorName(node.Shape, ErrID._28, node.PageNum))

                safeties
                |> map (fun safeFullName -> dicQualifiedNameSegs.[safeFullName])
                |> iter (fun safeCondV ->
                    match dicVertex.[node.Key] |> box with
                    | :? ISafetyConditoinHolder as holder -> holder.SafetyConditions.Add(DuSafetyConditionCall(safeCondV)) |> ignore
                    | _ -> failwithlog "Error"))

        [<Extension>]
        static member MakeApiTxRx(doc: pptDoc) =
            let dicFlow = doc.DicFlow
            //1. 원본처리
            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType.IsIF)
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                let sys = dicFlow.[node.PageNum].System
                let api = sys.ApiItems.Where(fun w -> w.Name = node.IfName).First()

                let findReal (trxName: string) =
                    let flowName, realName =
                        if trxName.Contains(".") then
                            trxName.Split('.').[0], trxName.Split('.').[1]
                        else
                            flow.Name, trxName

                    if dicFlow.Values.Where(fun w -> w.Name = flowName).IsEmpty() then
                        Office.ErrorPPT(
                            Name,
                            ErrID._42,
                            $"원인이름{flowName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]",
                            node.PageNum,
                            node.Shape.ShapeID()
                        )

                    let vertex = sys.TryFindRealVertex(flowName, realName)

                    if vertex.IsNone then
                        Office.ErrorPPT(
                            Name,
                            ErrID._41,
                            $"원인이름{realName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]",
                            node.PageNum,
                            node.Shape.ShapeID()
                        )
                    //match vertex.Value with
                    //| :? Real as r -> r
                    //| _ -> failwithlog "Error "
                    vertex.Value

                let txs = node.IfTXs |> map findReal
                let rxs = node.IfRXs |> map findReal
                api.AddTXs(txs) |> ignore
                api.AddRXs(rxs) |> ignore)

        [<Extension>]
        static member UpdateActionIO(doc: pptDoc, sys: DsSystem) =
            let pageTables = doc.GetTables(System.Enum.GetValues(typedefof<IOColumn>).Length)
            if RuntimeDS.Package <> Simulation 
            && activeSys.IsSome && activeSys.Value = sys
            && pageTables.isEmpty()
            then  failwithf "IO Table이 없습니다. Add IO Table을 수행하세요"

            pageTables
            |> Seq.collect (fun (pageIndex, table) ->
                table.Rows
                    .Cast<DataRow>()
                    .ToArray()
                    .Where(fun r -> r.ItemArray[(int) IOColumn.Case] <> $"{IOColumn.Case}") // head row 제외
                |> Seq.map (fun row -> pageIndex, row))
            |> Seq.groupBy (fun (_, row) -> row.ItemArray.[(int) IOColumn.Name].ToString())
            |> Seq.iter (fun (name, rows) ->
                let rowsWithIndexes = rows |> Seq.toArray

                if rowsWithIndexes.Length > 1 && name <> "" then
                    // Handle the exception for duplicate names here
                    failwithf "Duplicate name: %s" name)

            ApplyIO(sys, pageTables)

        [<Extension>]
        static member UpdateLayouts(doc: pptDoc, sys: DsSystem) =
            let layouts = doc.GetLayouts()
            layouts.Iter(fun (layout, path, dev, rect)->
                let device = sys.Devices.FirstOrDefault(fun f-> f.Name = dev)
                if(device.IsNonNull()) 
                then let xywh = Xywh(rect.X, rect.Y, rect.Width, rect.Height)
                     let lay = layout.Replace(";", "_")
                     device.ChannelPoints.Add($"{lay};{path.Trim()}", xywh) |> ignore
                else Office.ErrorPPT(ErrorCase.Name, ErrID._61, $"layout {dev}", 0, 0u)
            )        


        [<Extension>]
        static member GetLoadNodes(doc: pptDoc) =
            let calls = doc.Nodes.Where(fun n -> n.NodeType.IsCall && n.Alias.IsNone)

            let loads =
                doc.Nodes.Where(fun n -> n.NodeType.IsLoadSys)
                |> Seq.collect (fun s -> s.CopySys.Keys)

            calls
                .Where(fun call -> not (loads.Contains(call.CallName)))
                .Select(fun call ->
                    { DevName = call.CallName
                      ApiName = call.CallApiName })


        [<Extension>]
        static member GetlibApisNSys(systems: DsSystem seq) =
            let libApisNSys = Dictionary<string, DsSystem>() //다중 lib 로딩시 시스템간 중복 에러 체크 및 libApisNSys 시스템 할당

            systems.Iter(fun sys ->
                sys.ApiItems.Iter(fun api ->
                    try
                        libApisNSys.Add(api.Name, sys)
                    with ex ->
                        let errSystems =
                            systems.Where(fun w -> w.ApiItems.Select(fun s -> s.Name = api.Name).any ())

                        let errText = (String.Join(", ", errSystems.Select(fun s -> s.Name)))
                        failwithf $"{api.Name} exists on the same system. [{errText}]"))

            libApisNSys



        [<Extension>]
        static member BuildSystem(doc: pptDoc, sys: DsSystem) =
            
            doc.MakeJobs(sys)
            doc.MakeFlows(sys) |> ignore


            //자동생성
            if activeSys.IsSome && activeSys.Value = sys
            then 
                doc.MakeAutoGenBtnLamp(sys)
            //수동생성
            //doc.MakeButtons(sys)
            //doc.MakeLamps(sys)

            doc.MakeConditions(sys)
            //segment 리스트 만들기
            doc.MakeSegment(sys)
            //Edge  만들기
            doc.MakeEdges(sys)
            //Safety 만들기
            doc.MakeSafeties(sys)
            //ApiTxRx  만들기
            doc.MakeApiTxRx()
            //AnimationPoint  만들기
            doc.MakeAnimationPoint(sys)
            doc.IsBuilded <- true
