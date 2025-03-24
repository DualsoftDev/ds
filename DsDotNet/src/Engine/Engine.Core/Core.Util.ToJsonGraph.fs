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
    let getJsonName(vertex:Vertex) = 
            if vertex :? Alias 
            then vertex.NameComponents.CombineQuoteOnDemand()
            else vertex.QualifiedName

    /// Edge를 JSON으로 변환
    let edgeToJson (edge: Edge) = 
        createJson [
            JProperty("Source", JValue(getJsonName edge.Source))
            JProperty("Target", JValue(getJsonName edge.Target))
            JProperty("Symbol", JValue(edge.EdgeType.ToText()))
        ]

    /// AliasDefs를 JSON으로 변환
    let aliasDefsToJson (flow: Flow) =
        let aliasDefs = flow.AliasDefs
        aliasDefs.Values
        |> Seq.map (fun aliasDef ->
            let aliasTexts = 
                aliasDef.AliasTexts
                |> Seq.map (fun text -> JValue($"{flow.QualifiedName}.{text.QuoteOnDemand()}"))
                |> JArray
                
            let aliasTarget = 
                match aliasDef.AliasTarget with
                | Some(DuAliasTargetReal real) -> real.QualifiedName
                | Some(DuAliasTargetCall call) -> call.QualifiedName
                | None -> "ERROR"

            createJson [
                JProperty("AliasHolder", JValue(aliasTarget))
                JProperty("Texts", aliasTexts :> JToken)
            ]
        )
        |> JArray

        
    /// system  ISafetyAutoPreRequisiteHolder JSON 변환 함수
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
                    JProperty("Holder", JValue(name)),
                    JProperty("Conditions", JArray(conditions))
                )
            )
        )


    let callISafetyAutoPreConvertToJson (items:HashSet<SafetyAutoPreCondition>) (name:string)  =
        JObject(
            JProperty("Holder", JValue(name)),
            JProperty("Conditions"
                , JArray(items
                            .SelectMany(fun s->
                                let sys = s.GetCall().System
                                let job = s.GetCall().TargetJob
                                let calls = sys.GetVerticesOfJobCalls().Where(fun c->c.TargetJob = job)
                                calls.Select(fun c -> c.QualifiedName)
                                )
                            )
                    )
            )

    let callToJson (call: Call) =
        let taskDevCalls = call.System.GetTaskDevCalls() |> dict
        let taskDevs = call.TaskDefs |> Seq.collect (fun td -> taskDevCalls[td].Select(fun c->c.QualifiedName))
          

        JObject(
            JProperty("Name", JValue(getJsonName call)),
            JProperty("Type", JValue("Call")),
            JProperty("TaskDevNames", taskDevs),
            JProperty(
                "AutoPre", 
                if call.AutoPreConditions.any() 
                then 
                    callISafetyAutoPreConvertToJson 
                        call.AutoPreConditions 
                        call.QualifiedName 
                else null
            ),
            JProperty(
                "Safety", 
                if call.SafetyConditions.any() 
                then 
                    callISafetyAutoPreConvertToJson 
                        call.SafetyConditions 
                        call.QualifiedName 
                else null
            )
        )


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
            JProperty("Name", JValue(getJsonName real))
            JProperty("Type", JValue("Real"))
            JProperty("Vertices", vertices :> JToken)
            JProperty("Edges", edges :> JToken)
        ]
   
    /// Vertex를 JSON으로 변환
    let vertexToJson (vertex: Vertex) =
        match vertex with
        | :? Real as real -> realToJson real
        | :? Call as call -> callToJson call
        | _ ->
            createJson [
                JProperty("Name", JValue(vertex.QualifiedName))
                JProperty("Type", JValue("Vertex"))
            ]

    /// Flow를 JSON으로 변환
    let flowToJson (flow: Flow) =
        let vertices, edges = graphToJson flow.Graph
        let aliases = aliasDefsToJson flow

        createJson [
            JProperty("Name", JValue(flow.QualifiedName))
            JProperty("Type", JValue("Flow"))
            JProperty("Vertices", vertices :> JToken)
            JProperty("Edges", edges :> JToken)
            JProperty("Aliases", aliases :> JToken)
        ]

    /// 모든 Flow를 JSON으로 변환
    let flowsToJson (flows: Flow seq) =
        flows
        |> Seq.map flowToJson
        |> JArray


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
    
    /// 시스템의 Devices 데이터를 JSON으로 변환
    let deviceToJson (system: DsSystem) =
        let taskDevCalls = system.GetTaskDevCalls() |> dict
        let getDevTasks(device:Device) =
            system.GetTaskDevsCoin()
                |> Seq.filter (fun (td, _c)-> td.DeviceName = device.Name)
                |> Seq.map (fun (td, _c)-> 
                    let calls = taskDevCalls[td].Select(fun c->c.QualifiedName)|> JArray
                    createJson [
                         JProperty("Name", JValue(td.QualifiedName))
                         JProperty("Type", JValue("TaskDev"))
                         JProperty("Calls", calls :> JToken)
                    ] ) 
                |> JArray

        let getContainFlows(device:Device) =
            let calls = system.GetTaskDevCalls()
            let taskDevs = system.GetTaskDevs() |> Seq.filter (fun (td, _c)-> td.DeviceName = device.Name)
                                                |> Seq.map fst
            let containCalls =
                calls |> Seq.filter (fun (td, _call)-> taskDevs.Contains(td))
                      |> Seq.collect snd

            containCalls
                |> Seq.map (fun v -> JValue(v.Parent.GetFlow().Name))
                |> Seq.distinct 
                |> JArray

        system.Devices
        |> Seq.map (fun dev ->
            createJson [
                JProperty("Name", JValue(dev.Name))
                JProperty("Type", JValue("Device"))
                JProperty("Path", JValue(dev.AbsoluteFilePath))
                JProperty("TaskDevs", getDevTasks dev  :> JToken)
                JProperty("Flows", getContainFlows dev  :> JToken)
            ] ) 
        |> JArray

    /// 전체 시스템을 JSON으로 변환
    let systemToJsonGraph (system: DsSystem) =
        createJson [
            JProperty("Name", JValue(system.QualifiedName))
            JProperty("Type", JValue("DsSystem"))
            JProperty("Flows", flowsToJson system.Flows :> JToken)
            JProperty("Autopre", autopreToJson system :> JToken)
            JProperty("Safety", safetyToJson system :> JToken)
            JProperty("Devices", deviceToJson system :> JToken)
        ]

[<Extension>]
type SystemToJsonGraphExt =
    /// DsSystem을 JSON 문자열로 변환
    [<Extension>]
    static member ToJsonGraph (system: DsSystem) =
        let jsonGraph = systemToJsonGraph system
        JsonConvert.SerializeObject(jsonGraph, Formatting.Indented)
