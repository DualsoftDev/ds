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


    let ConvertRealEdge(real:Real, newNode:ViewNode, dummys:pptDummy seq)  =
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

    let ConvertFlowEdge(flow:Flow, dummys:pptDummy seq)  =
        let newNode = ViewNode()
        let edgeInfos = flow.ModelingEdges
        let lands = flow.Graph.Islands
        let dicV = flow.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dicDummy = Dictionary<string, ViewNode>()
        let dummyMembers = dummys.GetDummyMembers()

        let convertReal(vertex:Vertex) = 
            match vertex  with
                | :? Real as r -> ConvertRealEdge(r, dicV.[vertex], dummys)
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
                         ConvertRealEdge(r, dicV.[r], dummys) |> ignore 
                    if edge.Target :? Real 
                    then let r = edge.Target :?> Real
                         ConvertRealEdge(r, dicV.[r], dummys) |> ignore 

                    newNode.Edges.Add(MEI(dicV.[edge.Source], edge.EdgeSymbol, dicV.[edge.Target])) |>ignore)

        flow.GetDummyFlow(dummys, dicV, dicDummy) 
        |> Seq.iter(fun e-> newNode.Edges.Add(e) |>ignore)

        newNode


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
                            
                model.Systems
                |>Seq.collect(fun sys -> sys.Flows)
                |>Seq.map(fun flow -> 
                    let page = dicFlow.GetPage(flow)
                    let dummys = doc.Dummys.Where(fun f->f.Page = page)
                    let flowNode = ConvertFlowEdge(flow, dummys)
                    flowNode.Page <- page
                    flowNode.Flow <- Some(flow)
                    flowNode
                    )