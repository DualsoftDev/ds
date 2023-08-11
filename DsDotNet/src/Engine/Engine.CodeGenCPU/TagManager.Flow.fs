namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module FlowManagerModule =
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages
        let cpv name (flowTag:FlowTag) = createPlanVar s name DuBOOL true f (int flowTag)
        let fn = f.Name

        let f_rop        = cpv $"ROP_{fn}"     FlowTag.ready_op             // Ready Operation State
        let f_aop        = cpv $"AOP_{fn}"     FlowTag.auto_op              // Auto Operation State
        let f_mop        = cpv $"MOP_{fn}"     FlowTag.manual_op            // Manual Operation State
        let f_sop        = cpv $"SOP_{fn}"     FlowTag.drive_op             // Stop Operation State
        let f_eop        = cpv $"EOP_{fn}"     FlowTag.test_op              // Emergency Operation State
        let f_dop        = cpv $"DOP_{fn}"     FlowTag.stop_op              // Drive Operation Mode
        let f_top        = cpv $"TOP_{fn}"     FlowTag.emergency_op         // Test  Operation Mode (시운전)
        let f_iop        = cpv $"IOP_{fn}"     FlowTag.idle_op              // Idle  Operation Mode
        let f_readycondi = cpv $"SCR_{fn}"     FlowTag.auto_bit             //system condition ready
        let f_drivecondi = cpv $"SCD_{fn}"     FlowTag.manual_bit           //system condition drive
        let f_auto       = cpv $"auto_{fn}"    FlowTag.drive_bit
        let f_manual     = cpv $"manual_{fn}"  FlowTag.stop_bit
        let f_drive      = cpv $"drive_{fn}"   FlowTag.ready_bit
        let f_stop       = cpv $"stop_{fn}"    FlowTag.clear_bit
        let f_ready      = cpv $"ready_{fn}"   FlowTag.emg_bit
        let f_clear      = cpv $"clear_{fn}"   FlowTag.test_bit
        let f_emg        = cpv $"emg_{fn}"     FlowTag.home_bit
        let f_test       = cpv $"test_{fn}"    FlowTag.readycondi_bit
        let f_home       = cpv $"home_{fn}"    FlowTag.drivecondi_bit

        interface ITagManager with
            member x.Target = f
            member x.Storages = s


        member f.GetFlowTag(ft:FlowTag)     =
            let t =
                match ft with
                | FlowTag.ready_op        -> f_rop
                | FlowTag.auto_op         -> f_aop
                | FlowTag.manual_op       -> f_mop
                | FlowTag.drive_op        -> f_dop
                | FlowTag.test_op         -> f_top
                | FlowTag.stop_op         -> f_sop
                | FlowTag.emergency_op    -> f_eop
                | FlowTag.idle_op         -> f_iop
                | FlowTag.auto_bit        -> f_auto
                | FlowTag.manual_bit      -> f_manual
                | FlowTag.drive_bit       -> f_drive
                | FlowTag.stop_bit        -> f_stop
                | FlowTag.ready_bit       -> f_ready
                | FlowTag.clear_bit       -> f_clear
                | FlowTag.emg_bit         -> f_emg
                | FlowTag.test_bit        -> f_test
                | FlowTag.home_bit        -> f_home
                | FlowTag.readycondi_bit  -> f_readycondi
                | FlowTag.drivecondi_bit  -> f_drivecondi
                | _ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            t :?> PlanVar<bool>
