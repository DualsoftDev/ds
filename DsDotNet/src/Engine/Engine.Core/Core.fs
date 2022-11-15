// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Runtime.CompilerServices
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    let createFqdnObject (nameComponents:string array) = {
        new IQualifiedNamed with
            member _.Name with get() = nameComponents.LastOrDefault() and set(v) = failwith "ERROR"
            member _.NameComponents = nameComponents
            member x.QualifiedName = nameComponents.Combine() }

    [<AbstractClass>]
    type LoadedSystem(name:string, referenceSystem:DsSystem, containerSystem:DsSystem) =
        inherit FqdnObject(name, containerSystem)
        /// Loading 된 system 을 포함하는 container system
        member val ContainerSystem = containerSystem
        /// Loading 된 system 참조 용
        member val ReferenceSystem = referenceSystem

        member val UserSpecifiedFilePath:string = null with get, set
        member val AbsoluteFilePath:string = null with get, set

    and Device(referenceSystem:DsSystem, containerSystem:DsSystem) =
        inherit LoadedSystem(referenceSystem.Name, referenceSystem, containerSystem)

    and ExternalSystem(name:string, referenceSystem:DsSystem, containerSystem:DsSystem) =
        inherit LoadedSystem(name, referenceSystem, containerSystem)

    type DsSystem private (name:string, host:string) =
        inherit FqdnObject(name, createFqdnObject([||]))

        member val Devices = createNamedHashSet<LoadedSystem>()
        member val Variables = ResizeArray<Variable>()
        member val Commands = ResizeArray<Command>()
        member val Observes = ResizeArray<Observe>()

        member val Flows    = createNamedHashSet<Flow>()
        member val ApiItems4Export = createNamedHashSet<ApiItem>()
        member x.ApiItems = x.Devices.Collect(fun d -> d.ReferenceSystem.ApiItems4Export)
        member val ApiResetInfos = HashSet<ApiResetInfo>() with get, set
        ///시스템 전체시작 버튼누름시 수행되야하는 Real목록
        member val StartPoints = createQualifiedNamedHashSet<Real>()

        member _.Host = host

        ///시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ButtonDic()
        member val AutoButtons      = ButtonDic()
        member val StartButtons     = ButtonDic()
        member val ResetButtons     = ButtonDic()

        /// API name -> Address map.  A.+ = (%Q1234.2343, %I1234.2343)
        member val ApiAddressMap    = Dictionary<string[], Addresses>(nameComponentsComparer())


        ///시스템 핸들링 대상여부   true : mySystem / false : exSystem
        member val Active = false with get, set

        static member Create(name, host) =
            let system = DsSystem(name, host)
            system

    type Flow private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member val AliasMap = Dictionary<Fqdn, HashSet<string>>(nameComponentsComparer())

        member x.System = system
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"Duplicated flow name [{name}]"
            flow


    /// leaf or stem(parenting)
    /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, Call
    type [<AbstractClass>]
        Vertex (names:Fqdn, parent:ParentWrapper) =
        inherit FqdnObject(names.Combine(), parent.GetCore())

        interface INamedVertex
        member _.Parent = parent
        member _.PureNames = names
        override x.GetRelativeName(referencePath:Fqdn) = x.PureNames.Combine()

    /// Segment (DS Basic Unit)
    and [<DebuggerDisplay("{QualifiedName}")>]
        Real private (name:string, flow:Flow) =
        inherit Vertex([|name|], Flow flow)
        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member val Flow = flow

        member val SafetyConditions = HashSet<SafetyCondition>()
        static member Create(name:string, flow) =
            if (name.Contains ".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let segment = Real(name, flow)
            flow.Graph.AddVertex(segment) |> verifyM $"Duplicated segment name [{name}]"
            segment

    and SafetyCondition =
        | SafetyConditionReal of Real
        | SafetyConditionCall of Call


    and AliasTargetType =
        | RealTarget of Real
        | CallTarget of Call

    and Alias private (mnemonic:string, target:AliasTargetType, parent:ParentWrapper) =
        inherit Vertex([|mnemonic|], parent)

        static let tryFindAlias (graph:DsGraph) (mnemonic:string) =
            let existing = graph.TryFindVertex(mnemonic)
            match existing with
            | Some (:? Alias as a) -> Some a
            | Some v -> failwith "Alias name is already used by other vertex"
            | None -> None

        static let addAlias(flow:Flow, target:Fqdn, alias:string) =
            let map = flow.AliasMap
            if map.ContainsKey target then
                map[target].Add(alias) |> verifyM $"Duplicated alias name in AliasMap [{alias}]"
            else
                map.Add(target, HashSet[|alias|]) |>ignore

        member x.Target = target

        override x.GetRelativeName(referencePath:Fqdn) =
            match target with
            | RealTarget r -> x.Name
            | CallTarget c -> base.GetRelativeName(referencePath)

        static member Create(mnemonic, target:AliasTargetType, parent:ParentWrapper, skipAddFlowMap:bool) =
            let graph:DsGraph = parent.GetGraph()
            let creator() =
                let alias = Alias(mnemonic, target, parent)
                graph.AddVertex(alias) |> verifyM $"Duplicated child name [{mnemonic}]"
                if not skipAddFlowMap then
                    match target with
                    | RealTarget r -> addAlias(r.Flow, r.NameComponents.Skip(2).ToArray(), mnemonic)
                    | CallTarget c ->
                        match c.Parent with
                        | Real rParent -> addAlias(rParent.Flow, c.NameComponents.Skip(3).ToArray(), mnemonic)
                        | Flow fParent -> addAlias(fParent, c.NameComponents.Skip(2).ToArray(), mnemonic)
                alias

            let existing = tryFindAlias graph mnemonic
            match existing with
            | Some a -> a
            | _ -> creator()



    /// 외부 시스템 호출 객체
    and Call private (apiItem:ApiItem, parent:ParentWrapper) =
        inherit Vertex(apiItem.NameComponents, parent)
        static let create (graph:DsGraph) (apiItem:ApiItem) (parent:ParentWrapper) =
            let existing = graph.TryFindVertex(apiItem.QualifiedName)
            match existing with
            | Some (:? Call as v) -> v
            | Some v ->
                failwith $"Duplicated call name [{apiItem.QualifiedName}]"
            | _ ->
                let call = Call(apiItem, parent)
                graph.AddVertex(call) |> verifyM $"Duplicated call name [{apiItem.QualifiedName}]"
                call

        member _.ApiItem = apiItem
        member val Addresses:Addresses = null with get, set

        static member Create(apiItem:ApiItem, parent:ParentWrapper) = create (parent.GetGraph()) apiItem parent

        /// Graph 에 포함되지 않는 core.  Alias 에 숨은 core
        static member CreateNowhere(apiItem:ApiItem, parent:ParentWrapper) = Call(apiItem, parent)


    type ApiItem private (name:string, system:DsSystem) =
        (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        interface INamedVertex

        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()
        member x.AddTXs(txs:Real seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Real seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        member _.System = system
        member val Xywh:Xywh = null with get, set

        static member Create(name, system) =
            let cp = ApiItem(name, system)
            system.ApiItems4Export.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp

    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (system:DsSystem, operand1:string, operator:ModelingEdgeType, operand2:string) =
        member val Operand1 = operand1  // "+"
        member val Operand2 = operand2  // "-"
        member val Operator = operator  // "<||>"
        member x.ToDsText() = sprintf "%s %s %s" operand1 (operator.ToText()) operand2  //"+" <||> "-"
        static member Create(system, operand1, operator, operand2) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.ApiResetInfos.Add(ri) |> verifyM $"Duplicated interface ResetInfo [{ri.ToDsText()}]"
            ri


    ///Vertex의 부모의 타입을 구분한다.
    type ParentWrapper =
        | Flow of Flow //Real/Call/Alias 의 부모
        | Real of Real //Call/Alias      의 부모

    and DsGraph = Graph<Vertex, Edge>

    and ButtonDic = Dictionary<string, HashSet<Flow>>

    and Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
        inherit EdgeBase<Vertex>(source, target, edgeType)

        static member Create(graph:Graph<_,_>, source, target, edgeType:EdgeType) =
            let edge = Edge(source, target, edgeType)
            graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge

        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"


    (*
     * Extension methods
     *)

    type SafetyCondition with
        member x.ToText() =
            match x with
            | SafetyConditionReal real -> [real.Flow.Name; real.Name].Combine()
            | SafetyConditionCall call -> call.NameComponents.Combine()

    type ParentWrapper with
        member x.GetCore() =
            match x with
            | Flow f -> f :> FqdnObject
            | Real r -> r

        member x.GetSystem() =
            match x with
            | Flow f -> f.System
            | Real r -> r.Flow.System

        member x.GetGraph() =
            match x with
            | Flow f -> f.Graph
            | Real r -> r.Graph

        member x.GetModelingEdges() =
            match x with
            | Flow f -> f.ModelingEdges
            | Real r -> r.ModelingEdges


[<Extension>]
type CoreExt =
    [<Extension>] static member GetSystem(call:Call) = call.Parent.GetSystem()

    [<Extension>]
    static member AddModelEdge(flow:Flow, source:string, edgetext:string, target:string) =
        let src = flow.Graph.Vertices.Find(fun f->f.Name = source)
        let tgt = flow.Graph.Vertices.Find(fun f->f.Name = target)
        let modelingEdgeInfo = ModelingEdgeInfo(src, edgetext, tgt)
        flow.ModelingEdges.Add(modelingEdgeInfo) |> verifyM $"Duplicated edge [{src.Name}{edgetext}{tgt.Name}]"

    [<Extension>]
    static member AddModelEdge(flow:Flow, source:Vertex, modelEdgeType:ModelingEdgeType, target:Vertex) =
        let modelingEdgeInfo = ModelingEdgeInfo(source, modelEdgeType.ToText(), target)
        flow.ModelingEdges.Add(modelingEdgeInfo) |> verifyM $"Duplicated edge [{source.Name}{modelEdgeType.ToText()}{target.Name}]"

    [<Extension>]
    static member AddButton(sys:DsSystem, btnType:BtnType, btnName: string, flow:Flow) =
        if sys <> flow.System then failwithf $"button [{btnName}] in flow ({flow.System.Name} != {sys.Name}) is not same system"
        let dicButton =
            match btnType with
            | StartBTN       -> sys.StartButtons
            | ResetBTN       -> sys.ResetButtons
            | EmergencyBTN   -> sys.EmergencyButtons
            | AutoBTN        -> sys.AutoButtons

        if dicButton.ContainsKey btnName then
            dicButton.[btnName].Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
        else
            dicButton.Add(btnName, HashSet[|flow|] )

