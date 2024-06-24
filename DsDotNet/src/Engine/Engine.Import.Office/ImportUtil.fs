// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Concurrent
open PPTConnectionModule
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Dual.Common.Core.FS
open Engine.Import.Office
open Engine.Core
open System.Runtime.CompilerServices
open System
open System.IO
open System.Data
open LibraryLoaderModule
open System.Reflection

[<AutoOpen>]
module ImportU =
    
    [<Extension>]
    type ImportUtil =

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
                if
                    (edge.Causal = Interlock) //인터락 AugmentedTransitiveClosure 타입 만들기 재료
                then
                    resets.Add(src.Name, tgt.Name) |> ignore
                else
                    sys.ApiResetInfos.Add(
                        ApiResetInfo.Create(sys, edge.StartNode.Name, edge.Causal, edge.EndNode.Name, false)
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
                let mei = ApiResetInfo.Create(sys, src, Interlock, tgt, false)
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
            let IFNodes= 
                doc.Nodes
                |> Seq.filter (fun node -> node.NodeType.IsIF)

            IFNodes
            |> Seq.iter (fun node ->

                if checkName.Add(node.IfName) |> not then
                    let dupNode = IFNodes.First(fun n->n.IfName = node.IfName && n <> node) 
                    node.Shape.ErrorName($"{ErrID._25} 위치 \n page:{dupNode.PageNum},  name:{dupNode.PageTitle}", node.PageNum)

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
                if flowName.Contains(".")||flowName.Contains("__") then
                    Office.ErrorPPT(ErrorCase.Name, ErrID._20, page.Title, page.PageNum, 0u, "")

                dicFlow.Add(pageNum, Flow.Create(flowName, sys)) |> ignore)



        //MakeButtons 리스트 만들기
        [<Extension>]
        static member MakeButtons(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.filter (fun node -> node.ButtonDefs.any ())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.ButtonDefs.ForEach(fun b -> mySys.AddButton(b.Value, $"{flow.Name}_{b.Key}", "", "", flow)))

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.ButtonHeadPageDefs.any())
            |> Seq.iter (fun node ->
                        
                if dicFlow.length() = 0 then Office.ErrorShape(node.Shape, ErrID._60, node.PageNum)
                else 
                    dicFlow.Iter(fun flow ->
                        node.ButtonHeadPageDefs.ForEach(fun b -> mySys.AddButton(b.Value, b.Key, "", "", flow.Value)))
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
                node.LampDefs.Iter(fun l -> mySys.AddLamp(l.Value, $"{flow.Name}_{l.Key}", "", "", Some flow)))
            
            headPageLamps
            |> Seq.iter (fun node ->
                node.LampHeadPageDefs.Iter(fun l -> mySys.AddLamp(l.Value, l.Key, "", "", None)))
                
        //MakeReadyConditions 리스트 만들기
        [<Extension>]
        static member MakeConditions(doc: pptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            doc.Nodes
            |> Seq.filter (fun node -> node.CondiDefs.any())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.CondiDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, $"{flow.Name}_{c.Key}", "", "", flow)))

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.CondiHeadPageDefs.any())
            |> Seq.iter (fun node ->
                        
                if dicFlow.length() = 0 then Office.ErrorShape(node.Shape, ErrID._67, node.PageNum)
                else 
                    dicFlow.Iter(fun flow ->
                        node.CondiHeadPageDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, c.Key, "", "", flow.Value)))
                )     
                
        [<Extension>]
        static member MakeAnimationPoint(doc: pptDoc, mySys: DsSystem) =

            let addChannelPoints (loaded:LoadedSystem) (node:pptNode) = 
                if loaded.IsNull()
                    then node.Shape.ErrorName(ErrID._61, node.PageNum)
                    else
                        if node.Position.X >=0 && node.Position.Y >= 0
                        then 
                             let xywh = Xywh(node.Position.X, node.Position.Y
                                           , node.Position.Width, node.Position.Height) 
                             loaded.ChannelPoints[TextEmtpyChannel] <-xywh

            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType = CALL)
            |> Seq.filter (fun node -> not(node.IsFunction))
            |> Seq.iter (fun node ->
                match node.JobParam.JobMulti with
                | JobTypeMulti.MultiAction(_,cnt,_,_) -> 
                    for i in [1..cnt] do 
                        let multiName = getMultiDeviceName node.CallDevName  i
                        let dev = mySys.LoadedSystems.FirstOrDefault(fun f->f.Name = multiName)
                        addChannelPoints dev node
                | _ ->
                    let dev = mySys.LoadedSystems.FirstOrDefault(fun f->f.Name = node.CallDevName)
                    addChannelPoints dev node
                    )

            doc.Nodes
            |> Seq.filter (fun node -> node.IsFunction)
            |> Seq.filter (fun node -> mySys.LoadedSystems.any(fun f->f.Name = node.CallName))
            |> Seq.iter (fun node ->
                    let dev = mySys.LoadedSystems.FirstOrDefault(fun f->f.Name = node.CallName)
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
                let reals = pptNodes
                            |> Seq.filter (fun node -> node.Alias.IsNone)
                            |> Seq.filter (fun node -> node.NodeType.IsReal)
                            |> Seq.filter (fun node -> dicChildParent.ContainsKey(node) |> not)
                            |> Seq.sortBy (fun node -> node.NodeType = REALExF) //real 부터 생성 후 realExF 처리

                reals |> Seq.iter (fun node ->
                    match node.NodeType with
                    | REALExF -> // isOtherFlowRealAlias is false  (외부 플로우에 있을뿐 Or Alias가 아님)
                        let real = getOtherFlowReal (dicFlow.Values, node) :?> Real
                        dicVertex.Add(node.Key, Alias.Create(real.ParentNPureNames.Combine(), DuAliasTargetReal real, DuParentFlow dicFlow.[node.PageNum], false))
                    | _ ->
                        let real = Real.Create(node.Name, dicFlow.[node.PageNum])
                        real.Finished <- node.RealFinished
                        real.NoTransData <- node.RealNoTrans
                        dicVertex.Add(node.Key, real))

            let calls =
                pptNodes
                |> Seq.filter (fun node -> node.Alias.IsNone)
                |> Seq.filter (fun node -> node.NodeType.IsCall)


            let createCall () =
                calls
                |> Seq.iter (fun node ->
                        try

                            if dicChildParent.ContainsKey(node) then
                                createCallVertex (mySys, node, (dicVertex.[dicChildParent.[node].Key] :?> Real)|>DuParentReal, dicVertex)
                            else
                                createCallVertex (mySys, node, (dicFlow.[node.PageNum])|>DuParentFlow, dicVertex)

                        with ex ->
                            node.Shape.ErrorName(ex.Message, node.PageNum)
                            )

            let createAlias () =
                pptNodes
                |> Seq.filter (fun node -> node.IsAlias)
                |> Seq.iter (fun node ->

                    if node.IsFunction then
                        node.Shape.ErrorName($"Alias Function은 지원하지 않습니다.", node.PageNum)

                    let segOrg = dicVertex.[node.Alias.Value.Key]
                    
                    let alias =
                        let flow = dicFlow.[node.PageNum]
                        if node.NodeType = REALExF then // isOtherFlowRealAlias is true
                            let real = getOtherFlowReal (dicFlow.Values, node) :?> Real
                            Alias.Create(
                                String.Join("_", real.ParentNPureNames),
                                DuAliasTargetReal(real),
                                DuParentFlow(flow), true
                            )

                        elif dicChildParent.ContainsKey(node) then
                            let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                            let call = dicVertex.[node.Alias.Value.Key] :?> Call

                            Alias.Create(
                                $"{call.Name}_{node.AliasNumber}",
                                DuAliasTargetCall(segOrg :?> Call),
                                DuParentReal(real), false
                            )
                        else

                            match segOrg with
                            | :? Real as rt ->
                                Alias.Create(
                                    $"{rt.Name}_{node.AliasNumber}",
                                    DuAliasTargetReal(rt),
                                    DuParentFlow(flow) , false
                                )
                            | :? Call as ct ->
                                Alias.Create(
                                    $"{ct.Name}_{node.AliasNumber}",
                                    DuAliasTargetCall(ct),
                                    DuParentFlow(flow) , false
                                )
                            | _ -> failwithf "Error type"

                    dicVertex.Add(node.Key, alias))


            //Real 부터
            createReal ()
            //Call 처리
            createCall ()
            //Alias Node 처리 마감
            createAlias ()  
            
            //createFunction Node 처리 마감
            //createAliasFunction ()

            mySys.ReferenceSystems
                 .Iter(genClearRealAddForSingleReal)

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
                        .Where(fun call -> call.IsJob)
                        .Select(fun call -> call.TargetJob.Name, call)
                    |> dict

                let safeName (safety: string) =
                    if safety.Split('.').Length  = 2
                    then
                        let dev = (safety.Split('.')[0]).Trim()
                        let api = (safety.Split('.')[1]).Trim()

                        $"{flow.Name}__{dev}_{api}"

                    elif safety.Split('.').Length  = 3
                    then
                        let flow = (safety.Split('.')[0]).Trim()
                        let dev = (safety.Split('.')[1]).Trim()
                        let api = (safety.Split('.')[2]).Trim()

                        $"{flow}__{dev}_{api}"
                    else 
                        failWithLog $"error safety name format ({safety})"


                let safeties = node.Safeties |> map safeName |> toArray

                safeties //세이프티 입력 미등록 이름오류 체크
                |> iter (fun safeFullName ->
                    if not (mySys.Jobs.Select(fun f -> f.Name).Contains safeFullName) then
                        node.Shape.ErrorName($"{ErrID._28}(err:{safeFullName})", node.PageNum)
                        
                        
                        )

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

                let tx = node.IfTX |>  findReal
                let rx = node.IfRX |>  findReal
                api.TX <- tx 
                api.RX <- rx 
                )
        [<Extension>]
        static member UpdateActionIO(doc: pptDoc, sys: DsSystem, autoIO:bool) =
            let pageTables = doc.GetTables(System.Enum.GetValues(typedefof<IOColumn>).Length)
            if not(autoIO)
            && activeSys.IsSome && activeSys.Value = sys
            && pageTables.isEmpty()
            then  failwithf "IO Table이 없습니다. Add I/O Table을 수행하세요"
            
            pageTables
            |> Seq.filter (fun (_, table) -> table.Rows.Count > 0)
            |> Seq.filter (fun (_, table) -> table.Rows[0].ItemArray[(int) IOColumn.Case] = $"{IOColumn.Case}")
            |> Seq.collect (fun (pageIndex, table) ->
                table.Rows
                    .Cast<DataRow>()
                    .ToArray()
                    .Where(fun r -> r.ItemArray[(int) IOColumn.Case] <> $"{IOColumn.Case}") // head row 제외
                |> Seq.map (fun row -> pageIndex, row))
            |> Seq.groupBy (fun (_, row) -> getDevName row)
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
                    let flow, job, api = call.CallFlowNJobNApi
                    { DevName = call.CallName
                      ApiName = api })


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
        static member CreateGenBtnLamp(mySys: DsSystem) =
            let flows = mySys.Flows
            flows.Iter(fun flow ->
                        mySys.AddButton(BtnType.DuAutoBTN, "AutoSelect", "", "-", flow)
                        mySys.AddButton(BtnType.DuManualBTN, "ManualSelect", "", "-", flow)
                        mySys.AddButton(BtnType.DuDriveBTN, "DrivePushBtn", "", "-", flow)
                        mySys.AddButton(BtnType.DuPauseBTN, "PausePushBtn", "", "-", flow)
                        mySys.AddButton(BtnType.DuClearBTN, "ClearPushBtn", "", "-", flow)
                        mySys.AddButton(BtnType.DuEmergencyBTN, "EmergencyBtn", "", "-", flow)
                )

            mySys.AddLamp(LampType.DuAutoModeLamp   , "AutoModeLamp", "-", "", None)
            mySys.AddLamp(LampType.DuManualModeLamp , "ManualModeLamp", "-", "", None)
            mySys.AddLamp(LampType.DuIdleModeLamp   , "IdleModeLamp", "-", "", None)

            mySys.AddLamp(LampType.DuErrorStateLamp, "ErrorLamp", "-", "", None)
            mySys.AddLamp(LampType.DuOriginStateLamp, "OriginStateLamp", "-", "", None)
            mySys.AddLamp(LampType.DuReadyStateLamp , "ReadyStateLamp", "-", "", None)
            mySys.AddLamp(LampType.DuDriveStateLamp, "DriveLamp", "-", "", None)

        [<Extension>]
        static member ValidatePPTSystem(doc: pptDoc, sys: DsSystem) =

            (* Root Call 연결 없음 체크 *)
            let rootEdgeSrcs = sys.GetFlowEdges().Select(fun e->e.Source).Distinct()
       
            doc.Nodes.Where(fun n -> n.NodeType.IsCall && n.IsRootNode.Value)
                     .Iter(fun n -> 
                            let call = doc.DicVertex[n.Key]
                            if not(rootEdgeSrcs.Contains (call))
                            then
                                n.Shape.ErrorShape(ErrID._71, n.PageNum)
                )
            (* Multi Call Api별 갯수 동일 체크*)
            let calls = doc.Nodes
                                .Where(fun n -> n.NodeType.IsCall && not(n.IsFunction))
                                .GroupBy(fun n -> n.CallName)
            calls.Iter(fun call -> 
                let callEachCounts = call.Select(fun f->f.JobParam.DeviceCount)
                if callEachCounts.Distinct().Count() > 1
                then
                    let errNode = call.Select(fun f->f).First() 
                    errNode.Shape.ErrorShape(ErrID._72, errNode.PageNum)
            )

                                
         
        [<Extension>]
        static member BuildSystem(doc: pptDoc, sys: DsSystem, isLib:bool, isCreateBtnLLib:bool) =
            
            doc.MakeFlows(sys) |> ignore

            //자동생성
            if activeSys.IsSome && activeSys.Value = sys && not(isLib) && isCreateBtnLLib
            then                 
                sys.CreateGenBtnLamp()
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

            doc.ValidatePPTSystem(sys)
            doc.IsBuilded <- true
