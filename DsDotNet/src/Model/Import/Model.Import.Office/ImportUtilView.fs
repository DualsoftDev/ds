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

    let convertRealEdge(real:Real)  =
        let newNode = ViewNode()
        let edgeInfos = real.ModelingEdges
        let lands = real.Graph.Islands
        let dicV = real.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict

        lands
        |>Seq.iter(fun vertex -> 
                match vertex  with
                | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                | _ -> failwithf "vertex type ERROR" )

        edgeInfos
        |>Seq.iter(fun edge -> 
            let viewEdge = ModelingEdgeInfo(dicV.[edge.Source], edge.EdgeSymbol, dicV.[edge.Target])
            newNode.Edges.Add(viewEdge) |>ignore)

        newNode

    let convertFlowEdge(flow:Flow, dummys:pptDummy seq)  =
        let newNode = ViewNode()
        let edgeInfos = flow.ModelingEdges
        let lands = flow.Graph.Islands
        let dicV = flow.Graph.Vertices |> Seq.map(fun v-> v, ViewNode(v)) |> dict
        let dummyMembers = dummys  |> Seq.collect(fun f-> f.Members)


        lands
        |>Seq.iter(fun vertex -> 

            if dummyMembers.Contains(vertex)
            then ()
            else 
                match vertex  with
                | :? Real as r ->  newNode.Singles.Add(convertRealEdge(r)) |>ignore
                | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                | _ -> failwithf "vertex type ERROR" )

        edgeInfos
        |>Seq.iter(fun edge -> 
            let viewEdge =
                if dummyMembers.Contains(edge.Source) || dummyMembers.Contains(edge.Target)
                then 
                    let dummySrc  = dummys.Where(fun f->f.Members.Contains(edge.Source)).FirstOrDefault()
                    let dummyTgt  = dummys.Where(fun f->f.Members.Contains(edge.Target)).FirstOrDefault()
                    let src = if dummySrc.IsNull() then  dicV.[edge.Source] else dummySrc.CreateDummy();
                    let tgt = if dummyTgt.IsNull() then  dicV.[edge.Target] else dummyTgt.CreateDummy();
           
                    ModelingEdgeInfo(src, edge.EdgeSymbol, tgt)
                else 
                    ModelingEdgeInfo(dicV.[edge.Source], edge.EdgeSymbol, dicV.[edge.Target])
                
            newNode.Edges.Add(viewEdge) |>ignore
            )

        newNode


    let rec convertRuntimeEdge(graph:Graph<Vertex, Edge>)  =
        let newNode = ViewNode()
        let dicV = graph.Vertices.Select(fun v-> v, ViewNode(v)) |> dict

        graph.Islands
        |>Seq.iter(fun vertex -> 
                match vertex  with
                | :? Real as r ->  newNode.Singles.Add(convertRuntimeEdge(r.Graph)) |>ignore
                | :? Call | :? Alias-> newNode.Singles.Add(dicV.[vertex]) |>ignore
                | _ -> failwithf "vertex type ERROR" )

        graph.Edges
        |>Seq.iter(fun edge -> 
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
                    let flowNode = convertFlowEdge(flow, dummys)
                    flowNode.Page <- page
                    flowNode.Flow <- Some(flow)
                    flowNode
                    )
                                    
