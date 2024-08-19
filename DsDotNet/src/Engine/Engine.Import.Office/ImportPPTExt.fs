// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections
open PptConnectionModule
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
open Engine.Parser.FS
open Engine.Parser.FS.ModelParser

[<AutoOpen>]
module ImportU =

    [<Extension>]
    type ImportPptExt =

        //Interface Reset 정보 만들기
        [<Extension>]
        static member MakeInterfaceResets(doc: PptDoc, sys: DsSystem) =
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
        static member MakeInterfaces(doc: PptDoc, sys: DsSystem) =
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
        static member MakeFlows(doc: PptDoc, sys: DsSystem) =
            let checkName = HashSet<string>()
            let dicFlow = doc.DicFlow

            doc.Pages
            |> Seq.filter (fun page -> page.PageNum <> pptHeadPage)
            |> Seq.filter (fun page -> page.IsUsing)
            |> Seq.iter (fun page ->
                let pageNum = page.PageNum

                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                let flowName = if page.PageNum = pptHeadPage then $"{sysName}_Page1" else flowName

                dicFlow.Add(pageNum, Flow.Create(flowName, sys)) |> ignore)



        //MakeButtons 리스트 만들기
        [<Extension>]
        static member MakeButtons(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.filter (fun node -> node.ButtonDefs.any ())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]
                node.ButtonDefs.ForEach(fun b -> mySys.AddButton(b.Value, $"{flow.Name}.{b.Key}", "", "", flow)))

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
        static member MakeLamps(doc: PptDoc, mySys: DsSystem) =
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
                node.LampDefs.Iter(fun l -> mySys.AddLamp(l.Value, $"{flow.Name}.{l.Key}", "", "", Some flow)))

            headPageLamps
            |> Seq.iter (fun node ->
                node.LampHeadPageDefs.Iter(fun l -> mySys.AddLamp(l.Value, l.Key, "", "", None)))

        //MakeReadyConditions 리스트 만들기
        [<Extension>]
        static member MakeConditions(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            doc.Nodes
            |> Seq.filter (fun node -> node.CondiDefs.any())
            |> Seq.iter (fun node ->
                try

                    let flow = dicFlow.[node.PageNum]
                    node.CondiDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, $"{flow.Name}_{c.Key}", "", "", flow))
                with _ ->
                    Office.ErrorName(node.Shape, ErrID._67, node.PageNum)
                    )

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.CondiHeadPageDefs.any())
            |> Seq.iter (fun node ->

                if dicFlow.length() = 0 then Office.ErrorShape(node.Shape, ErrID._67, node.PageNum)
                else
                    dicFlow.Iter(fun flow ->
                        node.CondiHeadPageDefs.ForEach(fun c -> mySys.AddCondtion(c.Value, c.Key, "", "", flow.Value)))
                )

        [<Extension>]
        static member MakeAnimationPoint(doc: PptDoc, mySys: DsSystem) =

            let addChannelPoints (loaded:LoadedSystem) (node:PptNode) =
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
                let cnt = node.JobParam.TaskDevCount
                for i in [1..cnt] do
                    let multiName =
                        if cnt = 1
                        then
                            node.DevName
                        else
                            getMultiDeviceName node.DevName  i

                    let dev = mySys.LoadedSystems.FirstOrDefault(fun f->f.Name = multiName)
                    addChannelPoints dev node
                    )


        //real call alias  만들기
        [<Extension>]
        static member MakeSegment(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            let dicVertex = doc.DicVertex
            let dicAutoPreJob = doc.DicAutoPreJob

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
                        dicVertex.Add(node.Key, Alias.Create(real.ParentNPureNames.Combine("_"), DuAliasTargetReal real, DuParentFlow dicFlow.[node.PageNum], false))
                        node.UpdateRealProperty(real)
                    | _ ->
                        let real = Real.Create(node.Name, dicFlow.[node.PageNum])
                        dicVertex.Add(node.Key, real)
                        node.UpdateRealProperty(real)
                        )

            let callNAutoPres =
                pptNodes
                |> Seq.filter (fun node -> node.Alias.IsNone)
                |> Seq.filter (fun node -> node.NodeType.IsCall || node.NodeType = AUTOPRE)
                |> Seq.toArray



            let createCallNAutoPre () =
                let libInfos, _ = getLibraryInfos()
                callNAutoPres
                    .Filter(fun node -> not(mySys.LoadedSystems.Select(fun d->d.Name).Contains(node.DevName)))
                    .GroupBy(fun node -> node.DevName)
                    .Iter(fun kv ->

                        let libApis = kv.Where(fun d-> libInfos.ContainsKey(d.ApiPureName))
                                        .Select(fun d-> d.ApiPureName).Distinct()
                        let usedApis = kv.Select(fun d->d.ApiPureName).Distinct()

                        if libApis.any() && libApis.Count() <> usedApis.Count()
                        then
                            let errApis = usedApis.Except(libApis).JoinWith(", ")
                            let libFilePath  =libInfos[libApis.First()]
                            failWithLog $"{kv.Key} ({libFilePath}) 디바이스에\r\n{errApis} 인터페이스가 없습니다."
                     )

                callNAutoPres
                |> Seq.sortBy(fun node -> (node.PageNum, node.Position.Left, node.Position.Top))
                |> Seq.iter (fun node ->
                    try


                        if node.NodeType = AUTOPRE
                        then
                            if not(dicChildParent.ContainsKey node)
                            then
                                failWithLog $"{node.Name} 이름을 찾을 수 없습니다."

                            createAutoPre(mySys, node, (dicVertex[dicChildParent[node].Key] :?> Real)|>DuParentReal, dicAutoPreJob) |> ignore
                        else
                            if dicChildParent.ContainsKey(node) then
                                createCallVertex (mySys, node, (dicVertex[dicChildParent[node].Key] :?> Real)|>DuParentReal, dicVertex)
                            else
                                createCallVertex (mySys, node, (dicFlow[node.PageNum])|>DuParentFlow, dicVertex)
                    with ex ->
                        node.Shape.ErrorName(ex.Message, node.PageNum)
                        )

            let createAlias () =
                pptNodes
                |> Seq.filter (fun node -> node.IsAlias)
                |> Seq.iter (fun node ->

                    //if node.IsFunction then
                    //    node.Shape.ErrorName($"Alias Function은 지원하지 않습니다.", node.PageNum)
                    let segOrg =    dicVertex.[node.Alias.Value.Key]

                    let alias =
                        let flow = dicFlow.[node.PageNum]
                        if node.NodeType = REALExF then // isOtherFlowRealAlias is true
                            let real = getOtherFlowReal (dicFlow.Values, node) :?> Real
                            node.UpdateRealProperty(real)
                            let name = real.ParentNPureNames.Combine("_")
                            Alias.Create(
                                $"{name}_{node.AliasNumber}" ,
                                DuAliasTargetReal(real),
                                DuParentFlow(flow), true
                            )

                        elif dicChildParent.ContainsKey(node) then
                            let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                            let call = dicVertex.[node.Alias.Value.Key] :?> Call
                            ////node.UpdateRealProperty(real)
                            let name = call.DeviceNApi.Combine("_")
                            Alias.Create(
                                $"{name}_{node.AliasNumber}" ,
                                DuAliasTargetCall(segOrg :?> Call), 
                                DuParentReal(real), false
                            )
                        else

                            match segOrg with
                            | :? Real as rt ->
                                node.UpdateRealProperty(rt)
                                let otherFlow  =  flow <> rt.Flow
                                let name = if otherFlow then $"{rt.Flow.Name}{rt.Name}"else rt.Name
                                Alias.Create(
                                    $"{name}_{node.AliasNumber}" ,
                                    DuAliasTargetReal(rt),
                                    DuParentFlow(flow) , otherFlow
                                )
                            | :? Call as ct ->
                                let otherFlow  = flow.Name <> ct.TargetJob.NameComponents.Head()
                                let name = if otherFlow then ct.TargetJob.NameComponents.Combine() else ct.Name
                                Alias.Create(
                                    $"{name}_{node.AliasNumber}" ,
                                    DuAliasTargetCall(ct),
                                    DuParentFlow(flow) , otherFlow
                                )
                            | _ -> failwithf "Error type"

                    dicVertex.Add(node.Key, alias))


            //Real 부터
            createReal ()
            //Call 처리
            createCallNAutoPre ()
            //Alias Node 처리 마감
            createAlias ()

            updateSystemForSingleApi mySys


        //pptEdge 변환 및 등록
        [<Extension>]
        static member MakeEdges(doc: PptDoc, mySys: DsSystem) =
            let dicVertex = doc.DicVertex
            let dicFlow = doc.DicFlow
            let pptEdges = doc.Edges
            let parents = doc.Parents
            let dummys = doc.Dummys

            let convertEdge (edge: PptEdge, flow: Flow, srcs: Vertex seq, tgts: Vertex seq) =

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

            let getVertexs (pptNodes: PptNode seq) =
                pptNodes.Select(fun s -> dicVertex.[s.Key])

            let updateAutoPre(edge:PptEdge) =
                if edge.EndNode.NodeType = AUTOPRE
                then
                    edge.EndNode.Shape.ErrorName(ErrID._8, edge.EndNode.PageNum)

                let tgts =
                    if (dummys.IsMember(edge.EndNode)) then
                        dummys.GetMembers(edge.EndNode)|>toArray
                    else
                        [|edge.EndNode|]

                tgts.Iter(fun node->
                        let autoPreCondition =edge.StartNode.Job
                        node.AutoPres.Add(autoPreCondition)|>ignore
                        )

            dicEdges
            |> Seq.iter (fun dic ->
                let tgtNode = dic.Key
                let tgtEdges = dic.Value


                tgtEdges
                    .GroupBy(fun g-> g.Causal)
                    .Select(fun group -> group.Key, group.Select(id))
                    .Iter(fun (causal, edges) ->
                        edges.Iter(fun edge ->

                            if edge.StartNode.NodeType = AUTOPRE
                            then
                                updateAutoPre(edge)
                            else
                                let flow = dicFlow.[edge.PageNum]


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

        //Safety & AutoPre만들기
        [<Extension>]
        static member MakeSafetyAutoPre(doc: PptDoc, mySys: DsSystem) =
            let getJobInfo(job:Job) =
                seq{
                    let JobName = job.DequotedQualifiedName
                    let pureJobName = GetBracketsRemoveName JobName
                    yield JobName, job

                    if JobName <> pureJobName
                    then
                        yield pureJobName, job
                }

            let dicJobFromCall =
                doc.DicVertex.Values
                   .OfType<Call>().Where(fun call -> call.IsJob)
                   .SelectMany(fun call -> getJobInfo call.TargetJob)


            let dicJobFromAutoPre =
                doc.DicAutoPreJob.Values
                   .SelectMany(fun job -> getJobInfo job)



            let dicJob = (dicJobFromCall@dicJobFromAutoPre) |> dict

            doc.Nodes
            |> Seq.filter (fun node -> node.NodeType.IsCall || node.NodeType = AUTOPRE)
            |> Seq.iter(fun node ->

                node.Safeties
                |> iter (fun safeName ->
                    if not (dicJob.ContainsKey safeName) then
                        node.Shape.ErrorName($"{ErrID._80}(err:{safeName})", node.PageNum)

                    if node.Job.Combine() = safeName then
                        node.Shape.ErrorName($"{ErrID._81}(err:{safeName})", node.PageNum)
                        )

                node.AutoPres.Select(fun (j) -> j.Combine())
                |> iter (fun autoPres ->
                    if not (dicJob.ContainsKey autoPres) then
                        node.Shape.ErrorName($"{ErrID._82}(err:{autoPres})", node.PageNum)

                    if node.Job.Combine() = autoPres then
                        node.Shape.ErrorName($"{ErrID._83}(err:{autoPres})", node.PageNum)
                        )

                node.Safeties
                |> map (fun fullName -> dicJob.[fullName])
                |> iter (fun condJob  ->
                    match doc.DicVertex.[node.Key].GetPure() |> box with
                    | :? ISafetyAutoPreRequisiteHolder as holder ->
                            holder.SafetyConditions.Add(DuSafetyAutoPreConditionCall(condJob )) |> ignore
                    | _ ->
                            node.Shape.ErrorName($"{ErrID._28}(err:{doc.DicVertex.[node.Key].QualifiedName})", node.PageNum))

                node.AutoPres
                |> iter (fun (jobFqdn)  ->
                    let condJob =
                        if dicJob.ContainsKey (jobFqdn.Combine())
                        then
                            dicJob[jobFqdn.Combine()]
                        else
                            failWithLog $"AutoPres 대상이 없습니다. {jobFqdn}"

                    match doc.DicVertex.[node.Key].GetPure() |> box with
                    | :? ISafetyAutoPreRequisiteHolder as holder ->
                            holder.AutoPreConditions.Add(DuSafetyAutoPreConditionCall(condJob)) |> ignore
                    | _ ->
                            node.Shape.ErrorName($"{ErrID._28}(err:{doc.DicVertex.[node.Key].QualifiedName})", node.PageNum))
            )



        [<Extension>]
        static member MakeApiTxRx(doc: PptDoc) =
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

                    if dicFlow.Values.Where(fun w -> w.Name = flowName).IsEmpty then
                        Office.ErrorPpt(
                            Name,
                            ErrID._42,
                            $"원인이름{flowName}: 전체이름[{node.Shape.InnerText}] 해당도형[{node.Shape.ShapeName()}]",
                            node.PageNum,
                            node.Shape.ShapeID()
                        )

                    let vertex = sys.TryFindRealVertex(flowName, realName)

                    if vertex.IsNone then
                        Office.ErrorPpt(
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
        static member UpdateActionIO(doc: PptDoc, sys: DsSystem, autoIO:bool) =
            let pageTables = doc.GetTables(System.Enum.GetValues(typedefof<IOColumn>).Length)
            if not(autoIO)
                && activeSys.IsSome && activeSys.Value = sys
                && pageTables.isEmpty()
            then
                failwithf "IO Table이 없습니다. Add I/O Table을 수행하세요"

            // io table 에서 첫 row 가 "CASE" 인 table 들의 모든 row 들을 device name 별로 중복 검사?
            pageTables
            |> Seq.map (snd)        // only tables
            |> Seq.filter (fun table -> table.Rows.Count > 0)
            |> Seq.filter (fun table -> table.Rows[0].ItemArray[(int) IOColumn.Case] = $"{IOColumn.Case}")
            |> Seq.collect (fun table ->
                table.Rows
                    .Cast<DataRow>()
                    .Where(fun r -> r.ItemArray[(int) IOColumn.Case] <> $"{IOColumn.Case}") // head row 제외
                    .Select(fun row -> row))    // only rows
            |> Seq.groupBy (getDevName)
            |> Seq.iter (fun (name, rows) ->
                let rowsWithIndexes = rows |> Seq.toArray
                if rowsWithIndexes.Length > 1 && name <> "" then
                    // Handle the exception for duplicate names here
                    failwithf "Duplicate name: %s" name)

            ApplyIO(sys, pageTables)

        [<Extension>]
        static member UpdateLayouts(doc: PptDoc, sys: DsSystem) =
            let layouts = doc.GetLayouts()
            layouts.Iter(fun (layout, path, dev, rect)->
                match sys.Devices.TryFind(fun f-> f.Name = dev) with
                | Some device ->
                    let xywh = Xywh(rect.X, rect.Y, rect.Width, rect.Height)
                    let lay = layout.Replace(";", "_")
                    device.ChannelPoints.Add($"{lay};{path.Trim()}", xywh) |> ignore
                | None ->
                    Office.ErrorPpt(ErrorCase.Name, ErrID._61, $"layout {dev}", 0, 0u)
            )


        [<Extension>]
        static member GetLoadNodes(doc: PptDoc) =
            let calls = doc.Nodes.Where(fun n -> n.NodeType.IsCall && n.Alias.IsNone)

            let loads =
                doc.Nodes.Where(fun n -> n.NodeType.IsLoadSys)
                |> Seq.collect (fun s -> s.CopySys.Keys)

            calls
                //.Where(fun call -> not (loads.Contains(call.DevName)))
                .Select(fun call ->
                    { DevName = call.DevName
                      ApiName = call.ApiName })


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
            for flow in flows do
                mySys.AddButton(BtnType.DuAutoBTN,      "AutoSelect", "", "-", flow)
                mySys.AddButton(BtnType.DuManualBTN,    "ManualSelect", "", "-", flow)
                mySys.AddButton(BtnType.DuDriveBTN,     "DrivePushBtn", "", "-", flow)
                mySys.AddButton(BtnType.DuPauseBTN,     "PausePushBtn", "", "-", flow)
                mySys.AddButton(BtnType.DuClearBTN,     "ClearPushBtn", "", "-", flow)
                mySys.AddButton(BtnType.DuEmergencyBTN, "EmergencyBtn", "", "-", flow)

            mySys.AddLamp(LampType.DuAutoModeLamp   , "AutoModeLamp", "-", "", None)
            mySys.AddLamp(LampType.DuManualModeLamp , "ManualModeLamp", "-", "", None)
            mySys.AddLamp(LampType.DuIdleModeLamp   , "IdleModeLamp", "-", "", None)

            mySys.AddLamp(LampType.DuErrorStateLamp,  "ErrorLamp", "-", "", None)
            mySys.AddLamp(LampType.DuOriginStateLamp, "OriginStateLamp", "-", "", None)
            mySys.AddLamp(LampType.DuReadyStateLamp,  "ReadyStateLamp", "-", "", None)
            mySys.AddLamp(LampType.DuDriveStateLamp,  "DriveLamp", "-", "", None)

        [<Extension>]
        static member PreCheckPptSystem(doc: PptDoc, sys: DsSystem) =

            (* AUTOPRE Root에 배치 에러 체크*)
            doc.Nodes
                .Where(fun n -> n.NodeType = AUTOPRE)
                .Iter(fun n ->
                    if n.IsRootNode.IsSome && n.IsRootNode.Value then
                        n.Shape.ErrorShape(ErrID._28, n.PageNum) )


            let errCheck (calls:PptNode seq) =
                if calls.Count() > 1
                then
                    let errNode = calls.Select(fun f->f).First()
                    let errText = calls.Select(fun f-> $"{f.Name} page {f.FlowName}").JoinWith("\r\n")
                    errNode.Shape.ErrorShape(ErrID._72+ $"\r\n{errText}", errNode.PageNum)

            let callSet =
                doc.Nodes
                    .Where(fun n -> (n.NodeType.IsCall || n.NodeType = AUTOPRE) && not(n.IsFunction))
                    .ToArray()

            (* Multi Call Dev별 갯수 동일 체크*)
            callSet.GroupBy(fun n -> n.DevName)
                   .Select(fun calls ->
                            calls.DistinctBy(fun c-> (c.JobParam.TaskDevCount))
                    ).Iter(errCheck)

            (* Multi Call Api별 갯수 동일 체크*)
            callSet.GroupBy(fun n -> n.DevName+n.ApiPureName)
                   .Select(fun calls ->
                            calls.DistinctBy(fun c-> (c.JobParam.TaskDevCount
                                           , c.JobParam.TaskInCount
                                           , c.JobParam.TaskOutCount))
                    ).Iter(errCheck)


        [<Extension>]
        static member PostCheckPptSystem(doc: PptDoc, sys: DsSystem, isLib:bool) =

            (* Root Call 연결 없음 체크 *)
            let rootEdgeSrcs = sys.GetFlowEdges().Select(fun e->e.Source).Distinct()

            doc.Nodes.Where(fun n -> n.NodeType.IsCall && n.IsRootNode.Value)
                     .Iter(fun n ->
                            let call = doc.DicVertex[n.Key]
                            if not(rootEdgeSrcs.Contains (call))
                            then
                                n.Shape.ErrorShape(ErrID._71, n.PageNum)
                )

            (* 복수의 작업에서 SEQ 전송 Start Edge 체크*)
            if not(isLib) then
                doc.Nodes.Where(fun n -> n.NodeType.IsReal)
                    .Iter(fun n ->
                        let xs = doc.Edges.Where(fun e -> e.IsStartEdge && e.EndNode = n)
                                          .Select(fun e-> doc.DicVertex.[e.StartNode.Key])

                        let pureReals = xs.GetPureReals()
                        if pureReals.Where(fun f -> not(f.NoTransData)).Count() > 1 then
                            let error = String.Join("\r\n", (pureReals.Select(fun f->f.DequotedQualifiedName)))
                            failwithlog $"복수의 작업에서 SEQ 전송을 시도하고 있습니다. \r\n미전송 작업 이름에 취소선을 사용하세요 \r\n복수 작업 :\r\n {error}"
                    )



        [<Extension>]
        static member MakeRealProperty(doc: PptDoc, mySys: DsSystem) =
            let processProperty (mySys: DsSystem) (prop: DsProperty) =
                if prop.FQDN.Length <> 2 then
                    failwithf "Error: Name format Flow.Work: %s" (String.concat "." prop.FQDN)

                match mySys.TryFindRealVertex(prop.FQDN.[0], prop.FQDN.[1]) with
                | Some real ->
                    match prop.Type with
                    | "Motion" -> real.Motion <- Some prop.Value
                    | "Script" -> real.Script <- Some prop.Value
                    | _ -> failwithf "Error: %s Type not found" prop.Type
                | None ->
                    failwithf "Error: Real not found: %s" (String.concat "." prop.FQDN)

            let processPage (doc: PptDoc) (mySys: DsSystem) (systemRepo: ShareableSystemRepository) (page: PptPage) =
                match Office.GetSlideNoteText(doc.Doc, page.PageNum) with
                | note when
                       note.StartsWith ("[motions]")
                    || note.StartsWith ("[scripts]") ->

                    let dsText = $"[sys] temp = {{ [prop] = {{ {note} }}}}"
                    let dsProperties = WalkProperty(dsText, ParserOptions.Create4Simulation(systemRepo, "", "ActiveCpuName", None, DuNone))
                    dsProperties |> Seq.iter (processProperty mySys)
                | _ -> ()

            let systemRepo = ShareableSystemRepository()
            doc.Pages
                |> Seq.filter (fun page -> page.PageNum <> 1)
                |> Seq.iter (processPage doc mySys systemRepo)

        [<Extension>]
        static member MakeAddressBySlideNote(doc: PptDoc, mySys: DsSystem) =
            let processPage (doc: PptDoc) (mySys: DsSystem) (systemRepo: ShareableSystemRepository) (page: PptPage) =
                match Office.GetSlideNoteText(doc.Doc, page.PageNum) with
                | note when not(note.StartsWith("[sys]"))  ->
                    let dsText = $"[sys] temp = {{ [jobs] ={{ {note} }} }}"
                    let devApiDefinitions = WalkJobAddress(dsText, ParserOptions.Create4Simulation(systemRepo, "", "ActiveCpuName", None, DuNone))
                    devApiDefinitions
                    |> Seq.iter(fun a->
                        let apiFqdn = a.ApiFqnd.Combine()
                        match mySys.TaskDevs.TryFind(fun td->td.DeviceApiPureName(apiFqdn) = apiFqdn) with
                        | Some td ->
                            if not td.IsInAddressEmpty then
                                failwithf $"Error: {apiFqdn} InAddress already exists"

                            if not td.IsOutAddressEmpty then
                                failwithf $"Error: {apiFqdn} OutAddress already exists"

                            td.InAddress <- a.InAddress
                            td.OutAddress <- a.OutAddress

                        | None -> failwithf $"Error: {apiFqdn} not found"
                    )

                | _ -> ()

            let systemRepo = ShareableSystemRepository()
            doc.Pages
                |> Seq.filter (fun page -> page.PageNum = 1)
                |> Seq.iter (processPage doc mySys systemRepo)


        [<Extension>]
        static member BuildSystem(doc: PptDoc, sys: DsSystem, isLib:bool, isCreateBtnLLib:bool) =
            let isActive = activeSys.IsSome && activeSys.Value = sys
            doc.PreCheckPptSystem(sys)

            doc.MakeFlows(sys) |> ignore

            //자동생성
            if isActive && not(isLib) && isCreateBtnLLib
            then
                sys.CreateGenBtnLamp()

            //수동생성
            doc.MakeButtons(sys)
            doc.MakeLamps(sys)

            doc.MakeConditions(sys)
            //segment 리스트 만들기
            doc.MakeSegment(sys)
            //Edge  만들기
            doc.MakeEdges(sys)
            //Safety AutoPre 만들기
            doc.MakeSafetyAutoPre(sys)
            //ApiTxRx  만들기
            doc.MakeApiTxRx()
            //AnimationPoint  만들기
            doc.MakeAnimationPoint(sys)

            //RealTime속성 만들기
            doc.MakeRealProperty(sys)
            //Job 기본 Address SlideNote로 부터 가져오기
            doc.MakeAddressBySlideNote(sys)

            doc.PostCheckPptSystem(sys, isLib)
            doc.IsBuilded <- true
