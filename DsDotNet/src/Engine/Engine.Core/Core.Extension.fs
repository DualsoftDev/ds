// Copyright (c) Dualsoft  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreExtensionModule =
    let tryGetTaskDevParamInOut (paramInOutText:string) =
        match paramInOutText.Split(',') |> Seq.toList with
        | tx::rx when rx.Length = 1 -> Some (getTaskDevParam tx,  getTaskDevParam rx.Head)
        | _-> None

    let tryGetHwSysValueParamInOut (paramInOutText:string) =
        match paramInOutText.Split(',') |> Seq.toList with
        | tx::rx when rx.Length = 1 -> Some (tx,  rx.Head)
        | _-> None
        

    let checkHwSystem(system:DsSystem, targetFlow:Flow, itemName:string) =
        if system <> targetFlow.System then
            failwithf $"add item [{itemName}] in flow ({targetFlow.System.Name} != {system.Name}) is not same system"
        if itemName.Contains('.') then
            failwithf $"[{targetFlow}]{itemName} Error: \r\n이름 '.' 포함되서는 안됩니다."
           
    let getButtons (sys:DsSystem, btnType:BtnType) = sys.HwSystemDefs.OfType<ButtonDef>().Where(fun f->f.ButtonType = btnType)
    let getLamps (sys:DsSystem, lampType:LampType) = sys.HwSystemDefs.OfType<LampDef>().Where(fun f->f.LampType = lampType)
    let getConditions (sys:DsSystem, cType:ConditionType) = sys.HwSystemDefs.OfType<ConditionDef>().Where(fun f->f.ConditionType = cType)
    let getActions (sys:DsSystem, aType:ActionType) = sys.HwSystemDefs.OfType<ActionDef>().Where(fun f->f.ActionType = aType)

    let getRecursiveLoadeds (system:DsSystem) =
        let loadeds = Dictionary<LoadedSystem, DsSystem>()

        let rec recLoadeds(sys:LoadedSystem) =
            sys.ReferenceSystem
               .LoadedSystems
               .Where(fun w->not <| loadeds.ContainsKey w)// external 참조 자식들은 LoadedSystem 이 같다
               .Iter(fun s->
                    loadeds.Add(s, s.ReferenceSystem)
                    recLoadeds s
                )

        system.LoadedSystems.Iter(fun s ->loadeds.Add(s, s.ReferenceSystem)) //최상위 부터 등록
        system.LoadedSystems.Iter(recLoadeds)

        loadeds

    let tryGetPure(v:Vertex) : Vertex option =
        match v with
        | ( :? Real | :? Call) -> Some v
        | :? Alias  as a  ->
            let target = a.TargetWrapper.GetTarget()
            match target with
            | ( :? Real | :? Call) -> Some target
            | _ -> None
        |_ -> None

    let getPure(v:Vertex) : Vertex =
        tryGetPure v |> Option.defaultWith(fun () -> failwithlog $"ERROR: Failed to getPure({v})")

    /// Real 자신이거나 RealEx Target Real
    let tryGetPureReal(v:Vertex) : Real option=
        match tryGetPure(v) with
        | Some (:? Real as r) -> Some r
        | _ -> None

    /// Call 자신이거나 Alias Target Call
    let tryGetPureCall(v:Vertex) : Call option =
        match tryGetPure(v) with
        | Some (:? Call as c) -> Some c
        | _ -> None


    type DsSystem with
        member x.HWButtons    = x.HwSystemDefs.OfType<ButtonDef>()
        member x.HWConditions = x.HwSystemDefs.OfType<ConditionDef>()
        member x.HWActions    = x.HwSystemDefs.OfType<ActionDef>()
        member x.HWLamps      = x.HwSystemDefs.OfType<LampDef>()
        member x.LayoutInfos =
            x.LoadedSystems
            |> Seq.collect(fun s->
                s.ChannelPoints
                    .Where(fun kv -> kv.Key <> TextEmtpyChannel)
                    .Select(fun kv ->
                        let path = kv.Key
                        let xywh = kv.Value
                        let chName, url = path.Split(';')[0], path.Split(';')[1]
                        let typeScreen = if url = TextImageChannel
                                            then ScreenType.IMAGE
                                            else ScreenType.CCTV
                        { DeviceName = s.LoadedName; ChannelName = chName; Path= url; ScreenType = typeScreen; Xywh = xywh }))


        member x.AddButtonDef(btnType:BtnType, btnName:string, valueParamIO:ValueParamIO,  addr:Addresses, flow:Flow option) =
            if flow.IsSome then
                checkHwSystem(x, flow.Value, btnName)
          
         
            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn when flow.IsSome -> btn.SettingFlows.Add(flow.Value) |> verifyM $"중복 Button [flow:{flow.Value.Name} name:{btnName}]"
            | _ ->
                let flows = HashSet[match flow with | Some f -> f | None -> ()]
                x.HwSystemDefs.Add(ButtonDef(btnName,x, btnType, valueParamIO, addr, flows))
                |> verifyM $"중복 ButtonDef [name:{btnName}]"

        member x.AddLampDef(lmpType:LampType, lmpName: string, valueParamIO:ValueParamIO, addr:Addresses, flow:Flow option) =
            if not (valueParamIO.IsDefaultParam)
            then 
                failwithf $"LampDef [{lmpName}] Error: \r\nLamp는 타겟 Value 속성을 지정할 수 없습니다. 기본(true)"

            if flow.IsSome then
                checkHwSystem(x, flow.Value, lmpName)


            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> failwithf $"램프타입[{lmpType}]{lmpName}이 다른 Flow에 중복 정의 되었습니다.  위치:[{lmp.SettingFlows.First().Name}]"
            | None ->
                let flows = HashSet[match flow with | Some f -> f | None -> ()]
                x.HwSystemDefs.Add(LampDef(lmpName, x,lmpType, valueParamIO, addr, flows))
                |> verifyM $"중복 LampDef [name:{lmpName}]"
        
        
        // Method for adding actions, passing the ActionType and setting isCondition = false
        member x.AddAction(actionType: ActionType, actionName: string, valueParamIO:ValueParamIO, addr: Addresses, flow: Flow option) =
                        if addr.Out = TextSkip then
                            failwithf $"ActionDef [{actionName}] Error: \r\nOutput Address는 Skip 할 수 없습니다."

                        x.AddDefinition(None, Some(actionType), actionName, valueParamIO, addr, flow, false)
        // Method for adding conditions, passing the ConditionType and setting isCondition = true
        member x.AddCondition(condiType: ConditionType, condiName: string, valueParamIO:ValueParamIO, addr: Addresses, flow: Flow option) =
                       if addr.In = TextSkip then
                            failwithf $"ConditionDef [{condiName}] Error: \r\nInput Address는 Skip 할 수 없습니다."

                       x.AddDefinition(Some(condiType), None, condiName, valueParamIO, addr, flow, true)

        member private x.AddDefinition(condiType: ConditionType option, actionType: ActionType option, defName: string, valueParamIO:ValueParamIO, addr: Addresses, flow: Flow option, isCondition: bool) =
            if flow.IsSome then
                checkHwSystem(x, flow.Value, defName)

            let typeText = if isCondition then "Condition" else "Action"
            
            let flows = HashSet[match flow with | Some f -> f | None -> ()]
            if isCondition
            then
                match x.HWConditions.TryFind(fun f -> f.Name = defName) with
                | Some def when flow.IsSome ->
                    def.SettingFlows.Add(flow.Value) |> verifyM $"중복 {typeText} [flow:{flow.Value.Name} name:{defName}]"
                | _ ->
                    x.HwSystemDefs.Add(ConditionDef(defName, x, condiType.Value, valueParamIO, addr, flows))
                    |> verifyM $"중복 ConditionDef [flowname:{defName}]"
            else
                match x.HWActions.TryFind(fun f -> f.Name = defName) with
                | Some def when flow.IsSome ->
                    def.SettingFlows.Add(flow.Value) |> verifyM $"중복 {typeText} [flow:{flow.Value.Name} name:{defName}]"
                | _ ->
                    x.HwSystemDefs.Add(ActionDef(defName, x, actionType.Value, valueParamIO, addr, flows))
                    |> verifyM $"중복 ActionDef [name:{defName}]"
  

        member x.LayoutCCTVs = x.LayoutInfos  |> Seq.filter(fun f->f.ScreenType = ScreenType.CCTV)  |> Seq.map(fun f->f.ChannelName, f.Path)  |> distinct
        member x.LayoutImages = x.LayoutInfos |> Seq.filter(fun f->f.ScreenType = ScreenType.IMAGE) |> Seq.map(fun f->f.ChannelName) |> distinct

        member x.AutoHWButtons        = getButtons(x, DuAutoBTN)
        member x.ManualHWButtons      = getButtons(x, DuManualBTN)
        member x.DriveHWButtons       = getButtons(x, DuDriveBTN)
        member x.PauseHWButtons       = getButtons(x, DuPauseBTN)
        member x.ClearHWButtons       = getButtons(x, DuClearBTN)
        member x.EmergencyHWButtons   = getButtons(x, DuEmergencyBTN)
        member x.TestHWButtons        = getButtons(x, DuTestBTN)
        member x.HomeHWButtons        = getButtons(x, DuHomeBTN)
        member x.ReadyHWButtons       = getButtons(x, DuReadyBTN)

        member x.IdleHWLamps          = getLamps(x, DuIdleModeLamp)
        member x.AutoHWLamps          = getLamps(x, DuAutoModeLamp)
        member x.ManualHWLamps        = getLamps(x, DuManualModeLamp)
        member x.DriveHWLamps         = getLamps(x, DuDriveStateLamp)
        member x.ErrorLamps           = getLamps(x, DuErrorStateLamp)
        member x.TestHWLamps          = getLamps(x, DuTestDriveStateLamp)
        member x.ReadyHWLamps         = getLamps(x, DuReadyStateLamp)
        member x.OriginHWLamps        = getLamps(x, DuOriginStateLamp)
        member x.ErrorHWLamps         = getLamps(x, DuErrorStateLamp)

        member x.ReadyConditions      = getConditions(x, DuReadyState)
        member x.DriveConditions      = getConditions(x, DuDriveState)
        member x.EmergencyActions     = getActions(x, DuEmergencyAction)
        member x.PauseActions         = getActions(x, DuPauseAction)

        member x.GetMutualResetApis(src:ApiItem) =
            let getMutual(apiInfo:ApiResetInfo) =
                match apiInfo.Operator with
                | ModelingEdgeType.Interlock  ->
                    match src.Name = apiInfo.Operand1, src.Name = apiInfo.Operand2 with
                    | true, false -> Some apiInfo.Operand2
                    | false, true -> Some apiInfo.Operand1
                    | _ -> None
                | ModelingEdgeType.ResetEdge ->
                    match src.Name = apiInfo.Operand1 with
                    | true -> Some apiInfo.Operand2
                    | _ -> None
                | ModelingEdgeType.RevResetEdge ->
                    match src.Name = apiInfo.Operand2 with
                    | true -> Some apiInfo.Operand1
                    | _ -> None
                | _ -> None

            let resets = x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)

            resets.Select(fun s->x.ApiItems.Find(fun f -> f.DequotedQualifiedName = $"{x.Name}.{s.Value}"))

        member x.LoadedSysExist (name:string) = x.LoadedSystems.Select(fun f -> f.Name).Contains(name)
        member x.GetLoadedSys   (loadSys:DsSystem) = x.LoadedSystems.TryFind(fun f-> f.ReferenceSystem = loadSys)

    let getType (xs:TaskDevParam seq) =
        let types = xs.Map(fun f -> f.DataType).Distinct().ToArray()
        if types.Any() then
            if types.Length > 1 then
                let msg = "dataType miss matching error " + String.Join(",", types.Select(fun f -> f.ToText()))
                failwithlog msg
            else
                types.First()
        else
            DuBOOL


    type TaskDev with
        member x.IsInAddressEmpty            = x.InAddress  = TextAddrEmpty
        member x.IsInAddressSkipOrEmpty      = x.InAddress  = TextAddrEmpty || x.InAddress = TextSkip
        member x.IsOutAddressEmpty           = x.OutAddress = TextAddrEmpty
        member x.IsOutAddressSkipOrEmpty     = x.OutAddress = TextAddrEmpty || x.OutAddress = TextSkip
        member x.IsAddressEmpty              = x.IsInAddressEmpty  && x.IsOutAddressEmpty
        member x.IsAddressSkipOrEmpty        = x.IsOutAddressSkipOrEmpty  && x.IsInAddressSkipOrEmpty
        member x.IsMaunualAddressEmpty       = x.MaunualAddress = TextAddrEmpty
        member x.IsMaunualAddressSkipOrEmpty = x.MaunualAddress = TextAddrEmpty || x.MaunualAddress = TextSkip

        member x.SetInSymbol(symName:string)  =  x.TaskDevParamIO.InParam.Symbol <- symName
        member x.SetOutSymbol(symName:string) =  x.TaskDevParamIO.OutParam.Symbol <- symName
           
        member x.InDataType  = x.TaskDevParamIO.InParam.DataType
        member x.OutDataType  = x.TaskDevParamIO.OutParam.DataType

    type Job with

        member x.GetNullAddressDevTask() =
            let cnt = x.TaskDefs.Count()
            x.TaskDefs
            |> Seq.mapi (fun i d ->
                match x.AddressInCount, x.AddressOutCount with
                | inCnt, outCnt when (inCnt =cnt && outCnt =cnt ) ->None 
                | inCnt, outCnt ->
                    let inNullAddr = i < inCnt && d.IsInAddressEmpty
                    let outNullAddr = i < outCnt && d.IsOutAddressEmpty

                    if inNullAddr || outNullAddr then Some d else None
            )
            |> Seq.choose id

    let getTime  (time:uint32 option, nameFqdn:string)=
        let maxShortSpeedMSec =TimerModule.MinTickInterval|>float
        let v =
            time |> bind(fun t ->
                if RuntimeDS.Package.IsPackageSIM() then
                    match RuntimeDS.TimeSimutionMode  with
                    | TimeSimutionMode.TimeNone -> None
                    | TimeSimutionMode.TimeX1 ->   Some ((t|>float)* 1.0/1.0 )
                    | TimeSimutionMode.TimeX2 ->   Some ((t|>float)* 1.0/2.0 )
                    | TimeSimutionMode.TimeX4 ->   Some ((t|>float)* 1.0/4.0 )
                    | TimeSimutionMode.TimeX8 ->   Some ((t|>float)* 1.0/8.0 )
                    | TimeSimutionMode.TimeX16 ->  Some ((t|>float)* 1.0/16.0 )
                    | TimeSimutionMode.TimeX100 -> Some ((t|>float)* 1.0/100.0 )
                    | TimeSimutionMode.TimeX0_1 -> Some ((t|>float)* 1.0/0.1 )
                    | TimeSimutionMode.TimeX0_5 -> Some ((t|>float)* 1.0/0.5 )
                else
                    Some (t|>float)
                    )

        if v.IsSome && v.Value < maxShortSpeedMSec then
            failwithf $"시뮬레이션 배속을 재설정 하세요.현재설정({RuntimeDS.TimeSimutionMode}) {nameFqdn}
                        \r\n[최소동작시간 : {maxShortSpeedMSec}, 배속반영 동작 시간 : {v.Value}]"
        else
            v

    type Real with

        member x.TimeAvg = getTime (x.DsTime.AVG, x.QualifiedName)

        member x.TimeAvgExist = x.TimeAvg.IsSome && x.TimeAvg.Value <> 0.0

        member x.TimeSimMsec =
            if x.TimeAvg.IsNone then
                failwithf $"Error  TimeAvgMsec ({x.QualifiedName})"
            else
                x.TimeAvg.Value|>uint32

        member x.TimeStd = x.DsTime.STD

        member x.NoneAction = x.Motion.IsNone &&  x.Script.IsNone

        member x.ErrGoingOrigin = x.ExternalTags[ErrGoingOrigin]

        member x.MotionStartTag = x.ExternalTags[MotionStart]
        member x.ScriptStartTag = x.ExternalTags[ScriptStart]

        member x.MotionEndTag = x.ExternalTags[MotionEnd]
        member x.ScriptEndTag = x.ExternalTags[ScriptEnd]


    let getCallName (x:Call) =
        let getFuncName(f:DsFunc) = "#"+f.Name.QuoteOnDemand()
        match x.JobOrFunc with
        | JobType job ->
            let jobFqdn = job.NameComponents
            let valueParamText =  
                if x.ValueParamIO.IsDefaultParam
                then ""
                else $"({x.ValueParamIO.In.ToText()} {TextInOutSplit} {x.ValueParamIO.Out.ToText()})"

            let callOwnerFlow =  x.Parent.GetFlow().Name
            let jobOwnerFlow =  jobFqdn.Head()
            if callOwnerFlow = jobOwnerFlow then
                $"{jobFqdn.Skip(1).CombineQuoteOnDemand()}{valueParamText}"
            else
                $"{jobFqdn.CombineQuoteOnDemand()}{valueParamText}" //다른 Flow는 skip flow 없음

        | CommadFuncType func   -> getFuncName func
        | OperatorFuncType func ->  getFuncName func


    type Call with
        member x.IsFlowCall = x.Parent.GetCore() :? Flow
        member x.Flow = x.Parent.GetFlow()
        member x.NameForGraph = getCallName x
        member x.NameForProperty = (x.DequotedQualifiedName.Split('.')[1..]).CombineQuoteOnDemand()

        member x.System = x.Parent.GetSystem()
        member x.ErrorSensorOn        = x.ExternalTags[ErrorSensorOn    ]
        member x.ErrorSensorOff       = x.ExternalTags[ErrorSensorOff   ]
        member x.ErrorOnTimeOver      = x.ExternalTags[ErrorOnTimeOver  ]
        member x.ErrorOnTimeUnder     = x.ExternalTags[ErrorOnTimeUnder ]
        member x.ErrorOffTimeOver     = x.ExternalTags[ErrorOffTimeOver ]
        member x.ErrorOffTimeUnder    = x.ExternalTags[ErrorOffTimeUnder]
        member x.ErrorInterlock       = x.ExternalTags[ErrorInterlock]

    let inValidTaskDevTags (x:DsSystem) =
        x.Jobs
        |> Seq.collect(fun j-> j.TaskDefs)
        |> Seq.collect(fun d->
                [
                    yield d, "INPUT",d.InAddress
                    yield d, "OUTPUT",d.OutAddress
                ])
        |> Seq.filter(fun (_, _, addr) -> addr = TextAddrEmpty)
        |> Seq.map(fun (d, inout, _) -> $"{d.QualifiedName} <-{inout}")

    let inValidHwSystemTag (x:DsSystem) =
        x.HwSystemDefs
        |> Seq.collect(fun h ->
            [
                match h with
                | (:? ButtonDef | :? ConditionDef ) -> yield h, "INPUT", h.InAddress
                | :? LampDef -> yield h, "OUTPUT", h.OutAddress
                | _  -> failwith $"inValidHwSystemTag error {h.Name}"
            ])
        |> Seq.filter(fun (_, _, addr) -> addr = TextAddrEmpty)
        |> Seq.map(fun (h, inout, _) -> $"{h.Name} <-{inout}")


[<Extension>]
type SystemExt =
    [<Extension>]
    static member GetRecursiveLoadeds (x:DsSystem) : LoadedSystem seq = getRecursiveLoadeds(x).Keys
    [<Extension>]
    static member GetRecursiveLoadedSystems (x:DsSystem) : DsSystem seq = getRecursiveLoadeds(x).Values
    [<Extension>]
    static member GetButtons (x:DsSystem, btnType:BtnType) :IEnumerable<ButtonDef> = getButtons(x, btnType)
    [<Extension>]
    static member GetLamps (x:DsSystem, lampType:LampType) :IEnumerable<LampDef> = getLamps(x, lampType)
    [<Extension>]
    static member ValidTaskDevTag(x:DsSystem) = x |> inValidTaskDevTags |> Seq.any
    [<Extension>]
    static member ValidHwSystemTag(x:DsSystem) = x |> inValidHwSystemTag |> Seq.any
    [<Extension>]
    static member CheckValidInterfaceNchageParsingAddress(x:DsSystem) =
        let inValidTaskDevTags = inValidTaskDevTags(x);
        let inValidHwSystemTag = inValidHwSystemTag(x);
        if inValidTaskDevTags.Any() then
            failwith <| "Add I/O Table을 수행하세요\n\n" + String.Join("\n", inValidTaskDevTags)
        if inValidHwSystemTag.Any() then
            failwith <| "HW 조작 IO Table을 작성하세요\n\n" + String.Join("\n", inValidHwSystemTag)



    [<Extension>]
    static member GetDevice(x:TaskDev, sys:DsSystem) = sys.Devices.First(fun f->f.Name  = x.DeviceName)

    /// System 하부의 storages 반환.
    ///
    /// - skipInternal: 내부 변수 skip 여부
    [<Extension>]
    static member GetStorages(x:DsSystem, skipInternal:bool):IStorage seq =
        x.TagManager.Storages.Values
        |> filter (fun s -> not skipInternal || s.TagKind <> skipValueChangedForTagKind) // 내부변수
        |> distinct

