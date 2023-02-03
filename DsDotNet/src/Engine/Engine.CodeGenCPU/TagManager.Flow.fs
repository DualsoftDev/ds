namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module FlowManagerModule =

    


    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages

        let f_rop    = createPlanVar s $"ROP_{f.Name}"     DuBOOL true   f  (FlowTag.ready_op      |>int)    // Ready Operation State
        let f_aop    = createPlanVar s $"AOP_{f.Name}"     DuBOOL true   f  (FlowTag.auto_op       |>int)    // Auto Operation State
        let f_mop    = createPlanVar s $"MOP_{f.Name}"     DuBOOL true   f  (FlowTag.manual_op     |>int)    // Manual Operation State
        let f_sop    = createPlanVar s $"SOP_{f.Name}"     DuBOOL true   f  (FlowTag.drive_op      |>int)    // Stop Operation State
        let f_eop    = createPlanVar s $"EOP_{f.Name}"     DuBOOL true   f  (FlowTag.test_op       |>int)    // Emergency Operation State
        let f_dop    = createPlanVar s $"DOP_{f.Name}"     DuBOOL true   f  (FlowTag.stop_op       |>int)    // Drive Operation Mode
        let f_top    = createPlanVar s $"TOP_{f.Name}"     DuBOOL true   f  (FlowTag.emergency_op  |>int)    // Test  Operation Mode (시운전)
        let f_iop    = createPlanVar s $"IOP_{f.Name}"     DuBOOL true   f  (FlowTag.idle_op       |>int)    // Idle  Operation Mode
        let f_readycondi = createPlanVar s $"SCR_{f.Name}" DuBOOL true   f  (FlowTag.auto_bit      |>int)    //system condition ready
        let f_drivecondi = createPlanVar s $"SCD_{f.Name}" DuBOOL true   f  (FlowTag.manual_bit    |>int)    //system condition drive
        let f_auto   = createPlanVar s $"auto_{f.Name}"    DuBOOL true   f  (FlowTag.drive_bit     |>int)
        let f_manual = createPlanVar s $"manual_{f.Name}"  DuBOOL true   f  (FlowTag.stop_bit      |>int)
        let f_drive  = createPlanVar s $"drive_{f.Name}"   DuBOOL true   f  (FlowTag.ready_bit     |>int)
        let f_stop   = createPlanVar s $"stop_{f.Name}"    DuBOOL true   f  (FlowTag.clear_bit     |>int)
        let f_ready  = createPlanVar s $"ready_{f.Name}"   DuBOOL true   f  (FlowTag.emg_bit       |>int)
        let f_clear  = createPlanVar s $"clear_{f.Name}"   DuBOOL true   f  (FlowTag.test_bit      |>int)
        let f_emg    = createPlanVar s $"emg_{f.Name}"     DuBOOL true   f  (FlowTag.home_bit      |>int)
        let f_test   = createPlanVar s $"test_{f.Name}"    DuBOOL true   f  (FlowTag.readycondi_bit|>int)
        let f_home   = createPlanVar s $"home_{f.Name}"    DuBOOL true   f  (FlowTag.drivecondi_bit|>int)

        interface ITagManager with
            member x.Target = f
            member x.Storages = s


        member f.GetFlowTag(ft:FlowTag)     =
            let t =
                match ft with
                |FlowTag.ready_op        -> f_rop
                |FlowTag.auto_op         -> f_aop
                |FlowTag.manual_op       -> f_mop
                |FlowTag.drive_op        -> f_dop
                |FlowTag.test_op         -> f_top
                |FlowTag.stop_op         -> f_sop
                |FlowTag.emergency_op    -> f_eop
                |FlowTag.idle_op         -> f_iop
                |FlowTag.auto_bit        -> f_auto
                |FlowTag.manual_bit      -> f_manual
                |FlowTag.drive_bit       -> f_drive
                |FlowTag.stop_bit        -> f_stop
                |FlowTag.ready_bit       -> f_ready
                |FlowTag.clear_bit       -> f_clear
                |FlowTag.emg_bit         -> f_emg
                |FlowTag.test_bit        -> f_test
                |FlowTag.home_bit        -> f_home
                |FlowTag.readycondi_bit  -> f_readycondi
                |FlowTag.drivecondi_bit  -> f_drivecondi
                |_ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            t :?> PlanVar<bool>
