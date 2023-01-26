namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module FlowManagerModule =

    [<AutoOpen>]
    type FlowTag =
    |READY_OP           //Operation Mode
    |AUTO_OP
    |MANUAL_OP
    |DRIVE_OP
    |TEST_OP
    |STOP_OP
    |EMERGENCY_OP
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

        let f_rop    = createPlanVar s $"{f.Name}(ROP)"     sys     // Ready Operation Mode
        let f_aop    = createPlanVar s $"{f.Name}(AOP)"     sys     // Auto Operation Mode
        let f_mop    = createPlanVar s $"{f.Name}(MOP)"     sys     // Manual Operation Mode
        let f_dop    = createPlanVar s $"{f.Name}(DOP)"     sys     // Drive Operation Mode
        let f_top    = createPlanVar s $"{f.Name}(TOP)"     sys     //  Test  Operation Mode (시운전)
        let f_sop    = createPlanVar s $"{f.Name}(SOP)"     sys     // Stop State
        let f_eop    = createPlanVar s $"{f.Name}(EOP)"     sys     // Emergency State
        let f_readycondi = createPlanVar s $"{f.Name}(SCR)" sys //system condition ready
        let f_drivecondi = createPlanVar s $"{f.Name}(SCD)" sys //system condition drive
        let f_auto   = createPlanVar s $"{f.Name}_auto"     sys
        let f_manual = createPlanVar s $"{f.Name}_manual"   sys
        let f_drive  = createPlanVar s $"{f.Name}_drive"    sys
        let f_stop   = createPlanVar s $"{f.Name}_stop"     sys
        let f_ready  = createPlanVar s $"{f.Name}_ready"    sys
        let f_clear  = createPlanVar s $"{f.Name}_clear"    sys
        let f_emg    = createPlanVar s $"{f.Name}_emg"      sys
        let f_test   = createPlanVar s $"{f.Name}_test"     sys
        let f_home   = createPlanVar s $"{f.Name}_home"     sys

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
