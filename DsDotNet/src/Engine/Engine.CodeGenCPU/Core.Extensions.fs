namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System

[<AutoOpen>]
module ConvertCoreExt =
    
    
    type DsSystem with
        member s._on     = DsTag<bool>("_on", false)
        member s._off    = DsTag<bool>("_off", false)
        member s._auto   = DsTag<bool>("_auto", false)
        member s._manual = DsTag<bool>("_manual", false)
        member s._emg    = DsTag<bool>("_emg", false)
        member s._run    = DsTag<bool>("_run", false)
        member s._stop   = DsTag<bool>("_stop", false)
        member s._clear  = DsTag<bool>("_clear", false)
        member s._dryrun = DsTag<bool>("dryrun", false)
        member s._yy     = DsTag<int> ("_yy", 0)
        member s._mm     = DsTag<int> ("_mm", 0)
        member s._dd     = DsTag<int> ("_dd", 0)
        member s._h      = DsTag<int> ("_h", 0)
        member s._m      = DsTag<int> ("_m", 0)
        member s._s      = DsTag<int> ("_s", 0)
        member s._ms     = DsTag<int> ("_ms", 0)

//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with
        member f.eop    = DsTag<bool> ($"{f.Name}(EO)", false)   // Emergency Operation Mode
        member f.sop    = DsTag<bool> ($"{f.Name}(SO)", false)   // Stop Operation Mode
        member f.mop    = DsTag<bool> ($"{f.Name}(MO)", false)   // Manual Operation Mode 
        member f.rop    = DsTag<bool> ($"{f.Name}(RO)", false)   // Auto Run Operation Mode 
        member f.dop    = DsTag<bool> ($"{f.Name}(DO)", false)   // Dry Run Operation Mode (시운전) 
        member f.auto   = DsTag<bool>("_auto", false)
        member f.manual = DsTag<bool>("_manual", false)
        member f.emg    = DsTag<bool>("_emg", false)
        member f.run    = DsTag<bool>("_run", false)
        member f.stop   = DsTag<bool>("_stop", false)
        member f.clear  = DsTag<bool>("_clear", false)
        member f.dryrun = DsTag<bool>("dryrun", false)  



