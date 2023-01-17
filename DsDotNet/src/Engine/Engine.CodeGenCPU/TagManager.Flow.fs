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


    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =    
        let s =  f.System.TagManager.Storages

        let f_rop    = dsBit s $"{f.Name}(ROP)" false   // Ready Operation Mode
        let f_aop    = dsBit s $"{f.Name}(AOP)" false   // Auto Operation Mode
        let f_mop    = dsBit s $"{f.Name}(MOP)" false   // Manual Operation Mode 
        let f_dop    = dsBit s $"{f.Name}(DOP)" false   // Drive Operation Mode 
        let f_top    = dsBit s $"{f.Name}(TOP)" false   //  Test  Operation Mode (시운전) 
        let f_sop    = dsBit s $"{f.Name}(SOP)" false   // Stop State 
        let f_eop    = dsBit s $"{f.Name}(EOP)" false   // Emergency State 
        let f_auto   = dsBit s $"{f.Name}_auto" false
        let f_manual = dsBit s $"{f.Name}_manual" false
        let f_drive  = dsBit s $"{f.Name}_drive" false
        let f_stop   = dsBit s $"{f.Name}_stop" false
        let f_ready  = dsBit s $"{f.Name}_ready" false
        let f_clear  = dsBit s $"{f.Name}_clear" false
        let f_emg    = dsBit s $"{f.Name}_emg"  false
        let f_test   = dsBit s $"{f.Name}_test" false  
        let f_home   = dsBit s $"{f.Name}_home" false  
                
        interface ITagManager with
            member x.Target = f
            member x.Storages = s

        member f.GetFlowTag(ft:FlowTag)     = 
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
       
