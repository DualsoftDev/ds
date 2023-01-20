// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Common.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreExtensionModule =

    let getButtons (sys:DsSystem, btnType:BtnType) = sys.Buttons.Where(fun f->f.ButtonType = btnType)
    let getLamps (sys:DsSystem, lampType:LampType) = sys.Lamps.Where(fun f->f.LampType = lampType)
    let getConditions (sys:DsSystem, cType:ConditionType) = sys.Conditions.Where(fun f->f.ConditionType = cType)
    let getRecursiveSystems (sys:DsSystem) =
            [
                yield! sys.ReferenceSystems
                yield! sys.ReferenceSystems |> Seq.collect(fun f->f |> getRecursiveSystems)
            ] |> List.toSeq
    let getRecursiveLoadeds (sys:ISystem) =
            let dsSys =
                match sys  with
                | :? DsSystem as d -> d
                | :? Device as d -> d.ReferenceSystem
                | :? ExternalSystem as d -> d.ReferenceSystem
                | _ -> failwithlog "Error getRecursiveLoadeds"
            [
                yield! dsSys.LoadedSystems |> Seq.cast<ISystem>
                yield! dsSys.LoadedSystems |> Seq.collect(fun f-> f |> getRecursiveLoadeds)
            ] |> List.toSeq

    let checkSystem(system:DsSystem, targetFlow:Flow, itemName:string) =
                if system <> targetFlow.System
                then failwithf $"add item [{itemName}] in flow ({targetFlow.System.Name} != {system.Name}) is not same system"

    type DsSystem with
        member x.AddButton(btnType:BtnType, btnName:string, inAddress:TagAddress, outAddress:TagAddress, flow:Flow) =
            let checkUsedFlow() =
                let flows = x.Buttons.Where(fun f->f.ButtonType = btnType)
                            |> Seq.collect(fun b -> b.SettingFlows)
                flows.Contains(flow) |> not
                |> verifyM $"{btnType} {btnName} is assigned to a single flow : Duplicated flow [{flow.Name}]"

            checkSystem(x, flow, btnName)
            if btnType = DuAutoBTN || btnType = DuManualBTN
            then checkUsedFlow()

            match x.Buttons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.Buttons.Add(ButtonDef(btnName, btnType, inAddress, outAddress, HashSet[|flow|], HashSet[||])) // need button functions....
                      |> verifyM $"Duplicated ButtonDef [{btnName}]"

        member x.AddLamp(lmpType:LampType, lmpName: string, addr:string, flow:Flow) =
            checkSystem(x, flow, lmpName)

            match x.Lamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> lmp.SettingFlow <- flow
            | None -> x.Lamps.Add(LampDef(lmpName, lmpType, addr, flow, HashSet[||])) |> verifyM $"Duplicated LampDef [{lmpName}]" // need lamp functions....

        member x.AddCondtion(condiType:ConditionType, condiName: string, inAddr:string, flow:Flow) =
            checkSystem(x, flow, condiName)

            match x.Conditions.TryFind(fun f -> f.Name = condiName) with
            | Some condi -> condi.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.Conditions.Add(ConditionDef(condiName, condiType, inAddr, HashSet[|flow|], HashSet[||])) // need button functions....
                      |> verifyM $"Duplicated ButtonDef [{condiName}]"


        member x.SystemConditions   = x.Conditions |> Seq.map(fun con  -> con) //read only
        member x.SystemButtons      = x.Buttons |> Seq.map(fun btn  -> btn) //read only
        member x.SystemLamps        = x.Lamps   |> Seq.map(fun lamp -> lamp)//read only

        member x.AutoButtons        = getButtons(x, DuAutoBTN)
        member x.ManualButtons      = getButtons(x, DuManualBTN)
        member x.DriveButtons       = getButtons(x, DuDriveBTN)
        member x.StopButtons        = getButtons(x, DuStopBTN)
        member x.ClearButtons       = getButtons(x, DuClearBTN)
        member x.EmergencyButtons   = getButtons(x, DuEmergencyBTN)
        member x.TestButtons        = getButtons(x, DuTestBTN)
        member x.HomeButtons        = getButtons(x, DuHomeBTN)
        member x.ReadyButtons       = getButtons(x, DuReadyBTN)

        member x.DriveModeLamps     = getLamps(x, DuDriveModeLamp)
        member x.AutoModeLamps      = getLamps(x, DuAutoModeLamp)
        member x.ManualModeLamps    = getLamps(x, DuManualModeLamp)
        member x.StopModeLamps      = getLamps(x, DuStopModeLamp)
        member x.EmergencyModeLamps = getLamps(x, DuEmergencyModeLamp)
        member x.TestModeLamps      = getLamps(x, DuTestModeLamp)
        member x.ReadyModeLamps     = getLamps(x, DuReadyModeLamp)

        member x.ReadyConditions     = getConditions(x, DuReadyState)
        member x.DriveConditions     = getConditions(x, DuDriveState)

        member x.GetMutualResetApis(src:ApiItem) =
            let getMutual(apiInfo:ApiResetInfo) =
                match src.Name = apiInfo.Operand1, src.Name = apiInfo.Operand2 with
                |true, false -> Some apiInfo.Operand2
                |false, true -> Some apiInfo.Operand1
                |_ -> None

            x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)
                .Select(fun s->x.ApiItems.Find(fun f->f.Name = s.Value))

        member x.DeviceDefs = x.Jobs |> Seq.collect(fun s->s.DeviceDefs)
        member x.GetRecursiveSystems() =  x |> getRecursiveSystems

    type Call with
        member x.System = x.Parent.GetSystem()



[<Extension>]
type SystemExt =
    [<Extension>]
    static member GetRecursiveSystems (x:DsSystem) : DsSystem seq = x |> getRecursiveSystems
    [<Extension>]
    static member GetRecursiveLoadeds (x:DsSystem) : ISystem seq  = x |> getRecursiveLoadeds

