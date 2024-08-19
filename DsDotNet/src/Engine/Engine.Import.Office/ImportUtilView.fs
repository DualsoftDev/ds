// Copyright (c) Dualsoft  All Rights Reserved.
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
    let ConvertReal (real: Real, newNode: ViewNode, dummys: PptDummy seq) =
        let edgeInfos = real.ModelingEdges
        let lands = real.Graph.Islands
        let dicV = real.Graph.Vertices |> Seq.map (fun v -> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        if newNode.GetSingles().length () = 0 then
            lands
            |> Seq.filter (fun vertex -> dummyMembers.Contains(vertex) |> not)
            |> Seq.iter (fun vertex ->
                match vertex with
                | :? Call
                | :? Alias -> newNode.AddSingles(dicV.[vertex]) |> ignore
                | _ -> failwithf "vertex type ERROR")

        if newNode.GetEdges().length () = 0 then
            edgeInfos
            |> Seq.filter (fun edge ->
                (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))
                |> not)
            |> Seq.iter (fun edge ->
                edge.Sources
                |> Seq.iter (fun src ->
                    edge.Targets
                    |> Seq.iter (fun tgt ->
                        newNode.AddEdge(ModelingEdgeInfo<ViewNode>(dicV.[src], edge.EdgeSymbol, dicV.[tgt]))
                        |> ignore)))

        if newNode.DummyEdgeAdded |> not then
            let es = real.GetDummyEdgeReal(dummys, dicV, dicDummy)
            es |> Seq.iter (fun e -> newNode.AddEdge(e) |> ignore)


        if newNode.DummySingleAdded |> not then
            let ss = real.GetDummySingleReal(dummys, dicV, dicDummy)
            ss |> Seq.iter (fun v -> newNode.AddSingles(v) |> ignore)


    let ConvertFlow (flow: Flow, dummys: PptDummy seq) =
        let newNode = ViewNode(flow.Name, VFLOW)
        newNode.Flow <- Some flow
        let edgeInfos = flow.ModelingEdges
        let lands = flow.Graph.Islands
        let dicV = flow.Graph.Vertices |> Seq.map (fun v -> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        let convertReal (vertex: Vertex) =
            match vertex with
            | :? Real as r -> ConvertReal(r, dicV.[vertex], dummys)
            | :? Call
            | :? Alias -> ()
            | _ -> failwithf "vertex type ERROR"

            dicV.[vertex]


        lands
        |> Seq.filter (fun vertex -> dummyMembers.Contains(vertex) |> not)
        |> Seq.iter (fun vertex -> newNode.AddSingles(convertReal (vertex)) |> ignore)

        edgeInfos
        |> Seq.filter (fun edge ->
            (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))
            |> not)
        |> Seq.iter (fun edge ->
            edge.Sources
            |> Seq.iter (fun src ->
                if src :? Real then
                    let r = src :?> Real
                    ConvertReal(r, dicV.[r], dummys) |> ignore)
            edge.Targets
            |> Seq.iter (fun tgt ->
                if tgt :? Real then
                    let r = tgt :?> Real
                    ConvertReal(r, dicV.[r], dummys) |> ignore)


            edge.Sources
            |> Seq.iter (fun src ->
                edge.Targets
                |> Seq.iter (fun tgt ->
                    newNode.AddEdge(ModelingEdgeInfo<ViewNode>(dicV.[src], edge.EdgeSymbol, dicV.[tgt]))
                    )
                |> ignore)
                )


        let es = flow.GetDummyEdgeFlow(dummys, dicV, dicDummy)
        let ss = flow.GetDummySingleFlow(dummys, dicV, dicDummy)
        es |> Seq.iter (fun e -> newNode.AddEdge(e) |> ignore)
        ss |> Seq.iter (fun v -> newNode.AddSingles(v) |> ignore)


        newNode


    let UpdateLampNodes (system: DsSystem, flow: Flow, node: ViewNode) =
        let newNode = ViewNode("Lamps", VLAMP)
        let addLamps (lamps: seq<LampDef>) (lampType:LampType) =
            lamps 
            |> Seq.filter (fun w -> w.SettingFlows.Contains(flow) || w.SettingFlows.IsEmpty)
            |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, lampType)) |> ignore)

        addLamps system.AutoHWLamps DuAutoModeLamp
        addLamps system.ManualHWLamps DuManualModeLamp
        addLamps system.DriveHWLamps DuDriveStateLamp
        addLamps system.ErrorHWLamps DuErrorStateLamp
        addLamps system.TestHWLamps DuTestDriveStateLamp
        addLamps system.ReadyHWLamps DuReadyStateLamp
        addLamps system.IdleHWLamps DuIdleModeLamp
        addLamps system.OriginHWLamps DuOriginStateLamp

        if node.GetSingles().Count() > 0 then
            node.AddSingles(newNode) |> ignore

    let UpdateBtnNodes (system: DsSystem, flow: Flow, node: ViewNode) =

        let newNode = ViewNode("Buttons", VBUTTON)

        system.AutoHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuAutoBTN)) |> ignore)

        system.ManualHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuManualBTN)) |> ignore)

        system.DriveHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuDriveBTN)) |> ignore)

        system.PauseHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuPauseBTN)) |> ignore)

        system.ClearHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuClearBTN)) |> ignore)

        system.EmergencyHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuEmergencyBTN)) |> ignore)

        system.TestHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuTestBTN)) |> ignore)

        system.HomeHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuHomeBTN)) |> ignore)

        system.ReadyHWButtons.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuReadyBTN)) |> ignore)

        if newNode.GetSingles().Count() > 0 then
            node.AddSingles(newNode) |> ignore

    let UpdateConditionNodes (system: DsSystem, flow: Flow, node: ViewNode) =
        let newNode = ViewNode("Condition", VCONDITION)

        system.ReadyConditions.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuReadyState)) |> ignore)

        system.DriveConditions.Where(fun w -> w.SettingFlows.Contains(flow))
        |> Seq.iter (fun b -> newNode.AddSingles(ViewNode(b.Name, DuDriveState)) |> ignore)

        if newNode.GetSingles().Count() > 0 then
            node.AddSingles(newNode) |> ignore


    let UpdateApi (system: DsSystem, node: ViewNode) =
        let newNode = ViewNode("Interface", VIF)

        let flowApiNodes =
            system.ApiItems
            |> Seq.map (fun api -> api.Name, ViewNode(api.ToText(), VIF))
            |> dict

        system.ApiItems |> Seq.iter (fun api -> newNode.AddSingles(flowApiNodes.[api.Name]))

        system.ApiResetInfos |> Seq.iter (fun i ->
            let operand1Node = flowApiNodes.[i.Operand1.DeQuoteOnDemand()]
            let operand2Node = flowApiNodes.[i.Operand2.DeQuoteOnDemand()]
            newNode.AddEdge(ModelingEdgeInfo<ViewNode>(operand1Node, i.Operator.ToText(), operand2Node))
        )
        if newNode.GetSingles().Count() > 0 then
            node.AddSingles(newNode) |> ignore


    [<Extension>]
    type ImportViewUtil =

        [<Extension>]
        static member GetViewNodes(mySys: DsSystem) =
            let getFlowNodes (flows: Flow seq) =
                flows
                |> Seq.map (fun flow ->
                    let flowNode = ConvertFlow(flow, [])

                    UpdateLampNodes(flow.System, flow, flowNode)
                    UpdateBtnNodes(flow.System, flow, flowNode)
                    UpdateConditionNodes(flow.System, flow, flowNode)

                    UpdateApi(flow.System, flowNode)

                    flowNode)

            let viewNodes = getFlowNodes (mySys.Flows)

            viewNodes

        [<Extension>]
        static member GetViewNodesLoadingsNThis(mySys: DsSystem) =
            let loads = mySys.GetRecursiveLoadedSystems().SelectMany(fun s -> s.GetViewNodes())
            mySys.GetViewNodes() @ loads
