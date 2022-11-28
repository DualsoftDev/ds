// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    let createFqdnObject (nameComponents:string array) = {
        new IQualifiedNamed with
            member _.Name with get() = nameComponents.LastOrDefault() and set(v) = failwith "ERROR"
            member _.NameComponents = nameComponents
            member x.QualifiedName = nameComponents.Combine() }

    type DeviceLoadParameters = {
        /// Loading 된 system 입장에 자신을 포함하는 container system
        ContainerSystem        : DsSystem
        AbsoluteFilePath       : string
        /// Loading 을 위해서 사용자가 지정한 file path.  serialize 시, 절대 path 를 사용하지 않기 위한 용도로 사용된다.
        UserSpecifiedFilePath  : string
        /// *.ds 에 정의된 이름과 loading 할 때의 이름은 다를 수 있다.
        LoadedName             : string
    }

    [<AbstractClass>]
    type LoadedSystem(loadedSystem:DsSystem, param:DeviceLoadParameters) =
        inherit FqdnObject(param.LoadedName, param.ContainerSystem)
        /// 다른 device 을 Loading 하려는 system 입장에서 loading 된 system 참조 용
        member _.ReferenceSystem = loadedSystem

        /// Loading 된 system 입장에 자신을 포함하는 container system
        member _.ContainerSystem = param.ContainerSystem
        /// Loading 을 위해서 사용자가 지정한 file path.  serialize 시, 절대 path 를 사용하지 않기 위한 용도로 사용된다.
        member _.UserSpecifiedFilePath:string = param.UserSpecifiedFilePath
        member _.AbsoluteFilePath:string = param.AbsoluteFilePath

    /// *.ds file 을 읽어 들여서 새로운 instance 를 만들어 넣기 위한 구조
    and Device(loadedDevice:DsSystem, param:DeviceLoadParameters) =
        inherit LoadedSystem(loadedDevice, param)

    /// shared instance.  *.ds file 의 절대 경로 기준으로 하나의 instance 만 생성하고 이를 참조하는 개념
    and ExternalSystem(referenceSystem:DsSystem, param:DeviceLoadParameters) =
        inherit LoadedSystem(referenceSystem, param)

    type DsSystem (name:string, host:string) =
        inherit FqdnObject(name, createFqdnObject([||]))
        let devices = createNamedHashSet<LoadedSystem>()
        let apiUsages = ResizeArray<ApiUsage>()
        let addApiItemsForDevice (device: LoadedSystem) = device.CreateApiUsages() |> apiUsages.AddRange

        member val Flows    = createNamedHashSet<Flow>()
        member val Calls    = ResizeArray<Call>()

        member _.AddDevice(dev) = devices.Add(dev) |> ignore; addApiItemsForDevice dev
        member val Devices = devices |> seq
        member val Variables = ResizeArray<Variable>()
        member val Commands = ResizeArray<Command>()
        member val Observes = ResizeArray<Observe>()

        member val ApiInterfaces = createNamedHashSet<ApiInterface>()
        member x.ApiUsages = apiUsages |> seq
        member val ApiResetInfos = HashSet<ApiResetInfo>() with get, set
        ///시스템 전체시작 버튼누름시 수행되야하는 Real목록
        member val StartPoints = createQualifiedNamedHashSet<Real>()

        member _.Host = host

        ///시스템 버튼 소속 Flow 정보
        member val EmergencyButtons = ButtonDic()
        member val AutoButtons      = ButtonDic()
        member val StartButtons     = ButtonDic()
        member val ResetButtons     = ButtonDic()

    type Flow private (name:string, system:DsSystem) =
        inherit FqdnObject(name, system)
        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member val AliasDefs = Dictionary<Fqdn, AliasDef>(nameComponentsComparer())

        member x.System = system
        static member Create(name:string, system:DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"Duplicated flow name [{name}]"
            flow

    and AliasDef(aliasKey:Fqdn, target:AliasTargetWrapper option, mnemonics:string []) =
        member _.AliasKey = aliasKey
        member val AliasTarget = target with get, set
        member val Mnemonincs = mnemonics |> ResizeArray



    /// leaf or stem(parenting)
    /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, Call
    [<AbstractClass>]
    type Vertex (names:Fqdn, parent:ParentWrapper) =
        inherit FqdnObject(names.Combine(), parent.GetCore())

        interface INamedVertex
        member _.Parent = parent
        member _.PureNames = names
        override x.GetRelativeName(referencePath:Fqdn) = x.PureNames.Combine()

    // Subclasses = {Call | Real}
    type ISafetyConditoinHolder =
        abstract member SafetyConditions: HashSet<SafetyCondition>

    /// Segment (DS Basic Unit)
    [<DebuggerDisplay("{QualifiedName}")>]
    type Real private (name:string, flow:Flow) =
        inherit Vertex([|name|], Flow flow)

        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member val Flow = flow
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    /// Indirect to Call/Alias
    [<AbstractClass>]
    type Indirect (names:string seq, parent:ParentWrapper) =
        inherit Vertex(names |> Array.ofSeq, parent)
        new (name, parent) = Indirect([name], parent)

    and VertexCall private (name:string, target:Call, parent) =
        inherit Indirect(name, parent)

    and VertexAlias private (name:string, target:AliasTargetWrapper, parent) = // target : Real or Call or OtherFlowReal
        inherit Indirect(name, parent)

    and VertexOtherFlowRealCall private (names:Fqdn, target:Real, parent) =
        inherit Indirect(names, parent)

    /// Call 정의:
    type Call (name:string, apiItems:ApiCallDef seq) =
        inherit Named(name)
        member val ApiItems = apiItems.ToFSharpList()
        member val Xywh:Xywh = null with get, set
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    type TagAddress = string

    /// Main system 에서 loading 된 다른 system 의 API 를 바라보는 관점.  [calls] = { Ap = { A."+"(%Q1, %I1); } }
    type ApiCallDef (api:ApiUsage, tx:TagAddress, rx:TagAddress) =
        member _.ApiInterface = api
        member val TX = tx
        member val RX = rx

    /// 자신을 export 하는 관점에서 본 api's.  Interface 정의.   [interfaces] = { "+" = { F.Vp ~ F.Sp } }
    and ApiInterface private (name:string, system:DsSystem) =
        (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        interface INamedVertex

        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()
        member _.System = system

    and ApiUsage(loadedSystemName:string, api: ApiInterface) =
        inherit FqdnObject(api.Name, createFqdnObject([|loadedSystemName|]))
        member _.ApiInterface = api

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

    and AliasTargetWrapper =
        | AliasTargetReal of Real    // MyFlow or OtherFlow 의 Real 일 수 있다.
        | AliasTargetCall of Call

    and SafetyCondition =
        | SafetyConditionReal of Real
        | SafetyConditionCall of Call


    (* Abbreviations *)

    type DsGraph = Graph<Vertex, Edge>
    and ButtonDic = Dictionary<string, HashSet<Flow>>
    and Direct = Real

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

    type Real with
        static member Create(name: string, flow) =
            if (name.Contains ".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let segment = Real(name, flow)
            flow.Graph.AddVertex(segment) |> verifyM $"Duplicated segment name [{name}]"
            segment

    type ApiInterface with
        member x.AddTXs(txs:Real seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Real seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        static member Create(name, system) =
            let cp = ApiInterface(name, system)
            system.ApiInterfaces.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp
        static member Create(name, system, txs, rxs) =
            let ai4e = ApiInterface.Create(name, system)
            ai4e.AddTXs txs |> ignore
            ai4e.AddRXs rxs |> ignore
            ai4e

    type VertexCall with
        static member Create(name:string, target:Call, parent:ParentWrapper) =
            let v = VertexCall(name, target, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated call name [{name}]"
            v
    type VertexAlias with
        static member Create(name:string, target:AliasTargetWrapper, parent:ParentWrapper) =
            let createAliasDefOnDemand() =
                (* <*.ds> 파일에서 생성하는 경우는 alias 정의가 먼저 선행되지만,
                 * 메모리에서 생성해 나가는 경우는 alias 정의가 없으므로 거꾸로 채워나가야 한다.
                 *)
                let flow:Flow = parent.GetFlow()
                let aliasKey =
                    match target with
                    | AliasTargetReal r ->
                        (if r.Flow <> flow then [|r.Flow.Name|] else [||]) @ [| r.Name |]
                    | AliasTargetCall c -> [| c.Name |]
                let ads = flow.AliasDefs
                match ads.TryFind(aliasKey) with
                | Some ad -> ad.Mnemonincs.AddIfNotContains(name) |> ignore
                | None -> ads.Add(aliasKey, AliasDef(aliasKey, Some target, [|name|]))

            createAliasDefOnDemand()
            let v = VertexAlias(name, target, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated alias name [{name}]"
            v

    type VertexOtherFlowRealCall with
        static member Create(otherFlowReal:Real, parent:ParentWrapper) =
            let ofn, ofrn = otherFlowReal.Flow.Name, otherFlowReal.Name
            let v = VertexOtherFlowRealCall( [| ofn; ofrn |], otherFlowReal, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated other flow real call [{ofn}.{ofrn}]"
            v


    type SafetyCondition with
        member x.Core:obj =
            match x with
            | SafetyConditionReal real -> real
            | SafetyConditionCall call -> call

    type ParentWrapper with
        member x.GetCore() =
            match x with
            | Flow f -> f :> FqdnObject
            | Real r -> r
        member x.GetFlow() =
            match x with
            | Flow f -> f
            | Real r -> r.Flow

        member x.GetSystem() =
            match x with
            | Flow f -> f.System
            | Real r -> r.Flow.System

        member x.GetGraph():DsGraph =
            match x with
            | Flow f -> f.Graph
            | Real r -> r.Graph

        member x.GetModelingEdges() =
            match x with
            | Flow f -> f.ModelingEdges
            | Real r -> r.ModelingEdges

    type DsSystem with
        member x.AddButton(btnType:BtnType, btnName: string, flow:Flow) =
            if x <> flow.System then failwithf $"button [{btnName}] in flow ({flow.System.Name} != {x.Name}) is not same system"
            let dicButton =
                match btnType with
                | StartBTN       -> x.StartButtons
                | ResetBTN       -> x.ResetButtons
                | EmergencyBTN   -> x.EmergencyButtons
                | AutoBTN        -> x.AutoButtons

            match dicButton.TryFind btnName with
            | Some btn -> btn.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> dicButton.Add(btnName, HashSet[|flow|] )

    type LoadedSystem with
        member x.CreateApiUsages() =
            [ for ai in x.ReferenceSystem.ApiInterfaces -> ApiUsage(x.Name, ai) ]