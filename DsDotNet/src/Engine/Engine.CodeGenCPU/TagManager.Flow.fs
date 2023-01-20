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
        let s =  f.System.TagManager.Storages

        let f_rop    = planTag s $"{f.Name}(ROP)"      // Ready Operation Mode
        let f_aop    = planTag s $"{f.Name}(AOP)"      // Auto Operation Mode
        let f_mop    = planTag s $"{f.Name}(MOP)"      // Manual Operation Mode
        let f_dop    = planTag s $"{f.Name}(DOP)"      // Drive Operation Mode
        let f_top    = planTag s $"{f.Name}(TOP)"      //  Test  Operation Mode (시운전)
        let f_sop    = planTag s $"{f.Name}(SOP)"      // Stop State
        let f_eop    = planTag s $"{f.Name}(EOP)"      // Emergency State
        let f_readycondi = planTag s $"{f.Name}(SCR)"  //system condition ready
        let f_drivecondi = planTag s $"{f.Name}(SCD)"  //system condition drive
        let f_auto   = planTag s $"{f.Name}_auto"
        let f_manual = planTag s $"{f.Name}_manual"
        let f_drive  = planTag s $"{f.Name}_drive"
        let f_stop   = planTag s $"{f.Name}_stop"
        let f_ready  = planTag s $"{f.Name}_ready"
        let f_clear  = planTag s $"{f.Name}_clear"
        let f_emg    = planTag s $"{f.Name}_emg"
        let f_test   = planTag s $"{f.Name}_test"
        let f_home   = planTag s $"{f.Name}_home"

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
