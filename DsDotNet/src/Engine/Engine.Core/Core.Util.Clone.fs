namespace Engine.Core

open Dual.Common.Core.FS
open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module rec CoreCloneModule =

    type ApiItem with
        member private x.Clone(newSys: DsSystem , dicVertex:Dictionary<Vertex, Vertex>) =
            // 향후 ApiItem 항목 추가시 깊은복사 구현
            let tx, rx = dicVertex[x.TX]:?>Real, dicVertex[x.RX]:?>Real
            newSys.CreateApiItem(x.Name, tx, rx) |> ignore

    type ApiResetInfo with
        member private x.Clone(newSys: DsSystem) =
            // 향후 ApiResetInfo 항목 추가시 깊은복사 구현
            newSys.CreateApiResetInfo(x.Operand1, x.Operator, x.Operand2, x.AutoGenByFlow) |> ignore


    type Flow with
        member private x.Clone(newSystem: DsSystem) =
            let newFlow = newSystem.CreateFlow(x.Name)
            // 향후 Flow 항목 추가시 깊은복사 구현
            newFlow

    type Real with
        member private x.Clone(newFlow: Flow) =
            let newReal = newFlow.CreateReal(x.Name)
            // 향후 Real 항목 추가시 깊은복사 구현
            newReal.Motion <- x.Motion
            newReal.Script <- x.Script
            newReal.DsTime <- x.DsTime
            newReal.Finished <- x.Finished
            newReal.NoTransData <- x.NoTransData
            newReal.IsSourceToken <- x.IsSourceToken

            newReal

    type DsSystem with
        member x.Clone(newName:string) =
            if x.GetVertices().OfType<Call>().Filter(fun c -> c.IsJob).Any() then
                failwithlog "ERROR: Clone system Call is not supported"

            let news = DsSystem.Create(newName)
            let dicFlow = Dictionary<Flow, Flow>()  // old(Flow) -> new(Flow)
            let dicVertex = Dictionary<Vertex, Vertex>()  // old(Real, Alias) -> new(Real, Alias)

            // Clone Flows
            for f in x.Flows do
                let newFlow = f.Clone(news)
                news.Flows.Add(newFlow) |> ignore
                dicFlow.Add(f, newFlow) |> ignore

            // Clone Reals
            for r in x.GetVertices().OfType<Real>() do
                let newReal = r.Clone(dicFlow[r.Parent.GetFlow()])
                dicVertex.Add(r, newReal)

            // Clone Aliases
            for a in x.GetVertices().OfType<Alias>() do
                match a.TargetWrapper with
                | DuAliasTargetReal r ->
                    let newReal = dicVertex[r] :?> Real
                    let newFlow = dicFlow[a.Parent.GetFlow()]
                    // 향후 Alias 항목 추가시 깊은복사 구현
                    let newAlias = newFlow.CreateAlias(a.Name, newReal, a.IsExFlowReal)
                    dicVertex.Add(a, newAlias) |> ignore
                | _ -> failwith "ERROR"

            // Clone Flow Graph
            for f in x.Flows do
                let newFlow = dicFlow[f]
                newFlow.Graph.AddVertices(f.Graph.Islands.Select(fun v -> dicVertex[v])) |> ignore
                for edge in f.ModelingEdges do
                    let newSources = edge.Sources.Select(fun s -> dicVertex[s])
                    let newTargets = edge.Targets.Select(fun s -> dicVertex[s])
                    newFlow.CreateEdge(ModelingEdgeInfo<Vertex>(newSources, edge.EdgeType.ToText(), newTargets)) |> ignore


            x.ApiItems.Iter(fun f->f.Clone(news, dicVertex))
            x.ApiResetInfos.Iter(fun f->f.Clone(news))

            news