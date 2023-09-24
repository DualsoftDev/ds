// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open Microsoft.FSharp.Collections
open Dual.Common.Core.FS
open Engine.Import.Office
open Engine.Core
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module ImportViewModule =
    let ConvertReal(real:Real, newNode:ViewNode, dummys:pptDummy seq)  =
        let edgeInfos = real.ModelingEdges
        let lands = real.Graph.Islands
        let dicV = real.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        if newNode.GetSingles().length() = 0
        then
            lands
            |>Seq.filter(fun vertex -> dummyMembers.Contains(vertex) |> not)
            |>Seq.iter(fun vertex ->
                    match vertex  with
                    | :? CallDev | :? Alias-> newNode.AddSingles(dicV.[vertex]) |>ignore
                    | _ -> failwithf "vertex type ERROR" )

        if newNode.GetEdges().length() = 0
        then
            edgeInfos
            |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))|>not)
            |>Seq.iter(fun edge ->
                        edge.Sources
                        |> Seq.iter(fun src ->
                                    newNode.AddEdge(ModelingEdgeInfo<ViewNode>(dicV.[src], edge.EdgeSymbol, dicV.[edge.Targets[0]])) |>ignore)
            )
        if newNode.DummyEdgeAdded   |> not 
        then 
            let es = real.GetDummyEdgeReal(dummys, dicV, dicDummy)
            es |> Seq.iter(fun e-> newNode.AddEdge(e) |>ignore)
        
           
        if newNode.DummySingleAdded   |> not 
        then 
            let ss =  real.GetDummySingleReal(dummys, dicV, dicDummy) 
            ss |> Seq.iter(fun v-> newNode.AddSingles(v) |>ignore)
           

    let ConvertFlow(flow:Flow, dummys:pptDummy seq)  =
        let newNode = ViewNode(flow.Name, VFLOW)
        newNode.Flow <- Some flow
        let edgeInfos = flow.ModelingEdges
        let lands = flow.Graph.Islands
        let dicV = flow.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        let convertReal(vertex:Vertex) =
            match vertex  with
                | :? Real as r -> ConvertReal(r, dicV.[vertex], dummys)
                | :? CallDev | :? Alias-> ()
                | _ -> failwithf "vertex type ERROR"
            dicV.[vertex]


        lands
        |>Seq.filter(fun vertex -> dummyMembers.Contains(vertex) |> not)
        |>Seq.iter(fun vertex -> newNode.AddSingles(convertReal(vertex))|>ignore)

        edgeInfos
        |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))|>not)
        |>Seq.iter(fun edge ->
                    edge.Sources |> Seq.iter(fun src ->
                        if src:? Real
                        then let r = src :?> Real
                             ConvertReal(r, dicV.[r], dummys) |> ignore
                    )

                    assert(edge.Targets.Count() = 1)
                    if edge.Targets[0] :? Real
                    then let r = edge.Targets[0] :?> Real
                         ConvertReal(r, dicV.[r], dummys) |> ignore

                    edge.Sources
                    |> Seq.iter(fun src ->
                                    newNode.AddEdge(ModelingEdgeInfo<ViewNode>(dicV.[src], edge.EdgeSymbol, dicV.[edge.Targets[0]])) |>ignore)
            )


        let es = flow.GetDummyEdgeFlow(dummys, dicV, dicDummy)
        let ss =  flow.GetDummySingleFlow(dummys, dicV, dicDummy) 
        es |> Seq.iter(fun e-> newNode.AddEdge(e) |>ignore)
        ss |> Seq.iter(fun v-> newNode.AddSingles(v) |>ignore)
        

        newNode

    let UpdateLampNodes(system:DsSystem, flow:Flow, node:ViewNode)  =
        let newNode = ViewNode("Lamps", VLAMP)

        system.AutoLamps.Where(fun w->      w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuAutoLamp))      |>ignore)
        system.ManualLamps.Where(fun w->    w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuManualLamp))    |>ignore)
        system.DriveLamps.Where(fun w->     w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuDriveLamp))     |>ignore)
        system.StopLamps.Where(fun w->      w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuStopLamp))      |>ignore)
        system.EmergencyLamps.Where(fun w-> w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuEmergencyLamp)) |>ignore)
        system.TestLamps.Where(fun w->      w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuTestDriveLamp)) |>ignore)
        system.ReadyLamps.Where(fun w->     w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuReadyLamp))     |>ignore)
        system.IdleLamps.Where(fun w->      w.SettingFlow = flow) |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuIdleLamp))      |>ignore)

        if newNode.GetSingles().Count() > 0
        then node.AddSingles(newNode) |> ignore

    let UpdateBtnNodes(system:DsSystem, flow:Flow, node:ViewNode)  =

        let newNode = ViewNode("Buttons", VBUTTON)

        system.AutoButtons.Where(fun w->w.SettingFlows.Contains(flow))       |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuAutoBTN)     ) |>ignore)
        system.ManualButtons.Where(fun w->w.SettingFlows.Contains(flow))     |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuManualBTN)   ) |>ignore)
        system.DriveButtons.Where(fun w->w.SettingFlows.Contains(flow))      |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuDriveBTN)    ) |>ignore)
        system.StopButtons.Where(fun w->w.SettingFlows.Contains(flow))       |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuStopBTN)     ) |>ignore)
        system.ClearButtons.Where(fun w->w.SettingFlows.Contains(flow))      |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuClearBTN)    ) |>ignore)
        system.EmergencyButtons.Where(fun w->w.SettingFlows.Contains(flow))  |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuEmergencyBTN)) |>ignore)
        system.TestButtons.Where(fun w->w.SettingFlows.Contains(flow))       |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuTestBTN)     ) |>ignore)
        system.HomeButtons.Where(fun w->w.SettingFlows.Contains(flow))       |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuHomeBTN)     ) |>ignore)
        system.ReadyButtons.Where(fun w->w.SettingFlows.Contains(flow))      |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuReadyBTN)    ) |>ignore)

        if newNode.GetSingles().Count() > 0
        then node.AddSingles(newNode) |> ignore

    let UpdateConditionNodes(system:DsSystem, flow:Flow, node:ViewNode)  =
        let newNode = ViewNode("Condition", VCONDITION)

        system.AutoButtons.Where(fun w->w.SettingFlows.Contains(flow))       |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuReadyState)) |>ignore)
        system.ManualButtons.Where(fun w->w.SettingFlows.Contains(flow))     |> Seq.iter(fun b-> newNode.AddSingles(ViewNode(b.Name, DuDriveState)) |>ignore)

        if newNode.GetSingles().Count() > 0
        then node.AddSingles(newNode) |> ignore

    let UpdateApiItems(system:DsSystem, page:int, pptNodes: pptNode seq, node:ViewNode)  =

        let newNode = ViewNode("Interface", VIF)

        system.ApiItems
        |> Seq.iter(fun api ->

            let findApiNode = pptNodes.Where(fun f->f.Name = api.Name && f.PageNum = page)
            if findApiNode.Count() > 0
            then newNode.AddSingles(ViewNode(api.Name, VIF)) |>ignore

            )

        if newNode.GetSingles().Count() > 0
        then node.AddSingles(newNode) |> ignore

    //let rec ConvertRuntimeEdge(graph:Graph<Vertex, Edge>)  =
    //    let newNode = ViewNode()
    //    let dicV = graph.Vertices.Select(fun v-> v, ViewNode(v)) |> dict
    //    let convertReal(vertex:Vertex) =
    //        match vertex  with
    //            | :? Real as r ->  newNode.Singles.Add(ConvertRuntimeEdge(r.Graph)) |>ignore
    //            | :? CallDev | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
    //            | _ -> failwithf "vertex type ERROR"

    //    graph.Islands |>Seq.iter(fun vertex -> convertReal(vertex))
    //    graph.Edges
    //    |>Seq.iter(fun edge ->
    //        convertReal(edge.Source)
    //        convertReal(edge.Target)
    //        let viewEdge = ModelingEdgeInfo(dicV.[edge.Source], edge.EdgeType.ToText(), dicV.[edge.Target])
    //        newNode.Edges.Add(viewEdge) |>ignore)

    //    newNode

    [<Extension>]
    type ImportViewUtil =
        [<Extension>]
        static member ConvertViewNodes (mySys:DsSystem) =
                    mySys.Flows.Select(fun f ->ConvertFlow (f, []))

        [<Extension>]
        static member MakeGraphView (doc:pptDoc, mySys:DsSystem) =
                let dicVertex = doc.DicVertex
                let dicFlow = doc.DicFlow

                doc.Dummys |> Seq.iter(fun dummy -> dummy.Update(dicVertex))
                let getFlowNodes(flows:Flow seq) =
                    flows |>Seq.map(fun flow ->
                        let page =  dicFlow.Where(fun w-> w.Value = flow).First().Key
                        let dummys = doc.Dummys.Where(fun f->f.Page = page)
                        let flowNode = ConvertFlow(flow, dummys)

                        UpdateLampNodes(flow.System, flow, flowNode)
                        UpdateBtnNodes(flow.System, flow, flowNode)
                        UpdateConditionNodes(flow.System, flow, flowNode)

                        UpdateApiItems(flow.System, page, doc.DicNodes.Values.Where(fun f->f.NodeType.IsIF), flowNode)

                        flowNode.Page <- page; //flowNode.Flow <- Some(flow)
                        flowNode)

                let viewNodes =  getFlowNodes(mySys.Flows)

                viewNodes
