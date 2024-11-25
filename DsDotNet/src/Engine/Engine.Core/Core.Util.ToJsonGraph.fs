namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Reflection
open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<AutoOpen>]
module internal ToJsonGraphModule =

    /// 공통적으로 JProperty 배열에서 JObject를 생성하는 유틸리티 함수
    let createJson properties = JObject(properties |> Array.ofSeq)

    /// Edge를 JSON으로 변환
    let edgeToJson (edge: Edge) =
        createJson [
            JProperty("source", JValue(edge.Source.QualifiedName))
            JProperty("target", JValue(edge.Target.QualifiedName))
            JProperty("symbol", JValue(edge.EdgeType.ToText()))
        ]

    /// AliasDefs를 JSON으로 변환
    let aliasDefsToJson (flow: Flow) =
        let aliasDefs = flow.AliasDefs
        aliasDefs.Values
        |> Seq.map (fun aliasDef ->
            let aliasTexts = 
                aliasDef.AliasTexts
                |> Seq.map (fun text -> JValue(text.QuoteOnDemand()))
                |> JArray

            let aliasTarget = 
                match aliasDef.AliasTarget with
                | Some(DuAliasTargetReal real) -> real.GetAliasTargetToDs(flow).CombineQuoteOnDemand()
                | Some(DuAliasTargetCall call) -> call.GetAliasTargetToDs(flow).CombineQuoteOnDemand()
                | None -> "ERROR"

            createJson [
                JProperty("aliasKey", JValue(aliasTarget))
                JProperty("texts", aliasTexts :> JToken)
            ]
        )
        |> JArray

    /// Graph 데이터를 JSON으로 변환
    let graphToJson (graph: DsGraph) =
        let vertices = 
            graph.Vertices
            |> Seq.map (fun vertex ->
                createJson [
                    JProperty("name", JValue(vertex.QualifiedName))
                ]
            )
            |> JArray

        let edges = 
            graph.Edges
            |> Seq.map edgeToJson
            |> JArray

        vertices, edges

    /// Real 객체를 JSON으로 변환
    let realToJson (real: Real) =
        let vertices, edges = graphToJson real.Graph
        createJson [
            JProperty("name", JValue(real.QualifiedName))
            JProperty("type", JValue("Real"))
            JProperty("vertices", vertices :> JToken)
            JProperty("edges", edges :> JToken)
        ]

    /// Vertex를 JSON으로 변환
    let vertexToJson (vertex: Vertex) =
        match vertex with
        | :? Real as real -> realToJson real
        | _ ->
            createJson [
                JProperty("name", JValue(vertex.QualifiedName))
                JProperty("type", JValue("Vertex"))
            ]

    /// Flow를 JSON으로 변환
    let flowToJson (flow: Flow) =
        let vertices = 
            flow.Graph.Vertices
            |> Seq.map vertexToJson
            |> JArray

        let edges = 
            flow.Graph.Edges
            |> Seq.map edgeToJson
            |> JArray

        let aliases = aliasDefsToJson flow

        createJson [
            JProperty("name", JValue(flow.QualifiedName))
            JProperty("type", JValue("Flow"))
            JProperty("vertices", vertices :> JToken)
            JProperty("edges", edges :> JToken)
            JProperty("aliases", aliases :> JToken)
        ]

    /// 모든 Flow를 JSON으로 변환
    let flowsToJson (flows: Flow seq) =
        flows
        |> Seq.map flowToJson
        |> JArray

    /// 전체 시스템을 JSON으로 변환
    let systemToJsonGraph (system: DsSystem) =
        createJson [
            JProperty("name", JValue(system.QualifiedName))
            JProperty("flows", flowsToJson system.Flows :> JToken)
        ]

[<Extension>]
type SystemToJsonGraphExt =
    /// DsSystem을 JSON 문자열로 변환
    [<Extension>]
    static member ToJsonGraph (system: DsSystem) =
        let jsonGraph = systemToJsonGraph system
        JsonConvert.SerializeObject(jsonGraph, Formatting.Indented)
