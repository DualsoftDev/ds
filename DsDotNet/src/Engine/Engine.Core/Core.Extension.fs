// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreExtensionModule =

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


    let checkSystem(system:DsSystem, targetFlow:Flow, itemName:string) =
                if system <> targetFlow.System
                then failwithf $"add item [{itemName}] in flow ({targetFlow.System.Name} != {system.Name}) is not same system"

    let getButtons (sys:DsSystem, btnType:BtnType) = sys.HWButtons.Where(fun f->f.ButtonType = btnType)
    let getLamps (sys:DsSystem, lampType:LampType) = sys.HWLamps.Where(fun f->f.LampType = lampType)
    let getConditions (sys:DsSystem, cType:ConditionType) = sys.HWConditions.Where(fun f->f.ConditionType = cType)


    type DsSystem with
        member x.AddButton(btnType:BtnType, btnName:string, inAddress:TagAddress, outAddress:TagAddress, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, btnName)
          
            let existBtns = x.HWButtons.Where(fun f->f.ButtonType = btnType)
                            |>Seq.filter(fun b -> b.SettingFlows.Contains(flow))

            if existBtns.Where(fun w->w.Name = btnName).any()
            then failwithf $"버튼타입[{btnType}]{btnName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.HWButtons.Add(ButtonDef(btnName,x, btnType, inAddress, outAddress, HashSet[|flow|], funcs))
                      |> verifyM $"Duplicated ButtonDef [{btnName}]"
                      HwSystemItem.CreateHWApi(btnName, x) |> ignore

        member x.AddLamp(lmpType:LampType, lmpName: string, inAddr:string, outAddr:string, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, lmpName)

            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> failwithf $"램프타입[{lmpType}]{lmpName}이 다른 Flow에 중복 정의 되었습니다.  위치:[{lmp.SettingFlows.First().Name}]"
            | None -> x.HWLamps.Add(LampDef(lmpName, x,lmpType, inAddr, outAddr, HashSet[flow], funcs))
                      |> verifyM $"Duplicated LampDef [{lmpName}]"
                      HwSystemItem.CreateHWApi(lmpName, x) |> ignore

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddr:string, outAddr:string, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, condiName)

            match x.HWConditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.HWConditions.Add(ConditionDef(condiName,x, condiType, inAddr, outAddr, HashSet[|flow|], funcs))
                      |> verifyM $"Duplicated ConditionDef [{condiName}]"
                      HwSystemItem.CreateHWApi(condiName, x) |> ignore


        member x.HWSystemConditions     = x.HWConditions :> seq<_>
        member x.HWButtons            = x.HWButtons :> seq<_>
        member x.HWLamps              = x.HWLamps   :> seq<_>

        member x.AutoHWButtons        = getButtons(x, DuAutoBTN)
        member x.ManualHWButtons      = getButtons(x, DuManualBTN)
        member x.DriveHWButtons       = getButtons(x, DuDriveBTN)
        member x.StopHWButtons        = getButtons(x, DuStopBTN)
        member x.ClearHWButtons       = getButtons(x, DuClearBTN)
        member x.EmergencyHWButtons   = getButtons(x, DuEmergencyBTN)
        member x.TestHWButtons        = getButtons(x, DuTestBTN)
        member x.HomeHWButtons        = getButtons(x, DuHomeBTN)
        member x.ReadyHWButtons       = getButtons(x, DuReadyBTN)

        member x.DriveHWLamps         = getLamps(x, DuDriveLamp)
        member x.AutoHWLamps          = getLamps(x, DuAutoLamp)
        member x.ManualHWLamps        = getLamps(x, DuManualLamp)
        member x.StopHWLamps          = getLamps(x, DuStopLamp)
        member x.EmergencyHWLamps     = getLamps(x, DuEmergencyLamp)
        member x.TestHWLamps          = getLamps(x, DuTestDriveLamp)
        member x.ReadyHWLamps         = getLamps(x, DuReadyLamp)
        member x.IdleHWLamps          = getLamps(x, DuIdleLamp)

        member x.ReadyConditions     = getConditions(x, DuReadyState)
        member x.DriveConditions     = getConditions(x, DuDriveState)

        member x.HWSystemDefs = x.HWButtons.OfType<HwSystemDef>() 
                                @ x.HWLamps.OfType<HwSystemDef>() 
                                @ x.HWConditions.OfType<HwSystemDef>()

        member x.GetMutualResetApis(src:ApiItem) =
            let getMutual(apiInfo:ApiResetInfo) =
                match src.Name.QuoteOnDemand() = apiInfo.Operand1, src.Name.QuoteOnDemand() = apiInfo.Operand2 with
                |true, false -> Some apiInfo.Operand2
                |false, true -> Some apiInfo.Operand1
                |_ -> None

            let resets = x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)

            resets.Select(fun s->x.ApiItems.Find(fun f->f.QualifiedName = $"{x.Name.QuoteOnDemand()}.{s.Value}"))

        member x.DeviceDefs = x.Jobs |> Seq.collect(fun s->s.DeviceDefs)
        member x.LoadedSysExist (name:string) = x.LoadedSystems.Select(fun f -> f.Name).Contains(name)
        member x.GetLoadedSys   (name:string) = x.LoadedSystems.TryFind(fun f-> f.Name = name)

    type Call with
        member x.System = x.Parent.GetSystem()

    let inValidActionTags (x:DsSystem) = 
                    x.Jobs |> Seq.collect(fun j-> j.DeviceDefs)
                           |> Seq.collect(fun d-> 
                                  [  
                                     if d.ApiItem.RXs.any()
                                     then yield d, "IN",d.InAddress
                                     if d.ApiItem.TXs.any()
                                     then yield d, "OUT",d.OutAddress
                                  ])
                           |> Seq.filter(fun (_, _, addr) -> addr = TextAddrEmpty) 
                           |> Seq.map(fun (d, inout, _) -> $"{d.QualifiedName}_{inout}") 

    let inValidHwSystemTag (x:DsSystem) = 
                    x.HWSystemDefs 
                           |> Seq.collect(fun h -> 
                                [
                                    match h with
                                    | :? ButtonDef
                                    | :? ConditionDef -> yield h, "IN", h.InAddress
                                    | :? LampDef      -> yield h, "OUT", h.OutAddress
                                    | _  -> failwith $"inValidHwSystemTag error {h.Name}"
                                ])
                           |> Seq.filter(fun (_, _, addr) -> addr = TextAddrEmpty) 
                           |> Seq.map(fun (h, inout, _) -> $"{h.Name}_{inout}") 
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
    static member CheckValidInterfaceTags(x:DsSystem) =
                let inValidActionTags = inValidActionTags(x);
                let inValidHwSystemTag = inValidHwSystemTag(x);
                if inValidActionTags.Any() then
                    failwithf $"Device IO Table을 작성하세요 \n{String.Join('\n', inValidActionTags)}"
                if inValidHwSystemTag.Any() then
                    failwithf $"HW 조작 IO Table을 작성하세요 \n{String.Join('\n', inValidHwSystemTag)}"

