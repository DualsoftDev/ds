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
module rec ToJsonGraphModule =

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
                | Some(DuAliasTargetReal real) -> real.QualifiedName
                | Some(DuAliasTargetCall call) -> call.QualifiedName
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
            |> Seq.map vertexToJson
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
    /// Call 객체를 JSON으로 변환
    let callToJson (call: Call) =
        // TaskDefs를 JSON으로 변환
        let taskDevs = 
            call.TaskDefs
            |> Seq.map (fun td ->
                createJson [
                    JProperty("name", JValue(td.QualifiedName))
                    JProperty("type", JValue("TaskDev"))
                ]
            )
            |> JArray

        createJson [
            JProperty("name", JValue(call.QualifiedName))
            JProperty("type", JValue("Call"))
            JProperty("taskDevs", taskDevs :> JToken) // Tasks 추가
        ]


    /// Vertex를 JSON으로 변환
    let vertexToJson (vertex: Vertex) =
        match vertex with
        | :? Real as real -> realToJson real
        | :? Call as call -> callToJson call
        | _ ->
            createJson [
                JProperty("name", JValue(vertex.QualifiedName))
                JProperty("type", JValue("Vertex"))
            ]

    /// Flow를 JSON으로 변환
    let flowToJson (flow: Flow) =
        let vertices, edges = graphToJson flow.Graph
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

    /// 공용 데이터 수집 및 JSON 변환 함수
    let collectAndConvertToJson 
        (system: DsSystem)
        (predicate: ISafetyAutoPreRequisiteHolder -> bool) // 조건 필터
        (getName: ISafetyAutoPreRequisiteHolder -> string) // 이름 추출
        (getConditions: ISafetyAutoPreRequisiteHolder -> seq<string>) // 조건 추출
        =
        // Collect holders that match the predicate
        let holders =
            [
                for f in system.Flows do
                    yield! f.Graph.Vertices.OfType<ISafetyAutoPreRequisiteHolder>()
                    for r in f.Graph.Vertices.OfType<Real>() do
                        yield! r.Graph.Vertices.OfType<ISafetyAutoPreRequisiteHolder>()
            ]
            |> List.distinct
            |> List.filter predicate

        // Convert holders to JSON
        JArray(
            holders
            |> Seq.map (fun holder ->
                let name = getName holder
                let conditions = getConditions holder |> Seq.toList
                JObject(
                    JProperty("Holder", name),
                    JProperty("Conditions", JArray(conditions))
                )
            )
        )

    /// 시스템의 Safety 데이터를 JSON으로 변환
    let safetyToJson (system: DsSystem) =
        collectAndConvertToJson
            system
            (fun holder -> holder.SafetyConditions.Any()) // Safety 조건 필터
            (fun holder -> holder.GetCall().QualifiedName) // Holder 이름 추출
            (fun holder -> holder.SafetyConditions |> Seq.map (fun v -> v.GetCall().QualifiedName)) // Safety 조건 추출

    /// 시스템의 AutoPre 데이터를 JSON으로 변환
    let autopreToJson (system: DsSystem) =
        collectAndConvertToJson
            system
            (fun holder -> holder.AutoPreConditions.Any()) // AutoPre 조건 필터
            (fun holder -> holder.GetCall().QualifiedName) // Holder 이름 추출
            (fun holder -> holder.AutoPreConditions |> Seq.map (fun v -> v.GetCall().QualifiedName)) // AutoPre 조건 추출

    /// 전체 시스템을 JSON으로 변환
    let systemToJsonGraph (system: DsSystem) =
        createJson [
            JProperty("name", JValue(system.QualifiedName))
            JProperty("type", JValue("DsSystem"))
            JProperty("flows", flowsToJson system.Flows :> JToken)
            JProperty("autopre", autopreToJson system :> JToken)
            JProperty("safety", safetyToJson system :> JToken)
        ]

[<Extension>]
type SystemToJsonGraphExt =
    /// DsSystem을 JSON 문자열로 변환
    [<Extension>]
    static member ToJsonGraph (system: DsSystem) =
        let jsonGraph = systemToJsonGraph system
        JsonConvert.SerializeObject(jsonGraph, Formatting.Indented)
