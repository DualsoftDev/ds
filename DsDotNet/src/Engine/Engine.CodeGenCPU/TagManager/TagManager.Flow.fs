namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module FlowManagerModule =
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages

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


            // BUTTON : predomiCATh
            FlowTag.auto_btn
            FlowTag.manual_btn
            FlowTag.drive_btn
            FlowTag.pause_btn
            FlowTag.ready_btn
            FlowTag.clear_btn
            FlowTag.emg_btn
            FlowTag.test_btn
            FlowTag.home_btn

            // LAMP : predomiCATh
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
        let tagDic = flowTags |> map (fun t -> t, cpv t) |> Tuple.toReadOnlyDictionary

        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag) =
            match tagDic.TryGetValue(ft) with
            | true, planVar -> planVar
            | _ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            :?> PlanVar<bool>
