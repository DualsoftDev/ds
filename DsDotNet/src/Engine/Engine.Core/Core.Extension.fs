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
    let getConditions (sys:DsSystem, cType:ConditionType) = sys.Conditions.Where(fun f->f.ConditionType = cType)


    type DsSystem with
        member x.AddButton(btnType:BtnType, btnName:string, inAddress:TagAddress, outAddress:TagAddress, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, btnName)
          
            let flows = x.HWButtons.Where(fun f->f.ButtonType = btnType)
                        |> Seq.collect(fun b -> b.SettingFlows)
            flows.Contains(flow) |> not
            |> verifyM $"버튼타입[{btnType}]{btnName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWButtons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.HWButtons.Add(ButtonDef(btnName, btnType, inAddress, outAddress, HashSet[|flow|], funcs))
                      |> verifyM $"Duplicated ButtonDef [{btnName}]"

        member x.AddLamp(lmpType:LampType, lmpName: string, addr:string, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, lmpName)

            x.HWLamps.Select(fun f->f.Name).Contains(lmpName) |> not
            |> verifyM $"램프타입[{lmpType}]{lmpName}이 중복 정의 되었습니다.  위치:[{flow.Name}]"

            match x.HWLamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> lmp.SettingFlow <- flow
            | None -> x.HWLamps.Add(LampDef(lmpName, lmpType, addr, flow, funcs))
                      |> verifyM $"Duplicated LampDef [{lmpName}]"

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddr:string, flow:Flow, funcs:HashSet<Func>) =
            checkSystem(x, flow, condiName)

            match x.Conditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.Conditions.Add(ConditionDef(condiName, condiType, inAddr, HashSet[|flow|], funcs))
                      |> verifyM $"Duplicated ConditionDef [{condiName}]"


        member x.SystemConditions     = x.Conditions :> seq<_>
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