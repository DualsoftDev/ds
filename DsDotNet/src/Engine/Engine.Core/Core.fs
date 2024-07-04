// Copyright (c) Dualsoft  All Rights Reserved.
// Dualsoft에 저작권이 있습니다. 모든 권한 보유.

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
        RelativeFilePath: string
        /// *.ds 파일에서 정의된 이름과 로딩할 때의 이름이 다를 수 있습니다.
        LoadedName: string
        ShareableSystemRepository: ShareableSystemRepository
        LoadingType: ParserLoadingType
    }

    // 장치 LayoutInfo  정의
    type DeviceLayoutInfo = {
        DeviceName: string
        ChannelName: string
        Path: string
        ScreenType: ScreenType
        Xywh: Xywh
    }

    [<AbstractClass>]
    type LoadedSystem (loadedSystem: DsSystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
        inherit FqdnObject(param.LoadedName, param.ContainerSystem)
        let mutable loadedName = param.LoadedName // 로딩 주체에 따라 런타임에 변경
        do  
            if not(param.AbsoluteFilePath  |> PathManager.isPathRooted)
            then raise (new ArgumentException($"The AbsoluteFilePath must be PathRooted ({param.AbsoluteFilePath})"))
            if param.RelativeFilePath |> PathManager.isPathRooted
            then raise (new ArgumentException($"The RelativeFilePath must be not PathRooted ({param.RelativeFilePath})"))

        interface ISystem 
     
        member _.LoadedName with get() = loadedName and set(value) = loadedName <- value
        member _.AutoGenFromParentSystem  = autoGenFromParentSystem
        
        ///CCTV 경로 및 배경 이미지 경로 복수의 경로에 배치가능
        member val ChannelPoints = Dictionary<string, Xywh>()

        /// 다른 장치를 로딩하려는 시스템에서 로딩된 시스템을 참조합니다.
        member _.ReferenceSystem = loadedSystem
        member _.ContainerSystem = param.ContainerSystem
        member _.RelativeFilePath:string = param.RelativeFilePath
        member _.AbsoluteFilePath:string = param.AbsoluteFilePath
        member _.LoadingType: ParserLoadingType = param.LoadingType

    /// *.ds 파일을 읽어 새로운 인스턴스를 만들어 삽입하는 구조입니다.
    and Device (loadedDevice: DsSystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
        inherit LoadedSystem(loadedDevice, param, autoGenFromParentSystem)
        static let mutable id = 0
        do
            id <- id + 1
        member val Id = id with get

    /// 공유 인스턴스. *.ds 파일의 절대 경로를 기준으로 하나의 인스턴스만 생성하고 이를 참조하는 개념입니다.
    and ExternalSystem (loadedSystem: DsSystem, param: DeviceLoadParameters, autoGenFromParentSystem:bool) =
        inherit LoadedSystem(loadedSystem, param, autoGenFromParentSystem)

    type DsSystem (name: string) =
        inherit FqdnObject(name, createFqdnObject([||]))
        let loadedSystems = createNamedHashSet<LoadedSystem>()
        let apiUsages = ResizeArray<ApiItem>()
        let variables = ResizeArray<VariableData>()
        let actionVariables = ResizeArray<ActionVariable>()

        let addApiItemsForDevice (device: LoadedSystem) = device.ReferenceSystem.ApiItems |> apiUsages.AddRange
        let channelInfos =
            loadedSystems 
            |> Seq.collect(fun s-> 
                s.ChannelPoints.Where(fun kv -> kv.Key <> TextEmtpyChannel)
                               .Select(fun kv ->
                                    let path = kv.Key
                                    let xywh = kv.Value
                                    let chName, url = path.Split(';')[0], path.Split(';')[1]
                                    let typeScreen = if url = TextImageChannel
                                                     then ScreenType.IMAGE  
                                                     else ScreenType.CCTV
                                    { DeviceName = s.LoadedName; ChannelName = chName; Path= url; ScreenType = typeScreen; Xywh = xywh })) 


        interface ISystem 
        member _.AddLoadedSystem(childSys) = 
            loadedSystems.Add(childSys)
            |> verifyM $"중복로드된 시스템 이름 [{childSys.Name}]"
            addApiItemsForDevice childSys

        member _.ReferenceSystems = loadedSystems.Select(fun s -> s.ReferenceSystem)
        member _.LoadedSystems = loadedSystems |> seq
        member _.Devices = loadedSystems.OfType<Device>() |> Seq.toArray 
        member _.ExternalSystems = loadedSystems.OfType<ExternalSystem>() |> Seq.toArray
        member _.LayoutInfos = channelInfos
      
        member _.ApiUsages = apiUsages |> seq
        member val Jobs = ResizeArray<Job>()
        member val Functions = ResizeArray<Func>()

        member _.Variables = variables |> seq
        member _.ActionVariables = actionVariables |> seq

        member val Flows = createNamedHashSet<Flow>()
        ///사용자 정의 API 
        member val ApiItems = createNamedHashSet<ApiItem>()
        ///HW HMI 전용 API (물리 ButtonDef LampDef ConditionDef 정의에 따른 API)
        member val HwSystemDefs = createNamedHashSet<HwSystemDef>()
        member val ApiResetInfos = HashSet<ApiResetInfo>()
        member val StartPoints = createQualifiedNamedHashSet<Real>()

        member x.AddVariables(variableData:VariableData) = 
                if variables.any(fun v->v.Name = variableData.Name)
                then 
                    failWithLog $"중복된 변수가 있습니다. {variableData.Name} "
                else 
                    variables.Add(variableData) 
                    
        member x.AddActionVariables(actionVariable:ActionVariable) = 
                if actionVariables.any(fun v->v.Name = actionVariable.Name)
                then 
                    failWithLog $"중복된 심볼이 있습니다. {actionVariable.Name}({actionVariable.Address})"
                else 
                    actionVariables.Add(actionVariable)

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



    and AliasDef(aliasKey: Fqdn, target: AliasTargetWrapper option, aliasTexts: string [], isOtherFlowRealAlias:bool) =
        member _.AliasKey = aliasKey
        member _.IsOtherFlowRealAlias = isOtherFlowRealAlias // 다른 플로우의 Real 이며 내부에서 Alias로 Or 사용하는 경우
        member val AliasTarget = target with get, set
        member val AliasTexts = aliasTexts |> ResizeArray

    /// leaf or stem(parenting)
    /// Graph 상의 vertex 를 점유하는 named object : Real, Alias, Call
    [<AbstractClass>]
    type Vertex (names:Fqdn, parent:ParentWrapper)  =
        inherit FqdnObject(names.Combine(), parent.GetCore())
        interface INamedVertex
        
        member _.Parent = parent
        
        member _.PureNames = names
        member val Time: int option = None with get, set
        
        member _.ParentNPureNames = ([parent.GetCore().Name] @ names).ToArray()
        override x.GetRelativeName(_referencePath:Fqdn) = x.PureNames.Combine()

    // Subclasses = {Call | Real}
    type ISafetyConditoinHolder =
        abstract member SafetyConditions: HashSet<SafetyCondition>
    // Subclasses = {Call}
    type IAutoPrerequisiteHolder =
        abstract member AutoPreConditions: HashSet<AutoPreCondition>
        abstract member GetAutoPreCall : unit -> Call
         
    /// Indirect to Call/Alias/RealOtherFlow/CallSys
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
        member val ExternalTags = HashSet<ExternalTagSet>()
        member val ParentApiSensorExpr = getNull<IExpression>() with get, set
        //member val RealData:byte[] = [||] with get, set
        member val RealData:byte = 0uy with get, set //array타입으로 향후 변경

        member val DsTime:DsTime = DsTime() with get, set
        member val Finished:bool = false with get, set
        member val NoTransData:bool = false with get, set
        
        member x.Flow = flow

        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()

    type CallType =
        | JobType of Job
        | CommadFuncType of CommandFunction
        | OperatorFuncType of OperatorFunction

    type Call(callType: CallType, parent) =
        inherit Indirect
            (match callType with
            | JobType job -> job.Name
            | CommadFuncType func -> func.Name
            | OperatorFuncType func -> func.Name
            , parent)

        let isJob = function
            | JobType _  -> true
            | _ -> false

        let isCommand = function
            | CommadFuncType _ -> true
            | _ -> false        

        let isOperator = function
            | OperatorFuncType _ -> true
            | _ -> false
            
        member x.TargetJob =
            match callType with
            | JobType job -> job
            | _ -> failwithlog $"{x.QualifiedName} is not JobType."

        member x.TargetFunc =
            match callType with
            | CommadFuncType func -> func :> Func
            | OperatorFuncType func -> func :> Func
            | _ -> failwithlog $"{x.QualifiedName} is not FunctionType."

        /// Indicates if the target includes a job.
        member _.IsJob = isJob callType 
        member _.IsPureCommand = isCommand callType  
        member _.IsPureOperator = isOperator callType  
        member _.CallOperatorType  = 
            match callType with
            | JobType _ -> DuOPUnDefined
            | CommadFuncType _ -> DuOPUnDefined
            | OperatorFuncType func -> func.OperatorType

        member _.CallCommandType  = 
            match callType with
            | JobType _ -> DuCMDUnDefined
            | CommadFuncType func -> func.CommandType
            | OperatorFuncType _ -> DuCMDUnDefined

        member val ExternalTags = HashSet<ExternalTagSet>()
        member val Disabled:bool = false with get, set
        interface ISafetyConditoinHolder with
            member val SafetyConditions = HashSet<SafetyCondition>()
        interface IAutoPrerequisiteHolder with
            member x.GetAutoPreCall(): Call = x
            member val AutoPreConditions = HashSet<AutoPreCondition>()

    and Alias private (names:string seq, target:AliasTargetWrapper, parent, isOtherFlowRealAlias) = // target : Real or Call or OtherFlowReal
        inherit Indirect(names, parent)
        member _.TargetWrapper = target
        member _.IsOtherFlowRealAlias = isOtherFlowRealAlias
        member _.IsSameFlow = target.GetTarget() 
                              |> fun v -> v.Parent.GetFlow() = parent.GetFlow() 

    type InOutDataType = DataType*DataType


     
      /// Main system 에서 loading 된 다른 device 의 API 를 바라보는 관점.  
    ///[jobs] = { Ap = { A."+"(%I1:true:1500, %Q1:true:500); } } job1 = { Dev.Api(InParam, OutParam), Dev... }
    type TaskDev (api:ApiItem, parentJob:string, inParam:DevParam, outParam:DevParam, deviceName:string) =
        inherit FqdnObject(api.Name, createFqdnObject([|deviceName|]))
        let inParams  = Dictionary<string, DevParam>()
        let outParams = Dictionary<string, DevParam>()
        do  
            inParams.Add (parentJob, inParam)
            outParams.Add (parentJob, outParam)

        member x.ApiItem = api
        ///LoadedSystem은 이름을 재정의 하기 때문에 ApiName을 제공 함
        member x.ApiName = (x:>FqdnObject).QualifiedName
        member x.ApiStgName = $"{deviceName}_{api.Name}"
        member x.DeviceName = deviceName

        member x.InParams = inParams 
        member x.OutParams = outParams

      
        member x.InAddress
            with get() = inParams.First().Value  |> fun (d) -> d.DevAddress
            and set(v) = inParams.ToArray().Iter(fun (kv)-> changeParam (kv.Key,inParams, v, kv.Value.DevName))

        member x.OutAddress
            with get() = outParams.First().Value  |> fun (d) -> d.DevAddress
            and set(v) = outParams.ToArray().Iter(fun (kv)-> changeParam (kv.Key,outParams, v, kv.Value.DevName))
   
        member val MaunualActionAddress = TextAddrEmpty with get, set

        //CPU 생성시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        //CPU 생성시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set
        //CPU 생성시 할당됨 MaunualTag

        member val IsRootOnlyDevice = false  with get, set

    /// Job 정의: Call 이 호출하는 Job 항목
    type Job (name:string, system:DsSystem, tasks:TaskDev seq) =
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        let mutable jobParam = JobParam(ActionNormal, JobTypeMulti.Single)
        member x.JobParam = jobParam
        member x.UpdateJobParam(newJobParam: JobParam) =
            jobParam <- newJobParam

        member x.ActionType = x.JobParam.JobAction 
        member x.JobMulti = x.JobParam.JobMulti 
        member x.AddressInCount = x.JobParam.JobMulti.AddressInCount
        member x.AddressOutCount = x.JobParam.JobMulti.AddressOutCount
        member x.System = system
        member x.DeviceDefs = tasks


        member x.ApiDefs = tasks.Select(fun t->t.ApiItem)

    [<AbstractClass>]
    type HwSystemDef (name: string, system:DsSystem, flows:HashSet<Flow>, inParam:DevParam, outParam:DevParam)  =
        inherit FqdnObject(name, system)
        member x.Name = name
        member x.System = system
        member val SettingFlows = flows with get, set
        //SettingFlows 없으면 전역 시스템 설정
        member val IsGlobalSystemHw = flows.IsEmpty()
        member val InParam  = inParam   with get, set
        member val OutParam = outParam  with get, set


        member x.InAddress
            with get() = x.InParam  |> fun (d) -> d.DevAddress
            and set(v) = x.InParam <- changeDevParam  x.InParam v x.InParam.DevName

        member x.OutAddress
            with get() = x.OutParam  |> fun (d) -> d.DevAddress
            and set(v) = x.OutParam <- changeDevParam  x.OutParam v x.OutParam.DevName

        /// CPU 생성 시 할당됨 InTag
        member val InTag = getNull<ITag>() with get, set
        /// CPU 생성 시 할당됨 OutTag
        member val OutTag = getNull<ITag>() with get, set


    and ButtonDef (name: string, system:DsSystem, btnType: BtnType, inDevParam: DevParam, outDevParam: DevParam, flows: HashSet<Flow>) =
        inherit HwSystemDef(name, system, flows, inDevParam, outDevParam)
        member x.ButtonType = btnType
        member val ErrorEmergency = getNull<IStorage>() with get, set

    and LampDef (name: string, system:DsSystem,lampType: LampType, inDevParam: DevParam, outDevParam: DevParam,  flows: HashSet<Flow>) =
        inherit HwSystemDef(name, system, flows, inDevParam, outDevParam) //inAddress lamp check bit
        member x.LampType = lampType

    and ConditionDef (name: string, system:DsSystem, conditionType: ConditionType, inDevParam: DevParam, outDevParam: DevParam,  flows: HashSet<Flow>) =
        inherit HwSystemDef(name,  system, flows, inDevParam, outDevParam) // outAddress condition check bit
        member x.ConditionType = conditionType
        member val ErrorCondition = getNull<IStorage>() with get, set


    /// 자신을 export 하는 관점에서 본 api's.  Interface 정의.   [interfaces] = { "+" = { F.Vp ~ F.Sp } }
    and ApiItem private (name:string, system:DsSystem, timeParam:TimeParam option) =
        (* createFqdnObject : system 이 다른 system 에 포함되더라도, name component 를 더 이상 확장하지 않도록 cut *)
        inherit FqdnObject(name, createFqdnObject([|system.Name|]))
        interface INamedVertex

        member _.Name = name
        member _.ApiSystem = system
        member _.TimeParam = timeParam
      
        member val TX = getNull<Real>() with get, set
        member val RX = getNull<Real>() with get, set
        override x.ToText() = 
            $"{name}\r\n[{x.TX.Name} ~ {x.RX.Name}]"
                 
    /// API 의 reset 정보:  "+" <||> "-";
    and ApiResetInfo private (operand1:string, operator:ModelingEdgeType, operand2:string, autoGenByFlow:bool) =
        member _.AutoGenByFlow = autoGenByFlow 
        member _.Operand1 = operand1  // "+"
        member _.Operand2 = operand2  // "-"
        member _.Operator = operator  // "<|>", "|>", "<|"
        member _.ToDsText() = 
            let src = operand1.QuoteOnDemand()
            let tgt = operand2.QuoteOnDemand()
            sprintf "%s %s %s"  src (operator |> toTextModelEdge) tgt  //"+" <|> "-"
        static member Create(system:DsSystem, operand1, operator, operand2, autoGenByFlow) =
            let ri = ApiResetInfo(operand1, operator, operand2, autoGenByFlow)
            system.ApiResetInfos.Add(ri) |> verifyM $"중복 interface ResetInfo [{ri.ToDsText()}]"
            ri

    (* Abbreviations *)

    type DsGraph = Graph<Vertex, Edge>
    and Direct = Real

    and Edge private (source:Vertex, target:Vertex, edgeType:EdgeType) =
        inherit EdgeBase<Vertex>(source, target, edgeType)

        static member Create(graph:Graph<_,_>, source, target, edgeType:EdgeType) =
            let edge = Edge(source, target, edgeType)
            graph.AddEdge(edge) |> verifyM $"중복 edge [{source.Name}{edgeType.ToText()}{target.Name}]"
            edge

        override x.ToString() = $"{x.Source.QualifiedName} {x.EdgeType.ToText()} {x.Target.QualifiedName}"

    and AliasTargetWrapper =
        | DuAliasTargetReal of Real
        | DuAliasTargetCall of Call
        member x.RealTarget() =
            match x with | DuAliasTargetReal   r -> Some r |_ -> None
        member x.CallTarget() =
            match x with | DuAliasTargetCall   c -> Some c |_ -> None
   

    and AutoPreCondition =
        | DuAutoPreConditionCall of Call
        member x.GetAutoPreCall() =
            match x with
            | DuAutoPreConditionCall c -> c
        member x.Core:obj =
            match x with
            | DuAutoPreConditionCall call -> call
        member x.Name:string =
            match x with
            | DuAutoPreConditionCall call -> call.Name

    and SafetyCondition =
        | DuSafetyConditionReal of Real
        | DuSafetyConditionCall of Call

        member x.GetSafetyCall() =
            match x with
            | DuSafetyConditionReal _ -> None
            | DuSafetyConditionCall c -> Some c

        member x.GetSafetyReal() =
            match x with
            | DuSafetyConditionReal r -> Some r
            | DuSafetyConditionCall _ -> None 

        member x.Core:obj =
            match x with
            | DuSafetyConditionReal real -> real
            | DuSafetyConditionCall call -> call
        member x.Name:string =
            match x with
            | DuSafetyConditionReal real -> real.Name
            | DuSafetyConditionCall call -> call.Name

  
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



    type AliasTargetWrapper with
        member x.GetTarget() : Vertex =
            match x with
            | DuAliasTargetReal real -> real
            | DuAliasTargetCall call -> call

    type Real with
        static member Create(name: string, flow) =
            if (name.Contains ".")  then
                logWarn $"Suspicious segment name [{name}]. Check it."

            let real = Real(name, flow)
            flow.Graph.AddVertex(real) |> verifyM $"중복 segment name [{name}]"
            real

        member x.GetAliasTargetToDs(aliasFlow:Flow) =
                if x.Flow <> aliasFlow
                then [|x.Flow.Name; x.Name|]  //other flow
                else [| x.Name |]             //my    flow
        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions

    type OperatorFunction with
        static member Create(name:string,  excuteCode:string) =
            let op = OperatorFunction(name)
            updateOperator op excuteCode
            op
            
    type CommandFunction with
        static member Create(name:string, excuteCode:string) =
            let cmd = CommandFunction(name)
            cmd.CommandType <- if excuteCode = "" then DuCMDUnDefined else DuCMDCode 
            cmd.CommandCode <- excuteCode
            cmd 


    let addCallVertex(parent:ParentWrapper) call = parent.GetGraph().AddVertex(call) |> verifyM $"중복 call name [{call.Name}]"
    type Call with
        static member Create(target:Job, parent:ParentWrapper) =
            let call = Call(target|>JobType, parent)
            addCallVertex parent call
            call

        static member Create(func:Func, parent:ParentWrapper) =
            let callType = 
                match func with
                | :? CommandFunction -> CommadFuncType (func :?> CommandFunction)
                | :? OperatorFunction -> OperatorFuncType (func :?> OperatorFunction)
                | _->failwithlog "Error"

            let call = Call(callType, parent)
            addCallVertex parent call
            call   

     

        member x.GetAliasTargetToDs() =
            match x.Parent.GetCore() with
                | :? Flow -> [x.Name].ToArray()
                | :? Real -> x.ParentNPureNames
                | _->failwithlog "Error"

        member x.SafetyConditions = (x :> ISafetyConditoinHolder).SafetyConditions
        member x.AutoPreConditions = (x :> IAutoPrerequisiteHolder).AutoPreConditions


    type Alias with
        static member Create(name:string, target:AliasTargetWrapper, parent:ParentWrapper, isOtherFlowRealAlias) =
            let createAliasDefOnDemand(isOtherFlowReal) =
                (* <*.ds> 파일에서 생성하는 경우는 alias 정의가 먼저 선행되지만,
                 * 메모리에서 생성해 나가는 경우는 alias 정의가 없으므로 거꾸로 채워나가야 한다.
                 *)
                let flow:Flow = parent.GetFlow()
                let aliasKey =
                    match target with
                    | DuAliasTargetReal r -> r.GetAliasTargetToDs(flow)
                    | DuAliasTargetCall c -> c.GetAliasTargetToDs()
                let ads = flow.AliasDefs
                match ads.TryFind(aliasKey) with
                | Some ad -> ad.AliasTexts.AddIfNotContains(name) |> ignore
                | None -> ads.Add(aliasKey, AliasDef(aliasKey, Some target, [|name|], isOtherFlowReal))

            createAliasDefOnDemand(isOtherFlowRealAlias)
            let alias = Alias(name.DeQuoteOnDemand().SplitBy('.'), target, parent, isOtherFlowRealAlias)
            if parent.GetCore() :? Real
            then
                target.RealTarget().IsNone
                |> verifyM $"Vertex {name} children type error"

            parent.GetGraph().AddVertex(alias) |> verifyM $"중복 alias name [{name}]"
            alias

    type ApiItem with
        static member Create(name, system) =
            let cp = ApiItem(name, system, None)
            system.ApiItems.Add(cp) |> verifyM $"중복 interface prototype name [{name}]"
            cp
        static member Create(name, system, tx, rx) =
            let ai4e = ApiItem.Create(name, system)
            ai4e.TX <-  tx
            ai4e.RX <-  rx
            ai4e


