// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Common.FS
open System
open System.Reactive.Subjects

[<AutoOpen>]
module CoreModule =
    /// Creates FQDN(Fully Qualified Domain Name) object
    let createFqdnObject (nameComponents:string array) = {
        new IQualifiedNamed with
            member _.Name with get() = nameComponents.LastOrDefault() and set(v) = failwithlog "ERROR"
            member _.NameComponents = nameComponents
            member x.QualifiedName = nameComponents.Combine() }

    type ParserLoadingType = DuNone | DuDevice | DuExternal
    /// External system loading 시, 공유하기 위한 정보를 담을 곳
    type ShareableSystemRepository = Dictionary<string, DsSystem>

    type DeviceLoadParameters = {
        /// Loading 된 system 입장에 자신을 포함하는 container system
        ContainerSystem        : DsSystem
        AbsoluteFilePath       : string
        /// Loading 을 위해서 사용자가 지정한 file path.  serialize 시, 절대 path 를 사용하지 않기 위한 용도로 사용된다.
        UserSpecifiedFilePath  : string
        /// *.ds 에 정의된 이름과 loading 할 때의 이름은 다를 수 있다.
        LoadedName             : string
        ShareableSystemRepository: ShareableSystemRepository
        HostIp : string option
        LoadingType:ParserLoadingType
    }

    [<AbstractClass>]
    type LoadedSystem(loadedSystem:DsSystem, param:DeviceLoadParameters)  =
        inherit FqdnObject(param.LoadedName, param.ContainerSystem)
        interface ISystem with
            member val ValueChangeSubject: Subject<IStorage * obj> = new Subject<IStorage*obj>()
        //시스템단위로 이벤트 변화 처리
        member x.ValueChangeSubject = (x :> ISystem).ValueChangeSubject

        /// 다른 device 을 Loading 하려는 system 입장에서 loading 된 system 참조 용
        member _.ReferenceSystem = loadedSystem

        /// Loading 된 system 입장에 자신을 포함하는 container system
        member _.ContainerSystem = param.ContainerSystem
        /// Loading 을 위해서 사용자가 지정한 file path.  serialize 시, 절대 path 를 사용하지 않기 위한 용도로 사용된다.
        member _.UserSpecifiedFilePath:string = param.UserSpecifiedFilePath
        member _.AbsoluteFilePath:string = param.AbsoluteFilePath
        member _.LoadedName:string = param.LoadedName
        member _.OriginName:string = loadedSystem.Name

    /// *.ds file 을 읽어 들여서 새로운 instance 를 만들어 넣기 위한 구조
    and Device(loadedDevice:DsSystem, param:DeviceLoadParameters) =
        inherit LoadedSystem(loadedDevice, param)

    /// shared instance.  *.ds file 의 절대 경로 기준으로 하나의 instance 만 생성하고 이를 참조하는 개념
    and ExternalSystem(loadedSystem:DsSystem, param:DeviceLoadParameters) =
        inherit LoadedSystem(loadedSystem, param)
        member _.HostIp = param.HostIp

    type DsSystem (name:string, hostIp:string) (*, ?onCreation:DsSystem -> unit) as this*) =
        inherit FqdnObject(name, createFqdnObject([||]))
        //    // this system 객체가 생성되고 나서 수행해야 할 작업 수행.  external system loading 시, 공유하기 위한 정보를 marking
        //    onCreation.Iter(fun f -> f this)

        let loadedSystems = createNamedHashSet<LoadedSystem>()
        let apiUsages = ResizeArray<ApiItem>()
        let addApiItemsForDevice (device: LoadedSystem) = device.ReferenceSystem.ApiItems |> apiUsages.AddRange
        interface ISystem with
            member val ValueChangeSubject: Subject<IStorage * obj> =  new Subject<IStorage*obj>()
        //시스템단위로 이벤트 변화 처리
        member x.ValueChangeSubject = (x :> ISystem).ValueChangeSubject

        member val Flows   = createNamedHashSet<Flow>()
        //시스템에서 호출가능한 작업리스트 (Call => Job => ApiItems => Addresses)
        member val Jobs    = ResizeArray<Job>()


        member _.AddLoadedSystem(childSys) = loadedSystems.Add(childSys)
                                             |> verifyM $"Duplicated LoadedSystem name [{childSys.Name}]"
                                             addApiItemsForDevice childSys

        member _.ReferenceSystems = loadedSystems.Select(fun s->s.ReferenceSystem)
        member _.LoadedSystems    = loadedSystems |> seq
        member _.Devices          = loadedSystems.OfType<Device>()
        member _.ExternalSystems  = loadedSystems.OfType<ExternalSystem>()
        member _.ApiUsages = apiUsages |> seq
        member _.HostIp = hostIp

        /// 사용자 입력 code block(s).  "<@{" 와 "}@>" 사이의 text(s) : todo 복수개의 block 이 허용되면, serialize 할 때 해당 위치에 맞춰서 serialize 해야 하는데...
        member val OriginalCodeBlocks = ResizeArray<string>()
        member val Statements = ResizeArray<Statement>()
        member val Variables = ResizeArray<VariableData>()

        member val ApiItems = createNamedHashSet<ApiItem>()
        member val ApiResetInfos = HashSet<ApiResetInfo>()
        ///시스템 전체시작 버튼누름시 수행되야하는 Real목록
        member val StartPoints = createQualifiedNamedHashSet<Real>()
        ///시스템 버튼 소속 Flows 정보 setting은 AddButton 사용
        member val internal Buttons = HashSet<ButtonDef>()
        ///시스템 램프 소속 Flow 정보  setting은 AddLamp 사용
        member val internal Lamps   = HashSet<LampDef>()
        ///시스템 조건 (운전/준비) 정보  setting은 AddCondition 사용
        member val internal Conditions   = HashSet<ConditionDef>()



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

    and ButtonDef (name:string, btnType:BtnType, inAddress:TagAddress, outAddress:TagAddress, flows:HashSet<Flow>, funcs:HashSet<Func>) =
        member x.Name = name
        member x.ButtonType = btnType
        ///버튼 동작을 위한 외부 IO 입력 주소
        member val InAddress = inAddress with get,set
        ///버튼 동작을 위한 외부 IO 출력 주소
        member val OutAddress = outAddress  with get,set

        //CPU 생성시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        //CPU 생성시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set
        member val SettingFlows  = flows with get, set
        member val Funcs  = funcs with get, set


    and LampDef (name:string, lampType:LampType, outAddress:TagAddress, flow:Flow, funcs:HashSet<Func>) =
        member x.Name = name
        member x.LampType = lampType
        ///램프 동작을 위한 외부 IO 출력 주소
        member val OutAddress = outAddress  with get,set

        //CPU 생성시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set
        ///단일 Flow 단위로 Lamp 상태 출력
        member val SettingFlow  = flow with get, set
        member val Funcs  = funcs with get, set

    and ConditionDef (name:string, conditionType:ConditionType, inAddress:TagAddress, flows:HashSet<Flow>, funcs:HashSet<Func>) =
        member x.Name = name
        member x.ConditionType = conditionType
        ///조건을 위한 외부 IO 출력 주소
        member val InAddress = inAddress  with get,set

        //CPU 생성시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        ///단일 Flow 단위로 Condition 상태 출력
        member val SettingFlows  = flows with get, set
        member val Funcs  = funcs with get, set

    and AliasDef(aliasKey:Fqdn, target:AliasTargetWrapper option, mnemonics:string []) =
        member _.AliasKey = aliasKey
        member val AliasTarget = target with get, set
        member val Mnemonincs = mnemonics |> ResizeArray

    /// leaf or stem(parenting)
    /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, Call
    [<AbstractClass>]
    type Vertex (names:Fqdn, parent:ParentWrapper)  =
        inherit FqdnObject(names.Combine(), parent.GetCore())

        interface INamedVertex
        member _.Parent = parent
        member _.PureNames = names
        member _.ParentNPureNames = ([parent.GetCore().Name] @ names).ToArray()
        override x.GetRelativeName(referencePath:Fqdn) = x.PureNames.Combine()

    // Subclasses = {Call | Real | RealOtherFlow}
    type ISafetyConditoinHolder =
        abstract member SafetyConditions: HashSet<SafetyCondition>

    /// Indirect to Call/Alias/RealOtherFlow/RealOtherSystem
    [<AbstractClass>]
    type Indirect (names:string seq, parent:ParentWrapper) =
        inherit Vertex(names |> Array.ofSeq, parent)
        new (name, parent) = Indirect([name], parent)

    /// Segment (DS Basic Unit)
    [<DebuggerDisplay("{QualifiedName}")>]
    type Real private (name:string, flow:Flow) =
        inherit Vertex([|name|], DuParentFlow flow)

        member val Graph = DsGraph()
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        member _.Flow = flow
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and RealOtherFlow private (names:Fqdn, target:Real, parent) =
        inherit Indirect(names, parent)
        member _.Real = target
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and RealOtherSystem private (target:Job, parent) =
        inherit Indirect(target.Name, parent)
        member _.Real = target
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and Call private (target:Job, parent) =
        inherit Indirect(target.Name, parent)
        member _.CallTargetJob = target
        member val Xywh:Xywh = null with get, set
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and Alias private (name:string, target:AliasTargetWrapper, parent) = // target : Real or Call or OtherFlowReal
        inherit Indirect(name, parent)
        member _.TargetWrapper = target

    /// Job 정의: Call 이 호출하는 Job 항목
    type Job (name:string, tasks:DsTask list) =
        inherit Named(name)
        member x.DeviceDefs = tasks.OfType<TaskDevice>()
        member x.LinkDefs   = tasks.OfType<TaskLink>()
        member val Funcs  = HashSet<Func>() with get, set//todo ToDsText, parsing

    type TagAddress = string
    [<AbstractClass>]
    type DsTask (api:ApiItem, loadedName:string) =
        member _.ApiItem = api
        ///LoadedSystem은 이름을 재정의 하기 때문에 ApiName을 제공 함
        member val ApiName = getRawName [loadedName;api.Name] true

    /// Main system 에서 loading 된 다른 system 의 API 를 바라보는 관점.  [jobs] = { Ap = { A."+"(%I1, %Q1); } }
    type TaskDevice (api:ApiItem, inAddress:TagAddress, outAddress:TagAddress, deviceName:string) =
        inherit DsTask(api, deviceName)
        member val InAddress   = inAddress  with get, set
        member val OutAddress  = outAddress with get, set
        //CPU 생성시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        //CPU 생성시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set

    type TaskLink (api:ApiItem, systemName:string) =
        inherit DsTask(api, systemName)

    /// 자신을 export 하는 관점에서 본 api's.  Interface 정의.   [interfaces] = { "+" = { F.Vp ~ F.Sp } }
    and ApiItem private (name:string, system:DsSystem) =
        (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        interface INamedVertex

        member _.Name = name
        member _.System = system
        member val TXs = createQualifiedNamedHashSet<Real>()
        member val RXs = createQualifiedNamedHashSet<Real>()


    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (system:DsSystem, operand1:string, operator:ModelingEdgeType, operand2:string) =
        member _.Operand1 = operand1  // "+"
        member _.Operand2 = operand2  // "-"
        member _.Operator = operator  // "<||>"
        member _.ToDsText() = sprintf "%s %s %s" operand1 (operator.ToText()) operand2  //"+" <||> "-"
        static member Create(system, operand1, operator, operand2) =
            let ri = ApiResetInfo(system, operand1, operator, operand2)
            system.ApiResetInfos.Add(ri) |> verifyM $"Duplicated interface ResetInfo [{ri.ToDsText()}]"
            ri

    (* Abbreviations *)

    type DsGraph = Graph<Vertex, Edge>
    and Direct = Real

    and Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
        inherit EdgeBase<Vertex>(source, target, edgeType)

        static member Create(graph:Graph<_,_>, source, target, edgeType:EdgeType) =
            let edge = Edge(source, target, edgeType)
            graph.AddEdge(edge) |> verifyM $"Duplicated edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge

        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

    and AliasTargetWrapper =
        | DuAliasTargetReal of Real
        | DuAliasTargetCall of Call
        | DuAliasTargetRealExFlow of RealOtherFlow    // MyFlow or RealOtherFlow 의 Real 일 수 있다.
        | DuAliasTargetRealExSystem of RealOtherSystem
        member x.RealTarget() =
            match x with | DuAliasTargetReal   r -> Some r |_ -> None
        member x.CallTarget() =
            match x with | DuAliasTargetCall   c -> Some c |_ -> None
        member x.RealExFlowTarget() =
            match x with | DuAliasTargetRealExFlow rx -> Some rx |_ -> None
        member x.RealExSystemTarget() =
            match x with | DuAliasTargetRealExSystem  rx -> Some rx |_ -> None

    and SafetyCondition =
        | DuSafetyConditionReal of Real
        | DuSafetyConditionCall of Call
        | DuSafetyConditionRealExFlow of RealOtherFlow    // MyFlow or RealOtherFlow 의 Real 일 수 있다.
        | DuSafetyConditionRealExSystem of RealOtherSystem    // MyFlow or RealOtherFlow 의 Real 일 수 있다.

          ///Vertex의 부모의 타입을 구분한다.
    type ParentWrapper =
        | DuParentFlow of Flow //Real/Call/Alias 의 부모
        | DuParentReal of Real //Call/Alias      의 부모

    type ParentWrapper with
        member x.GetCore() =
            match x with
            | DuParentFlow f -> f :> FqdnObject
            | DuParentReal r -> r
        member x.GetFlow() =
            match x with
            | DuParentFlow f -> f
            | DuParentReal r -> r.Flow
        member x.GetSystem() =
            match x with
            | DuParentFlow f -> f.System
            | DuParentReal r -> r.Flow.System
        member x.GetGraph():DsGraph =
            match x with
            | DuParentFlow f -> f.Graph
            | DuParentReal r -> r.Graph
        member x.GetModelingEdges() =
            match x with
            | DuParentFlow f -> f.ModelingEdges
            | DuParentReal r -> r.ModelingEdges

    type SafetyCondition with
        member x.Core:obj =
            match x with
            | DuSafetyConditionReal real -> real
            | DuSafetyConditionCall call -> call
            | DuSafetyConditionRealExFlow  realOtherFlow -> realOtherFlow
            | DuSafetyConditionRealExSystem  realOtherSystem -> realOtherSystem

    type AliasTargetWrapper with
        member x.GetTarget() : Vertex =
            match x with
            | DuAliasTargetReal real -> real
            | DuAliasTargetCall call -> call
            | DuAliasTargetRealExFlow otherFlowReal -> otherFlowReal
            | DuAliasTargetRealExSystem otherSystemReal -> otherSystemReal

    type Real with
        static member Create(name: string, flow) =
            if (name.Contains ".") (*&& not <| (name.StartsWith("\"") && name.EndsWith("\""))*) then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let real = Real(name, flow)
            flow.Graph.AddVertex(real) |> verifyM $"Duplicated segment name [{name}]"
            real

        member x.GetAliasTargetToDs(aliasFlow:Flow) =
                if x.Flow <> aliasFlow
                then [|x.Flow.Name; x.Name|]  //other flow
                else [| x.Name |]             //my    flow
        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions


    type RealExF = RealOtherFlow
    type RealOtherFlow with
        static member Create(otherFlowReal:Real, parent:ParentWrapper) =
            let ofn, ofrn = otherFlowReal.Flow.Name, otherFlowReal.Name
            let ofr = RealOtherFlow( [| ofn; ofrn |], otherFlowReal, parent)
            parent.GetGraph().AddVertex(ofr) |> verifyM $"Duplicated other flow real call [{ofn}.{ofrn}]"
            ofr

        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions

    type RealExS = RealOtherSystem
    type RealOtherSystem with
        static member Create(target:Job, parent:ParentWrapper) =
            let exSysReal = RealOtherSystem(target, parent)
            parent.GetGraph().AddVertex(exSysReal) |> verifyM $"Duplicated other flow real call [{exSysReal}]"
            exSysReal

        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow as f -> [x.Name].ToArray()
                | :? Real as r -> x.ParentNPureNames
                | _->failwithlog "Error"
        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions

    type Call with
        static member Create(target:Job, parent:ParentWrapper) =
            let call = Call(target, parent)
            parent.GetGraph().AddVertex(call) |> verifyM $"Duplicated call name [{target.Name}]"
            call

        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow as f -> [x.Name].ToArray()
                | :? Real as r -> x.ParentNPureNames
                | _->failwithlog "Error"
        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions


    type Alias with
        static member Create(name:string, target:AliasTargetWrapper, parent:ParentWrapper) =
            let createAliasDefOnDemand() =
                (* <*.ds> 파일에서 생성하는 경우는 alias 정의가 먼저 선행되지만,
                 * 메모리에서 생성해 나가는 경우는 alias 정의가 없으므로 거꾸로 채워나가야 한다.
                 *)
                let flow:Flow = parent.GetFlow()
                let aliasKey =
                    match target with
                    | DuAliasTargetReal r -> r.GetAliasTargetToDs(flow)
                    | DuAliasTargetCall c -> c.GetAliasTargetToDs()
                    | DuAliasTargetRealExFlow rf -> rf.Real.GetAliasTargetToDs(flow)
                    | DuAliasTargetRealExSystem rs -> rs.GetAliasTargetToDs() // 고쳐야 함
                let ads = flow.AliasDefs
                match ads.TryFind(aliasKey) with
                | Some ad -> ad.Mnemonincs.AddIfNotContains(name) |> ignore
                | None -> ads.Add(aliasKey, AliasDef(aliasKey, Some target, [|name|]))

            createAliasDefOnDemand()
            let alias = Alias(name, target, parent)
            if parent.GetCore() :? Real
            then
                (target.RealTarget().IsNone && target.RealExFlowTarget().IsNone)
                |> verifyM $"Vertex {name} children type error"

            parent.GetGraph().AddVertex(alias) |> verifyM $"Duplicated alias name [{name}]"
            alias

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