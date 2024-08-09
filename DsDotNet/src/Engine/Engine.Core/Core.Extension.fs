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

    let checkSystem(system:DsSystem, targetFlow:Flow, itemName:string) =
        if system <> targetFlow.System then
            failwithf $"add item [{itemName}] in flow ({targetFlow.System.Name} != {system.Name}) is not same system"

    let getButtons (sys:DsSystem, btnType:BtnType) = sys.HwSystemDefs.OfType<ButtonDef>().Where(fun f->f.ButtonType = btnType)
    let getLamps (sys:DsSystem, lampType:LampType) = sys.HwSystemDefs.OfType<LampDef>().Where(fun f->f.LampType = lampType)
    let getConditions (sys:DsSystem, cType:ConditionType) = sys.HwSystemDefs.OfType<ConditionDef>().Where(fun f->f.ConditionType = cType)


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

        member x.AddButton(btnType:BtnType, btnName:string, taskDevParamIO:TaskDevParamIO,  addr:Addresses, flow:Flow) =
            checkSystem(x, flow, btnName)

            let existBtns =
                x.HWButtons.Where(fun f->f.ButtonType = btnType)
                |> Seq.filter(fun b -> b.SettingFlows.Contains(flow))

            if existBtns.Any(fun w->w.Name = btnName) then
                failwithf $"버튼타입[{btnType}]{btnName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"중복 Button [flow:{flow.Name} name:{btnName}]"
            | None ->
                x.HwSystemDefs.Add(ButtonDef(btnName,x, btnType, taskDevParamIO, addr, HashSet[|flow|]))
                |> verifyM $"중복 ButtonDef [flow:{flow.Name} name:{btnName}]"

        member x.AddButton(btnType:BtnType, btnName:string, inAddress:string, outAddress:string, flow:Flow) =
            x.AddButton(btnType, btnName,  defaultTaskDevParamIO(), Addresses(inAddress ,outAddress), flow)

        member x.AddLamp(lmpType:LampType, lmpName: string, taskDevParamIO:TaskDevParamIO, addr:Addresses, flow:Flow option) =
            if flow.IsSome then
                checkSystem(x, flow.Value, lmpName)

            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> failwithf $"램프타입[{lmpType}]{lmpName}이 다른 Flow에 중복 정의 되었습니다.  위치:[{lmp.SettingFlows.First().Name}]"
            | None ->
                let flows = HashSet[match flow with | Some f -> f | None -> ()]
                x.HwSystemDefs.Add(LampDef(lmpName, x,lmpType, taskDevParamIO, addr, flows))
                |> verifyM $"중복 LampDef [name:{lmpName}]"

        member x.AddLamp(lmpType:LampType, lmpName:string, inAddress:string, outAddress:string,  flow:Flow option) =
            x.AddLamp(lmpType, lmpName, defaultTaskDevParamIO(), Addresses(inAddress ,outAddress),  flow)


        member x.AddCondtion(condiType:ConditionType, condiName: string, taskDevParamIO:TaskDevParamIO, addr:Addresses, flow:Flow) =
            checkSystem(x, flow, condiName)

            match x.HWConditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"중복 Condtion [flow:{flow.Name} name:{condiName}]"
            | None ->
                x.HwSystemDefs.Add(ConditionDef(condiName,x, condiType, taskDevParamIO,  addr, HashSet[|flow|]))
                |> verifyM $"중복 ConditionDef [flow:{flow.Name} name:{condiName}]"

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddress:string, outAddress:string, flow:Flow) =
                x.AddCondtion(condiType, condiName, defaultTaskDevParamIO(), Addresses(inAddress ,outAddress), flow)

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

        member x.ReadyConditions     = getConditions(x, DuReadyState)
        member x.DriveConditions     = getConditions(x, DuDriveState)

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
        let types = xs.Choose(fun f -> f.DevType).Distinct().ToArray()
        if types.Any() then
            if types.Length > 1 then
                failwithlog $"dataType miss matching error {String.Join(',', types.Select(fun f -> f.ToText()))}"
            else
                types.First()
        else
            DuBOOL


    type TaskDev with

        member x.FirstApi = x.ApiItems.First()
        member x.IsAnalogSensor = x.InTag.DataType <> typedefof<bool>
        member x.IsAnalogActuator = x.OutTag.DataType <> typedefof<bool>


        member x.GetInParam(jobFqdn:string) =
            match x.DicTaskTaskDevParamIO[jobFqdn].TaskDevParamIO.InParam with
            | Some v -> v
            | None -> defaultTaskDevParam()
        member x.GetInParam(job:Job) = x.GetInParam (job.DequotedQualifiedName)

        member x.GetOutParam(jobFqdn:string) =
            match x.DicTaskTaskDevParamIO[jobFqdn].TaskDevParamIO.OutParam with
            | Some v -> v
            | None -> defaultTaskDevParam()
        member x.GetOutParam(job:Job) = x.GetOutParam (job.DequotedQualifiedName)

        member x.GetApiParam(jobFqdn:string) = x.DicTaskTaskDevParamIO[jobFqdn]
        member x.GetApiParam(job:Job) = x.GetApiParam(job.DequotedQualifiedName)

        member x.GetApiItem(jobFqdn:string) = x.GetApiParam(jobFqdn).ApiItem
        member x.GetApiItem(job:Job) = x.GetApiParam(job.DequotedQualifiedName).ApiItem

        ///LoadedSystem은 이름을 재정의 하기 때문에 ApiName을 제공 함
        member x.GetApiStgName(jobFqdn:string) = $"{x.DeviceName}_{x.GetApiItem(jobFqdn).Name}"
        member x.GetApiStgName(job:Job) =  x.GetApiStgName(job.DequotedQualifiedName)

        member x.DeviceApiPureName(jobFqdn:string) = $"{x.DeviceName}.{x.GetApiItem(jobFqdn).PureName}"  //STN2.Device1."ROTATE(IN300_OUT400)"  파레메터 없는 ROTATE 순수이름만
        member x.DeviceApiPureName(job:Job) = x.DeviceApiPureName(job.DequotedQualifiedName)

        member x.DeviceApiToDsText(jobFqdn:string) = $"{x.DeviceName.QuoteOnDemand()}.{x.GetApiItem(jobFqdn).Name.QuoteOnDemand()}"
        member x.DeviceApiToDsText(job:Job) = x.DeviceApiToDsText(job.DequotedQualifiedName)


        member x.AddOrUpdateApiTaskDevParam(jobFqdn:string, api:ApiItem, taskDevParamIO:TaskDevParamIO) =
            if x.ApiItems.any(fun f->f.PureName <> api.PureName) then
                failwithf $"ApiItem이 다릅니다. {x.QualifiedName} {api.QualifiedName}"

            if not (x.DicTaskTaskDevParamIO.ContainsKey jobFqdn) then
                x.DicTaskTaskDevParamIO.Add(jobFqdn, {TaskDevParamIO = taskDevParamIO; ApiItem = api})
            else
                ()
                //failWithLog $"중복된 TaskDevParamIO {jobFqdn} {x.QualifiedName}"

        member x.AddOrUpdateApiTaskDevParam(job:Job, api:ApiItem, taskDevParamIO:TaskDevParamIO) =
            x.AddOrUpdateApiTaskDevParam(job.DequotedQualifiedName, api, taskDevParamIO)

        member x.IsInAddressEmpty            = x.InAddress  = TextAddrEmpty
        member x.IsInAddressSkipOrEmpty      = x.InAddress  = TextAddrEmpty || x.InAddress = TextSkip
        member x.IsOutAddressEmpty           = x.OutAddress = TextAddrEmpty
        member x.IsOutAddressSkipOrEmpty     = x.OutAddress = TextAddrEmpty || x.OutAddress = TextSkip
        member x.IsAddressEmpty              = x.IsInAddressEmpty  && x.IsOutAddressEmpty
        member x.IsAddressSkipOrEmpty        = x.IsOutAddressSkipOrEmpty  && x.IsInAddressSkipOrEmpty
        member x.IsMaunualAddressEmpty       = x.MaunualAddress = TextAddrEmpty
        member x.IsMaunualAddressSkipOrEmpty = x.MaunualAddress = TextAddrEmpty || x.MaunualAddress = TextSkip

        member x.SetInSymbol(symName:string option) =
            if symName.IsSome then
                for param in x.DicTaskTaskDevParamIO.Values do
                    match param.TaskDevParamIO.InParam with
                    | Some inParam ->
                        inParam.DevName <- symName
                    | None ->
                        param.TaskDevParamIO.InParam <- Some(TaskDevParam(symName, None, None, None))

        member x.SetOutSymbol(symName:string option) =
            if symName.IsSome then
                for param in x.DicTaskTaskDevParamIO.Values do
                    match param.TaskDevParamIO.OutParam with
                    | Some outParam ->
                        outParam.DevName <- symName
                    | None ->
                        param.TaskDevParamIO.OutParam <- Some(TaskDevParam(symName, None, None, None))


        member x.InDataType  = getType (x.InParams)
        member x.OutDataType = getType (x.OutParams)

    type Job with

        member x.ApiDefs = x.TaskDefs |> Seq.collect(fun t->t.ApiItems)
        //member x.OnDelayTime =  //test ahn
        //    let times = x.TaskDefs.Choose(fun t-> t.InParams[x.DequotedQualifiedName].Time)
        //    if times.GroupBy(fun t->t).Count() > 1
        //    then
        //        let errTask = String.Join(", ",  x.TaskDefs.Select(fun t-> $"{t.Name} {t.InParams[x.DequotedQualifiedName].Time}"))
        //        failWithLog $"다른 시간이 설정된 tasks가 있습니다. {errTask}"

        //    if times.any() then times.First() |> Some else None

        member x.GetNullAddressDevTask() =
            x.TaskDefs
            |> Seq.mapi (fun i d ->
                match x.JobTaskDevInfo.InCount, x.JobTaskDevInfo.OutCount with
                | None, None -> None
                | inCntOpt, outCntOpt ->
                    let inCnt = Option.defaultValue 0 inCntOpt
                    let outCnt = Option.defaultValue 0 outCntOpt
                    let inNullAddr = i < inCnt && d.IsInAddressEmpty
                    let outNullAddr = i < outCnt && d.IsOutAddressEmpty

                    if inNullAddr || outNullAddr then Some d else None
            )
            |> Seq.choose id

    let getTime  (time:float option, nameFqdn:string)=
        let maxShortSpeedSec = (float TimerModule.MinTickInterval) / 1000.0
        let v =
            time |> bind(fun t ->
                if RuntimeDS.Package.IsPackageSIM() then
                    match RuntimeDS.TimeSimutionMode  with
                    | TimeSimutionMode.TimeNone -> None
                    | TimeSimutionMode.TimeX1 ->   Some (t * 1.0/1.0 )
                    | TimeSimutionMode.TimeX2 ->   Some (t * 1.0/2.0 )
                    | TimeSimutionMode.TimeX4 ->   Some (t * 1.0/4.0 )
                    | TimeSimutionMode.TimeX8 ->   Some (t * 1.0/8.0 )
                    | TimeSimutionMode.TimeX16 ->  Some (t * 1.0/16.0 )
                    | TimeSimutionMode.TimeX100 -> Some (t * 1.0/100.0 )
                    | TimeSimutionMode.TimeX0_1 -> Some (t * 1.0/0.1 )
                    | TimeSimutionMode.TimeX0_5 -> Some (t * 1.0/0.5 )
                else
                    Some t)

        if v.IsSome && v.Value < maxShortSpeedSec then
            failwithf $"시뮬레이션 배속을 재설정 하세요.현재설정({RuntimeDS.TimeSimutionMode}) {nameFqdn}
                        \r\n[최소동작시간 : {maxShortSpeedSec}, 배속반영 동작 시간 : {v.Value}]"
        else
            v

    type Real with

        member x.TimeDelay = getTime (x.DsTime.TON, x.QualifiedName)
        member x.TimeAvg = getTime (x.DsTime.AVG, x.QualifiedName)

        member x.TimeDelayExist = x.TimeDelay.IsSome && x.TimeDelay.Value <> 0.0
        member x.TimeAvgExist = x.TimeAvg.IsSome && x.TimeAvg.Value <> 0.0

        member x.TimeAvgMsec =
            if x.TimeAvg.IsNone then
                failwithf $"Error  TimeAvgMsec ({x.QualifiedName})"
            else
                Convert.ToUInt32( x.TimeAvg.Value *1000.0 )

        member x.TimeStd = x.DsTime.STD

        member x.NoneAction = x.Motion.IsNone &&  x.Script.IsNone

        member x.ErrGoingOrigin = x.ExternalTags.First(fun (t,_)-> t = ErrGoingOrigin)|> snd

        member x.MotionStartTag = x.ExternalTags.First(fun (t,_)-> t = MotionStart)|> snd
        member x.ScriptStartTag = x.ExternalTags.First(fun (t,_)-> t = ScriptStart)|> snd

        member x.MotionEndTag = x.ExternalTags.First(fun (t,_)-> t = MotionEnd)|> snd
        member x.ScriptEndTag = x.ExternalTags.First(fun (t,_)-> t = ScriptEnd)|> snd


    let getCallName (x:Call) =
        match x.JobOrFunc with
        | JobType job ->
            let jobFqdn = job.NameComponents
            let callOwnerFlow =  x.Parent.GetFlow().Name
            let jobOwnerFlow =  jobFqdn.Head()
            if callOwnerFlow = jobOwnerFlow then
                jobFqdn.Skip(1).CombineQuoteOnDemand()
            else
                jobFqdn.CombineQuoteOnDemand() //다른 Flow는 skip flow 없음

        | CommadFuncType func -> func.Name.QuoteOnDemand()+"()"
        | OperatorFuncType func -> "#"+func.Name.QuoteOnDemand()


    type Call with
        member x.IsFlowCall = x.Parent.GetCore() :? Flow
        member x.NameForGraph = getCallName x

        member x.System = x.Parent.GetSystem()
        member x.ErrorSensorOn        = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOn)       |> snd
        member x.ErrorSensorOff       = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOff)      |> snd
        member x.ErrorOnTimeOver      = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeOver)     |> snd
        member x.ErrorOnTimeShortage  = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeShortage) |> snd
        member x.ErrorOffTimeOver     = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeOver)    |> snd
        member x.ErrorOffTimeShortage = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeShortage)|> snd

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
            failwithf $"Add I/O Table을 수행하세요 \n\n{String.Join('\n', inValidTaskDevTags)}"
        if inValidHwSystemTag.Any() then
            failwithf $"HW 조작 IO Table을 작성하세요 \n\n{String.Join('\n', inValidHwSystemTag)}"



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


    //[<Extension>]
    //static member ToTextForTaskDevPara(x:TaskDev, jobName:string) = toTextInOutDev (x.GetInParam(jobName)) (x.GetOutParam(jobName))

    //[<Extension>]
    //static member ToTextForTaskDevPara(x:HwSystemDef) = toTextInOutDev x.InParam x.OutParam

    //[<Extension>]
    //static member IsSensorNot(x:TaskDevParam) =
    //            match x.DevValueNType with
    //            |Some(v, ty) when ty = DuBOOL -> not (Convert.ToBoolean(v))  //RX 기본은 True
    //            |_ -> false

