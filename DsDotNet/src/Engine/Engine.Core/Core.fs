// Copyright (c) Dual Inc.  All Rights Reserved.
// Dual Inc.에 저작권이 있습니다. 모든 권한 보유.

namespace rec Engine.Core

open System.Linq
open System.Diagnostics
open System.Collections.Generic
open Dual.Common.Core.FS
open System
open System.Reactive.Subjects
open System.ComponentModel
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreModule =
    /// FQDN(Fully Qualified Domain Name) 객체를 생성합니다.
    let createFqdnObject (nameComponents: string array) = 
        {
            new IQualifiedNamed with
                member _.Name with get() = nameComponents.LastOrDefault() and set(_v) = failwithlog "ERROR"
                member _.NameComponents = nameComponents
                member x.QualifiedName = nameComponents.Combine()
        }

    // 파서 로딩 타입 정의
    type ParserLoadingType = DuNone | DuDevice | DuExternal 

    /// External 시스템 로딩 시 공유할 정보를 저장하는 클래스
    type ShareableSystemRepository = Dictionary<string, DsSystem>

    // 장치 로딩 파라미터 정의
    type DeviceLoadParameters = {
        /// 로딩된 시스템이 속한 컨테이너 시스템
        ContainerSystem: DsSystem
        AbsoluteFilePath: string
        /// 로딩을 위해 사용자가 지정한 파일 경로. 직렬화 시에는 절대 경로를 사용하지 않기 위한 용도로 사용됩니다.
        UserSpecifiedFilePath: string
        /// *.ds 파일에서 정의된 이름과 로딩할 때의 이름이 다를 수 있습니다.
        LoadedName: string
        ShareableSystemRepository: ShareableSystemRepository
        HostIp: string option
        LoadingType: ParserLoadingType
    }

    [<AbstractClass>]
    type LoadedSystem (loadedSystem: DsSystem, param: DeviceLoadParameters) =
        inherit FqdnObject(param.LoadedName, param.ContainerSystem)
        let mutable loadedName = param.LoadedName // 로딩 주체에 따라 런타임에 변경
        interface ISystem 
        member _.LoadedName with get() = loadedName and set(value) = loadedName <- value
        
        /// 다른 장치를 로딩하려는 시스템에서 로딩된 시스템을 참조합니다.
        member _.ReferenceSystem = loadedSystem
        member _.ContainerSystem = param.ContainerSystem
        member _.UserSpecifiedFilePath:string = param.UserSpecifiedFilePath
        member _.AbsoluteFilePath:string = param.AbsoluteFilePath
        member _.LoadingType: ParserLoadingType = param.LoadingType

    /// *.ds 파일을 읽어 새로운 인스턴스를 만들어 삽입하는 구조입니다.
    and Device (loadedDevice: DsSystem, param: DeviceLoadParameters) =
        inherit LoadedSystem(loadedDevice, param)

    /// 공유 인스턴스. *.ds 파일의 절대 경로를 기준으로 하나의 인스턴스만 생성하고 이를 참조하는 개념입니다.
    and ExternalSystem (loadedSystem: DsSystem, param: DeviceLoadParameters) =
        inherit LoadedSystem(loadedSystem, param)
        member _.HostIp = param.HostIp

    type DsSystem (name: string, hostIp: string) =
        inherit FqdnObject(name, createFqdnObject([||]))
        let loadedSystems = createNamedHashSet<LoadedSystem>()
        let apiUsages = ResizeArray<ApiItem>()
        let addApiItemsForDevice (device: LoadedSystem) = device.ReferenceSystem.ApiItems |> apiUsages.AddRange

        interface ISystem 
        member _.AddLoadedSystem(childSys) = 
            loadedSystems.Add(childSys)
            |> verifyM $"중복된 로드된 시스템 이름 [{childSys.Name}]"
            addApiItemsForDevice childSys

        member _.ReferenceSystems = loadedSystems.Select(fun s -> s.ReferenceSystem)
        member _.LoadedSystems = loadedSystems |> seq
        member _.Devices = loadedSystems.OfType<Device>() |> Seq.toArray 
        member _.ExternalSystems = loadedSystems.OfType<ExternalSystem>() |> Seq.toArray
        member _.ApiUsages = apiUsages |> seq
        member _.HostIp = hostIp
        member val Jobs = ResizeArray<Job>()
        member val Flows = createNamedHashSet<Flow>()
        member val OriginalCodeBlocks = ResizeArray<string>()
        member val Statements = ResizeArray<Statement>()
        member val Variables = ResizeArray<VariableData>()
        member val ApiItems = createNamedHashSet<ApiItem>()
        member val ApiResetInfos = HashSet<ApiResetInfo>()
        member val StartPoints = createQualifiedNamedHashSet<Real>()
        member val internal Buttons = HashSet<ButtonDef>()
        member val internal Lamps = HashSet<LampDef>()
        member val internal Conditions = HashSet<ConditionDef>()

    type Flow private (name: string, system: DsSystem) =
        inherit FqdnObject(name, system)
        
        member val Graph = DsGraph()
        
        member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
        
        member val AliasDefs = Dictionary<Fqdn, AliasDef>(nameComponentsComparer())

        
        member x.System = system

        static member Create(name: string, system: DsSystem) =
            let flow = Flow(name, system)
            system.Flows.Add(flow) |> verifyM $"중복된 플로우 이름 [{name}]"
            flow

    and ButtonDef (name: string, btnType: BtnType, inAddress: TagAddress, outAddress: TagAddress, flows: HashSet<Flow>, funcs: HashSet<Func>) =
        member x.Name = name
        member x.ButtonType = btnType
        /// 버튼 작동을 위한 외부 IO 입력 주소
        member val InAddress = inAddress with get, set
        /// 버튼 작동을 위한 외부 IO 출력 주소
        member val OutAddress = outAddress  with get, set
        /// CPU 생성 시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        /// CPU 생성 시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set
        member val SettingFlows = flows with get, set
        member val Funcs = funcs with get, set

    and LampDef (name: string, lampType: LampType, outAddress: TagAddress, flow: Flow, funcs: HashSet<Func>) =
        member x.Name = name
        member x.LampType = lampType
        /// 램프 작동을 위한 외부 IO 출력 주소
        member val OutAddress = outAddress  with get, set
        /// CPU 생성 시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set
        /// 단일 플로우 단위로 램프 상태 출력
        member val SettingFlow = flow with get, set
        member val Funcs = funcs with get, set

    and ConditionDef (name: string, conditionType: ConditionType, inAddress: TagAddress, flows: HashSet<Flow>, funcs: HashSet<Func>) =
        member x.Name = name
        member x.ConditionType = conditionType
        /// 조건을 위한 외부 IO 출력 주소
        member val InAddress = inAddress  with get, set
        /// CPU 생성 시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        /// 단일 플로우 단위로 조건 상태 출력
        member val SettingFlows = flows with get, set
        member val Funcs = funcs with get, set

    and AliasDef(aliasKey: Fqdn, target: AliasTargetWrapper option, mnemonics: string []) =
        member _.AliasKey = aliasKey
        member val AliasTarget = target with get, set
        member val Mnemonics = mnemonics |> ResizeArray

    /// leaf or stem(parenting)
    /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, CallDev
    [<AbstractClass>]
    type Vertex (names:Fqdn, parent:ParentWrapper)  =
        inherit FqdnObject(names.Combine(), parent.GetCore())
        interface INamedVertex
        
        member _.Parent = parent
        
        member _.PureNames = names
        
        member _.ParentNPureNames = ([parent.GetCore().Name] @ names).ToArray()
        override x.GetRelativeName(_referencePath:Fqdn) = x.PureNames.Combine()

    // Subclasses = {CallDev | Real | RealOtherFlow}
    type ISafetyConditoinHolder =
        abstract member SafetyConditions: HashSet<SafetyCondition>

    /// Indirect to CallDev/Alias/RealOtherFlow/CallSys
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

        member x.Finished:bool = name = "RET"  //parser 적용전까지는 임시로 사용
        //member val Finished:bool = false with get, set

    and RealOtherFlow private (names:Fqdn, target:Real, parent)  =
        inherit Indirect(names, parent)
        member _.Real = target
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    [<AbstractClass>]
    type Call (target:Job, parent) =
        inherit Indirect(target.Name, parent)
        member _.CallTargetJob = target
        member val Xywh:Xywh = null with get, set
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    and CallSys private (target:Job, parent) =
        inherit Call(target, parent)

    and CallDev private (target:Job, parent) =
        inherit Call(target, parent)

    and Alias private (name:string, target:AliasTargetWrapper, parent) = // target : Real or CallDev or OtherFlowReal
        inherit Indirect(name, parent)
        member _.TargetWrapper = target

    /// Job 정의: CallDev 이 호출하는 Job 항목
    type Job (name:string, tasks:DsTask list) =
        inherit Named(name)
        let mutable funcs = HashSet<Func>()
        member x.DeviceDefs = tasks.OfType<TaskDev>()
        member x.LinkDefs   = tasks.OfType<TaskSys>()
        member x.SetFuncs(func) = 
                    tasks.Iter(fun t->t.Funcs <- func) 
                    funcs <- func
        member x.Funcs = funcs.ToArray() //일괄 셋팅만 가능 append 불가

    type TagAddress = string
    [<AbstractClass>]
    [<DebuggerDisplay("{ApiName}")>]
    type DsTask (api:ApiItem, loadedName:string) as this =
        inherit FqdnObject(api.Name, createFqdnObject([|loadedName|]))
        member _.ApiItem = api
        ///LoadedSystem은 이름을 재정의 하기 때문에 ApiName을 제공 함
        member val ApiName = this.QualifiedName
        member val Funcs  = HashSet<Func>() with get, set

    /// Main system 에서 loading 된 다른 system 의 API 를 바라보는 관점.  [jobs] = { FWD = Mt.fwd; }
    type TaskSys (api:ApiItem, systemName:string) =
        inherit DsTask(api, systemName)

    /// Main system 에서 loading 된 다른 devcie 의 API 를 바라보는 관점.  [jobs] = { Ap = { A."+"(%I1, %Q1); } }
    ///
    /// Old name : JobDef
    type TaskDev (api:ApiItem, inAddress:TagAddress, outAddress:TagAddress, deviceName:string) =
        inherit DsTask(api, deviceName)
        member val InAddress   = inAddress  with get, set
        member val OutAddress  = outAddress with get, set
        //CPU 생성시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        //CPU 생성시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set


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
    and ApiResetInfo private (operand1:string, operator:ModelingEdgeType, operand2:string) =
        member _.Operand1 = operand1  // "+"
        member _.Operand2 = operand2  // "-"
        member _.Operator = operator  // "<||>"
        member _.ToDsText() = 
            let src = operand1.QuoteOnDemand()
            let tgt = operand2.QuoteOnDemand()
            sprintf "%s %s %s"  src (operator.ToText()) tgt  //"+" <||> "-"
        static member Create(system:DsSystem, operand1, operator, operand2) =
            let ri = ApiResetInfo(operand1, operator, operand2)
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
        | DuAliasTargetCall of CallDev
        | DuAliasTargetRealExFlow of RealOtherFlow    // MyFlow or RealOtherFlow 의 Real 일 수 있다.
        | DuAliasTargetRealExSystem of CallSys
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
        | DuSafetyConditionCall of CallDev
        | DuSafetyConditionRealExFlow of RealOtherFlow    // MyFlow or RealOtherFlow 의 Real 일 수 있다.
        | DuSafetyConditionRealExSystem of CallSys    // MyFlow or RealOtherFlow 의 Real 일 수 있다.

          ///Vertex의 부모의 타입을 구분한다.
    type ParentWrapper =
        | DuParentFlow of Flow //Real/CallDev/Alias 의 부모
        | DuParentReal of Real //CallDev/Alias      의 부모

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

    type CallSys with
        static member Create(target:Job, parent:ParentWrapper) =
            let exSysReal = CallSys(target, parent)
            parent.GetGraph().AddVertex(exSysReal) |> verifyM $"Duplicated other flow real call [{exSysReal}]"
            exSysReal

        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow -> [x.Name].ToArray()
                | :? Real -> x.ParentNPureNames
                | _->failwithlog "Error"
        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions

    type CallDev with
        static member Create(target:Job, parent:ParentWrapper) =
            let call = CallDev(target, parent)
            parent.GetGraph().AddVertex(call) |> verifyM $"Duplicated call name [{target.Name}]"
            call

        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow -> [x.Name].ToArray()
                | :? Real -> x.ParentNPureNames
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
                | Some ad -> ad.Mnemonics.AddIfNotContains(name) |> ignore
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