namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module FlowManagerModule =
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages
        let cpv name (flowTag:FlowTag) = createPlanVar s name DuBOOL true f (int flowTag) f.System
        let fn = f.Name

        let f_rop        = cpv $"ROP_{fn}"     FlowTag.ready_mode             // Ready Operation State
        let f_aop        = cpv $"AOP_{fn}"     FlowTag.auto_mode              // Auto Operation State
        let f_mop        = cpv $"MOP_{fn}"     FlowTag.manual_mode            // Manual Operation State
        let f_sop        = cpv $"SOP_{fn}"     FlowTag.stop_mode             // Stop Operation State
        let f_eop        = cpv $"EOP_{fn}"     FlowTag.emg_mode              // Emergency Operation State
        let f_dop        = cpv $"DOP_{fn}"     FlowTag.drive_mode              // Drive Operation Mode
        let f_top        = cpv $"TOP_{fn}"     FlowTag.test_mode         // Test  Operation Mode (시운전)
        let f_iop        = cpv $"IOP_{fn}"     FlowTag.idle_mode              // Idle  Operation Mode
        let f_oop        = cpv $"OOP_{fn}"     FlowTag.origin_mode              // origin   Operation Mode
        let f_hop        = cpv $"HOP_{fn}"     FlowTag.homing_mode              // homing   Operation Mode
        
        //let f_readycondi = cpv $"SCR_{fn}"     FlowTag.readycondi_btn             //system condition ready
        //let f_drivecondi = cpv $"SCD_{fn}"     FlowTag.drivecondi_btn           //system condition drive
        let f_auto_btn       = cpv $"auto_btn_{fn}"    FlowTag.auto_btn       
        let f_manual_btn     = cpv $"manual_btn_{fn}"  FlowTag.manual_btn     
        let f_drive_btn      = cpv $"drive_btn_{fn}"   FlowTag.drive_btn
        let f_stop_btn       = cpv $"stop_btn_{fn}"    FlowTag.stop_btn
        let f_ready_btn      = cpv $"ready_btn_{fn}"   FlowTag.ready_btn
        let f_clear_btn      = cpv $"clear_btn_{fn}"   FlowTag.clear_btn
        let f_emg_btn        = cpv $"emg_btn_{fn}"     FlowTag.emg_btn
        let f_test_btn       = cpv $"test_btn_{fn}"    FlowTag.test_btn
        let f_home_btn       = cpv $"home_btn_{fn}"    FlowTag.home_btn

        let f_auto_lamp      = cpv $"auto_lamp_{fn}"    FlowTag.auto_lamp       
        let f_manual_lamp    = cpv $"manual_lamp_{fn}"  FlowTag.manual_lamp     
        let f_drive_lamp     = cpv $"drive_lamp_{fn}"   FlowTag.drive_lamp
        let f_stop_lamp      = cpv $"stop_lamp_{fn}"    FlowTag.stop_lamp
        let f_ready_lamp     = cpv $"ready_lamp_{fn}"   FlowTag.ready_lamp
        let f_clear_lamp     = cpv $"clear_lamp_{fn}"   FlowTag.clear_lamp
        let f_emg_lamp       = cpv $"emg_lamp_{fn}"     FlowTag.emg_lamp
        let f_test_lamp      = cpv $"test_lamp_{fn}"    FlowTag.test_lamp
        let f_home_lamp      = cpv $"home_lamp_{fn}"    FlowTag.home_lamp


        let f_stop_error      = cpv $"error_{fn}"   FlowTag.flowStopError
        let f_stop_pause      = cpv $"pause_{fn}"   FlowTag.flowStopPause
        let f_stop_condiErr   = cpv $"condiErr_{fn}"   FlowTag.flowStopConditionErr
       
        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag)     =
            let t =
                match ft with
                | FlowTag.ready_mode        -> f_rop
                | FlowTag.auto_mode         -> f_aop
                | FlowTag.manual_mode       -> f_mop
                | FlowTag.drive_mode        -> f_dop
                | FlowTag.test_mode         -> f_top
                | FlowTag.stop_mode         -> f_sop
                | FlowTag.emg_mode          -> f_eop
                | FlowTag.idle_mode         -> f_iop
                | FlowTag.origin_mode       -> f_oop
                | FlowTag.homing_mode       -> f_hop
                

                | FlowTag.auto_btn        -> f_auto_btn
                | FlowTag.manual_btn      -> f_manual_btn
                | FlowTag.drive_btn       -> f_drive_btn
                | FlowTag.stop_btn        -> f_stop_btn
                | FlowTag.ready_btn       -> f_ready_btn
                | FlowTag.clear_btn       -> f_clear_btn
                | FlowTag.emg_btn         -> f_emg_btn
                | FlowTag.test_btn        -> f_test_btn
                | FlowTag.home_btn        -> f_home_btn
                
                | FlowTag.auto_lamp        -> f_auto_lamp
                | FlowTag.manual_lamp      -> f_manual_lamp
                | FlowTag.drive_lamp       -> f_drive_lamp
                | FlowTag.stop_lamp        -> f_stop_lamp
                | FlowTag.ready_lamp       -> f_ready_lamp
                | FlowTag.clear_lamp       -> f_clear_lamp
                | FlowTag.emg_lamp         -> f_emg_lamp
                | FlowTag.test_lamp        -> f_test_lamp
                | FlowTag.home_lamp        -> f_home_lamp

                | FlowTag.flowStopError    -> f_stop_error
                | FlowTag.flowStopPause    -> f_stop_pause
                | FlowTag.flowStopConditionErr    -> f_stop_condiErr
                | _ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            t :?> PlanVar<bool>
