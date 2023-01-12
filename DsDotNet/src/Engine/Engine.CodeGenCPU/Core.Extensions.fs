namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System

[<AutoOpen>]
module rec ConvertCoreExt =
    
    type InOut = | In | Out | Memory
    let private getIOs(name, address, inOut:InOut): ITagWithAddress   =  
        let plcName = match inOut with 
                        | In  -> $"{name}_I" 
                        | Out -> $"{name}_O" 
                        | Memory -> failwith "error: Memory not supported "

        (PlcTag(plcName, address, false) :> ITagWithAddress)

    let getVM(v:Vertex) = v.VertexManager :?> VertexManager
    let getVMReal(v:Vertex) = v.VertexManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.VertexManager :?> VertexMCoin


    type DsSystem with
        member s._on     = DsTag<bool>("_on", true)
        member s._off    = DsTag<bool>("_off", false)
        member s._auto   = DsTag<bool>("_auto", false)
        member s._manual = DsTag<bool>("_manual", false)
        member s._drive  = DsTag<bool>("_drive", false)
        member s._stop   = DsTag<bool>("_stop", false)
        member s._clear  = DsTag<bool>("_clear", false)
        member s._emg    = DsTag<bool>("_emg", false)
        member s._test    = DsTag<bool>("test", false)
        member s._home    = DsTag<bool>("home", false)
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
        //자신이 사용된 API Plan Send
        member s.GetPSs(r:Real) = 
            s.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)
            
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
        member f.rop    = DsTag<bool> ($"{f.Name}(RO)", false)   // Ready Operation Mode
        member f.aop    = DsTag<bool> ($"{f.Name}(AO)", false)   // Auto Operation Mode
        member f.mop    = DsTag<bool> ($"{f.Name}(MO)", false)   // Manual Operation Mode 
        member f.dop    = DsTag<bool> ($"{f.Name}(DO)", false)   // Drive Operation Mode 
        member f.top    = DsTag<bool> ($"{f.Name}(TO)", false)   // Test Run Operation Mode (시운전) 
        member f.sop    = DsTag<bool> ($"{f.Name}(SO)", false)   // Stop State 
        member f.eop    = DsTag<bool> ($"{f.Name}(EO)", false)   // Emergency State 
        member f.auto   = DsTag<bool>("auto", false)
        member f.manual = DsTag<bool>("manual", false)
        member f.drive  = DsTag<bool>("drive", false)
        member f.stop   = DsTag<bool>("stop", false)
        member f.clear  = DsTag<bool>("clear", false)
        member f.emg    = DsTag<bool>("emg", false)
        member f.test   = DsTag<bool>("test", false)  
        member f.home   = DsTag<bool>("home", false)  

        //버튼 IO PLC TAG
        member f.autoIns    = getButtonInputs  (f, f.System.AutoButtons) 
        member f.autoOuts   = getButtonOutputs (f, f.System.AutoButtons) 
        member f.manualIns  = getButtonInputs (f, f.System.ManualButtons) 
        member f.manualOuts = getButtonOutputs (f, f.System.ManualButtons) 
        member f.driveIns   = getButtonInputs (f, f.System.DriveButtons) 
        member f.driveOuts  = getButtonOutputs (f, f.System.DriveButtons) 
        member f.stopIns    = getButtonInputs (f, f.System.StopButtons) 
        member f.stopOuts   = getButtonOutputs (f, f.System.StopButtons) 
        member f.clearIns   = getButtonInputs (f, f.System.ClearButtons) 
        member f.clearOuts  = getButtonOutputs (f, f.System.ClearButtons) 
        member f.emgIns     = getButtonInputs (f, f.System.EmergencyButtons) 
        member f.emgOuts    = getButtonOutputs (f, f.System.EmergencyButtons) 
        member f.testIns    = getButtonInputs (f, f.System.TestButtons) 
        member f.testOuts   = getButtonOutputs (f, f.System.TestButtons) 
        member f.homeIns    = getButtonInputs (f, f.System.HomeButtons) 
        member f.homeOuts   = getButtonOutputs (f, f.System.HomeButtons) 

        //램프 IO PLC TAG
        member f.autoModelampOuts   = getLampOutputs (f, f.System.AutoModeLamps) 
        member f.manualModelampOuts = getLampOutputs (f, f.System.ManualModeLamps) 
        member f.driveModelampOuts  = getLampOutputs (f, f.System.DriveModeLamps) 
        member f.stopModelampOuts   = getLampOutputs (f, f.System.StopModeLamps) 
        member f.readylampOuts      = getLampOutputs (f, f.System.ReadyModeLamps) 
        
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
                
        member f.DriveExpr = f.System._drive.Expr 
                           <||> f.drive.Expr 
                           <||> if f.driveIns.any() 
                                then f.driveIns.ToOr() else f.System._off.Expr

        member f.TestExpr = f.System._test.Expr 
                           <||> f.test.Expr 
                           <||> if f.testIns.any() 
                                then f.testIns.ToOr() else f.System._off.Expr

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
        member c.V = c.VertexManager :?> VertexMCoin
        member c.UsingTon = c.CallTargetJob.Funcs.Where(fun f->f.Name = TextOnDelayTimer).any()
        member c.UsingCtr = c.CallTargetJob.Funcs.Where(fun f->f.Name = TextRingCounter).any()

        member c.PresetTime =   if c.UsingTon 
                                then c.CallTargetJob.Funcs.First(fun f->f.Name = TextOnDelayTimer).GetDelayTime()
                                else failwith $"{c.Name} not use timer"

        member c.PresetCounter = if c.UsingCtr 
                                 then c.CallTargetJob.Funcs.First(fun f->f.Name = TextRingCounter).GetRingCount()
                                 else failwith $"{c.Name} not use counter"
                            
        member c.INs  = c.CallTargetJob.JobDefs.Select(fun j -> j.ActionIN)
        member c.OUTs = c.CallTargetJob.JobDefs.Select(fun j -> j.ActionOut)
        member c.PlanSends  = c.CallTargetJob.JobDefs.Select(fun j  -> j.ApiItem.PS)
        member c.PlanReceives  = c.CallTargetJob.JobDefs.Select(fun j  -> j.ApiItem.PR)

        
        member c.MutualResets = 
            c.CallTargetJob.JobDefs
                .SelectMany(fun j -> j.ApiItem.System.GetMutualResetApis(j.ApiItem))
                .SelectMany(fun a -> c.System.JobDefs.Where(fun w-> w.ApiItem = a))
    
    type Real with
        member r.V = r.VertexManager :?> VertexMReal
        member r.CoinRelays = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.CR)
        member r.ErrorTXs = r.Graph.Vertices.Select(getVM).Select(fun f->f.E1)
        member r.ErrorRXs = r.Graph.Vertices.Select(getVM).Select(fun f->f.E2)

    type Alias with
        member a.V = a.VertexManager :?> VertexMCoin                    

    type RealOtherFlow with
        member r.V = r.VertexManager :?> VertexMCoin                    

    type JobDef with
        member jd.ActionIN  = jd.InTag :?> PlcTag<bool>
        member jd.ActionOut  = jd.OutTag :?> PlcTag<bool>
        member jd.RXs  = jd.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.EP)
                                            
        member jd.MutualResets(x:DsSystem) = 
                jd.ApiItem.System.GetMutualResetApis(jd.ApiItem)
                    .SelectMany(fun a -> x.JobDefs.Where(fun w-> w.ApiItem = a))
    
    type Vertex with 
        member r.V = r.VertexManager :?> VertexManager                    
        
    type ApiItem with
        member a.PS = DsTag<bool>($"{a.Name}(PS)", false)
        member a.PR = DsTag<bool>($"{a.Name}(PR)", false)
