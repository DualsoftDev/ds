namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System

[<AutoOpen>]
module ConvertCoreExt =
    
    let private getButtonIOs(flow:Flow, btns:ButtonDef seq) : ButtonDef seq = 
            btns.Where(fun b -> b.SettingFlows.Contains(flow))

    type InOut = | In | Out

    let private getIOs(name, address, inOut:InOut): PlcTag<bool>   =  
            PlcTag(name, address, false)
                          
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

        //member s.GenerationLampIO() = 
        //    s.Lamps.Where(fun w->w.OutTag.IsNone)
        //           .ForEach(fun l->l.OutTag  <- Some (getIOs($"{l.Name}_O", l.OutAddress)))

        member s.GenerationButtonIO() = 
            s.Buttons.Where(fun w -> w.OutTag.IsNone)
                     .ForEach(fun b->b.OutTag <- Some (PlcTag($"{b.Name}_I", b.OutAddress, false)))
            s.Buttons.Where(fun w -> w.InTag.IsNone)
                     .ForEach(fun b->b.InTag  <- Some (PlcTag($"{b.Name}_O", b.InAddress, false)))
        
        member s.GenerationJobIO() = 
            let jobDefs = s.Jobs |> Seq.collect(fun j -> j.JobDefs)
            jobDefs.Where(fun w -> w.InTag.IsNone)
                   .ForEach(fun jdef->jdef.InTag <- Some (PlcTag($"{jdef.ApiName}_I", jdef.InAddress, false)))
            jobDefs.Where(fun w -> w.OutTag.IsNone)
                   .ForEach(fun jdef->jdef.OutTag <- Some (PlcTag($"{jdef.ApiName}_O", jdef.OutAddress, false)))




//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with
        member f.eop    = DsTag<bool> ($"{f.Name}(EO)", false)   // Emergency Operation Mode
        member f.sop    = DsTag<bool> ($"{f.Name}(SO)", false)   // Stop Operation Mode
        member f.mop    = DsTag<bool> ($"{f.Name}(MO)", false)   // Manual Operation Mode 
        member f.rop    = DsTag<bool> ($"{f.Name}(RO)", false)   // Auto Run Operation Mode 
        member f.dop    = DsTag<bool> ($"{f.Name}(DO)", false)   // Dry Run Operation Mode (시운전) 
        member f.auto   = DsTag<bool>("auto", false)
        member f.manual = DsTag<bool>("manual", false)
        member f.emg    = DsTag<bool>("emg", false)
        member f.run    = DsTag<bool>("run", false)
        member f.stop   = DsTag<bool>("stop", false)
        member f.clear  = DsTag<bool>("clear", false)
        member f.dryrun = DsTag<bool>("dryrun", false)  

        member f.autoINs    = getButtonIOs (f, f.System.AutoButtons     ) 
        member f.manualIOs  = getButtonIOs (f, f.System.ManualButtons   ) 
        member f.emgIOs     = getButtonIOs (f, f.System.EmergencyButtons) 
        member f.stopIOs    = getButtonIOs (f, f.System.StopButtons     ) 
        member f.runIOs     = getButtonIOs (f, f.System.RunButtons      ) 
        member f.dryIOs     = getButtonIOs (f, f.System.DryRunButtons   ) 
        member f.clearIOs   = getButtonIOs (f, f.System.ClearButtons    ) 
        


