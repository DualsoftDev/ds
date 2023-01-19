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

        let f_rop    = dsTag s $"{f.Name}(ROP)" DuBOOL // Ready Operation Mode
        let f_aop    = dsTag s $"{f.Name}(AOP)" DuBOOL // Auto Operation Mode
        let f_mop    = dsTag s $"{f.Name}(MOP)" DuBOOL // Manual Operation Mode
        let f_dop    = dsTag s $"{f.Name}(DOP)" DuBOOL // Drive Operation Mode
        let f_top    = dsTag s $"{f.Name}(TOP)" DuBOOL //  Test  Operation Mode (시운전)
        let f_sop    = dsTag s $"{f.Name}(SOP)" DuBOOL // Stop State
        let f_eop    = dsTag s $"{f.Name}(EOP)" DuBOOL // Emergency State
        let f_readycondi = dsTag s $"{f.Name}(SCR)" DuBOOL  //system condition ready
        let f_drivecondi = dsTag s $"{f.Name}(SCD)" DuBOOL  //system condition drive
        let f_auto   = dsTag s $"{f.Name}_auto"     DuBOOL
        let f_manual = dsTag s $"{f.Name}_manual"   DuBOOL
        let f_drive  = dsTag s $"{f.Name}_drive"    DuBOOL
        let f_stop   = dsTag s $"{f.Name}_stop"     DuBOOL
        let f_ready  = dsTag s $"{f.Name}_ready"    DuBOOL
        let f_clear  = dsTag s $"{f.Name}_clear"    DuBOOL
        let f_emg    = dsTag s $"{f.Name}_emg"      DuBOOL
        let f_test   = dsTag s $"{f.Name}_test"     DuBOOL
        let f_home   = dsTag s $"{f.Name}_home"     DuBOOL

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
            t :?> DsTag<bool>
