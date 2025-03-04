// Copyright (c) Dualsoft  All Rights Reserved.
// Dualsoft에 저작권이 있습니다. 모든 권한 보유.

namespace Engine.Core

open System
open System.Linq
open System.Diagnostics
open System.Collections.Generic
open System.Reflection

open Dual.Common.Core.FS
open Dual.Common.Base.FS
open Engine.Common


[<AutoOpen>]
module rec CoreModule =
    [<AutoOpen>]
    module SystemModule =

        type DsSystem internal (name: string, vertexDic:Dictionary<string, FqdnObject>, vertexHandlers:GraphVertexAddRemoveHandlers option) =
            inherit FqdnObject(name, createFqdnObject([||]))

            static let assem = Assembly.GetExecutingAssembly()
            static let runtimeLangVersion = assem.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion |> Version.Parse
            static let runtimeEngineVersion = assem.GetCustomAttribute<AssemblyFileVersionAttribute>().Version |> Version.Parse

            interface ISystem

            member val private _vertexDic = vertexDic
            member val VertexAddRemoveHandlers = vertexHandlers with get, set       // UnitTest 환경에서만 set 허용

            /// System Loading (메모리 작업 중) 여부.  System 생성이 끝나는 순간에 false
            member val Loading = true with get, set

            member val ApiUsages = ResizeArray<ApiItem>()
            member val Jobs = ResizeArray<Job>()
            member val Functions = ResizeArray<DsFunc>()
            member val LoadedSystems = createNamedHashSet<LoadedSystem>()

            member val Variables = ResizeArray<VariableData>()
            member val ActionVariables = ResizeArray<ActionVariable>()

            member val Flows = createNamedHashSet<Flow>()

            ///사용자 정의 API
            member val ApiItems = createNamedHashSet<ApiItem>()


            ///HW HMI 전용 API (물리 ButtonDef LampDef ConditionDef 정의에 따른 API)
            member val HwSystemDefs = createNamedHashSet<HwSystemDef>()
            member val ApiResetInfos = HashSet<ApiResetInfo>()

            member val LangVersion = runtimeLangVersion.Duplicate() with get, set
            member val EngineVersion = runtimeEngineVersion.Duplicate() with get, set
            static member RuntimeLangVersion = runtimeLangVersion
            static member RuntimeEngineVersion = runtimeEngineVersion

        type LoadedSystem with
            member x.ReferenceSystem = x.ReferenceISystem :?> DsSystem
            member x.ContainerSystem = x.ContainerISystem :?> DsSystem

        type DsSystem with
            member x.AddFqdnVertex(fqdn, vertex) = x._vertexDic.Add(fqdn, vertex)
            member x.TryFindFqdnVertex(fqdn) = x._vertexDic.TryFindValue(fqdn)

            ///내시스템이 사용한 interface
            member x.TaskDevs = x.Jobs.SelectMany(fun j->j.TaskDefs)
            member x.ReferenceSystems = x.LoadedSystems.Select(fun s -> s.ReferenceSystem) |> distinct
            member x.Devices = x.LoadedSystems.OfType<Device>() |> Seq.toArray
            member x.ExternalSystems = x.LoadedSystems.OfType<ExternalSystem>()





            member x.AddVariables(variableData:VariableData) =
                if x.Variables.Any(fun v-> v.Name = variableData.Name) then
                    failWithLog $"중복된 변수가 있습니다. {variableData.Name} "

                x.Variables.Add(variableData)

            member x.AddActionVariables(actionVariable:ActionVariable) =
                if x.ActionVariables.Any(fun v-> v.Name = actionVariable.Name) then
                    failWithLog $"중복된 심볼이 있습니다. {actionVariable.Name}({actionVariable.Address})"

                x.ActionVariables.Add(actionVariable)

            member x.AddLoadedSystem(childSys) =
                x.LoadedSystems.Add(childSys) |> verifyM $"중복로드된 시스템 이름 [{childSys.Name}]"

                // loaded device 도 vertex dic 에 포함할지 말지 여부에 따라서
                if isNull(x._vertexDic) then
                    assert(isInUnitTest())
                else
                    x.AddFqdnVertex(childSys.Name, childSys)

                childSys.ReferenceSystem.ApiItems |> x.ApiUsages.AddRange


    [<AutoOpen>]
    module WrapperSafetyModule =

        type AliasTargetWrapper =
            | DuAliasTargetReal of Real
            | DuAliasTargetCall of Call
            member x.RealTarget() =
                match x with | DuAliasTargetReal r -> Some r |_ -> None
            member x.CallTarget() =
                match x with | DuAliasTargetCall c -> Some c |_ -> None

        type AliasTargetWrapper with
            member x.GetTarget() : Vertex =
                match x with
                | DuAliasTargetReal real -> real
                | DuAliasTargetCall call -> call


        // Subclasses = {Call}
        type ISafetyAutoPreRequisiteHolder =
            abstract member SafetyConditions: HashSet<SafetyAutoPreCondition>
            abstract member AutoPreConditions: HashSet<SafetyAutoPreCondition>
            abstract member GetCall: unit -> Call

        and SafetyAutoPreCondition =
            | DuSafetyAutoPreConditionCall of Call
            member x.GetCall() =
                match x with
                | DuSafetyAutoPreConditionCall call -> call
            member x.Core:obj =
                match x with
                | DuSafetyAutoPreConditionCall call -> call
            member x.Name:string =
                match x with
                | DuSafetyAutoPreConditionCall call -> call.Name



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
            member x.GetSystem():DsSystem =
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

            member x.AddVertex v = x.GetGraph().AddVertex(v)



        and AliasDef(aliasKey: Fqdn, target: AliasTargetWrapper option, aliasTexts: string []) =
            member _.AliasKey = aliasKey
            member val AliasTarget = target with get, set
            member val AliasTexts = aliasTexts |> ResizeArray


    [<AutoOpen>]
    module GraphItemsModule =
        type Flow internal (name: string, system: DsSystem) =
            inherit FqdnObject(name, system)

            do
                checkFlowName  name

            member val Graph = DsGraph(system.VertexAddRemoveHandlers)
            member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
            member val AliasDefs = Dictionary<Fqdn, AliasDef>(nameComponentsComparer())
            member x.System = system



        /// leaf or stem(parenting)
        /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, Call
        [<AbstractClass>]
        type Vertex (names:Fqdn, parent:ParentWrapper)  =
            inherit FqdnObject(names.Combine(), parent.GetCore())
            interface INamedVertex

            member _.Parent = parent

            member _.PureNames = names
            member val TokenSourceOrder: int option = None with get, set //1 부터 할당

            member _.ParentNPureNames = ([parent.GetCore().Name] @ names).ToArray()
            override x.GetRelativeName(_referencePath:Fqdn) = x.PureNames.Combine()


        type Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
            inherit DsEdgeBase<Vertex>(source, target, edgeType)

            static member Create(graph:TDsGraph<_,_>, source, target, edgeType:EdgeType) =
                let edge = Edge(source, target, edgeType)
                graph.AddEdge(edge) |> verifyM $"중복 edge [{source.Name}{edgeType.ToText()}{target.Name}]"
                edge

            override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"




        /// Indirect to Call/Alias/RealOtherFlow/CallSys
        [<AbstractClass>]
        type Indirect (names:string seq, parent:ParentWrapper) =
            inherit Vertex(names |> Array.ofSeq, parent)
            new (name, parent) = Indirect([name], parent)

        /// Segment (DS Basic Unit)
        [<DebuggerDisplay("{QualifiedName}")>]
        type Real internal (name:string, flow:Flow) =
            inherit Vertex([|name|], DuParentFlow flow)
            let mutable motion:string option = None
            let mutable script:string option = None

            let setIfNotAlreadySet field value fieldName =
                match field with
                | Some _ -> failWithLog $"{fieldName} is already set {flow.Name}.{name}"
                | None -> value

            member x.Motion
                with get() = motion
                and set(v) = motion <- setIfNotAlreadySet motion v "Motion"

            member x.Script
                with get() = script
                and set(v) = script <- setIfNotAlreadySet script v "Script"

            member x.Time
                with get() = x.DsTime.AVG
                and set(v) = x.DsTime.AVG <- v

            member x.Flow = flow
            member val Graph = DsGraph(flow.System.VertexAddRemoveHandlers)
            member val ModelingEdges = HashSet<ModelingEdgeInfo<Vertex>>()
            member val ExternalTags = Dictionary<ExternalTag, IStorage>()
            member val ParentApiSensorExpr = getNull<IExpression>() with get, set

            //member val RealData:byte[] = [||] with get, set
            member val DsTime:DsTime = DsTime() with get, set

            member val Finished:bool = false with get, set
            member val NoTransData:bool = false with get, set
            member val IsSourceToken:bool = false with get, set
            member val RepeatCount:UInt32 option = None with get, set

        type Real with
            member x.GetAliasTargetToDs(aliasFlow:Flow) =
                [|
                    if x.Flow <> aliasFlow then
                        yield x.Flow.Name // other flow
                    yield x.Name  // my flow
                |]


        type CallType =
            | JobType of Job
            | CommadFuncType of CommandFunction
            | OperatorFuncType of OperatorFunction

        type Call internal (jobOrFunc:CallType, parent:ParentWrapper, valueParamIO:ValueParamIO) =
            inherit Indirect (
                // indirect 의 인자로 name, parent 를 제공
                match jobOrFunc with
                | JobType job ->
                    let jncs = job.NameComponents
                    if jncs.Head() = parent.GetFlow().Name then
                        jncs.Skip(1).CombineDequoteOnDemand()
                    else
                        jncs.CombineDequoteOnDemand()
                | CommadFuncType func -> func.Name
                | OperatorFuncType func -> func.Name
                , parent
            )

            let isJob = function
                | JobType _  -> true
                | _ -> false

            let isCommand = function
                | CommadFuncType _ -> true
                | _ -> false

            let isOperator = function
                | OperatorFuncType _ -> true
                | _ -> false
            do
                match jobOrFunc with
                | JobType job ->
                    if not(valueParamIO.IsDefaultParam) then
                        job.TaskDefs.Iter(fun (td: TaskDev) -> updateTaskDevDatatype(td.TaskDevParamIO, valueParamIO, td.DequotedQualifiedName))
                | _ -> ()



            interface IVertex

            member internal x.OnCreated() = parent.AddVertex x |> verifyM $"중복 call name [{x.Name}]"

            member x.TargetJob =
                match jobOrFunc with
                | JobType job -> job
                | _ -> failwithlog $"{x.QualifiedName} is not JobType."

            member x.TaskDefs = if x.IsJob then x.TargetJob.TaskDefs else Seq.empty
            member x.ApiDefs  = if x.IsJob then x.TargetJob.ApiDefs  else Seq.empty

            member x.TargetFunc =
                match jobOrFunc with
                | CommadFuncType func -> func :> DsFunc |> Some
                | OperatorFuncType func -> func :> DsFunc  |> Some
                | _ -> None

            /// Indicates if the target includes a job.
            member _.IsJob = isJob jobOrFunc
            member _.IsCommand = isCommand jobOrFunc
            member _.IsOperator = isOperator jobOrFunc
            member _.JobOrFunc = jobOrFunc


            member val CallTime = CallTime() with get, set
            member val CallActionType = CallActionType.ActionNormal with get, set
            member val ExternalTags = Dictionary<ExternalTag, IStorage>()
            member val Disabled:bool = false with get, set
            member val ValueParamIO = valueParamIO

            interface ISafetyAutoPreRequisiteHolder with
                member x.GetCall() = x
                member val SafetyConditions = HashSet<SafetyAutoPreCondition>()
                member val AutoPreConditions = HashSet<SafetyAutoPreCondition>()

        type Call with
            member x.DeviceNApi = x.TargetJob.NameComponents.Skip(1)
            member x.GetAliasTargetToDs(aliasFlow:Flow) =
                    let orgFlowName = x.TargetJob.NameComponents.Head()
                    if orgFlowName = aliasFlow.Name then
                        match x.Parent.GetCore() with
                            | :? Real as r -> [r.Name]@x.DeviceNApi
                            | :? Flow -> x.DeviceNApi
                            | _ -> failwithlog "Error"

                    else
                        [orgFlowName]@x.DeviceNApi //other flow
            member x.SafetyConditions  = (x :> ISafetyAutoPreRequisiteHolder).SafetyConditions
            member x.AutoPreConditions = (x :> ISafetyAutoPreRequisiteHolder).AutoPreConditions


        type Alias internal (name:string , target:AliasTargetWrapper, parent, isExFlowReal:bool) = // target : Real or Call or OtherFlowReal
            inherit Indirect([|name|], parent)
            member _.TargetWrapper = target
            member _.IsExFlowReal = isExFlowReal
            member _.IsSameFlow = target.GetTarget()
                                  |> fun v -> v.Parent.GetFlow() = parent.GetFlow()







    type InOutDataType = DataType*DataType

    [<AutoOpen>]
    module ApiItemsModule =

        /// 자신을 export 하는 관점에서 본 api's.  Interface 정의.   [interfaces] = { "+" = { F.Vp ~ F.Sp } }
        type ApiItem internal (name:string, dsSystem:DsSystem) =
            (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
            inherit FqdnObject(name, createFqdnObject([|dsSystem.Name|]))
            interface INamedVertex

            member _.Name = name
            member _.ApiSystem = dsSystem

            member val TX = getNull<Real>() with get, set
            member val RX = getNull<Real>() with get, set
            override x.ToText() =
                $"{name}\r\n[{x.TX.Name} ~ {x.RX.Name}]"

        /// API 의 reset 정보:  "+" <||> "-";
        type ApiResetInfo internal (operand1:string, operator:ModelingEdgeType, operand2:string, autoGenByFlow:bool) =
            member _.AutoGenByFlow = autoGenByFlow
            member _.Operand1 = operand1  // "+"
            member _.Operand2 = operand2  // "-"
            member _.Operator = operator  // "<|>", "|>", "<|"


        /// Main system 에서 loading 된 다른 device 의 API 를 바라보는 관점.
        ///[jobs] = { Ap = { A."+"(%I1:true:1500, %Q1:true:500); } } job1 = { Dev.Api(InParam, OutParam), Dev... }
        //type ApiParam =
        //    {
        //        ApiItem : ApiItem
        //        TaskDevParamIO : TaskDevParamIO
        //    }

        (*
            apiParam:
	            ApiItem: ADV
	            TaskDevParamIO:
		            InParam:
		            OutParam:
            jobName : STN1.Device1.ADV
            device: STN1__Device1
            system: HelloDS
        *)
        type TaskDev internal (deviceName:string, apiItem:ApiItem, parentSys:DsSystem) =
            inherit FqdnObject(apiItem.PureName, createFqdnObject([|parentSys.Name;deviceName|]))


            //member x.ApiPureName = (x:>FqdnObject).QualifiedName
            member x.ApiSystemName = apiItem.ApiSystem.Name //needs test animation

            member x.DeviceName = deviceName
            member x.ParentSystem = parentSys
            member x.ApiItem  = apiItem
            member x.FullName = $"{deviceName}.{x.Name}"
            member x.FullNameToDsText = $"{deviceName.QuoteOnDemand()}.{x.Name.QuoteOnDemand()}"

            member val IsRootOnlyDevice = false with get, set
            member val TaskDevParamIO = defaultTaskDevParamIO() with get, set


            member val ManualAddress = TextAddrEmpty with get, set

            //CPU 생성시 할당됨 InTag
            member val InTag = getNull<ITag>() with get, set
            //CPU 생성시 할당됨 OutTag
            member val OutTag = getNull<ITag>() with get, set

        /// Job 정의: Call 이 호출하는 Job 항목
        type Job (names:Fqdn, system:DsSystem, tasks:TaskDev list) =
            inherit FqdnObject(names.Last(), createFqdnObject(names.SkipLast(1).ToArray()))
            member x.System = system
            member x.TaskDefs = tasks
            member x.Name = failWithLog $"{names.Combine()} Name using 'DequotedQualifiedName'"





        type ApiItem with
            member x.PureName = x.Name.Split([|'(';')'|]).First()

        type ApiResetInfo with
            member x.ToDsText() =
                let src = x.Operand1
                let tgt = x.Operand2
                sprintf "%s %s %s"  src (x.Operator |> toTextModelEdge) tgt  //"+" <|> "-"


        type TaskDev with
            member x.InAddress  with get() = x.TaskDevParamIO.InParam.Address  and set(v) = x.TaskDevParamIO.InParam.Address <- v
            member x.OutAddress with get() = x.TaskDevParamIO.OutParam.Address and set(v) = x.TaskDevParamIO.OutParam.Address <- v
            member x.IsAnalogSensor = x.InTag.IsNonNull() && x.InTag.DataType <> typedefof<bool>
            member x.IsAnalogActuator = x.OutTag.IsNonNull() && x.OutTag.DataType <> typedefof<bool>
            member x.IsAnalog = x.IsAnalogSensor || x.IsAnalogActuator



        type Job with
            member x.TaskDevCount     = x.TaskDefs.Count()
            member x.AddressInCount   = x.TaskDefs.Filter(fun t->t.TaskDevParamIO.InParam.Address <> TextNotUsed).Count()
            member x.AddressOutCount  = x.TaskDefs.Filter(fun t->t.TaskDevParamIO.OutParam.Address <> TextNotUsed).Count()

            member x.ApiDefs = x.TaskDefs |> map _.ApiItem


    [<AutoOpen>]
    module CreationModule =
        let internal fwdCreateEdgeOnFlow = ref (fun (_flow:Flow) (_mei:ModelingEdgeInfo<Vertex>) -> failwithlog "Should be reimplemented." : Edge[])
        let internal fwdCreateEdgeOnReal = ref (fun (_real:Real) (_mei:ModelingEdgeInfo<Vertex>) -> failwithlog "Should be reimplemented." : Edge[])

        type DsSystem with  // Create, Create{Flow, ApiItem, ApiResetInfo, TaskDev}
            // [NOTE] GraphVertex {
            static member Create4Test(name) =
                assert (isInUnitTest())     // UnitTest 에서만 사용.  일반 코드에서는 DsSystem.Create(name) 을 사용할 것.
                DsSystem(name, null, None)

            static member Create(name) =
                let vertexDic = Dictionary<string, FqdnObject>()
                let vertexHandlers =
                    let onAdded (v:IVertexKey) =
                        let q = v :?> FqdnObject
                        vertexDic.TryAdd(q.DequotedQualifiedName, q)
                    let onRemoved (v:IVertexKey) =
                        let q = v :?> FqdnObject
                        vertexDic.Remove(q.DequotedQualifiedName)

                    GraphVertexAddRemoveHandlers(onAdded, onRemoved)

                let system = DsSystem(name, vertexDic, Some vertexHandlers)
                vertexDic.Add(name, system)
                system


            member x.CreateFlow(flowName:string) =
                let system = x
                let flow = Flow(flowName, system)
                system.Flows.Add(flow) |> verifyM $"중복된 플로우 이름 [{flowName}]"
                flow

            member x.CreateApiItem(name:string) =
                let system = x
                ApiItem(name, system)
                |> tee(fun ai ->
                    system.ApiItems.Add(ai) |> verifyM $"중복 interface prototype name [{name}]")


            member x.CreateApiItem(name:string, tx, rx) =
                let system = x
                system.CreateApiItem(name)
                |> tee(fun ai ->
                    ai.TX <- tx
                    ai.RX <- rx)


            member x.CreateApiResetInfo(operand1, operator, operand2, autoGenByFlow) =
                let system = x
                ApiResetInfo(operand1, operator, operand2, autoGenByFlow)
                |> tee(fun ri ->
                    system.ApiResetInfos.Add(ri) |> verifyM $"중복 interface ResetInfo [{ri.ToDsText()}]")
            // [NOTE] GraphVertex }


            member x.CreateTaskDev(devName:string, apiItem: ApiItem): TaskDev = TaskDev(devName, apiItem, x)
            member x.CreateTaskDev(devName:string, apiName: string): TaskDev =
                let sys:DsSystem = x
                let apis = sys.ApiItems.Where(fun w -> w.Name = apiName).ToFSharpList()

                let api:ApiItem =
                    // Check if the API already exists
                    match apis with
                    | api::[] -> api
                    | [] ->
                        // Add a default flow if no flows exist
                        let flow =
                            match sys.Flows.TryHead() with
                            | Some h -> h
                            | None -> sys.CreateFlow("genFlow")

                        let realName = $"{apiName}"
                        let reals = flow.Graph.Vertices.OfType<Real>().ToArray()
                        if reals.Any(fun w -> w.Name = realName) then
                            failwithf $"real {realName} 중복 생성에러"

                        // Create a new Real
                        let newReal:Real = flow.CreateReal(realName)
                        newReal.Motion <- Some($"genMotion_{apiName}")

                        flow.Graph.Vertices.OfType<Real>().Iter(fun r->r.Finished <- false)  //기존 Real이 원위치 취소
                        newReal.Finished <- true    //마지막 Real이 원위치


                          // Create and add a new ApiItem
                        let newApi = sys.CreateApiItem(apiName, newReal, newReal)
                        sys.ApiItems.Add newApi |> ignore

                        if flow.Graph.Vertices.OfType<Real>().Count() > 1 then  //2개 부터 인터락 리셋처리
                            // Iterate over reals up to newReal
                            reals
                                .TakeWhile(fun r -> r <> newReal)
                                .Iter(fun r ->
                                    let exAliasName = $"{r.Name}Alias_{newReal.Name}"
                                    let myAliasName = $"{newReal.Name}Alias_{r.Name}"
                                    let exAlias = flow.CreateAlias(exAliasName, r, false)
                                    let myAlias:Alias = flow.CreateAlias(myAliasName, newReal, false)

                                    // Create an edge between myAlias and exAlias
                                    let mei = ModelingEdgeInfo<Vertex>(myAlias, "<|>", exAlias)
                                    (!fwdCreateEdgeOnFlow) flow mei |> ignore )

                            // Potentially update other ApiItems based on the new ApiItem
                            //sys.ApiItems.TakeWhile(fun a -> a <> newApi)  autoGenByFlow 처리로 인해 필요없음
                            //     .Iter(fun a -> ApiResetInfo.Create(sys, a.Name, ModelingEdgeType.Interlock, newApi.Name) |> ignore)

                        newApi
                    | _ ->
                        failwithf $"system {sys.Name} api {apiName} 중복 존재"

                sys.CreateTaskDev(devName, api)


        type Flow with  // Create{Real, Alias, Call, Edge}
            member x.CreateReal(name:string) =
                let flow = x
                if name.Contains "." then
                    logWarn $"Suspicious segment name [{name}]. Check it."

                let real = Real(name, flow)
                flow.Graph.AddVertex(real) |> verifyM $"중복 segment name [{name}]"
                real

            /// see Real.CreateAlias
            member x.CreateAlias(name:string, target:Call, isExFlowReal) =
                Alias.Create(name, DuAliasTargetCall target, DuParentFlow x, isExFlowReal)

            /// see Real.CreateAlias
            member x.CreateAlias(name:string, target:Real, isExFlowReal) =
                Alias.Create(name, DuAliasTargetReal target, DuParentFlow x, isExFlowReal)

            member x.CreateCall(target:Job, valueParamIO:ValueParamIO) =
                let parent:ParentWrapper = DuParentFlow x
                parent.CreateCall(target, valueParamIO)
            member x.CreateCall(target:Job) = x.CreateCall(target, defaultValueParamIO())
            member x.CreateEdge(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) = (!fwdCreateEdgeOnFlow) x modelingEdgeInfo      // fwdCreateEdgeOnFlow refers Flow.CreateEdgeImpl

        type Real with  // Create{Alias, Call, Edge}
            /// see Flow.CreateAlias
            member x.CreateAlias(name:string, target:Call, isExFlowReal) =
                Alias.Create(name, DuAliasTargetCall target, DuParentReal x, isExFlowReal)

            member x.CreateCall(target:Job, valueParamIO:ValueParamIO) =
                let parent:ParentWrapper = DuParentReal x
                parent.CreateCall(target, valueParamIO)
            member x.CreateCall(target:Job) = x.CreateCall(target, defaultValueParamIO())
            member x.CreateEdge(modelingEdgeInfo:ModelingEdgeInfo<Vertex>) = (!fwdCreateEdgeOnReal) x modelingEdgeInfo      // fwdCreateEdgeOnReal refers Real.CreateEdgeImpl

        type ParentWrapper with // CreateCall
            member x.CreateCall(target:Job, valueParamIO:ValueParamIO) =
                let parent:ParentWrapper = x
                let call = Call(JobType target, parent, valueParamIO)
                let duplicated =
                    parent.GetSystem().Flows
                        .SelectMany(fun f -> f.Graph.Vertices.OfType<Call>())
                        .Any(fun c -> c.QualifiedName = call.QualifiedName)
                if duplicated then
                    failwithlog $"중복 call name [{call.Name}]"

                call |> tee(fun c -> c.OnCreated())
            member x.CreateCall(target:Job) = x.CreateCall(target, defaultValueParamIO())
            member x.CreateCall(func:DsFunc) =
                let callType =
                    match func with
                    | :? CommandFunction -> CommadFuncType (func :?> CommandFunction)
                    | :? OperatorFunction -> OperatorFuncType (func :?> OperatorFunction)
                    | _ -> failwithlog "Error"
                Call(callType, x, defaultValueParamIO()) |> tee(fun c -> c.OnCreated())

        type Alias with
            /// DsSystem.CreateAlias 를 사용할 것 (internal usage only)
            static member internal Create(name:string, target:AliasTargetWrapper, parent:ParentWrapper, isExFlowReal) =
                let createAliasDefOnDemand() =
                    (* <*.ds> 파일에서 생성하는 경우는 alias 정의가 먼저 선행되지만,
                     * 메모리에서 생성해 나가는 경우는 alias 정의가 없으므로 거꾸로 채워나가야 한다.
                     *)
                    let flow:Flow = parent.GetFlow()
                    let aliasKey =
                        match target with
                        | DuAliasTargetCall c -> c.GetAliasTargetToDs(flow).ToArray()
                        | DuAliasTargetReal r -> r.GetAliasTargetToDs(flow).ToArray()

                    match flow.AliasDefs.TryFindValue(aliasKey) with
                    | Some ad -> ad.AliasTexts.AddIfNotContains(name) |> ignore
                    | None -> flow.AliasDefs.Add(aliasKey, AliasDef(aliasKey, Some target, [|name|]))


                createAliasDefOnDemand()


                let alias = Alias(name, target, parent, isExFlowReal)
                if parent.GetCore() :? Real then
                    target.RealTarget().IsNone
                    |> verifyM $"Vertex {name} children type error"

                parent.GetGraph().AddVertex(alias) |> verifyM $"중복 alias name [{name}]"
                alias


    [<AutoOpen>]
    module HwDefModule =

        [<AbstractClass>]
        type HwSystemDef (name: string, system:DsSystem, flows:Flow seq, valueParamIO:ValueParamIO, taskDevParamIO:TaskDevParamIO, addr:Addresses)  =
            inherit FqdnObject(name, system)
            member x.Name = name
            member x.System = system
            member val SettingFlows = HashSet flows
            member val ValueParamIO = valueParamIO
            member val TaskDevParamIO = taskDevParamIO
            member val InAddress = addr.In with get, set
            member val OutAddress = addr.Out with get, set

            /// CPU 생성 시 할당됨 InTag
            member val InTag = getNull<ITag>() with get, set
            /// CPU 생성 시 할당됨 OutTag
            member val OutTag = getNull<ITag>() with get, set


        type HwSystemDef with
            member x.IsDefaultValueParamIO = x.ValueParamIO.IsDefaultParam
            member x.IsDefaultTaskDevParamIO = x.TaskDevParamIO.IsDefaultParam
            //SettingFlows 없으면 전역 시스템 설정
            member x.IsGlobalSystemHw = x.SettingFlows.IsEmpty() || (x.System.Flows |> forall x.SettingFlows.Contains)
            member x.InDataType  = x.ValueParamIO.InDataType
            member x.OutDataType  = x.ValueParamIO.OutDataType



        and ButtonDef (name:string, system:DsSystem, btnType: BtnType, valueParamIO:ValueParamIO, addr:Addresses, flows:Flow seq) =
            inherit HwSystemDef(name, system, flows, valueParamIO, defaultTaskDevParamIO(), addr)
            member x.ButtonType = btnType
            member val ErrorEmergency = getNull<IStorage>() with get, set

        and LampDef (name:string, system:DsSystem,lampType: LampType, valueParamIO:ValueParamIO, addr:Addresses, flows:Flow seq) =
            inherit HwSystemDef(name, system, flows, valueParamIO, defaultTaskDevParamIO(), addr) //inAddress lamp check bit
            member x.LampType = lampType

        and ConditionDef (name:string, system:DsSystem, conditionType: ConditionType, valueParamIO:ValueParamIO, addr:Addresses, flows:Flow seq) =
            inherit HwSystemDef(name, system, flows, valueParamIO, defaultTaskDevParamIO(), addr) // outAddress condition check bit
            member x.ConditionType = conditionType
            member val ErrorCondition = getNull<IStorage>() with get, set

        and ActionDef (name:string, system:DsSystem, actionType: ActionType, valueParamIO:ValueParamIO, addr:Addresses, flows:Flow seq) =
            inherit HwSystemDef(name, system, flows, valueParamIO, defaultTaskDevParamIO(), addr) // outAddress condition check bit
            member x.ActionType = actionType

        type OperatorFunction with
            static member Create(name:string,  excuteCode:string) =
                let op = OperatorFunction(name)
                updateOperator op excuteCode
                op

        type CommandFunction with
            static member Create(name:string, excuteCode:string) =
                let cmd = CommandFunction(name)
                cmd.CommandCode <- excuteCode
                cmd

    type DsGraph = TDsGraph<Vertex, Edge>
    type Direct = Real
