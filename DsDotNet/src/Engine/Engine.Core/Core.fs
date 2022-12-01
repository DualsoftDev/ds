// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Common.FS

[<AutoOpen>]
module CoreModule =
    /// Creates FQDN(Fully Qualified Domain Name) object
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
        let apiUsages = ResizeArray<ApiItem>()
        let addApiItemsForDevice (device: LoadedSystem) = device.ReferenceSystem.ApiItems |> apiUsages.AddRange

        member val Flows   = createNamedHashSet<Flow>()
        member val Jobs    = ResizeArray<Job>()

        member _.AddDevice(dev) = devices.Add(dev) |> ignore; addApiItemsForDevice dev
        member val Devices = devices |> seq
        member val Variables = ResizeArray<Variable>()
        member val Commands = ResizeArray<Command>()
        member val Observes = ResizeArray<Observe>()

        member val ApiItems = createNamedHashSet<ApiItem>()
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
        member _.ParentNPureNames = ([parent.GetCore().Name] @ names).ToArray()
        override x.GetRelativeName(referencePath:Fqdn) = x.PureNames.Combine()

    // Subclasses = {Call | Real}
    type ISafetyConditoinHolder =
        abstract member SafetyConditions: HashSet<SafetyCondition>
    
    /// Indirect to Call/Alias
    [<AbstractClass>]
    type Indirect (names:string seq, parent:ParentWrapper) =
        inherit Vertex(names |> Array.ofSeq, parent)
        new (name, parent) = Indirect([name], parent)

    /// Segment (DS Basic Unit)
    [<DebuggerDisplay("{QualifiedName}")>]
    type Real private (name:string, flow:Flow) =
        inherit Vertex([|name|], ParentFlow flow)

        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member val Flow = flow
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and RealOtherFlow private (names:Fqdn, target:Real, parent) =
        inherit Indirect(names, parent)
        member val Real = target

    and Call private (target:Job, parent) =
        inherit Indirect(target.Name, parent)
        member val CallTarget = target
        member val Xywh:Xywh = null with get, set
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and Alias private (name:string, target:AliasTargetWrapper, parent) = // target : Real or Call or OtherFlowReal
        inherit Indirect(name, parent)
        member val ApiTarget = target

    /// JobDefs 정의: Call 이 호출하는 Job 항목
    type Job (name:string, apiItems:JobDef seq) =
        inherit Named(name)
        member val ApiItems = apiItems.ToFSharpList()
    

    type TagAddress = string
    /// Main system 에서 loading 된 다른 system 의 API 를 바라보는 관점.  [jobs] = { Ap = { A."+"(%Q1, %I1); } }
    type JobDef (api:ApiItem, outTag:TagAddress, inTag:TagAddress, deviceName:string) =
        member _.ApiItem = api
        member val InTag   = inTag
        member val OutTag  = outTag
        member val ApiName = getRawName [deviceName;api.Name] true

    /// 자신을 export 하는 관점에서 본 api's.  Interface 정의.   [interfaces] = { "+" = { F.Vp ~ F.Sp } }
    and ApiItem private (name:string, system:DsSystem) =
        (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        interface INamedVertex

        member val Name = name
        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()
        member _.System = system

    //and ApiUsage(loadedSystemName:string, api: ApiItem) =
    //    inherit FqdnObject(api.Name, createFqdnObject([|loadedSystemName|]))
    //    member _.ApiItem = api

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
        | ParentFlow of Flow //Real/Call/Alias 의 부모
        | ParentReal of Real //Call/Alias      의 부모

    and AliasTargetWrapper =
        | AliasTargetReal of Real
        | AliasTargetRealOtherFlow of RealOtherFlow
        | AliasTargetCall of Call

    and SafetyCondition =
        | SafetyConditionReal of Real
        | SafetyConditionRealOtherFlow of RealOtherFlow
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

    type RealOtherFlow with
        static member Create(otherFlowReal:Real, parent:ParentWrapper) =
            let ofn, ofrn = otherFlowReal.Flow.Name, otherFlowReal.Name
            let v = RealOtherFlow( [| ofn; ofrn |], otherFlowReal, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated other flow real call [{ofn}.{ofrn}]"
            v

    type Call with
        static member Create(target:Job, parent:ParentWrapper) =
            let v = Call(target, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated call name [{target.Name}]"
            v

    type Alias with
        static member Create(name:string, target:AliasTargetWrapper, parent:ParentWrapper) =
            let createAliasDefOnDemand() =
                (* <*.ds> 파일에서 생성하는 경우는 alias 정의가 먼저 선행되지만,
                 * 메모리에서 생성해 나가는 경우는 alias 정의가 없으므로 거꾸로 채워나가야 한다.
                 *)
                let flow:Flow = parent.GetFlow()
                let aliasKey =
                    match target with
                    | AliasTargetReal r -> r.GetAliasTargetToDs(flow)
                    | AliasTargetCall c -> c.GetAliasTargetToDs()
                let ads = flow.AliasDefs
                match ads.TryFind(aliasKey) with
                | Some ad -> ad.Mnemonincs.AddIfNotContains(name) |> ignore
                | None -> ads.Add(aliasKey, AliasDef(aliasKey, Some target, [|name|]))

            createAliasDefOnDemand()
            let v = Alias(name, target, parent)
            parent.GetGraph().AddVertex(v) |> verifyM $"Duplicated alias name [{name}]"
            v

    type ApiItem with
        member x.AddTXs(txs:Real seq) = txs |> Seq.forall(fun tx -> x.TXs.Add(tx))
        member x.AddRXs(rxs:Real seq) = rxs |> Seq.forall(fun rx -> x.RXs.Add(rx))
        static member Create(name, system) =
            let cp = ApiItem(name, system)
            system.ApiItems.Add(cp) |> verifyM $"Duplicated interface prototype name [{name}]"
            cp
        static member Create(name, system, txs, rxs) =
            let ai4e = ApiItem.Create(name, system)
            ai4e.AddTXs txs |> ignore
            ai4e.AddRXs rxs |> ignore
            ai4e

    type SafetyCondition with
        member x.Core:obj =
            match x with
            | SafetyConditionReal real -> real
            | SafetyConditionCall call -> call

    type ParentWrapper with
        member x.GetCore() =
            match x with
            | ParentFlow f -> f :> FqdnObject
            | ParentReal r -> r
        member x.GetFlow() =
            match x with
            | ParentFlow f -> f
            | ParentReal r -> r.Flow

        member x.GetSystem() =
            match x with
            | ParentFlow f -> f.System
            | ParentReal r -> r.Flow.System

        member x.GetGraph():DsGraph =
            match x with
            | ParentFlow f -> f.Graph
            | ParentReal r -> r.Graph

        member x.GetModelingEdges() =
            match x with
            | ParentFlow f -> f.ModelingEdges
            | ParentReal r -> r.ModelingEdges

    type Call with
        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow as f -> [x.Name].ToArray()
                | :? Real as r -> x.ParentNPureNames
                | _->failwith "Error"

    type Real with
        member x.GetAliasTargetToDs(aliasFlow:Flow) =
                if x.Flow <> aliasFlow
                then [|x.Flow.Name; x.Name|]  //other flow
                else [| x.Name |]             //my    flow

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
