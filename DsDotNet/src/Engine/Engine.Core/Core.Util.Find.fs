namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open System
open System.Collections.Generic

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(model:Model, fqdn:NameComponents) : obj =
        let n = fqdn.Length
        match n with
        | 0 -> failwith "ERROR: name not given"
        | 1 -> model.Systems.First(fun sys -> sys.Name = fqdn[0])
        | 2 -> model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1])
        | 3 -> model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2])
        | 4 ->
            let seg = model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2]) :?> RealInFlow
            seg.Graph.FindVertex(fqdn[3])
        | _ -> failwith "ERROR"
    let findGraphVertexT<'V when 'V :> IVertex>(model:Model, fqdn:NameComponents) =
        let v = findGraphVertex(model, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findApiItem(model:Model, apiPath:NameComponents) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        let sys = model.Systems.First(fun sys -> sys.Name = apiPath[0])
        let x = sys.Api.Items.FindWithName(apiKey)
        x

    let findSystem(model:Model, systemName:string) =
        model.Systems.First(fun sys -> sys.Name = systemName)

[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(model:Model, fqdn:NameComponents) = findGraphVertex(model, fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(model:Model, fqdn:NameComponents) = findGraphVertexT<'V>(model, fqdn)
    [<Extension>] static member FindApiItem(model:Model, apiPath:NameComponents) = findApiItem(model, apiPath)
    [<Extension>] static member FindSystem(model:Model, systemName:string) = findSystem(model, systemName)


//// system copy : 
//// 1. 메모리를 구조적으로 생성하는 방법
//// 2. 기존 source system 을 ds 언어로 dump 한 후, 이를 재 parsing 하는 방법
//[<AutoOpen>]
//module internal CopySystemModule =
//    let copyApiItem(source:ApiItem) =
//        let s = source
//        let apiItem = ApiItem.Create(s.Name, s.System)
//        apiItem
//    let copySegment(source:SegmentBase, targetFlow:Flow) : SegmentBase =
//        let s = source
//        match s with
//        | :? Segment ->
//            Segment.Create(s.Name, targetFlow)
//        | :? InFlowAlias as ali ->
//            InFlowAlias.Create(s.Name, targetFlow, ali.AliasKey)
//        | :? InFlowApiCall as call ->
//            let apiItem = copyApiItem(call.ApiItem)
//            InFlowApiCall.Create(apiItem, targetFlow)
//        | _ ->
//            failwith "ERROR"
        
//    let copyFlow(source:Flow, targetSystem:DsSystem) =
//        let s = source
//        let sg = s.Graph
//        let flow = Flow.Create(s.Name, targetSystem)
//        let dict = Dictionary<SegmentBase, SegmentBase>()
//        for v in sg.Vertices do
//            copySegment(v)

//        ()
//    let copySystem(source:DsSystem, newName:string) =
//        let s = source
//        let sys = DsSystem.Create(newName, s.Cpu, s.Model)
//        for sf in s.Flows do
//            copyFlow(sf, sys)




