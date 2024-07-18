// Copyright (c) Dualsoft  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Text.RegularExpressions

[<AutoOpen>]
module CoreExtensionModule =
    
    

    let getDevParamInOut (paramInOutText:string) = 
        match paramInOutText.Split(',') |> Seq.toList with
        | tx::rx when rx.Length = 1 -> getDevParam tx,  getDevParam rx.Head
        | _-> failwithlog $"{paramInOutText} getDevParamInOut format error ex) 출력값:출력유지시간~센서값:센서지연시간"
    

    let checkSystem(system:DsSystem, targetFlow:Flow, itemName:string) =
                if system <> targetFlow.System
                then failwithf $"add item [{itemName}] in flow ({targetFlow.System.Name} != {system.Name}) is not same system"

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

          ///Call 자신이거나 Alias Target Call
    let getPureCall(v:Vertex) : Call option=
        match v with
        | :? Call  as c  ->  Some (c)
        | :? Alias as a  ->
            match a.TargetWrapper.GetTarget() with
            | :? Call as call -> Some call
            | _ -> None
        |_ -> None

        ///Real 자신이거나 RealEx Target Real
    let getPureReal(v:Vertex)  : Real =
        match v with
        | :? Real   as r  -> r
        | :? Alias  as a  ->
            match a.TargetWrapper.GetTarget() with
            | :? Real as real -> real
            | _ -> failwithlog $"{v.Name} is not real!!"
        |_ -> failwithlog $"{v.Name} is not real!!"

    let getPure(v:Vertex) : Vertex =
        match v with
        | ( :? Real | :? Call) -> v
        | :? Alias  as a  ->
            let target = a.TargetWrapper.GetTarget()
            match target with
            | ( :? Real | :? Call) -> target
            | _ -> failwithlog "Error"
        |_ -> failwithlog "Error"


    type DsSystem with
        member x.HWButtons    = x.HwSystemDefs.OfType<ButtonDef>()
        member x.HWConditions = x.HwSystemDefs.OfType<ConditionDef>()
        member x.HWLamps      = x.HwSystemDefs.OfType<LampDef>()

        member x.AddButton(btnType:BtnType, btnName:string, inDevParam:DevParam, outDevParam:DevParam,  addr:Addresses, flow:Flow) =
            checkSystem(x, flow, btnName)
          
            let existBtns = x.HWButtons.Where(fun f->f.ButtonType = btnType)
                            |>Seq.filter(fun b -> b.SettingFlows.Contains(flow))

            if existBtns.Where(fun w->w.Name = btnName).any()
            then failwithf $"버튼타입[{btnType}]{btnName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"중복 Button [flow:{flow.Name} name:{btnName}]"
            | None -> 
                      x.HwSystemDefs.Add(ButtonDef(btnName,x, btnType, inDevParam, outDevParam, addr, HashSet[|flow|]))
                      |> verifyM $"중복 ButtonDef [flow:{flow.Name} name:{btnName}]"

        member x.AddButton(btnType:BtnType, btnName:string, inAddress:string, outAddress:string, flow:Flow) =
            x.AddButton(btnType, btnName, defaultDevParam(), defaultDevParam(), Addresses(inAddress ,outAddress), flow)       

        member x.AddLamp(lmpType:LampType, lmpName: string, inDevParam:DevParam, outDevParam:DevParam, addr:Addresses, flow:Flow option) =
            if flow.IsSome then
                checkSystem(x, flow.Value, lmpName)

            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> failwithf $"램프타입[{lmpType}]{lmpName}이 다른 Flow에 중복 정의 되었습니다.  위치:[{lmp.SettingFlows.First().Name}]"
            | None -> 
                      let flows = if flow.IsSome then  HashSet[flow.Value] else HashSet[]
                      x.HwSystemDefs.Add(LampDef(lmpName, x,lmpType, inDevParam, outDevParam, addr, flows))
                      |> verifyM $"중복 LampDef [name:{lmpName}]"
        
        member x.AddLamp(lmpType:LampType, lmpName:string, inAddress:string, outAddress:string,  flow:Flow option) =
                x.AddLamp(lmpType, lmpName, defaultDevParam(), defaultDevParam(),Addresses(inAddress ,outAddress),  flow)       


        member x.AddCondtion(condiType:ConditionType, condiName: string, inDevParam:DevParam, outDevParam:DevParam, addr:Addresses, flow:Flow) =
            checkSystem(x, flow, condiName)

            match x.HWConditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"중복 Condtion [flow:{flow.Name} name:{condiName}]"
            | None -> 
                      x.HwSystemDefs.Add(ConditionDef(condiName,x, condiType, inDevParam, outDevParam,  addr, HashSet[|flow|]))
                      |> verifyM $"중복 ConditionDef [flow:{flow.Name} name:{condiName}]"

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddress:string, outAddress:string, flow:Flow) =
                x.AddCondtion(condiType, condiName, defaultDevParam(), defaultDevParam(), Addresses(inAddress ,outAddress), flow)       

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
                    |true, false -> Some apiInfo.Operand2
                    |false, true -> Some apiInfo.Operand1
                    |_ -> None
                | ModelingEdgeType.ResetEdge -> 
                    match src.Name = apiInfo.Operand1 with
                    |true -> Some apiInfo.Operand2
                    |_ -> None
                | ModelingEdgeType.RevResetEdge -> 
                    match src.Name = apiInfo.Operand2 with
                    |true -> Some apiInfo.Operand1
                    |_ -> None
                | _ -> None

            let resets = x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)

            resets.Select(fun s->x.ApiItems.Find(fun f->f.UnqualifiedName = $"{x.Name}.{s.Value}"))

        member x.LoadedSysExist (name:string) = x.LoadedSystems.Select(fun f -> f.Name).Contains(name)
        member x.GetLoadedSys   (loadSys:DsSystem) = x.LoadedSystems.TryFind(fun f-> f.ReferenceSystem = loadSys)
         
    let getType (xs:DevParam seq) = 
        if xs.Where(fun f->f.DevType.IsSome).Any() 
                then 
                    let types = xs.Choose(fun f->f.DevType)
                    if types.Distinct().Count() > 1
                    then 
                        failwithlog $"dataType miss matching error {String.Join(',', types.Select(fun f->f.ToText()))}"
                    else
                        types.First()

                else DuBOOL

    type TaskDev with
    
        member x.IsInAddressEmpty = x.InAddress = TextAddrEmpty
        member x.IsInAddressSkipOrEmpty = x.InAddress = TextAddrEmpty || x.InAddress = TextSkip
        member x.IsOutAddressEmpty = x.OutAddress = TextAddrEmpty
        member x.IsOutAddressSkipOrEmpty = x.OutAddress = TextAddrEmpty || x.OutAddress = TextSkip
        member x.IsAddressEmpty = x.IsInAddressEmpty  && x.IsOutAddressEmpty
        member x.IsAddressSkipOrEmpty = x.IsOutAddressSkipOrEmpty  && x.IsInAddressSkipOrEmpty
        member x.IsMaunualAddressEmpty = x.MaunualAddress = TextAddrEmpty
        member x.IsMaunualAddressSkipOrEmpty = x.MaunualAddress = TextAddrEmpty || x.MaunualAddress = TextSkip

        member x.GetInParam(job:Job) = x.InParams[job.UnqualifiedName]
        member x.AddOrUpdateInParam(jobName:string, newDevParam:DevParam) = addOrUpdateParam (jobName,  x.InParams, newDevParam)

        member x.GetOutParam(job:Job) = x.OutParams[job.UnqualifiedName]
        member x.AddOrUpdateOutParam(jobName:string, newDevParam:DevParam) = addOrUpdateParam (jobName,  x.OutParams, newDevParam)

        member x.SetInSymbol(symName:string option) =
            x.InParams.ToList() |> Seq.iter(fun kv -> 
                changeParam (kv.Key, x.InParams, symName)
            )
            

        member x.SetOutSymbol(symName:string option) = 
                x.OutParams.ToList() |> Seq.iter(fun kv -> 
                changeParam (kv.Key, x.OutParams, symName)
            )


        member x.InDataType = getType x.InParams.Values
        member x.OutDataType  =getType x.OutParams.Values
        
    type Job with
        member x.OnDelayTime = 
            let times = x.TaskDefs.Choose(fun t-> t.InParams[x.UnqualifiedName].Time)
            if times.GroupBy(fun t->t).Count() > 1
            then 
                let errTask = String.Join(", ",  x.TaskDefs.Select(fun t-> $"{t.Name} {t.InParams[x.UnqualifiedName].Time}"))
                failWithLog $"다른 시간이 설정된 tasks가 있습니다. {errTask}"
                
            if times.any() then times.First() |> Some else None


        member x.UpdateDevParam(inParam: DevParam, outParam: DevParam) =
                x.TaskDefs.Iter(fun d-> 
                    d.AddOrUpdateInParam (x.UnqualifiedName, inParam)
                    d.AddOrUpdateOutParam(x.UnqualifiedName, outParam)
                )


        member x.GetNullAddressDevTask() = 
            match x.JobMulti with
            | Single -> 
                x.TaskDefs
                |> Seq.filter (fun f -> f.IsAddressEmpty && not(f.GetInParam(x).IsDefaultParam))
            | MultiAction (_, _, inCnt, outCnt) -> 
                x.TaskDefs
                |> Seq.mapi (fun i d -> 
                    let empty = d.IsAddressEmpty && not(d.GetInParam(x).IsDefaultParam && d.GetOutParam(x).IsDefaultParam)
                    if inCnt.IsSome && inCnt.Value = i && empty then 
                        Some d
                    elif outCnt.IsSome && outCnt.Value = i && empty then 
                        Some d
                    else
                     None
                )
                |> Seq.choose id

                        
    type Real with
    
        member x.TimeAvg = 
            let maxShortSpeedSec = (TimerModule.MinTickInterval|>float)/1000.0
            let v = 
                if x.DsTime.AVG.IsNone then None
                else 
                    if RuntimeDS.Package.IsPackageSIM() 
                    then
                        match RuntimeDS.TimeSimutionMode  with
                        | TimeSimutionMode.TimeNone -> None
                        | TimeSimutionMode.TimeX1 ->   Some (x.DsTime.AVG.Value * 1.0/1.0 )
                        | TimeSimutionMode.TimeX2 ->   Some (x.DsTime.AVG.Value * 1.0/2.0 )
                        | TimeSimutionMode.TimeX4 ->   Some (x.DsTime.AVG.Value * 1.0/4.0 )
                        | TimeSimutionMode.TimeX8 ->   Some (x.DsTime.AVG.Value * 1.0/8.0 )
                        | TimeSimutionMode.TimeX16 ->  Some (x.DsTime.AVG.Value * 1.0/16.0 ) 
                        | TimeSimutionMode.TimeX100 -> Some (x.DsTime.AVG.Value * 1.0/100.0 ) 
                        | TimeSimutionMode.TimeX0_1 -> Some (x.DsTime.AVG.Value * 1.0/0.1 )
                        | TimeSimutionMode.TimeX0_5 -> Some (x.DsTime.AVG.Value * 1.0/0.5 )
                    else 
                        x.DsTime.AVG

            if v.IsSome && v.Value < maxShortSpeedSec 
            then failwithf $"시뮬레이션 배속을 재설정 하세요.현재설정({RuntimeDS.TimeSimutionMode}) {x.QualifiedName}
                            \r\n[최소동작시간 : {maxShortSpeedSec}, 배속반영 동작 시간 : {v.Value}]"
            else v 
                    


        member x.TimeAvgMsec = 
                if x.TimeAvg.IsNone
                then failwithf $"Error  TimeAvgMsec ({x.QualifiedName})"
                else Convert.ToUInt32( x.TimeAvg.Value *1000.0 )


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
                if callOwnerFlow = jobOwnerFlow
                then 
                    jobFqdn.Skip(1).CombineQuoteOnDemand() 
                else 
                    jobFqdn.CombineQuoteOnDemand() //다른 Flow는 skip flow 없음
                             
            | CommadFuncType func -> func.Name.QuoteOnDemand()+"()"
            | OperatorFuncType func -> "#"+func.Name.QuoteOnDemand()


    type Call with
        
        member x.NameForGraph = getCallName x 

        member x.System = x.Parent.GetSystem()
        member x.ErrorSensorOn = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOn)|> snd
        member x.ErrorSensorOff = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOff)|> snd
        member x.ErrorOnTimeOver = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeOver)|> snd
        member x.ErrorOnTimeShortage = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeShortage)|> snd
        member x.ErrorOffTimeOver = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeOver)|> snd
        member x.ErrorOffTimeShortage = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeShortage)|> snd

    let inValidTaskDevTags (x:DsSystem) = 
                    x.Jobs |> Seq.collect(fun j-> j.TaskDefs)
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
                                    | :? ButtonDef
                                    | :? ConditionDef -> yield h, "INPUT", h.InAddress
                                    | :? LampDef      -> yield h, "OUTPUT", h.OutAddress
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
    //static member ToTextForDevParam(x:TaskDev, jobName:string) = toTextInOutDev (x.GetInParam(jobName)) (x.GetOutParam(jobName))

    //[<Extension>]
    //static member ToTextForDevParam(x:HwSystemDef) = toTextInOutDev x.InParam x.OutParam

    //[<Extension>]
    //static member IsSensorNot(x:DevParam) = 
    //            match x.DevValueNType with
    //            |Some(v, ty) when ty = DuBOOL -> not (Convert.ToBoolean(v))  //RX 기본은 True
    //            |_ -> false

