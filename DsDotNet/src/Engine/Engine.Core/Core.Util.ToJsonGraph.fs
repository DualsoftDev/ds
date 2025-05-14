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

    let createJson (properties: (string * obj) list) =
        dict properties :> obj

    let getJsonName (vertex: Vertex) =
        if vertex :? Alias then
            vertex.NameComponents.CombineQuoteOnDemand()
        else
            vertex.QualifiedName

    let edgeToJson (edge: Edge) =
        createJson [
            "Source", getJsonName edge.Source :> obj
            "Target", getJsonName edge.Target :> obj
            "Symbol", edge.EdgeType.ToText() :> obj
        ]

    let aliasDefsToJson (flow: Flow) =
        flow.AliasDefs.Values
        |> Seq.map (fun aliasDef ->
            let aliasTexts =
                aliasDef.AliasTexts
                |> Seq.map (fun text -> $"{flow.QualifiedName}.{text.QuoteOnDemand()}" :> obj)
                |> Seq.toList

            let aliasTarget =
                match aliasDef.AliasTarget with
                | Some(DuAliasTargetReal real) -> real.QualifiedName
                | Some(DuAliasTargetCall call) -> call.QualifiedName
                | None -> "ERROR"

            createJson [
                "AliasHolder", aliasTarget :> obj
                "Texts", aliasTexts :> obj
            ]
        )
        |> Seq.toList :> obj

    let collectAndConvertToJson (system: DsSystem) (predicate: ISafetyAutoPreRequisiteHolder -> bool) (getName: ISafetyAutoPreRequisiteHolder -> string) (getConditions: ISafetyAutoPreRequisiteHolder -> seq<string>) =
        let holders =
            [
                for f in system.Flows do
                    yield! f.Graph.Vertices.OfType<ISafetyAutoPreRequisiteHolder>()
                    for r in f.Graph.Vertices.OfType<Real>() do
                        yield! r.Graph.Vertices.OfType<ISafetyAutoPreRequisiteHolder>()
            ]
            |> List.distinct
            |> List.filter predicate

        holders
        |> Seq.map (fun holder ->
            createJson [
                "Holder", getName holder :> obj
                "Conditions", getConditions holder |> Seq.map (fun v -> v :> obj) |> Seq.toList :> obj
            ]
        )
        |> Seq.toList :> obj

    let callISafetyAutoPreConvertToJson (items: HashSet<SafetyAutoPreCondition>) (name: string) =
        createJson [
            "Holder", name :> obj
            "Conditions",
            items
            |> Seq.collect(fun s ->
                let sys = s.GetCall().System
                let job = s.GetCall().TargetJob
                sys.GetVerticesOfJobCalls()
                    |> Seq.filter(fun c -> c.TargetJob = job)
                    |> Seq.map(fun c -> c.QualifiedName :> obj)
            )
            |> Seq.toList :> obj
        ]

    let callToJson (call: Call) =
        let taskDevCalls = call.System.GetTaskDevCalls() |> dict
        let taskDevs =
            call.TaskDefs
            |> Seq.collect (fun td -> taskDevCalls[td] |> Seq.map(fun c -> c.QualifiedName :> obj))
            |> Seq.toList

        createJson [
            "Name", getJsonName call :> obj
            "Type", "Call" :> obj
            "TaskDevNames", taskDevs :> obj
            "AutoPre", if call.AutoPreConditions.any() then callISafetyAutoPreConvertToJson call.AutoPreConditions call.QualifiedName else null
            "Safety", if call.SafetyConditions.any() then callISafetyAutoPreConvertToJson call.SafetyConditions call.QualifiedName else null
        ]

    let graphToJson (graph: DsGraph) =
        let vertices =
            graph.Vertices
            |> Seq.map vertexToJson
            |> Seq.toList

        let edges =
            graph.Edges
            |> Seq.map edgeToJson
            |> Seq.toList

        vertices, edges

    let realToJson (real: Real) =
        let vertices, edges = graphToJson real.Graph
        createJson [
            "Name", getJsonName real :> obj
            "Type", "Real" :> obj
            "Vertices", vertices :> obj
            "Edges", edges :> obj
        ]

    let vertexToJson (vertex: Vertex) =
        match vertex with
        | :? Real as real -> realToJson real
        | :? Call as call -> callToJson call
        | _ ->
            createJson [
                "Name", vertex.QualifiedName :> obj
                "Type", "Vertex" :> obj
            ]

    let flowToJson (flow: Flow) =
        let vertices, edges = graphToJson flow.Graph
        let aliases = aliasDefsToJson flow

        createJson [
            "Name", flow.QualifiedName :> obj
            "Type", "Flow" :> obj
            "Vertices", vertices :> obj
            "Edges", edges :> obj
            "Aliases", aliases
        ]

    let flowsToJson (flows: Flow seq) =
        flows
        |> Seq.map flowToJson
        |> Seq.toList :> obj

    let safetyToJson (system: DsSystem) =
        collectAndConvertToJson system (fun holder -> holder.SafetyConditions.Any()) (fun holder -> holder.GetCall().QualifiedName) (fun holder -> holder.SafetyConditions |> Seq.map (fun v -> v.GetCall().QualifiedName))

    let autopreToJson (system: DsSystem) =
        collectAndConvertToJson system (fun holder -> holder.AutoPreConditions.Any()) (fun holder -> holder.GetCall().QualifiedName) (fun holder -> holder.AutoPreConditions |> Seq.map (fun v -> v.GetCall().QualifiedName))

    let deviceToJson (system: DsSystem) =
        let taskDevCalls = system.GetTaskDevCalls() |> dict

        let getDevTasks (device: Device) =
            system.GetTaskDevs()
                |> Seq.filter (fun (td, _) -> td.DeviceName = device.Name)
                |> Seq.map (fun (td, _) ->
                    let calls = taskDevCalls[td] |> Seq.map(fun c -> c.QualifiedName :> obj) |> Seq.toList
                    createJson [
                        "Name", td.QualifiedName :> obj
                        "Type", "TaskDev" :> obj
                        "Calls", calls :> obj
                    ]
                )
                |> Seq.toList

        let getContainFlows (device: Device) =
            let calls = system.GetTaskDevCalls()
            let taskDevs =
                system.GetTaskDevs()
                |> Seq.filter (fun (td, _) -> td.DeviceName = device.Name)
                |> Seq.map fst
                |> HashSet

            calls
            |> Seq.filter (fun (td, _) -> taskDevs.Contains(td))
            |> Seq.collect snd
            |> Seq.map (fun v -> v.Parent.GetFlow().Name :> obj)
            |> Seq.distinct
            |> Seq.toList

        system.Devices
        |> Seq.map (fun dev ->
            createJson [
                "Name", dev.Name :> obj
                "Type", "Device" :> obj
                "Path", dev.AbsoluteFilePath :> obj
                "TaskDevs", getDevTasks dev :> obj
                "Flows", getContainFlows dev :> obj
            ]
        )
        |> Seq.toList :> obj

    let systemToJsonGraph (system: DsSystem) =
        createJson [
            "Name", system.QualifiedName :> obj
            "Type", "DsSystem" :> obj
            "Flows", flowsToJson system.Flows
            "Autopre", autopreToJson system
            "Safety", safetyToJson system
            "Devices", deviceToJson system
        ]

[<Extension>]
type SystemToJsonGraphExt =
    [<Extension>]
    static member ToJsonGraph(system: DsSystem, ?indented: bool) =
        let jsonGraph = systemToJsonGraph system
        let formatting =
            match indented with
            | Some true -> Formatting.Indented
            | _ -> Formatting.None

        JsonConvert.SerializeObject(jsonGraph, formatting)