namespace Model.Import.Office

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module internal ToCopyModule =


    //Safety Copy
    let private copySafety(origFlow:Flow, copyFlow:Flow) =
        let copySys = copyFlow.System
        let origSys = origFlow.System
        let findReal (realName:string) = copySys.FindGraphVertex<Real>([|copySys.Name;copyFlow.Name;realName|]) 
        origSys.Flows
            .ForEach(fun flow->
                flow.Graph.Vertices.Where(fun w->w :? Real).Cast<Real>()
                    .ForEach(fun real->
                            real.SafetyConditions.ForEach(fun safety ->
                                let copyReal = findReal(real.Name)
                                copyReal.SafetyConditions.Add(findReal(safety.Name)) |>ignore
                                )
                        )
                )

    //TxRx Copy
    let copyTxRx(origApi:ApiItem, copyApi:ApiItem) =
        let copySys = copyApi.System
        let origSys = origApi.System
        let findReal (flowName:string, realName:string) = copySys.FindGraphVertex<Real>([|copySys.Name;flowName;realName|]) 
        origSys.ApiItems
                .ForEach(fun apiOrig ->
                    let apiCopy = copySys.ApiItems.First(fun f->f.Name = apiOrig.Name)
                    //자신의 리얼을 찾아서 넣음
                    let txs = apiOrig.TXs |> Seq.map(fun f-> findReal(f.Flow.Name, f.Name))
                    let rxs = apiOrig.RXs |> Seq.map(fun f-> findReal(f.Flow.Name, f.Name))

                    apiCopy.AddTXs(txs)  |> ignore
                    apiCopy.AddRXs(rxs)  |> ignore
                            )

    //VertexEdge Flow Copy
    let private copyVertexEdge(origFlow:Flow, copyFlow:Flow) =

        let origModel = origFlow.System.Model
        let copySys   = copyFlow.System
        let findReal (realName:string)  = copySys.FindGraphVertex<Real>([|copySys.Name;copyFlow.Name;realName|]) 
        let findCall (callName:string)  = copySys.FindGraphVertex<Call>([|copySys.Name;copyFlow.Name;callName|]) 
        let findCallInReal(realName:string, name:string) = copySys.FindGraphVertex<Call>([|copySys.Name;copyFlow.Name;realName;name|]) 
        let findInReal(realName:string, name:string) = copySys.FindGraphVertex<Vertex>([|copySys.Name;copyFlow.Name;realName;name|])

        let copyReal(name, graph:DsGraph) =
            let findVertex = graph.TryFindVertex(name)
            if findVertex.IsNone
                then Real.Create(name, copyFlow)
                else findVertex.Value :?> Real

        let copyCallInReal(apiItem:ApiItem, real:Real) =
            let findVertex = real.Graph.TryFindVertex(apiItem.Name)
            if findVertex.IsNone
                then Call.Create(apiItem, Real real)
                else findVertex.Value :?> Call

        let copyCallInFlow(apiItem:ApiItem, flow:Flow) =
            let findVertex = flow.Graph.TryFindVertex(apiItem.Name)
            if findVertex.IsNone
                then Call.Create(apiItem, Flow flow)
                else findVertex.Value :?> Call


        let copyVertex(origFlow:Flow, copyFlow:Flow) =
            //Step 1-1) Real, Call Vertices 처리 부터
            origFlow.Graph.Vertices.ForEach(fun vertexInFlow ->
                match vertexInFlow with
                | :? Real as orgiReal
                   ->  let copyReal = copyReal(orgiReal.Name,  copyFlow.Graph)
                       orgiReal.Graph.Vertices
                            .ForEach(fun vInReal->
                                match vInReal with
                                | :? Call as orgiCall -> copyCallInReal (orgiCall.ApiItem, copyReal) |> ignore
                                | _ -> () )

                | :? Call as orgiCall
                    -> copyCallInFlow (orgiCall.ApiItem, copyFlow) |> ignore
                | _ -> ()
            )

            //Step 1-2) Alias Node 처리
            origFlow.Graph.Vertices.ForEach(fun vertexInFlow ->
                match vertexInFlow with
                | :? Real as orgiReal
                    ->
                        orgiReal.Graph.Vertices
                            .ForEach(fun vInReal->
                                match vInReal  with
                                | :? Alias as orgiAlias -> 
                                    match orgiAlias.Target with 
                                    | RealTarget rt -> failwithf "Error : Real안에 Real타깃 Alias불가" 
                                    | CallTarget ct -> 
                                            let r = findReal(orgiReal.Name)
                                            Alias.Create(orgiAlias.Name, CallTarget(findCallInReal(orgiReal.Name, ct.Name)), Real(findReal(orgiReal.Name)), false)|>ignore
                                    //| CallTarget ct -> Alias.Create(orgiAlias.Name, CallTarget(ct), Real(findReal(orgiReal.Name)), false)|>ignore
                                | _ -> () )

                | :? Alias as orgiAlias ->
                        match orgiAlias.Target with
                        | RealTarget rt -> Alias.Create(vertexInFlow.Name, RealTarget(rt), Flow(copyFlow), false)|>ignore
                        | CallTarget ct -> Alias.Create(vertexInFlow.Name, CallTarget(ct), Flow(copyFlow), false)|>ignore
                | _ -> ()
            )


        let copyEdge(origFlow:Flow, copyFlow:Flow) =

            //Step 2-1) InFlow Edge 처리 부터
            origFlow.Graph.Edges.ForEach(fun e->
                Edge.Create(
                    copyFlow.Graph
                    , findReal(e.Source.Name)
                    , findReal(e.Target.Name)
                    , e.EdgeType) |> ignore
                )

            //Step 2-2) InReal Edge 처리
            origFlow.Graph.Vertices.ForEach(fun vInFlow ->
                match vInFlow  with
                | :? Real as orgiReal
                    ->
                        let copyReal = findReal(orgiReal.Name)
                        orgiReal.Graph.Edges.ForEach(fun e ->
                                Edge.Create(copyFlow.Graph
                                , findInReal(copyReal.Name, e.Source.Name)
                                , findInReal(copyReal.Name, e.Target.Name)
                                , e.EdgeType) |> ignore       )
                | _ -> () )


        //Step1)
        copyVertex(origFlow, copyFlow)
        //Step2)
        copyEdge(origFlow, copyFlow)


    let copyButtons(origSys:DsSystem, copySys:DsSystem) =
            origSys.StartButtons.ForEach(fun btn ->
                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(StartBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
            origSys.ResetButtons.ForEach(fun btn ->
                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(ResetBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
            origSys.AutoButtons.ForEach(fun btn ->
                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(AutoBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))
            origSys.EmergencyButtons.ForEach(fun btn ->
                btn.Value.ForEach(fun tgtFlow -> copySys.AddButton(EmergencyBTN, btn.Key, copySys.FindFlow(tgtFlow.Name))))

    //ApiItem Copy
    let copyApi(origApiItem:ApiItem, copySys:DsSystem) =
        let copyApiItem =ApiItem.Create(origApiItem.Name, copySys)
        copyApiItem

    //ApiInfo Copy
    let copyApiInfo(orig:ApiResetInfo, copySys:DsSystem) =
        let copyApiResetInfo = ApiResetInfo.Create(copySys, orig.Operand1, orig.Operator, orig.Operand2 )
        copyApiResetInfo

    //Flow Copy
    let copyFlow(origFlow:Flow, copyFlow:Flow) =
        copyVertexEdge(origFlow, copyFlow)
        copySafety(origFlow, copyFlow)
        copyFlow

    //System Copy
    let copySystem(origSys:DsSystem, copySystem:DsSystem) =
        copyButtons(origSys, copySystem)
        origSys.Flows.ForEach(fun origFlow      ->
                        let newFlow = Flow.Create(origFlow.Name, copySystem)
                        copyFlow(origFlow, newFlow)|>ignore)
        origSys.ApiItems.ForEach(fun origApi    -> copyApi(origApi, copySystem)|>ignore)
        origSys.ApiResetInfos.ForEach(fun orig  -> copyApiInfo(orig, copySystem)|>ignore)
        copySystem

    //Model Copy
    //let copyModel(origModel:Model) =
    //    let copyMoodel = Model()
    //    origModel.Systems.ForEach(fun origSys ->
    //        let copySys = DsSystem.Create($"{origSys.Name}_Copy", origSys.Host, copyMoodel)
    //        copySystem(origSys, copySys)|>ignore)
    //    copyMoodel

[<Extension>]
type ToCopyModuleHelper =
    [<Extension>] static member TryFindSystem(model:Model, systemName:string)    =
                    if model.TheSystem.IsSome 
                    then  if model.TheSystem.Value.Name = systemName 
                            then model.TheSystem.Value
                            else model.TheSystem.Value.Systems.FirstOrDefault(fun sys -> sys.Name = systemName)
                                       
                    else model.Systems.FirstOrDefault(fun sys -> sys.Name = systemName)
                    
    //[<Extension>] static member ToCopy(model:Model) = copyModel(model)
    [<Extension>] static member ToCopy(system:DsSystem, copySys:DsSystem) = copySystem(system, copySys)
    [<Extension>] static member ToCopy(system:DsSystem, copySysName:string) =
                    //copy 시에 상위레벨 복사 무시
                    let copySys = DsSystem.Create(copySysName, system.Host, DsSystem.CreateTopLevel("",""))
                    copySystem(system, copySys)
    [<Extension>] static member ToCopy(flow:Flow, copySystem:DsSystem) =
                    let newFlow = Flow.Create(flow.Name, copySystem)
                    copyFlow(flow, newFlow)
    [<Extension>] static member ToCopy(flow:Flow, copyflow:Flow)   = copyFlow(flow, copyflow)
    [<Extension>] static member ToCopy(apiItem:ApiItem, copySystem:DsSystem) = copyApi(apiItem, copySystem)
    [<Extension>] static member ToCopy(apiItem:ApiItem, copyApiItem:ApiItem) = copyTxRx(apiItem, copyApiItem)
    [<Extension>] static member ToCopy(apiResetInfo:ApiResetInfo, copySystem:DsSystem) = copyApiInfo(apiResetInfo, copySystem)



