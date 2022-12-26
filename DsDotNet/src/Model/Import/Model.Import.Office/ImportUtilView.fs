// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open Microsoft.FSharp.Collections
open Engine.Common.FS
open Model.Import.Office
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

        if newNode.Singles.length() = 0
        then 
            lands
            |>Seq.filter(fun vertex -> dummyMembers.Contains(vertex) |> not)
            |>Seq.iter(fun vertex -> 
                    match vertex  with
                    | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                    | _ -> failwithf "vertex type ERROR" )

        if newNode.Edges.length() = 0
        then 
            edgeInfos
            |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))|>not)
            |>Seq.iter(fun edge -> newNode.Edges.Add(ModelingEdgeInfo(dicV.[edge.Sources[0]], edge.EdgeSymbol, dicV.[edge.Targets[0]])) |>ignore)

        real.GetDummyReal(dummys, dicV, dicDummy) 
        |> Seq.iter(fun e-> 
            if newNode.DummyAdded |> not
            then newNode.Edges.Add(e) |>ignore
            )

    let ConvertFlow(flow:Flow, dummys:pptDummy seq)  =
        let newNode = ViewNode()
        newNode.Flow <- Some flow
        let edgeInfos = flow.ModelingEdges
        let lands = flow.Graph.Islands
        let dicV = flow.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        let convertReal(vertex:Vertex) = 
            match vertex  with
                | :? Real as r -> ConvertReal(r, dicV.[vertex], dummys)
                | :? Call | :? Alias-> ()
                | _ -> failwithf "vertex type ERROR" 
            dicV.[vertex]

        lands
        |>Seq.filter(fun vertex -> dummyMembers.Contains(vertex) |> not)
        |>Seq.iter(fun vertex -> newNode.Singles.Add(convertReal(vertex))|>ignore)

        edgeInfos
        |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Sources[0]) || dummyMembers.Contains(edge.Targets[0]))|>not)
        |>Seq.iter(fun edge -> 
                    if edge.Sources[0] :? Real 
                    then let r = edge.Sources[0] :?> Real
                         ConvertReal(r, dicV.[r], dummys) |> ignore 
                    if edge.Targets[0] :? Real 
                    then let r = edge.Targets[0] :?> Real
                         ConvertReal(r, dicV.[r], dummys) |> ignore 
                    
                    newNode.Edges.Add(ModelingEdgeInfo<ViewNode>(dicV.[edge.Sources[0]], edge.EdgeSymbol, dicV.[edge.Targets[0]])) |>ignore)

        flow.GetDummyFlow(dummys, dicV, dicDummy) 
        |> Seq.iter(fun e-> newNode.Edges.Add(e) |>ignore)

        newNode

    let UpdateBtnNodes(system:DsSystem, flow:Flow, node:ViewNode)  =

        let newNode = ViewNode("Buttons", BUTTON)

        system.ButtonSet.AutoButtons.Where(fun w->w.Value.Contains(flow))       |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuAutoBTN)) |>ignore)
        system.ButtonSet.ClearButtons.Where(fun w->w.Value.Contains(flow))      |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuClearBTN)) |>ignore)
        system.ButtonSet.StartButtons.Where(fun w->w.Value.Contains(flow))      |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuStartBTN)) |>ignore)
        system.ButtonSet.EmergencyButtons.Where(fun w->w.Value.Contains(flow))  |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuEmergencyBTN)) |>ignore)
        system.ButtonSet.ManualButtons.Where(fun w->w.Value.Contains(flow))     |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuManualBTN)) |>ignore)
        system.ButtonSet.StopButtons.Where(fun w->w.Value.Contains(flow))       |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuStopBTN)) |>ignore)
        system.ButtonSet.StartDryButtons.Where(fun w->w.Value.Contains(flow))   |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, DuStartDryBTN)) |>ignore)
        
        if newNode.Singles.Count > 0
        then node.Singles.Add(newNode) |> ignore
    

    let UpdateApiItems(system:DsSystem, page:int, pptNodes: pptNode seq, node:ViewNode)  =

        let newNode = ViewNode("Interface", IF)

        system.ApiItems 
        |> Seq.iter(fun api ->
            
            let findApiNode = pptNodes.Where(fun f->f.Name = api.Name && f.PageNum = page)
            if findApiNode.Count() > 0
            then newNode.Singles.Add(ViewNode(api.Name, IF)) |>ignore
            
            )

        if newNode.Singles.Count > 0
        then node.Singles.Add(newNode) |> ignore

    let rec ConvertRuntimeEdge(graph:Graph<Vertex, Edge>)  =
        let newNode = ViewNode()
        let dicV = graph.Vertices.Select(fun v-> v, ViewNode(v)) |> dict
        let convertReal(vertex:Vertex) = 
            match vertex  with
                | :? Real as r ->  newNode.Singles.Add(ConvertRuntimeEdge(r.Graph)) |>ignore
                | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                | _ -> failwithf "vertex type ERROR" 

        graph.Islands |>Seq.iter(fun vertex -> convertReal(vertex))
        graph.Edges
        |>Seq.iter(fun edge -> 
            convertReal(edge.Source)
            convertReal(edge.Target)
            let viewEdge = ModelingEdgeInfo(dicV.[edge.Source], edge.EdgeType.ToText(), dicV.[edge.Target])
            newNode.Edges.Add(viewEdge) |>ignore)

        newNode

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

                        UpdateBtnNodes(flow.System, flow, flowNode)
                        UpdateApiItems(flow.System, page, doc.DicNodes.Values.Where(fun f->f.NodeType = IF), flowNode)

                        flowNode.Page <- page; //flowNode.Flow <- Some(flow)
                        flowNode)

                let viewNodes =  getFlowNodes(mySys.Flows)
                    
                viewNodes 
