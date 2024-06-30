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

        member x.AddButton(btnType:BtnType, btnName:string, inDevParam:DevParam, outDevParam:DevParam, flow:Flow) =
            checkSystem(x, flow, btnName)
          
            let existBtns = x.HWButtons.Where(fun f->f.ButtonType = btnType)
                            |>Seq.filter(fun b -> b.SettingFlows.Contains(flow))

            if existBtns.Where(fun w->w.Name = btnName).any()
            then failwithf $"버튼타입[{btnType}]{btnName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"중복 Button [flow:{flow.Name} name:{btnName}]"
            | None -> 
                      x.HwSystemDefs.Add(ButtonDef(btnName,x, btnType, inDevParam, outDevParam, HashSet[|flow|]))
                      |> verifyM $"중복 ButtonDef [flow:{flow.Name} name:{btnName}]"

        member x.AddButton(btnType:BtnType, btnName:string, inAddress:string, outAddress:string, flow:Flow) =
            x.AddButton(btnType, btnName, inAddress|>defaultDevParam, outAddress|>defaultDevParam, flow)       

        member x.AddLamp(lmpType:LampType, lmpName: string, inDevParam:DevParam, outDevParam:DevParam, flow:Flow option) =
            if flow.IsSome then
                checkSystem(x, flow.Value, lmpName)

            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> failwithf $"램프타입[{lmpType}]{lmpName}이 다른 Flow에 중복 정의 되었습니다.  위치:[{lmp.SettingFlows.First().Name}]"
            | None -> 
                      let flows = if flow.IsSome then  HashSet[flow.Value] else HashSet[]
                      x.HwSystemDefs.Add(LampDef(lmpName, x,lmpType, inDevParam, outDevParam, flows))
                      |> verifyM $"중복 LampDef [name:{lmpName}]"
        
        member x.AddLamp(lmpType:LampType, lmpName:string, inAddress:string, outAddress:string, flow:Flow option) =
                x.AddLamp(lmpType, lmpName, inAddress|>defaultDevParam, outAddress|>defaultDevParam, flow)       


        member x.AddCondtion(condiType:ConditionType, condiName: string, inDevParam:DevParam, outDevParam:DevParam, flow:Flow) =
            checkSystem(x, flow, condiName)

            match x.HWConditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"중복 Condtion [flow:{flow.Name} name:{condiName}]"
            | None -> 
                      x.HwSystemDefs.Add(ConditionDef(condiName,x, condiType, inDevParam, outDevParam, HashSet[|flow|]))
                      |> verifyM $"중복 ConditionDef [flow:{flow.Name} name:{condiName}]"

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddress:string, outAddress:string, flow:Flow) =
                x.AddCondtion(condiType, condiName, inAddress|>defaultDevParam, outAddress|>defaultDevParam, flow)       

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
                    match src.Name.QuoteOnDemand() = apiInfo.Operand1, src.Name.QuoteOnDemand() = apiInfo.Operand2 with
                    |true, false -> Some apiInfo.Operand2
                    |false, true -> Some apiInfo.Operand1
                    |_ -> None
                | ModelingEdgeType.ResetEdge -> 
                    match src.Name.QuoteOnDemand() = apiInfo.Operand1 with
                    |true -> Some apiInfo.Operand2
                    |_ -> None
                | ModelingEdgeType.RevResetEdge -> 
                    match src.Name.QuoteOnDemand() = apiInfo.Operand2 with
                    |true -> Some apiInfo.Operand1
                    |_ -> None
                | _ -> None

            let resets = x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)

            resets.Select(fun s->x.ApiItems.Find(fun f->f.QualifiedName = $"{x.Name.QuoteOnDemand()}.{s.Value.QuoteOnDemand()}"))

        member x.DeviceDefs = x.Jobs |> Seq.collect(fun s->s.DeviceDefs)
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


        member x.GetInParam(jobName:string) = x.InParams[jobName]
        member x.AddOrUpdateInParam(jobName:string, newDevParam:DevParam) = addOrUpdateParam (jobName,  x.InParams, newDevParam)

        member x.GetOutParam(jobName:string) = x.OutParams[jobName]
        member x.AddOrUpdateOutParam(jobName:string, newDevParam:DevParam) = addOrUpdateParam (jobName,  x.OutParams, newDevParam)

        member x.SetInSymbol(symName:string option) =
            x.InParams.ToList() |> Seq.iter(fun kv -> 
                changeParam (kv.Key, x.InParams,  x.InParams[kv.Key].DevAddress, symName)
            )
            

        member x.SetOutSymbol(symName:string option) = 
                x.OutParams.ToList() |> Seq.iter(fun kv -> 
                changeParam (kv.Key, x.OutParams,  x.OutParams[kv.Key].DevAddress, symName)
            )


        member x.InDataType = getType x.InParams.Values
        member x.OutDataType  =getType x.OutParams.Values


    type Real with
    
        member x.TimeAvg = x.DsTime.AVG 
        member x.TimeAvgMsec = Convert.ToUInt32( x.DsTime.AVG.Value*1000.0 )
        member x.TimeStd = x.DsTime.STD
        member x.Path3D = x.DsTime.Path3D 
        member x.Script = x.DsTime.Script
        member x.NoneAction = x.Path3D.IsNone &&  x.Script.IsNone 

        member x.ErrGoingOrigin = x.ExternalTags.First(fun (t,_)-> t = ErrGoingOrigin)|> snd  

        member x.MotionStartTag = x.ExternalTags.First(fun (t,_)-> t = MotionStart)|> snd  
        member x.ScriptStartTag = x.ExternalTags.First(fun (t,_)-> t = ScriptStart)|> snd  
        member x.TimeStartTag = x.ExternalTags.First(fun (t,_)-> t = TimeStart)|> snd

        member x.MotionEndTag = x.ExternalTags.First(fun (t,_)-> t = MotionEnd)|> snd  
        member x.ScriptEndTag = x.ExternalTags.First(fun (t,_)-> t = ScriptEnd)|> snd  
        member x.TimeEndTag = x.ExternalTags.First(fun (t,_)-> t = TimeEnd)|> snd  

    type Call with
        member x.IsOperator = (x.Parent.GetCore() :? Flow)

        member x.System = x.Parent.GetSystem()
        member x.ErrorSensorOn = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOn)|> snd
        member x.ErrorSensorOff = x.ExternalTags.First(fun (t,_)-> t = ErrorSensorOff)|> snd
        member x.ErrorOnTimeOver = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeOver)|> snd
        member x.ErrorOnTimeShortage = x.ExternalTags.First(fun (t,_)-> t = ErrorOnTimeShortage)|> snd
        member x.ErrorOffTimeOver = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeOver)|> snd
        member x.ErrorOffTimeShortage = x.ExternalTags.First(fun (t,_)-> t = ErrorOffTimeShortage)|> snd

    let inValidActionTags (x:DsSystem) = 
                    x.Jobs |> Seq.collect(fun j-> j.DeviceDefs)
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
    static member ValidActionTag(x:DsSystem) = x |> inValidActionTags |> Seq.any
    [<Extension>]
    static member ValidHwSystemTag(x:DsSystem) = x |> inValidHwSystemTag |> Seq.any
    [<Extension>]
    static member CheckValidInterfaceNchageParsingAddress(x:DsSystem) =
                let inValidActionTags = inValidActionTags(x);
                let inValidHwSystemTag = inValidHwSystemTag(x);
                if inValidActionTags.Any() then
                     failwithf $"Add I/O Table을 수행하세요 \n\n{String.Join('\n', inValidActionTags)}"
                if inValidHwSystemTag.Any() then
                    failwithf $"HW 조작 IO Table을 작성하세요 \n\n{String.Join('\n', inValidHwSystemTag)}"



    [<Extension>]
    static member GetDevice(x:TaskDev, sys:DsSystem) = sys.Devices.First(fun f->f.Name  = x.DeviceName)

    [<Extension>]
    static member ToTextForDevParam(x:TaskDev, jobName:string) = toTextInOutDev (x.GetInParam(jobName)) (x.GetOutParam(jobName))

    [<Extension>]
    static member ToTextForDevParam(x:HwSystemDef) = toTextInOutDev x.InParam x.OutParam

    //[<Extension>]
    //static member IsSensorNot(x:DevParam) = 
    //            match x.DevValueNType with
    //            |Some(v, ty) when ty = DuBOOL -> not (Convert.ToBoolean(v))  //RX 기본은 True
    //            |_ -> false

