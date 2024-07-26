namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module FlowManagerModule =
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages
        let cpv  (flowTag:FlowTag) =
            let name = getStorageName f (int flowTag)
            createPlanVar s name DuBOOL true f (int flowTag) f.System

        let f_iop        = cpv  FlowTag.idle_mode              // Idle  Operation Mode
        let f_aop        = cpv  FlowTag.auto_mode              // Auto Operation State
        let f_mop        = cpv  FlowTag.manual_mode            // Manual Operation State
       
       
        let f_e_st        = cpv   FlowTag.error_state             // error State 
        let f_d_st        = cpv   FlowTag.drive_state              // Drive State
        let f_r_st        = cpv   FlowTag.ready_state             // Ready  State
        let f_t_st        = cpv   FlowTag.test_state         // Test  Operation State (시운전)
        let f_o_st        = cpv   FlowTag.origin_state              // origin   State
        let f_g_st        = cpv   FlowTag.going_state              // going  State
        let f_emg_st      = cpv   FlowTag.emergency_state             // Emergency  State
        let f_p_st        = cpv   FlowTag.pause_state             // pause  State

        let f_auto_btn    = cpv  FlowTag.auto_btn       
        let f_manual_btn  = cpv  FlowTag.manual_btn     
        let f_drive_btn   = cpv  FlowTag.drive_btn
        let f_pause_btn   = cpv  FlowTag.pause_btn
        let f_ready_btn   = cpv  FlowTag.ready_btn
        let f_clear_btn   = cpv  FlowTag.clear_btn
        let f_emg_btn     = cpv  FlowTag.emg_btn
        let f_test_btn    = cpv  FlowTag.test_btn
        let f_home_btn    = cpv  FlowTag.home_btn
        
        let f_auto_lamp      = cpv   FlowTag.auto_lamp       
        let f_manual_lamp    = cpv   FlowTag.manual_lamp     
        let f_drive_lamp     = cpv   FlowTag.drive_lamp
        let f_pause_lamp     = cpv   FlowTag.pause_lamp
        let f_ready_lamp     = cpv   FlowTag.ready_lamp
        let f_clear_lamp     = cpv   FlowTag.clear_lamp
        let f_emg_lamp       = cpv   FlowTag.emg_lamp
        let f_test_lamp      = cpv   FlowTag.test_lamp
        let f_home_lamp      = cpv   FlowTag.home_lamp

        let f_stop_error      = cpv   FlowTag.flowStopError
        let f_stop_readyCondition   = cpv   FlowTag.flowReadyCondition
        let f_stop_driveCondition   = cpv   FlowTag.flowDriveCondition
       
        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag)     =
            let t =
                match ft with
                | FlowTag.ready_state       -> f_r_st
                | FlowTag.auto_mode         -> f_aop
                | FlowTag.manual_mode       -> f_mop
                | FlowTag.drive_state       -> f_d_st
                | FlowTag.test_state        -> f_t_st
                | FlowTag.error_state       -> f_e_st
                | FlowTag.idle_mode         -> f_iop
                | FlowTag.origin_state      -> f_o_st
                | FlowTag.going_state       -> f_g_st
                | FlowTag.emergency_state   -> f_emg_st
                | FlowTag.pause_state       -> f_p_st
                
                
                | FlowTag.auto_btn        -> f_auto_btn
                | FlowTag.manual_btn      -> f_manual_btn
                | FlowTag.drive_btn       -> f_drive_btn
                | FlowTag.pause_btn       -> f_pause_btn
                | FlowTag.ready_btn       -> f_ready_btn
                | FlowTag.clear_btn       -> f_clear_btn
                | FlowTag.emg_btn         -> f_emg_btn
                | FlowTag.test_btn        -> f_test_btn
                | FlowTag.home_btn        -> f_home_btn
                
                | FlowTag.auto_lamp        -> f_auto_lamp
                | FlowTag.manual_lamp      -> f_manual_lamp
                | FlowTag.drive_lamp       -> f_drive_lamp
                | FlowTag.pause_lamp       -> f_pause_lamp
                | FlowTag.ready_lamp       -> f_ready_lamp
                | FlowTag.clear_lamp       -> f_clear_lamp
                | FlowTag.emg_lamp         -> f_emg_lamp
                | FlowTag.test_lamp        -> f_test_lamp
                | FlowTag.home_lamp        -> f_home_lamp

                | FlowTag.flowStopError         -> f_stop_error
                | FlowTag.flowReadyCondition    -> f_stop_readyCondition
                | FlowTag.flowDriveCondition    -> f_stop_driveCondition
                | _ -> failwithlog $"Error : GetFlowTag {ft} type not support!!"
            t :?> PlanVar<bool>
