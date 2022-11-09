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
    let MEI = ModelingEdgeInfo


    let ConvertReal(real:Real, newNode:ViewNode, dummys:pptDummy seq)  =
        let edgeInfos = real.ModelingEdges
        let lands = real.Graph.Islands
        let dicV = real.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        lands
        |>Seq.filter(fun vertex -> dummyMembers.Contains(vertex) |> not)
        |>Seq.iter(fun vertex -> 
                match vertex  with
                | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                | _ -> failwithf "vertex type ERROR" )

        edgeInfos
        |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Source) || dummyMembers.Contains(edge.Target))|>not)
        |>Seq.iter(fun edge -> newNode.Edges.Add(MEI(dicV.[edge.Source], edge.EdgeSymbol, dicV.[edge.Target])) |>ignore)

        real.GetDummyReal(dummys, dicV, dicDummy) |> Seq.iter(fun e-> newNode.Edges.Add(e) |>ignore)

    let ConvertFlow(flow:Flow, dummys:pptDummy seq)  =
        let newNode = ViewNode()
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
        |>Seq.filter(fun edge -> (dummyMembers.Contains(edge.Source) || dummyMembers.Contains(edge.Target))|>not)
        |>Seq.iter(fun edge -> 
                    if edge.Source :? Real 
                    then let r = edge.Source :?> Real
                         ConvertReal(r, dicV.[r], dummys) |> ignore 
                    if edge.Target :? Real 
                    then let r = edge.Target :?> Real
                         ConvertReal(r, dicV.[r], dummys) |> ignore 

                    newNode.Edges.Add(MEI(dicV.[edge.Source], edge.EdgeSymbol, dicV.[edge.Target])) |>ignore)

        flow.GetDummyFlow(dummys, dicV, dicDummy) 
        |> Seq.iter(fun e-> newNode.Edges.Add(e) |>ignore)

        newNode

    let UpdateBtnNodes(system:DsSystem, flow:Flow, node:ViewNode)  =

        let newNode = ViewNode("Buttons", BUTTON)

        system.AutoButtons.Where(fun w->w.Value.Contains(flow))     |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, AutoBTN)) |>ignore)
        system.ResetButtons.Where(fun w->w.Value.Contains(flow))    |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, ResetBTN)) |>ignore)
        system.StartButtons.Where(fun w->w.Value.Contains(flow))    |> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, StartBTN)) |>ignore)
        system.EmergencyButtons.Where(fun w->w.Value.Contains(flow))|> Seq.iter(fun b-> newNode.Singles.Add(ViewNode(b.Key, EmergencyBTN)) |>ignore)
        
        if newNode.Singles.Count > 0
        then node.Singles.Add(newNode) |> ignore
    

    let UpdateApiItems(system:DsSystem, flow:Flow, node:ViewNode)  =

        let newNode = ViewNode("Interface", IF)

        system.ApiItems |> Seq.iter(fun api -> newNode.Singles.Add(ViewNode(api.Name, IF)) |>ignore)
        
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
        static member MakeGraphView (doc:pptDoc, model:Model) =
                doc.Dummys |> Seq.iter(fun dummy -> dummy.Update(dicVertex))

                let getFlowNodes(flows:Flow seq) = 
                    flows |>Seq.map(fun flow -> 
                        let page = dicFlow.GetPage(flow)
                        let dummys = doc.Dummys.Where(fun f->f.Page = page)
                        let flowNode = ConvertFlow(flow, dummys)

                        UpdateBtnNodes(flow.System, flow, flowNode)
                        UpdateApiItems(flow.System, flow, flowNode)

                        flowNode.Page <- page; flowNode.Flow <- Some(flow)
                        flowNode)

                let viewNodes = 
                    model.Systems
                    |>Seq.map(fun sys -> sys, sys.Flows)
                    |>Seq.collect(fun (sys, flows) -> 
                            let flowNodes = getFlowNodes(flows)

                            flowNodes
                            )
                viewNodes 
