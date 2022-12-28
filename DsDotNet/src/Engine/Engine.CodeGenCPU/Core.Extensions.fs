namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System

[<AutoOpen>]
module ConvertCoreExt =
    
    let private getButtonInputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Where(fun b -> b.InTag.IsSome)
                .Select(fun b -> b.InTag).Cast<PlcTag<bool>>()

    let private getButtonOutputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Where(fun b -> b.OutTag.IsSome)
                .Select(fun b -> b.OutTag).Cast<PlcTag<bool>>()

    let private getLampOutputs(flow:Flow, btns:LampDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlow = flow)
                .Where(fun b -> b.OutTag.IsSome)
                .Select(fun b -> b.OutTag).Cast<PlcTag<bool>>()

    type InOut = | In | Out | Memory
    let private getIOs(name, address, inOut:InOut): ITagWithAddress option  =  
            let plcName = match inOut with 
                          | In  -> $"{name}_I" 
                          | Out -> $"{name}_I" 
                          | Memory -> failwith "error: Memory not supported "

            if address = "" then None
                            else Some (PlcTag(plcName, address, false) :> ITagWithAddress)
                          
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

        member s.GenerationLampIO() =
            s.SystemLamps.Where(fun w -> w.OutTag.IsNone)
                   .ForEach(fun b->b.OutTag  <- getIOs(b.Name, b.OutAddress, In))

        member s.GenerationButtonIO() = 
            s.SystemButtons.Where(fun w -> w.InTag.IsNone)
                     .ForEach(fun b->b.InTag  <- getIOs(b.Name, b.OutAddress, In))
            s.SystemButtons.Where(fun w -> w.OutTag.IsNone)
                     .ForEach(fun b->b.OutTag <- getIOs(b.Name, b.OutAddress, Out))
            
        member s.GenerationJobIO() = 
            let jobDefs = s.Jobs |> Seq.collect(fun j -> j.JobDefs)
            jobDefs.Where(fun w -> w.InTag.IsNone)
                   .ForEach(fun jdef->jdef.InTag <- getIOs(jdef.ApiName, jdef.InAddress, In))
            jobDefs.Where(fun w -> w.OutTag.IsNone)
                   .ForEach(fun jdef->jdef.OutTag <- getIOs(jdef.ApiName, jdef.OutAddress, Out))

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

        //버튼 IO PLC TAG
        member f.autoIns    = getButtonInputs  (f, f.System.AutoButtons) 
        member f.autoOuts   = getButtonOutputs (f, f.System.AutoButtons) 
        member f.manualIns  = getButtonInputs (f, f.System.ManualButtons) 
        member f.manualOuts = getButtonOutputs (f, f.System.ManualButtons) 
        member f.emgIns     = getButtonInputs (f, f.System.EmergencyButtons) 
        member f.emgOuts    = getButtonOutputs (f, f.System.EmergencyButtons) 
        member f.stopIns    = getButtonInputs (f, f.System.StopButtons) 
        member f.stopOuts   = getButtonOutputs (f, f.System.StopButtons) 
        member f.runIns     = getButtonInputs (f, f.System.RunButtons) 
        member f.runOuts    = getButtonOutputs (f, f.System.RunButtons) 
        member f.dryIns     = getButtonInputs (f, f.System.DryRunButtons) 
        member f.dryOuts    = getButtonOutputs (f, f.System.DryRunButtons) 
        member f.clearIns   = getButtonInputs (f, f.System.ClearButtons) 
        member f.clearOuts  = getButtonOutputs (f, f.System.ClearButtons) 

        //램프 IO PLC TAG
        member f.runModelampOuts    = getLampOutputs (f, f.System.RunModeLamps) 
        member f.dryrunModelampOuts = getLampOutputs (f, f.System.DryRunModeLamps) 
        member f.manualModelampOuts = getLampOutputs (f, f.System.ManualModeLamps) 
        member f.stopModelampOuts   = getLampOutputs (f, f.System.StopModeLamps) 
