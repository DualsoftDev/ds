namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System

[<AutoOpen>]
module FlowManagerModule =

    [<Flags>]
    type FlowTag =
    |READY_OP           //Operation Mode
    |AUTO_OP
    |MANUAL_OP
    |DRIVE_OP
    |TEST_OP
    |STOP_OP
    |EMERGENCY_OP
    |IDLE_OP
    |AUTO_BIT           //Flow bit
    |MANUAL_BIT
    |DRIVE_BIT
    |STOP_BIT
    |READY_BIT
    |CLEAR_BIT
    |EMG_BIT
    |TEST_BIT
    |HOME_BIT
    |READYCONDI_BIT
    |DRIVECONDI_BIT


    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =
        let sys =  f.System
        let s =  sys.TagManager.Storages

        let f_rop    = createPlanVarBool s $"{f.Name}_ROP_"          // Ready Operation State
        let f_aop    = createPlanVarBool s $"{f.Name}_AOP_"          // Auto Operation State
        let f_mop    = createPlanVarBool s $"{f.Name}_MOP_"          // Manual Operation State
        let f_sop    = createPlanVarBool s $"{f.Name}_SOP_"          // Stop Operation State
        let f_eop    = createPlanVarBool s $"{f.Name}_EOP_"          // Emergency Operation State
        let f_dop    = createPlanVarBool s $"{f.Name}_DOP_"          // Drive Operation Mode
        let f_top    = createPlanVarBool s $"{f.Name}_TOP_"          // Test  Operation Mode (시운전)
        let f_iop    = createPlanVarBool s $"{f.Name}_IOP_"          // Idle  Operation Mode
        let f_readycondi = createPlanVarBool s $"{f.Name}_SCR_"      //system condition ready
        let f_drivecondi = createPlanVarBool s $"{f.Name}_SCD_"      //system condition drive
        let f_auto   = createPlanVarBool s $"{f.Name}_auto"
        let f_manual = createPlanVarBool s $"{f.Name}_manual"
        let f_drive  = createPlanVarBool s $"{f.Name}_drive"
        let f_stop   = createPlanVarBool s $"{f.Name}_stop"
        let f_ready  = createPlanVarBool s $"{f.Name}_ready"
        let f_clear  = createPlanVarBool s $"{f.Name}_clear"
        let f_emg    = createPlanVarBool s $"{f.Name}_emg"
        let f_test   = createPlanVarBool s $"{f.Name}_test"
        let f_home   = createPlanVarBool s $"{f.Name}_home"

        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag)     =
            let t =
                match ft with
                |READY_OP        -> f_rop
                |AUTO_OP         -> f_aop
                |MANUAL_OP       -> f_mop
                |DRIVE_OP        -> f_dop
                |TEST_OP         -> f_top
                |STOP_OP         -> f_sop
                |EMERGENCY_OP    -> f_eop
                |IDLE_OP         -> f_iop
                |AUTO_BIT        -> f_auto
                |MANUAL_BIT      -> f_manual
                |DRIVE_BIT       -> f_drive
                |STOP_BIT        -> f_stop
                |READY_BIT       -> f_ready
                |CLEAR_BIT       -> f_clear
                |EMG_BIT         -> f_emg
                |TEST_BIT        -> f_test
                |HOME_BIT        -> f_home
                |READYCONDI_BIT  -> f_readycondi
                |DRIVECONDI_BIT  -> f_drivecondi
            t
