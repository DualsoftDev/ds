namespace rec Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open Engine.Common.FS
open System

[<AutoOpen>]
module ConvertCoreExt =

    type InOut = | In | Out | Memory
    let private createIOPLCTag(name, address, inOut:InOut): ITagWithAddress   =
        let plcName = match inOut with
                        | In  -> $"{name}_I"
                        | Out -> $"{name}_O"
                        | Memory -> failwithlog "error: Memory not supported "

        (PlcTag(plcName, address, false) :> ITagWithAddress)

    let getVM(v:Vertex) = v.VertexManager :?> VertexManager
    let getVMReal(v:Vertex) = v.VertexManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.VertexManager :?> VertexMCoin

    let hasTime (xs:Func seq) = xs.Any(fun f->f.Name = TextOnDelayTimer)
    let hasCount(xs:Func seq) = xs.Any(fun f->f.Name = TextRingCounter)
    let hasMove (xs:Func seq) = xs.Any(fun f->f.Name = TextMove)
    let hasNot  (xs:Func seq) = xs.Any(fun f->f.Name = TextNot )

    type ApiItem with
        member a.PS = DsTag<bool>($"{a.Name}(PS)", false)
        member a.PR = DsTag<bool>($"{a.Name}(PR)", false)

    type DsSystem with
        member s.SystemManager  = SystemManager(s)
        //test ahn
        member s._on     = dsBit s "_on"  true
        member s._off    = dsBit s "_off" false
        member s._auto   = dsBit s "_auto" false
        member s._manual = dsBit s "_manual" false
        member s._drive  = dsBit s "_drive" false
        member s._stop   = dsBit s "_stop" false
        member s._emg    = dsBit s "_emg" false
        member s._test   = dsBit s "_test" false
        member s._ready  = dsBit s "_ready" false
        member s._clear  = dsBit s "_clear" false
        member s._home   = dsBit s "home" false
        member s._dtimeyy  = dsInt s "_yy" 0
        member s._dtimemm  = dsInt s "_mm" 0
        member s._dtimedd  = dsInt s "_dd" 0
        member s._dtimeh   = dsInt s "_h" 0
        member s._dtimem   = dsInt s "_m" 0
        member s._dtimes   = dsInt s "_s" 0
        member s._dtimems  = dsInt s "_ms" 0
        member s._tout   = dsUint16 s "_tout" 10000us
        //test ahn

        member s.GenerationLampIO() =
            for b in s.SystemLamps do
                b.OutTag  <- createIOPLCTag(b.Name, b.OutAddress, In)

        member s.GenerationButtonIO() =
            for b in s.SystemButtons do
                b.InTag  <- createIOPLCTag(b.Name, b.OutAddress, In)
                b.OutTag <- createIOPLCTag(b.Name, b.OutAddress, Out)

        member s.GenerationJobIO() =
            let jobDefs = s.Jobs |> Seq.collect(fun j -> j.JobDefs)
            for jdef in jobDefs do
                jdef.InTag  <- createIOPLCTag(jdef.ApiName, jdef.InAddress, In)
                jdef.OutTag <- createIOPLCTag(jdef.ApiName, jdef.OutAddress, Out)

        //[auto, manual] system HMI 두개다 선택이 안됨
        member s.ModeNoExpr = !!s._auto.Expr <&&> !!s._manual.Expr
        //자신이 사용된 API Plan Set Send
        member s.GetPSs(r:Real) =
            s.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)
        //자신이 사용된 API Plan Rst Send
        member s.GetPRs(r:Real) =
            s.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PR)

    let private getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool> seq =
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b ->
                    let inTag = (b.InTag :?> PlcTag<bool>).Expr
                    if hasNot(b.Funcs)then !!inTag else inTag    )

    let private getBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool>  =
        let exprs = getButtonExpr(f, btns)
        if exprs.any()
        then exprs.ToOr()
        else f.System._off.Expr

    let private getSelectBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool> seq =
        getButtonExpr(f, btns)

    let private getButtonOutputs(flow:Flow, btns:ButtonDef seq) : PlcTag<bool> seq =
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b -> b.OutTag)
                .Cast<PlcTag<bool>>()

    let private getLampOutputs(flow:Flow, btns:LampDef seq) : PlcTag<bool> seq =
            btns.Where(fun b -> b.SettingFlow = flow)
                .Select(fun b -> b.OutTag)
                .Cast<PlcTag<bool>>()

//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with
        //test ahn
        member f.rop    = dsBit f.System $"{f.Name}(ROM)" false   // Ready Operation Mode
        member f.aop    = dsBit f.System $"{f.Name}(AOM)" false   // Auto Operation Mode
        member f.mop    = dsBit f.System $"{f.Name}(MOM)" false   // Manual Operation Mode
        member f.dop    = dsBit f.System $"{f.Name}(DOM)" false   // Drive Operation Mode
        member f.top    = dsBit f.System $"{f.Name}(TOM)" false   //  Test  Operation Mode (시운전)
        member f.sop    = dsBit f.System $"{f.Name}(SOM)" false   // Stop State
        member f.eop    = dsBit f.System $"{f.Name}(EOM)" false   // Emergency State
        member f.auto   = dsBit f.System $"{f.Name}_auto" false
        member f.manual = dsBit f.System $"{f.Name}_manual" false
        member f.drive  = dsBit f.System $"{f.Name}_drive" false
        member f.stop   = dsBit f.System $"{f.Name}_stop" false
        member f.ready  = dsBit f.System $"{f.Name}_ready" false
        member f.clear  = dsBit f.System $"{f.Name}_clear" false
        member f.emg    = dsBit f.System $"{f.Name}_emg"  false
        member f.test   = dsBit f.System $"{f.Name}_test" false
        member f.home   = dsBit f.System $"{f.Name}_home" false
        //test ahn


        //select 버튼은 없을경우 항상 _on
         member f.SelectAutoExpr   = getSelectBtnExpr(f, f.System.AutoButtons  )
         member f.SelectManualExpr = getSelectBtnExpr(f, f.System.ManualButtons)

        //push 버튼은 없을경우 항상 _off
         member f.BtnDriveExpr = getBtnExpr(f, f.System.DriveButtons    )
         member f.BtnStopExpr  = getBtnExpr(f, f.System.StopButtons     )
         member f.BtnEmgExpr   = getBtnExpr(f, f.System.EmergencyButtons)
         member f.BtnTestExpr  = getBtnExpr(f, f.System.TestButtons     )
         member f.BtnReadyExpr = getBtnExpr(f, f.System.ReadyButtons    )
         member f.BtnClearExpr = getBtnExpr(f, f.System.ClearButtons    )
         member f.BtnHomeExpr  = getBtnExpr(f, f.System.HomeButtons     )

         member f.ModeAutoHwExpr =
            let auto     = if f.SelectAutoExpr.any()   then   f.SelectAutoExpr.ToAnd()  else f.System._on.Expr
            let ableAuto = if f.SelectManualExpr.any() then !!f.SelectManualExpr.ToOr() else f.System._on.Expr
            auto <&&> ableAuto

         member f.ModeManualHwExpr =
            let manual     = if f.SelectManualExpr.any() then   f.SelectManualExpr.ToAnd() else f.System._off.Expr
            let ableManual = if f.SelectAutoExpr.any()   then !!f.SelectAutoExpr.ToOr()    else f.System._on.Expr
            manual <&&> ableManual

         member f.ModeAutoSwHMIExpr   =    f.auto.Expr <&&> !!f.manual.Expr
         member f.ModeManualSwHMIExpr =  !!f.auto.Expr <&&>   f.manual.Expr


    type Call with
        member c.V = c.VertexManager :?> VertexMCoin
        member c.UsingTon  = c.CallTargetJob.Funcs |> hasTime
        member c.UsingCtr  = c.CallTargetJob.Funcs |> hasCount
        member c.UsingNot  = c.CallTargetJob.Funcs |> hasNot
        member c.UsingMove = c.CallTargetJob.Funcs |> hasMove

        member c.PresetTime =   if c.UsingTon
                                then c.CallTargetJob.Funcs.First(fun f->f.Name = TextOnDelayTimer).GetDelayTime()
                                else failwith $"{c.Name} not use timer"

        member c.PresetCounter = if c.UsingCtr
                                 then c.CallTargetJob.Funcs.First(fun f->f.Name = TextRingCounter).GetRingCount()
                                 else failwith $"{c.Name} not use counter"

        member c.INs           = c.CallTargetJob.JobDefs.Select(fun j -> j.ActionIN)
        member c.OUTs          = c.CallTargetJob.JobDefs.Select(fun j -> j.ActionOut)
        member c.PlanSends     = c.CallTargetJob.JobDefs.Select(fun j -> j.ApiItem.PS)
        member c.PlanReceives  = c.CallTargetJob.JobDefs.Select(fun j -> j.ApiItem.PR)


        member c.MutualResets =
            c.CallTargetJob.JobDefs
                .SelectMany(fun j -> j.ApiItem.System.GetMutualResetApis(j.ApiItem))
                .SelectMany(fun a -> c.System.JobDefs.Where(fun w-> w.ApiItem = a))

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

        member jd.MutualResets(x:DsSystem) =
                jd.ApiItem.System.GetMutualResetApis(jd.ApiItem)
                    .SelectMany(fun a -> x.JobDefs.Where(fun w-> w.ApiItem = a))


