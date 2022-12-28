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
            if x <> flow.System 
            then failwithf $"button [{btnName}] in flow ({flow.System.Name} != {x.Name}) is not same system"

            let getUsedFlow (btn:BtnType) =
                x.Buttons.Where(fun f->f.ButtonType = btn)
                |> Seq.collect(fun b -> b.SettingFlows)

            if btnType = DuAutoBTN 
            then not <| getUsedFlow(DuAutoBTN).Contains(flow)
                 |> verifyM $"AutoBTN {btnName} is assigned to a single flow : Duplicated flow [{flow.Name}]"
            if btnType = DuManualBTN 
            then not <| getUsedFlow(DuManualBTN).Contains(flow)
                 |> verifyM $"ManualBTN {btnName} is assigned to a single flow : Duplicated flow [{flow.Name}]"

            match x.Buttons.TryFind(fun f -> f.Name = btnName) with
            | Some btn -> btn.SettingFlows.Add(flow) |> verifyM $"Duplicated flow [{flow.Name}]"
            | None -> x.Buttons.Add(ButtonDef(btnName, btnType, inAddress, outAddress, HashSet[|flow|])) 
                      |> verifyM $"Duplicated ButtonDef [{btnName}]"
            
        member x.AddLamp(lmpType:LampType, lmpName: string, addr:string, flow:Flow) =
            if x <> flow.System then failwithf $"lamp [{lmpName}] in flow ({flow.System.Name} != {x.Name}) is not same system"

            match x.Lamps.TryFind(fun f -> f.Name = lmpName) with
            | Some lmp -> lmp.SettingFlow <- flow
            | None -> x.Lamps.Add(LampDef(lmpName, lmpType, addr, flow)) |> verifyM $"Duplicated LampDef [{lmpName}]"
        
        member x.SystemButtons    = x.Buttons |> Seq.map(fun btn  -> btn) //read only
        member x.SystemLamps      = x.Lamps   |> Seq.map(fun lamp -> lamp)//read only

        member x.AutoButtons      = getButtons(x, DuAutoBTN)
        member x.ManualButtons    = getButtons(x, DuManualBTN)
        member x.EmergencyButtons = getButtons(x, DuEmergencyBTN)
        member x.StopButtons      = getButtons(x, DuStopBTN)
        member x.RunButtons       = getButtons(x, DuRunBTN)
        member x.DryRunButtons    = getButtons(x, DuDryRunBTN)
        member x.ClearButtons     = getButtons(x, DuClearBTN)   
        member x.HomeButtons      = getButtons(x, DuHomeBTN)   

        member x.RunModeLamps       = getLamps(x, DuRunModeLamp)   
        member x.DryRunModeLamps    = getLamps(x, DuDryRunModeLamp)   
        member x.StopModeLamps      = getLamps(x, DuStopModeLamp)   
        member x.ManualModeLamps    = getLamps(x, DuManualModeLamp)   
        member x.EmergencyModeLamps = getLamps(x, DuEmergencyLamp)