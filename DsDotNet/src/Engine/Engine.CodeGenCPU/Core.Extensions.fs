namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System

[<AutoOpen>]
module ConvertCoreExt =
    
    type InOut = | In | Out | Memory
    let private getIOs(name, address, inOut:InOut): ITagWithAddress   =  
        let plcName = match inOut with 
                        | In  -> $"{name}_I" 
                        | Out -> $"{name}_O" 
                        | Memory -> failwith "error: Memory not supported "

        (PlcTag(plcName, address, false) :> ITagWithAddress)

    let getVM(v:Vertex) = v.VertexManager :?> VertexManager

    type ApiItem with
        member s.tx = DsTag<bool>("tx", false)
        member s.rx = DsTag<bool>("rx", false)

    type DsSystem with
        member s._on     = DsTag<bool>("_on", true)
        member s._off    = DsTag<bool>("_off", false)
        member s._auto   = DsTag<bool>("_auto", false)
        member s._manual = DsTag<bool>("_manual", false)
        member s._emg    = DsTag<bool>("_emg", false)
        member s._run    = DsTag<bool>("_run", false)
        member s._stop   = DsTag<bool>("_stop", false)
        member s._clear  = DsTag<bool>("_clear", false)
        member s._dryrun = DsTag<bool>("dryrun", false)
        member s._tout   = DsTag<uint16> ("_tout", 10000us)
        member s._yy     = DsTag<int> ("_yy", 0)
        member s._mm     = DsTag<int> ("_mm", 0)
        member s._dd     = DsTag<int> ("_dd", 0)
        member s._h      = DsTag<int> ("_h", 0)
        member s._m      = DsTag<int> ("_m", 0)
        member s._s      = DsTag<int> ("_s", 0)
        member s._ms     = DsTag<int> ("_ms", 0)

        member s.GenerationLampIO() =
            s.SystemLamps
                   .ForEach(fun b->b.OutTag  <- getIOs(b.Name, b.OutAddress, In))

        member s.GenerationButtonIO() = 
            s.SystemButtons
                     .ForEach(fun b-> b.InTag  <- getIOs(b.Name, b.OutAddress, In))
            s.SystemButtons
                     .ForEach(fun b->b.OutTag <- getIOs(b.Name, b.OutAddress, Out))
            
        member s.GenerationJobIO() = 
            let jobDefs = s.Jobs |> Seq.collect(fun j -> j.JobDefs)
            jobDefs
                   .ForEach(fun jdef->jdef.InTag <- getIOs(jdef.ApiName, jdef.InAddress, In))
            jobDefs
                   .ForEach(fun jdef->jdef.OutTag <- getIOs(jdef.ApiName, jdef.OutAddress, Out))

        //[auto, manual] system HMI 두개다 선택이 안됨
        member s.ModeNoExpr = !!s._auto.Expr <&&> !!s._manual.Expr
        //자신이 사용된 API Txs
        member s.GetTXs(r:Real)= s.ApiItems.Where(fun api->api.TXs.Contains(r)).Select(fun f->f.tx)
        member s.GetRXs(r:Real)= s.ApiItems.Where(fun api->api.RXs.Contains(r)).Select(fun f->f.rx)
            
    let private getButtonInputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b -> b.InTag)
                .Cast<PlcTag<bool>>()   

    let private getButtonOutputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b -> b.OutTag)
                .Cast<PlcTag<bool>>()   

    let private getLampOutputs(flow:Flow, btns:LampDef seq) : PlcTag<bool> seq = 
            btns.Where(fun b -> b.SettingFlow = flow)
                .Select(fun b -> b.OutTag)
                .Cast<PlcTag<bool>>()   
    
    let private getAutoManualIOs(autoIns:PlcTag<bool> seq, manualIns:PlcTag<bool> seq, sysOff:DsTag<bool>) =
          if autoIns.Count() > 1 || manualIns.Count() > 1
          then failwith "Error : One button(auto or manual) must be assigned to one flow"

          let auto    = if autoIns.Any()   then  autoIns.Head().Expr else sysOff.Expr
          let manual  = if manualIns.Any() then  manualIns.ToAnd()   else sysOff.Expr
          auto, manual

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
        member f.homeIns    = getButtonInputs (f, f.System.HomeButtons) 
        member f.homeOuts   = getButtonOutputs (f, f.System.HomeButtons) 

        //램프 IO PLC TAG
        member f.runModelampOuts    = getLampOutputs (f, f.System.RunModeLamps) 
        member f.dryrunModelampOuts = getLampOutputs (f, f.System.DryRunModeLamps) 
        member f.manualModelampOuts = getLampOutputs (f, f.System.ManualModeLamps) 
        member f.stopModelampOuts   = getLampOutputs (f, f.System.StopModeLamps) 
        member f.emergencylampOuts  = getLampOutputs (f, f.System.EmergencyModeLamps) 
        
        //[auto, manual] HW Input 두개다 선택이 안됨
        member f.ModeNoHWExpr = 
                let auto, manual = getAutoManualIOs (f.autoIns, f.manualIns, f.System._off)
                !!auto <&&> !!manual

        member f.ModeManualHwExpr = 
                let auto, manual = getAutoManualIOs (f.autoIns, f.manualIns, f.System._off)
                !!auto <&&> manual

        member f.ModeAutoHwExpr = 
                let auto, manual = getAutoManualIOs (f.autoIns, f.manualIns, f.System._off)
                auto <&&> !!manual

         member f.ModeAutoSwHMIExpr   =  f.auto.Expr <&&> !!f.manual.Expr
         member f.ModeManualSwHMIExpr =  !!f.auto.Expr <&&> f.manual.Expr
                
        member f.RunExpr = f.System._run.Expr 
                           <||> f.run.Expr 
                           <||> if f.runIns.any() 
                                then f.runIns.ToOr() else f.System._off.Expr

        member f.DryRunExpr = f.System._dryrun.Expr 
                           <||> f.dryrun.Expr 
                           <||> if f.dryIns.any() 
                                then f.dryIns.ToOr() else f.System._off.Expr

        member f.StopExpr = f.System._stop.Expr 
                           <||> f.stop.Expr 
                           <||> if f.stopIns.any() 
                                then f.stopIns.ToOr() else f.System._off.Expr
        
        //test ahn : plctag b접점 옵션 반영필요
        member f.EmgExpr = f.System._emg.Expr 
                           <||> f.emg.Expr 
                           <||> if f.emgIns.any() 
                                then f.emgIns.ToOr() else f.System._off.Expr


    type Call with
        member c.V = c.VertexManager :?> VertexManager
        member c.UsingTon = c.CallTarget.Observes.Where(fun f->f.Name = TextOnDelayTimer).any()
        member c.UsingCtr = c.CallTarget.Observes.Where(fun f->f.Name = TextRingCounter).any()

        member c.PresetTime =   if c.UsingTon 
                                then c.CallTarget.Observes.First(fun f->f.Name = TextOnDelayTimer).GetDelayTime()
                                else failwith $"{c.Name} not use timer"

        member c.PresetCounter = if c.UsingCtr 
                                 then c.CallTarget.Observes.First(fun f->f.Name = TextRingCounter).GetRingCount()
                                 else failwith $"{c.Name} not use counter"
                            
        member c.INs  = c.CallTarget.JobDefs.Select(fun j -> j.InTag).Cast<PlcTag<bool>>()
        member c.OUTs = c.CallTarget.JobDefs.Select(fun j -> j.OutTag).Cast<PlcTag<bool>>()
        member c.TXs  = c.CallTarget.JobDefs |> Seq.collect(fun (j: JobDef) -> j.ApiItem.TXs)
                                             |> Seq.map getVM |> Seq.map(fun f->f.ST)
        member c.RXs  = c.CallTarget.JobDefs |> Seq.collect(fun (j: JobDef) -> j.ApiItem.RXs) 
                                             |> Seq.map getVM |> Seq.map(fun f->f.ET)
        member c.MutualResetOuts = 
            c.CallTarget.JobDefs
                .SelectMany(fun j -> j.ApiItem.System.GetMutualResetApis(j.ApiItem))
                .SelectMany(fun a -> c.System.JobDefs.Where(fun w-> w.ApiItem = a))
                .Select(fun j -> j.OutTag).Cast<PlcTag<bool>>()
                .Cast<PlcTag<bool>>()
    
    type Real with
        member r.V = r.VertexManager :?> VertexManager
        member r.CoinRelays = r.Graph.Vertices.Select(getVM).Select(fun f->f.CR)
        member r.ErrorTXs = r.Graph.Vertices.Select(getVM).Select(fun f->f.E1)
        member r.ErrorRXs = r.Graph.Vertices.Select(getVM).Select(fun f->f.E2)

    type Alias with
        member a.V = a.VertexManager :?> VertexManager                    

    type RealOtherFlow with
        member a.V = a.VertexManager :?> VertexManager                    

  