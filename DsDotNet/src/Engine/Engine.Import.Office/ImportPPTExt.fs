// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Microsoft.FSharp.Collections
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open Engine.Import.Office
open Engine.Core
open System.Runtime.CompilerServices
open System
open System.Data
open Engine.Parser.FS
open Engine.Parser.FS.ModelParser
open Engine.Core.MapperDataModule

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
                        sys.CreateApiResetInfo(edge.StartNode.Name, edge.Causal, edge.EndNode.Name, false)
                    )
                    |> ignore)

            let dicIL = Dictionary<int, HashSet<string>>()

            let updateILInfo (src, tgt) =
                match dicIL.TryFind(fun dic -> dic.Value.Contains(src) || dic.Value.Contains(tgt)) with
                | Some dic ->
                    dic.Value.Add(src) |> ignore
                    dic.Value.Add(tgt) |> ignore
                | None -> dicIL.Add(dicIL.Count, [ src; tgt ] |> HashSet)

            let createInterlockInfos (src, tgt) =
                let mei = sys.CreateApiResetInfo(src, Interlock, tgt, false)
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
                sys.CreateApiItem(apiName) |> ignore)

            doc.MakeInterfaceResets sys

        //MFlow 리스트 만들기
        [<Extension>]
        static member MakeFlows(doc: PptDoc, sys: DsSystem) =

            doc.Pages
            |> Seq.filter (fun page -> page.PageNum <> pptHeadPage)
            |> Seq.filter (fun page -> page.IsUsing)
            |> Seq.iter (fun page ->
                let pageNum = page.PageNum

                let sysName, flowName = GetSysNFlow(doc.Name, page.Title, page.PageNum)
                let flowName = if page.PageNum = pptHeadPage then $"{sysName}_Page1" else flowName

                match doc.DicFlow.TryFind(fun kv -> kv.Value.Name = flowName) with
                | Some kv ->   doc.DicFlow.Add(pageNum, kv.Value) |> ignore
                | None -> doc.DicFlow.Add(pageNum, sys.CreateFlow(flowName)) |> ignore
                )

        //MakeButtons 리스트 만들기
        [<Extension>]
        static member MakeButtons(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow

            doc.Nodes
            |> Seq.filter (fun node -> node.ButtonDefs.Any ())
            |> Seq.iter (fun node ->
                let flow = dicFlow.[node.PageNum]

                node.ButtonDefs.ForEach(fun b ->
                    let fullName = b.Key
                    let pureName, devParamIO = getPureNValueParam(fullName, true)
                    mySys.AddButtonDef(b.Value, pureName, devParamIO, Addresses("", ""), Some flow))
                    )

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.ButtonHeadPageDefs.Any())
            |> Seq.iter (fun node ->

                if dicFlow.IsEmpty() then Office.ErrorShape(node.Shape, ErrID._60, node.PageNum)
                else
                        node.ButtonHeadPageDefs.ForEach(fun b ->
                            let fullName = b.Key
                            let pureName, devParamIO = getPureNValueParam(fullName, true)

                            mySys.AddButtonDef(b.Value, pureName, devParamIO, Addresses("", ""), None))
                )

        //MakeLamps 리스트 만들기
        [<Extension>]
        static member MakeLamps(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow
            let headPageLamps=  doc.NodesHeadPage |> Seq.filter (fun node -> node.LampHeadPageDefs.Any())
            let flowPageLamps=  doc.Nodes |> Seq.filter (fun node -> node.LampDefs.Any())
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
                node.LampDefs.Iter(fun l ->
                    let fullName = l.Key
                    let pureName, devParamIO = getPureNValueParam(fullName, false)
                    mySys.AddLampDef(l.Value, pureName, devParamIO, Addresses("", ""), Some flow)))

            headPageLamps
            |> Seq.iter (fun node ->
                node.LampHeadPageDefs.Iter(fun l ->
                    let fullName = l.Key
                    let pureName, devParamIO = getPureNValueParam(fullName, false)
                    mySys.AddLampDef(l.Value, pureName, devParamIO, Addresses("", ""), None)))

        //MakeReadyConditions 리스트 만들기
        [<Extension>]
        static member MakeConditionNActions(doc: PptDoc, mySys: DsSystem) =
            let dicFlow = doc.DicFlow

            let addCondition(fullName, conditionType:ConditionType, settingflow:Flow option) =
                let emptyAddr = Addresses("", "")
                let pureName, devParamIO = getPureNValueParam(fullName, true)
                mySys.AddCondition(conditionType, pureName, devParamIO, emptyAddr, settingflow)

            let addActiontion(fullName, aType:ActionType, settingflow:Flow option) =
                let emptyAddr = Addresses("", "")
                let pureName, devParamIO = getPureNValueParam(fullName, false)

                mySys.AddAction(aType, pureName, devParamIO, emptyAddr, settingflow)

            doc.Nodes
            |> Seq.filter (fun node -> node.CondiDefs.Any() || node.ActionDefs.Any())
            |> Seq.iter (fun node ->
                try
                    let flow = dicFlow.[node.PageNum]
                    node.CondiDefs.ForEach(fun c ->  addCondition (c.Key, c.Value, Some flow))
                    node.ActionDefs.ForEach(fun a ->  addActiontion (a.Key, a.Value, Some flow))
                with ex ->
                    Office.ErrorName(node.Shape, ex.Message, node.PageNum)
                    )

            doc.NodesHeadPage
            |> Seq.filter (fun node -> node.CondiHeadPageDefs.Any() || node.ActionHeadPageDefs.Any())
            |> Seq.iter (fun node ->
                if dicFlow.IsEmpty() then Office.ErrorShape(node.Shape, ErrID._67, node.PageNum)
                else
                    node.CondiHeadPageDefs.ForEach(fun c ->  addCondition (c.Key, c.Value,  None))
                    node.ActionHeadPageDefs.ForEach(fun c ->  addActiontion (c.Key, c.Value, None))
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
        static member MakeSegment(doc: PptDoc, mySys: DsSystem, target:HwTarget) =
            let dicFlow = doc.DicFlow
            let dicVertex = doc.DicVertex


            let pptNodes = doc.Nodes

            let dicChildParent =
                doc.Parents
                |> Seq.collect (fun (KeyValue(parent, children)) ->
                    children |> Seq.map (fun child -> child, parent))
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
                        let flow = dicFlow.[node.PageNum]
                        dicVertex.Add(node.Key, flow.CreateAlias(real.ParentNPureNames.Combine("_"), real, false))
                        node.UpdateRealProperty(real)
                    | _ ->
                        let real = dicFlow.[node.PageNum].CreateReal(node.Name)
                        dicVertex.Add(node.Key, real)
                        node.UpdateRealProperty(real)
                        )

            let callNAutoPres =
                pptNodes
                |> Seq.filter (fun node -> node.Alias.IsNone)
                |> Seq.filter (fun node -> node.NodeType.IsCall || node.NodeType = AUTOPRE)
                |> Seq.toArray



            let createCallNAutoPre () =
                let libConfig, _ = getLibraryConfig()
                let libInfos = libConfig.LibraryInfos
                callNAutoPres
                    .Filter(fun node -> not(node.IsFunction) && node.NodeType <> AUTOPRE)
                    .Filter(fun node -> not(mySys.LoadedSystems.Select(fun d->d.Name).Contains(node.DevName)))
                    .GroupBy(fun node -> node.DevName)
                    .Iter(fun kv ->

                        let libApis = kv.Where(fun d-> libInfos.ContainsKey(d.ApiName))
                                        .Select(fun d-> d.ApiName).Distinct()
                        let usedApis = kv.Select(fun d->d.ApiName).Distinct()

                        if libApis.Any() && libApis.Count() <> usedApis.Count()
                        then
                            let errApis = usedApis.Except(libApis).JoinWith(", ")
                            let libFilePath  =libInfos[libApis.First()]
                            failWithLog $"{kv.Key}은 시스템 Libaray Api를 사용하였습니다.\r\n{libFilePath}에 {errApis}가 없습니다. \r\n\r\n {libConfig} \r\n\r\n시스템 Libaray는 지정된 이름만 사용가능합니다."
                     )
                let platformTarget = target.Platform
                callNAutoPres
                |> Seq.filter(fun node -> node.NodeType = CALL) //Call만 처리
                |> Seq.sortBy(fun node -> (node.PageNum, node.Position.Left, node.Position.Top))
                |> Seq.iter (fun node ->
                    let parentWrapper =
                        if dicChildParent.ContainsKey node then
                            let parent = dicChildParent[node]
                            if not (dicVertex.ContainsKey parent.Key)
                            then
                                node.Shape.ErrorName($"이름이 같은 다른페이지 Work 내부에 복수정의", node.PageNum)
                            else
                                dicVertex[parent.Key] :?> Real |> DuParentReal
                        else
                            dicFlow[node.PageNum] |> DuParentFlow
                    createCallVertex (mySys, node, parentWrapper, platformTarget, dicVertex)
                )


            let createAlias () =
                pptNodes
                |> Seq.filter (fun node -> node.IsAlias)
                |> Seq.iter (fun node ->

                    if node.IsFunction then
                        node.Shape.ErrorName($"Alias Function은 지원하지 않습니다.", node.PageNum)
                    let segOrg =    dicVertex.[node.Alias.Value.Key]

                    let alias =
                        let flow = dicFlow.[node.PageNum]
                        if node.NodeType = REALExF then // isOtherFlowRealAlias is true
                            let real = getOtherFlowReal (dicFlow.Values, node) :?> Real
                            node.UpdateRealProperty(real)
                            let name = real.ParentNPureNames.Combine("_")
                            flow.CreateAlias( $"{name}_{node.AliasNumber}", real, true )

                        elif dicChildParent.ContainsKey(node) then
                            let real = dicVertex.[dicChildParent.[node].Key] :?> Real
                            let call = dicVertex.[node.Alias.Value.Key] :?> Call

                            if not(call.ValueParamIO.IsDefaultParam)
                            then
                                node.Shape.ErrorName($"Alias는 ValueParam은 지원하지 않습니다.", node.PageNum)

                            let name = call.DeviceNApi.Combine("_")
                            let call = segOrg :?> Call
                            real.CreateAlias($"{name}_{node.AliasNumber}", call, false )
                        else

                            match segOrg with
                            | :? Real as rt ->
                                node.UpdateRealProperty(rt)
                                let otherFlow  =  flow <> rt.Flow
                                let name = if otherFlow then $"{rt.Flow.Name}{rt.Name}"else rt.Name
                                flow.CreateAlias($"{name}_{node.AliasNumber}", rt, otherFlow)
                            | :? Call as ct ->
                                let otherFlow  = flow.Name <> ct.TargetJob.NameComponents.Head()
                                let name = if otherFlow then ct.TargetJob.NameComponents.Combine() else ct.Name

                                if not(ct.ValueParamIO.IsDefaultParam)
                                then
                                    node.Shape.ErrorName($"Alias는 ValueParam은 지원하지 않습니다.", node.PageNum)

                                flow.CreateAlias($"{name}_{node.AliasNumber}", ct, otherFlow)
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
                    //Action이 타겟으로 사용되었는지 체크
                    let tryActionTargetErrorCheck () =
                        match srcs |> Seq.tryHead, tgts |> Seq.tryHead with
                        | Some src, Some tgt -> edge.ConnectionShape.ErrorConnect(ErrID._86, src.Name, tgt.Name, edge.PageNum)
                        | _ -> ()

                    match not(edge.IsReverseEdge), tgts |> Seq.exists (fun tgt -> tgt :? Call), srcs |> Seq.exists (fun src -> src :? Call) with
                    | true, true, _ -> tryActionTargetErrorCheck()
                    | false, _, true -> tryActionTargetErrorCheck ()
                    | _ -> 
                        if (tgts.OfType<Call>().Any ()) then
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
                        //let autoPreCall = doc.DicAutoPreCall[edge.StartNode.Key]
                        node.AutoPres.Add(edge.StartNode.Name)|>ignore
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

            let dicCall =
                doc.DicVertex.Values
                   .OfType<Call>().Where(fun call -> not(call.IsFlowCall))
                   .OfType<Call>().Where(fun call -> call.IsJob)
                   .Select(fun call -> call.DequotedQualifiedName,  call) |> dict


            let getCallName(condiName:string, holder:Call) =
                let fqdn = condiName.Split('.').Select(fun s->s.Trim()).ToArray()
                let sName = mySys.Name;
                match fqdn.Length with
                | 2 ->
                    [|sName;holder.Flow.Name;holder.Parent.GetCore().Name;fqdn[0];fqdn[1]|].Combine()// 자신Work 내부의 Call
                | 3 ->
                    [|sName;holder.Flow.Name;fqdn[0];fqdn[1];fqdn[2]|].Combine()// 내 Flow 내부의 Work Call
                | 4 ->
                    [|sName;fqdn[0];fqdn[1];fqdn[2];fqdn[3]|].Combine()// 외부 Flow의 Work Call
                | _ ->
                    failWithLog ErrID._79

            doc.Nodes
            |> Seq.iter(fun node ->
                node.Safeties
                |> iter (fun condiName  ->
                    let holder = doc.DicVertex.[node.Key] :?> Call

                    let safetyFqdn = getCallName (condiName, holder)
                    let targeCall =
                        if dicCall.ContainsKey (safetyFqdn) then
                            dicCall[safetyFqdn]
                        else
                            node.Shape.ErrorName($"Safety 대상이 없습니다. 자신 Work경로만 생략 가능합니다.\n{condiName}", node.PageNum)

                    match doc.DicVertex.[node.Key].GetPure() |> box with
                    | :? ISafetyAutoPreRequisiteHolder as holder ->
                        holder.SafetyConditions.Add(DuSafetyAutoPreConditionCall(targeCall)) |> ignore
                    | _ ->
                        node.Shape.ErrorName($"SafetyConditions err:{doc.DicVertex.[node.Key].QualifiedName})", node.PageNum)
                        )

                node.AutoPres
                |> iter (fun fqdn  ->
                    let holder = doc.DicVertex.[node.Key] :?> Call
                    let autoPreFqdn = getCallName (fqdn, holder)

                    let targeCall =
                        if dicCall.ContainsKey (autoPreFqdn) then
                            dicCall[autoPreFqdn]
                        else
                            node.Shape.ErrorName($"AutoPre 대상이 없습니다. 자신 Work경로만 생략 가능합니다.\n{fqdn}", node.PageNum)

                    match doc.DicVertex.[node.Key].GetPure() |> box with
                    | :? ISafetyAutoPreRequisiteHolder as holder ->
                            holder.AutoPreConditions.Add(DuSafetyAutoPreConditionCall(targeCall)) |> ignore
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
                let sys = flow.System
                let api = sys.ApiItems.Where(fun w -> w.Name = node.IfName).First()

                let findReal (trxName: string) =
                    let flowName, realName =
                        if trxName.Contains(".") then
                            trxName.Split('.').[0], trxName.Split('.').[1]
                        else
                            flow.Name, trxName

                    if dicFlow.Values.Where(fun w -> w.Name = flowName).IsEmpty() then
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
        static member UpdateActionIO(doc: PptDoc, sys: DsSystem, autoIO:bool, hwTarget:HwTarget) =
            let pageTables = doc.GetTables(System.Enum.GetValues(typedefof<IOColumn>).Length)
            if not(autoIO)
                && activeSys.IsSome && activeSys.Value = sys
                && pageTables.IsEmpty()
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

            ApplyIO(sys, pageTables, hwTarget)

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
                            systems.Where(fun w -> w.ApiItems.Select(fun s -> s.Name = api.Name).Any())

                        let errText = (String.Join(", ", errSystems.Select(fun s -> s.Name)))
                        failwithf $"{api.Name} exists on the same system. [{errText}]"))

            libApisNSys

        [<Extension>]
        static member CreateGenBtnLamp(mySys: DsSystem) =
            let defParm = defaultValueParamIO()
            let defBtn  = Addresses ("", TextNotUsed)
            let defLamp = Addresses (TextNotUsed, "")
            mySys.AddButtonDef(BtnType.DuAutoBTN,      "AutoSelect",  defParm, defBtn , None)
            mySys.AddButtonDef(BtnType.DuManualBTN,    "ManualSelect",defParm, defBtn , None)
            mySys.AddButtonDef(BtnType.DuDriveBTN,     "DrivePushBtn",defParm, defBtn , None)
            mySys.AddButtonDef(BtnType.DuPauseBTN,     "PausePushBtn",defParm, defBtn , None)
            mySys.AddButtonDef(BtnType.DuClearBTN,     "ClearPushBtn",defParm, defBtn , None)
            mySys.AddButtonDef(BtnType.DuEmergencyBTN, "EmergencyBtn",defParm, defBtn , None)

            mySys.AddLampDef(LampType.DuAutoModeLamp   , "AutoModeLamp", defParm, defLamp ,  None)
            mySys.AddLampDef(LampType.DuManualModeLamp , "ManualModeLamp",defParm, defLamp , None)
            mySys.AddLampDef(LampType.DuIdleModeLamp   , "IdleModeLamp", defParm, defLamp ,  None)
            mySys.AddLampDef(LampType.DuErrorStateLamp,  "ErrorLamp", defParm, defLamp ,     None)
            mySys.AddLampDef(LampType.DuOriginStateLamp, "OriginStateLamp",defParm, defLamp ,None)
            mySys.AddLampDef(LampType.DuReadyStateLamp,  "ReadyStateLamp",defParm, defLamp , None)
            mySys.AddLampDef(LampType.DuDriveStateLamp,  "DriveLamp", defParm, defLamp ,     None)

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
                    .Where(fun n -> (n.NodeType.IsCall) && not(n.IsFunction))
                    .ToArray()

            (* Multi Call Dev별 갯수 동일 체크*)
            callSet.GroupBy(fun n -> n.DevName)
                   .Select(fun calls ->
                            calls.DistinctBy(fun c-> (c.JobParam.TaskDevCount))
                    ).Iter(errCheck)

            (* Multi Call Api별 갯수 동일 체크*)
            callSet.GroupBy(fun n -> n.DevName+n.ApiName)
                   .Select(fun calls ->
                            calls.DistinctBy(fun c-> (c.JobParam.TaskDevCount
                                           , c.JobParam.InCount
                                           , c.JobParam.OutCount))
                    ).Iter(errCheck)


        [<Extension>]
        static member PostCheckPptSystem(doc: PptDoc, sys: DsSystem, isLib:bool) =

            (* Root Call 연결 없음 체크 *)
            let rootEdgeSrcs = sys.GetFlowEdges().Select(fun e->e.Source).Distinct()

            doc.Nodes
                .Where(fun n -> n.NodeType.IsCall && n.IsRootNode.Value)
                .Iter(fun n ->
                    let call = doc.DicVertex[n.Key]
                    if not(rootEdgeSrcs.Contains (call)) then
                        n.Shape.ErrorShape(ErrID._71, n.PageNum)
                )

            (* 복수의 작업에서 SEQ 전송 Start Edge 체크*)
            if not(isLib) then
                doc.Nodes
                    .Where(fun n -> n.NodeType.IsReal)
                    .Iter(fun n ->
                        let xs =
                            doc.Edges
                                .Where(fun e -> e.IsStartEdge && e.EndNode = n)
                                .Select(fun e-> doc.DicVertex.[e.StartNode.Key])

                        let pureReals = xs.GetPureReals()
                        if pureReals.DistinctBy(fun w->w.GetPureReal()).Where(fun f -> not(f.NoTransData)).Count() > 1 then
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

                    let dsText = $"[sys] temp = {{ [prop] = {{ {note} }}}}".Replace("”", "\"").Replace("“", "\"")
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
                        match mySys.TaskDevs.TryFind(fun td->td.FullName = apiFqdn) with
                        | Some td ->
                            if not td.IsInAddressEmpty then
                                failwithf $"Error: {apiFqdn} InAddress already exists"

                            if not td.IsOutAddressEmpty then
                                failwithf $"Error: {apiFqdn} OutAddress already exists"

                            td.TaskDevParamIO.InParam.Address <- a.TaskDevParamIO.InParam.Address
                            td.TaskDevParamIO.OutParam.Address <-  a.TaskDevParamIO.OutParam.Address

                        | None -> failwithf $"Error: {apiFqdn} not found"
                    )

                | _ -> ()

            let systemRepo = ShareableSystemRepository()
            doc.Pages
            |> Seq.filter (fun page -> page.PageNum = 1)
            |> Seq.iter (processPage doc mySys systemRepo)

        [<Extension>]
        static member UpdateIOFromUserDeviceTags(doc: PptDoc, sys: DsSystem, hwTarget:HwDriveTarget) =
        
            match doc.HwIOType with
            | Some io when io = hwTarget ->  //설정드라이브랑 같아야 가져옴
                let dictTaskDev =  sys.TaskDevs.ToDictionary(fun td -> td.FullName) 
                doc.UserDeviceTags
                |> Seq.iter (fun api ->
                    let key = api.DeviceApiName
                    if(dictTaskDev.ContainsKey(key))
                    then
                        dictTaskDev[key].InAddress <- api.Input
                        dictTaskDev[key].OutAddress<- api.Output
                    )
            |_-> ()



        [<Extension>]
        static member BuildSystem(doc: PptDoc, sys: DsSystem, hwTarget:HwTarget, isLib:bool, isCreateBtnLLib:bool) =
            let isActive = activeSys.IsSome && activeSys.Value = sys
            doc.PreCheckPptSystem(sys)



            doc.MakeFlows(sys) |> ignore

            //자동생성
            if isActive && not(isLib) && isCreateBtnLLib then
                sys.CreateGenBtnLamp()

            //수동생성
            doc.MakeButtons(sys)
            doc.MakeLamps(sys)

            doc.MakeConditionNActions(sys)
            //segment 리스트 만들기
            doc.MakeSegment(sys, hwTarget)
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
            //doc.MakeAddressBySlideNote(sys)

            //IO Table로 부터 가져오기
            doc.UpdateIOFromUserDeviceTags(sys, hwTarget.HwDrive)

            doc.PostCheckPptSystem(sys, isLib)
            doc.IsBuilded <- true
