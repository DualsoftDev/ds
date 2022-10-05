namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open System
open System.Collections.Generic

// system copy : 
// 1. 메모리를 구조적으로 생성하는 방법
// 2. 기존 source system 을 ds 언어로 dump 한 후, 이를 재 parsing 하는 방법

[<AutoOpen>]
module internal ToDsTextModule =
    let segmentToDs(segmentBase:SegmentBase) =
        match segmentBase with
        | :? Segment as segment ->
            for v in segment.Graph.Vertices do
                ()
        | :? SegmentAlias as ali ->
            ()//SegmentAlias.Create(ali.Name, targetFlow, ali.AliasKey)
        | :? SegmentApiCall as call ->
            //let apiItem = copyApiItem(call.ApiItem)
            //SegmentApiCall.Create(apiItem, targetFlow)
            ()
        | _ ->
            failwith "ERROR"

    let flowGraphToDs(graph:Graph<SegmentBase, InFlowEdge>) =
        for v in graph.Vertices do
            segmentToDs v
    let flowToDs(flow:Flow) =
        flowGraphToDs(flow.Graph)
    let systemToDs(system:DsSystem) =
        for f in system.Flows do
            flowToDs f
    let modelToDs (model:Model) =
        for s in model.Systems do
            systemToDs s



