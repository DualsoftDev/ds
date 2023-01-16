namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module FlowManagerModule =
                                            
  
    /// Flow Manager : Flow Tag  를 관리하는 컨테이어
    type FlowManager (f:Flow)  =

        let f_rop    = dsBit f.System $"{f.Name}(ROM)" false   // Ready Operation Mode
        let f_aop    = dsBit f.System $"{f.Name}(AOM)" false   // Auto Operation Mode
        let f_mop    = dsBit f.System $"{f.Name}(MOM)" false   // Manual Operation Mode 
        let f_dop    = dsBit f.System $"{f.Name}(DOM)" false   // Drive Operation Mode 
        let f_top    = dsBit f.System $"{f.Name}(TOM)" false   //  Test  Operation Mode (시운전) 
        let f_sop    = dsBit f.System $"{f.Name}(SOM)" false   // Stop State 
        let f_eop    = dsBit f.System $"{f.Name}(EOM)" false   // Emergency State 
        let f_auto   = dsBit f.System $"{f.Name}_auto" false
        let f_manual = dsBit f.System $"{f.Name}_manual" false
        let f_drive  = dsBit f.System $"{f.Name}_drive" false
        let f_stop   = dsBit f.System $"{f.Name}_stop" false
        let f_ready  = dsBit f.System $"{f.Name}_ready" false
        let f_clear  = dsBit f.System $"{f.Name}_clear" false
        let f_emg    = dsBit f.System $"{f.Name}_emg"  false
        let f_test   = dsBit f.System $"{f.Name}_test" false  
        let f_home   = dsBit f.System $"{f.Name}_home" false  

        member f.rop     = f_rop   
        member f.aop     = f_aop   
        member f.mop     = f_mop   
        member f.dop     = f_dop   
        member f.top     = f_top   
        member f.sop     = f_sop   
        member f.eop     = f_eop   
        member f.auto    = f_auto  
        member f.manual  = f_manual
        member f.drive   = f_drive 
        member f.stop    = f_stop  
        member f.ready   = f_ready 
        member f.clear   = f_clear 
        member f.emg     = f_emg   
        member f.test    = f_test  
        member f.home    = f_home  

