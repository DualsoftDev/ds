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

        let f_iop        = cpv $"IOP_{fn}"     FlowTag.idle_mode              // Idle  Operation Mode
        let f_aop        = cpv $"AOP_{fn}"     FlowTag.auto_mode              // Auto Operation State
        let f_mop        = cpv $"MOP_{fn}"     FlowTag.manual_mode            // Manual Operation State
       
       
        let f_e_st        = cpv $"E_ST_{fn}"     FlowTag.error_state             // error State 
        let f_d_st        = cpv $"D_ST_{fn}"     FlowTag.drive_state              // Drive State
        let f_r_st        = cpv $"R_ST_{fn}"     FlowTag.ready_state             // Ready  State
        let f_t_st        = cpv $"T_ST_{fn}"     FlowTag.test_state         // Test  Operation State (시운전)
        let f_o_st        = cpv $"O_ST_{fn}"     FlowTag.origin_state              // origin   State
        let f_g_st        = cpv $"G_ST_{fn}"     FlowTag.going_state              // going  State
        let f_emg_st      = cpv $"EMG_ST_{fn}"   FlowTag.emergency_state             // Emergency  State
        let f_p_st        = cpv $"P_ST_{fn}"     FlowTag.pause_state             // pause  State

        let f_auto_btn       = cpv $"auto_btn_{fn}"    FlowTag.auto_btn       
        let f_manual_btn     = cpv $"manual_btn_{fn}"  FlowTag.manual_btn     
        let f_drive_btn      = cpv $"drive_btn_{fn}"   FlowTag.drive_btn
        let f_pause_btn      = cpv $"pause_btn_{fn}"   FlowTag.pause_btn
        let f_ready_btn      = cpv $"ready_btn_{fn}"   FlowTag.ready_btn
        let f_clear_btn      = cpv $"clear_btn_{fn}"   FlowTag.clear_btn
        let f_emg_btn        = cpv $"emg_btn_{fn}"     FlowTag.emg_btn
        let f_test_btn       = cpv $"test_btn_{fn}"    FlowTag.test_btn
        let f_home_btn       = cpv $"home_btn_{fn}"    FlowTag.home_btn
        
        let f_auto_lamp      = cpv $"auto_lamp_{fn}"    FlowTag.auto_lamp       
        let f_manual_lamp    = cpv $"manual_lamp_{fn}"  FlowTag.manual_lamp     
        let f_drive_lamp     = cpv $"drive_lamp_{fn}"   FlowTag.drive_lamp
        let f_pause_lamp     = cpv $"pause_lamp_{fn}"   FlowTag.pause_lamp
        let f_ready_lamp     = cpv $"ready_lamp_{fn}"   FlowTag.ready_lamp
        let f_clear_lamp     = cpv $"clear_lamp_{fn}"   FlowTag.clear_lamp
        let f_emg_lamp       = cpv $"emg_lamp_{fn}"     FlowTag.emg_lamp
        let f_test_lamp      = cpv $"test_lamp_{fn}"    FlowTag.test_lamp
        let f_home_lamp      = cpv $"home_lamp_{fn}"    FlowTag.home_lamp

        let f_stop_error      = cpv $"error_{fn}"   FlowTag.flowStopError
        let f_stop_readyCondition   = cpv $"condiReady_{fn}"   FlowTag.flowReadyCondition
        let f_stop_driveCondition   = cpv $"condiDrive_{fn}"   FlowTag.flowDriveCondition
       
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
