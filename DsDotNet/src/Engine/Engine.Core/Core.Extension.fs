// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Engine.Common.FS
open System

[<AutoOpen>]
module CoreExtensionModule =

    let getButtons (sys:DsSystem, btnType:BtnType) = sys.Buttons.Where(fun f->f.ButtonType = btnType)
    let getLamps (sys:DsSystem, lampType:LampType) = sys.Lamps.Where(fun f->f.LampType = lampType)

    type DsSystem with
        member x.AddButton(btnType:BtnType, btnName:string, inAddress:TagAddress, outAddress:TagAddress, flow:Flow) =
            let checkSystem() =
                if x <> flow.System
                then failwithf $"button [{btnName}] in flow ({flow.System.Name} != {x.Name}) is not same system"

            let checkUsedFlow() =
                let flows = x.Buttons.Where(fun f->f.ButtonType = btnType)
                            |> Seq.collect(fun b -> b.SettingFlows)
                flows.Contains(flow) |> not
                |> verifyM $"{btnType} {btnName} is assigned to a single flow : Duplicated flow [{flow.Name}]"

            checkSystem()
            if btnType = DuAutoBTN || btnType = DuManualBTN
            then checkUsedFlow()

            match x.Buttons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.Buttons.Add(ButtonDef(btnName, btnType, inAddress, outAddress, HashSet[|flow|]))
                      |> verifyM $"Duplicated ButtonDef [{btnName}]"

        member x.AddLamp(lmpType:LampType, lmpName: string, addr:string, flow:Flow) =
            if x <> flow.System then failwithf $"lamp [{lmpName}] in flow ({flow.System.Name} != {x.Name}) is not same system"

            match x.Lamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> lmp.SettingFlow <- flow
            | None -> x.Lamps.Add(LampDef(lmpName, lmpType, addr, flow)) |> verifyM $"Duplicated LampDef [{lmpName}]"

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


        member x.GetMutualResetApis(src:ApiItem) =
            let getMutual(apiInfo:ApiResetInfo) =
                match src.Name = apiInfo.Operand1, src.Name = apiInfo.Operand2 with
                |true, false -> Some apiInfo.Operand2
                |false, true -> Some apiInfo.Operand1
                |_ -> None

            x.ApiResetInfos.Select(getMutual).Where(fun w-> w.IsSome)
                .Select(fun s->x.ApiItems.Find(fun f->f.Name = s.Value))

        member x.JobDefs = x.Jobs |> Seq.collect(fun s->s.JobDefs)

    type Call with
        member x.System = x.Parent.GetSystem()
    