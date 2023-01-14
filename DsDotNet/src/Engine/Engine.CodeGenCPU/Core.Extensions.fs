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
        let plcName =
            match inOut with
            | In  -> $"{name}_I"
            | Out -> $"{name}_O"
            | Memory -> failwith "error: Memory not supported "

        (PlcTag(plcName, address, false) :> ITagWithAddress)

    let getVM(v:Vertex)     = v.VertexManager :?> VertexManager
    let getVMReal(v:Vertex) = v.VertexManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.VertexManager :?> VertexMCoin

    let hasTime (xs:Func seq) = xs.Any(fun f -> f.Name = TextOnDelayTimer  )
    let hasCount(xs:Func seq) = xs.Any(fun f -> f.Name = TextRingCounter)
    let hasMove (xs:Func seq) = xs.Any(fun f -> f.Name = TextMove)
    let hasNot  (xs:Func seq) = xs.Any(fun f -> f.Name = TextNot )

    type DsSystem with
        member s._on      = DsTag<bool>("_on"     , true)
        member s._off     = DsTag<bool>("_off"    , false)
        member s._auto    = DsTag<bool>("_auto"   , false)
        member s._manual  = DsTag<bool>("_manual" , false)
        member s._drive   = DsTag<bool>("_drive"  , false)
        member s._stop    = DsTag<bool>("_stop"   , false)
        member s._clear   = DsTag<bool>("_clear"  , false)
        member s._emg     = DsTag<bool>("_emg"    , false)
        member s._test    = DsTag<bool>("_test"   , false)
        member s._home    = DsTag<bool>("home"    , false)
        member s._tout    = DsTag<uint16> ("_tout", 10000us)
        member s._dtimeyy = DsTag<int> ("_yy", 0)
        member s._dtimemm = DsTag<int> ("_mm", 0)
        member s._dtimedd = DsTag<int> ("_dd", 0)
        member s._dtimeh  = DsTag<int> ("_h" , 0)
        member s._dtimem  = DsTag<int> ("_m" , 0)
        member s._dtimes  = DsTag<int> ("_s" , 0)
        member s._dtimems = DsTag<int> ("_ms", 0)

        member s.GenerationLampIO() =
            for b in s.SystemLamps do
                b.OutTag  <- getIOs(b.Name, b.OutAddress, In)

        member s.GenerationButtonIO() =
            for b in s.SystemButtons do
                b.InTag  <- getIOs(b.Name, b.OutAddress, In)
                b.OutTag <- getIOs(b.Name, b.OutAddress, Out)

        member s.GenerationJobIO() =
            for jdef in s.Jobs |> Seq.collect(fun j -> j.JobDefs) do
                jdef.InTag <- getIOs(jdef.ApiName, jdef.InAddress, In)
                jdef.OutTag <- getIOs(jdef.ApiName, jdef.OutAddress, Out)

        //[auto, manual] system HMI 두개다 선택이 안됨
        member s.ModeNoExpr = !!s._auto.Expr <&&> !!s._manual.Expr
        //자신이 사용된 API Plan Send
        member s.GetPSs(r:Real) =
            s.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)

    let private getButtonExpr(flow:Flow, btns:ButtonDef seq, sysBit:DsTag<bool>) : Expression<bool>  =
        let exprs =
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b ->
                    let inTag = (b.InTag :?> PlcTag<bool>).Expr
                    if hasNot(b.Funcs)then !!inTag else inTag
                    )
        if exprs.any()
        then exprs.ToOr()
        else sysBit.Expr

    let private getButtonOutputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq =
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b -> b.OutTag :?> PlcTag<bool>)

    let private getLampOutputs(flow:Flow, btns:LampDef seq) : PlcTag<bool> seq =
            btns.Where(fun b -> b.SettingFlow = flow)
                .Select(fun b -> b.OutTag :?> PlcTag<bool>)

    //let private getAutoManualIOs(autoIns:PlcTag<bool> seq, manualIns:PlcTag<bool> seq, sysOff:DsTag<bool>) =
    //      if autoIns.Count() > 1 || manualIns.Count() > 1
    //      then failwith "Error : One button(auto or manual) must be assigned to one flow"

    //      let auto    = if autoIns.Any()   then  autoIns.Head().Expr else sysOff.Expr
    //      let manual  = if manualIns.Any() then  manualIns.ToAnd()   else sysOff.Expr
    //      auto, manual

    //let getBtnAutoExpr  (f:Flow) = getButtonExpr(f, f.System.AutoButtons)
    //let getBtnManualExpr(f:Flow) = getButtonExpr(f, f.System.ManualButtons)
    //let getBtnDriveExpr (f:Flow) = getButtonExpr(f, f.System.DriveButtons)
    //let getBtnStopExpr  (f:Flow) = getButtonExpr(f, f.System.StopButtons)
    //let getBtnEmgExpr   (f:Flow) = getButtonExpr(f, f.System.EmergencyButtons)
    //let getBtnTestExpr  (f:Flow) = getButtonExpr(f, f.System.TestButtons)
    //let getBtnReadyExpr (f:Flow) = getButtonExpr(f, f.System.ReadyButtons)
    //let getBtnClearExpr (f:Flow) = getButtonExpr(f, f.System.ClearButtons)
    //let getBtnHomeExpr  (f:Flow) = getButtonExpr(f, f.System.HomeButtons)

//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with
        member f.rop    = DsTag<bool>($"{f.Name}(RO)", false)   // Ready Operation Mode
        member f.aop    = DsTag<bool>($"{f.Name}(AO)", false)   // Auto Operation Mode
        member f.mop    = DsTag<bool>($"{f.Name}(MO)", false)   // Manual Operation Mode
        member f.dop    = DsTag<bool>($"{f.Name}(DO)", false)   // Drive Operation Mode
        member f.top    = DsTag<bool>($"{f.Name}(TO)", false)   //  Test  Operation Mode (시운전)
        member f.sop    = DsTag<bool>($"{f.Name}(SO)", false)   // Stop State
        member f.eop    = DsTag<bool>($"{f.Name}(EO)", false)   // Emergency State
        member f.auto   = DsTag<bool>("auto",   false)
        member f.manual = DsTag<bool>("manual", false)
        member f.drive  = DsTag<bool>("drive",  false)
        member f.stop   = DsTag<bool>("stop",   false)
        member f.ready  = DsTag<bool>("ready",  false)
        member f.clear  = DsTag<bool>("clear",  false)
        member f.emg    = DsTag<bool>("emg",    false)
        member f.test   = DsTag<bool>("test",   false)
        member f.home   = DsTag<bool>("home",   false)


        //버튼 IO PLC TAG
        member f.autoOuts   = getButtonOutputs (f, f.System.AutoButtons)
        member f.manualOuts = getButtonOutputs (f, f.System.ManualButtons)
        member f.driveOuts  = getButtonOutputs (f, f.System.DriveButtons)
        member f.stopOuts   = getButtonOutputs (f, f.System.StopButtons)
        member f.clearOuts  = getButtonOutputs (f, f.System.ClearButtons)
        member f.emgOuts    = getButtonOutputs (f, f.System.EmergencyButtons)
        member f.testOuts   = getButtonOutputs (f, f.System.TestButtons)
        member f.homeOuts   = getButtonOutputs (f, f.System.HomeButtons)
        member f.readyOuts  = getButtonOutputs (f, f.System.ReadyButtons)


        //램프 IO PLC TAG
        member f.autoModelampOuts   = getLampOutputs (f, f.System.AutoModeLamps)
        member f.manualModelampOuts = getLampOutputs (f, f.System.ManualModeLamps)
        member f.driveModelampOuts  = getLampOutputs (f, f.System.DriveModeLamps)
        member f.stopModelampOuts   = getLampOutputs (f, f.System.StopModeLamps)
        member f.readylampOuts      = getLampOutputs (f, f.System.ReadyModeLamps)

        //select 버튼은 없을경우 항상 _on
         member f.BtnAutoExpr   = getButtonExpr(f, f.System.AutoButtons     , f.System._on)
         member f.BtnManualExpr = getButtonExpr(f, f.System.ManualButtons   , f.System._on)

        //push 버튼은 없을경우 항상 _off
         member f.BtnDriveExpr  = getButtonExpr(f, f.System.DriveButtons    , f.System._off)
         member f.BtnStopExpr   = getButtonExpr(f, f.System.StopButtons     , f.System._off)
         member f.BtnEmgExpr    = getButtonExpr(f, f.System.EmergencyButtons, f.System._off)
         member f.BtnTestExpr   = getButtonExpr(f, f.System.TestButtons     , f.System._off)
         member f.BtnReadyExpr  = getButtonExpr(f, f.System.ReadyButtons    , f.System._off)
         member f.BtnClearExpr  = getButtonExpr(f, f.System.ClearButtons    , f.System._off)
         member f.BtnHomeExpr   = getButtonExpr(f, f.System.HomeButtons     , f.System._off)

         member f.ModeAutoHwExpr      =    f.BtnAutoExpr <&&> !!f.BtnManualExpr
         member f.ModeManualHwExpr    =  !!f.BtnAutoExpr <&&>   f.BtnManualExpr

         member f.ModeAutoSwHMIExpr   =    f.auto.Expr <&&> !!f.manual.Expr
         member f.ModeManualSwHMIExpr =  !!f.auto.Expr <&&>   f.manual.Expr


    type Call with
        member c.V = c.VertexManager :?> VertexMCoin
        member private c.fs = c.CallTargetJob.Funcs
        member c.UsingTon  = c.fs |> hasTime
        member c.UsingCtr  = c.fs |> hasCount
        member c.UsingNot  = c.fs |> hasNot
        member c.UsingMove = c.fs |> hasMove

        member c.PresetTime =   if c.UsingTon
                                then c.CallTargetJob.Funcs.First(fun f->f.Name = TextOnDelayTimer).GetDelayTime()
                                else failwith $"{c.Name} not use timer"

        member c.PresetCounter = if c.UsingCtr
                                 then c.CallTargetJob.Funcs.First(fun f->f.Name = TextRingCounter).GetRingCount()
                                 else failwith $"{c.Name} not use counter"

        member private c.jdfs = c.CallTargetJob.JobDefs
        member c.INs          = c.jdfs.Select(fun j -> j.ActionIN)
        member c.OUTs         = c.jdfs.Select(fun j -> j.ActionOut)
        member c.PlanSends    = c.jdfs.Select(fun j -> j.ApiItem.PS)
        member c.PlanReceives = c.jdfs.Select(fun j -> j.ApiItem.PR)


        member c.MutualResets = [
            for j in c.jdfs do
            for a in j.ApiItem.System.GetMutualResetApis(j.ApiItem) do
                yield! c.System.JobDefs.Where(fun w-> w.ApiItem = a)
        ]

    type Real with
        member r.V = r.VertexManager :?> VertexMReal
        member r.CoinRelays = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.CR)
        member r.ErrorTXs   = r.Graph.Vertices.Select(getVM    ).Select(fun f->f.E1)
        member r.ErrorRXs   = r.Graph.Vertices.Select(getVM    ).Select(fun f->f.E2)

    type Alias with
        member a.V = a.VertexManager :?> VertexMCoin

    type RealOtherFlow with
        member r.V = r.VertexManager :?> VertexMCoin

    type Vertex with
        member r.V = r.VertexManager :?> VertexManager

    type JobDef with
        member jd.ActionIN  = jd.InTag  :?> PlcTag<bool>
        member jd.ActionOut = jd.OutTag :?> PlcTag<bool>
        member jd.RXs       = jd.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.EP)

        member jd.MutualResets(sys:DsSystem) = [
            for a in jd.ApiItem.System.GetMutualResetApis(jd.ApiItem) do
                yield! sys.JobDefs.Where(fun w-> w.ApiItem = a)
        ]

    type ApiItem with
        member a.PS = DsTag<bool>($"{a.Name}(PS)", false)
        member a.PR = DsTag<bool>($"{a.Name}(PR)", false)
