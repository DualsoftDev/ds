namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module FlowManagerModule =
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow, isActive:bool, activeSys:DsSystem)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages
        let activeSysManager = activeSys.TagManager :?> SystemManager
        /// Create Plan Var
        let cpv  (flowTag:FlowTag) =
            let name = getStorageName f (int flowTag)
            createPlanVar s name DuBOOL true f (int flowTag) f.System

        let flowTags = [|
            FlowTag.idle_mode             // Idle  Operation Mode
            FlowTag.auto_mode             // Auto Operation State
            FlowTag.manual_mode           // Manual Operation State

            FlowTag.error_state          // error State
            FlowTag.drive_state          // Drive State
            FlowTag.ready_state          // Ready State
            FlowTag.test_state           // Test Operation State (시운전)
            FlowTag.origin_state         // origin State
            FlowTag.going_state          // going State
            FlowTag.emergency_state      // Emergency State
            FlowTag.pause_state          // pause State

            // BUTTON 
            FlowTag.auto_btn
            FlowTag.manual_btn
            FlowTag.drive_btn
            FlowTag.pause_btn
            FlowTag.ready_btn
            FlowTag.clear_btn
            FlowTag.emg_btn
            FlowTag.test_btn
            FlowTag.home_btn

            // LAMP 
            FlowTag.auto_lamp
            FlowTag.manual_lamp
            FlowTag.drive_lamp
            FlowTag.pause_lamp
            FlowTag.ready_lamp
            FlowTag.clear_lamp
            FlowTag.emg_lamp
            FlowTag.test_lamp
            FlowTag.home_lamp

            FlowTag.flowStopError
            FlowTag.flowReadyCondition
            FlowTag.flowDriveCondition
        |]
        ///자식 시스템 부모 신호 공유 
        let getActiveSysTag(t) = 
            match t with
            | FlowTag.auto_btn    -> activeSysManager.GetSystemTag(SystemTag.auto_btn)      |>Some 
            | FlowTag.manual_btn  -> activeSysManager.GetSystemTag(SystemTag.manual_btn)    |>Some 
            | FlowTag.drive_btn   -> activeSysManager.GetSystemTag(SystemTag.drive_btn)     |>Some 
            | FlowTag.pause_btn   -> activeSysManager.GetSystemTag(SystemTag.pause_btn)     |>Some 
            | FlowTag.ready_btn   -> activeSysManager.GetSystemTag(SystemTag.ready_btn)     |>Some 
            | FlowTag.clear_btn   -> activeSysManager.GetSystemTag(SystemTag.clear_btn)     |>Some 
            | FlowTag.emg_btn     -> activeSysManager.GetSystemTag(SystemTag.emg_btn)       |>Some 
            | FlowTag.test_btn    -> activeSysManager.GetSystemTag(SystemTag.test_btn)      |>Some 
            | FlowTag.home_btn    -> activeSysManager.GetSystemTag(SystemTag.home_btn)      |>Some 
            | FlowTag.auto_lamp   -> activeSysManager.GetSystemTag(SystemTag.auto_lamp)     |>Some 
            | FlowTag.manual_lamp -> activeSysManager.GetSystemTag(SystemTag.manual_lamp)   |>Some 
            | FlowTag.drive_lamp  -> activeSysManager.GetSystemTag(SystemTag.drive_lamp)    |>Some 
            | FlowTag.pause_lamp  -> activeSysManager.GetSystemTag(SystemTag.pause_lamp)    |>Some 
            | FlowTag.ready_lamp  -> activeSysManager.GetSystemTag(SystemTag.ready_lamp)    |>Some 
            | FlowTag.clear_lamp  -> activeSysManager.GetSystemTag(SystemTag.clear_lamp)    |>Some 
            | FlowTag.emg_lamp    -> activeSysManager.GetSystemTag(SystemTag.emg_lamp)      |>Some 
            | FlowTag.test_lamp   -> activeSysManager.GetSystemTag(SystemTag.test_lamp)     |>Some 
            | FlowTag.home_lamp   -> activeSysManager.GetSystemTag(SystemTag.home_lamp)     |>Some 

            | FlowTag.idle_mode       -> activeSysManager.GetSystemTag(SystemTag.idleMonitor)     |>Some 
            | FlowTag.auto_mode       -> activeSysManager.GetSystemTag(SystemTag.autoMonitor)     |>Some 
            | FlowTag.manual_mode     -> activeSysManager.GetSystemTag(SystemTag.manualMonitor)   |>Some 
            | FlowTag.error_state     -> activeSysManager.GetSystemTag(SystemTag.errorMonitor)    |>Some 
            | FlowTag.drive_state     -> activeSysManager.GetSystemTag(SystemTag.driveMonitor)    |>Some 
            | FlowTag.ready_state     -> activeSysManager.GetSystemTag(SystemTag.readyMonitor)    |>Some 
            | FlowTag.test_state      -> activeSysManager.GetSystemTag(SystemTag.testMonitor)     |>Some 
            | FlowTag.origin_state    -> activeSysManager.GetSystemTag(SystemTag.originMonitor)   |>Some 
            | FlowTag.going_state     -> activeSysManager.GetSystemTag(SystemTag.goingMonitor)    |>Some 
            | FlowTag.emergency_state -> activeSysManager.GetSystemTag(SystemTag.emergencyMonitor)|>Some 
            | FlowTag.pause_state     -> activeSysManager.GetSystemTag(SystemTag.pauseMonitor)    |>Some 
            | _ -> None

        let tagDic =
            flowTags 
            |> map (fun t -> 
                if isActive 
                then 
                    t, cpv t
                else 
                    match getActiveSysTag(t) with
                    |Some planVar -> t, planVar
                    |_-> t, cpv t
            ) 
            |> Tuple.toReadOnlyDictionary

        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag) =
            match tagDic.TryGetValue(ft) with
            | true, planVar -> planVar
            | _ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            :?> PlanVar<bool>
