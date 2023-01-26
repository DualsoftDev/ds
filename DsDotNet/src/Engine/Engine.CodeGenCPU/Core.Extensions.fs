namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module ConvertCoreExt =



    let hasTime (xs:Func seq) = xs.Any(fun f->f.Name = TextOnDelayTimer)
    let hasCount(xs:Func seq) = xs.Any(fun f->f.Name = TextRingCounter)
    let hasMove (xs:Func seq) = xs.Any(fun f->f.Name = TextMove)
    let hasNot  (xs:Func seq) = xs.Any(fun f->f.Name = TextNot )

    let getVM(v:Vertex) = v.TagManager :?> VertexManager
    let getVMReal(v:Vertex) = v.TagManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.TagManager :?> VertexMCoin

    let getSM (x:DsSystem) = x.TagManager :?> SystemManager
    let getFM (x:Flow)     = x.TagManager :?> FlowManager
    let getAM (x:ApiItem)  = x.TagManager :?> ApiItemManager

    type ApiItem with
        member a.PS     = getAM(a).GetApiTag(ApiTag.PLANSET)
        member a.PR     = getAM(a).GetApiTag(ApiTag.PLANRST)
        member a.PE     = getAM(a).GetApiTag(ApiTag.PLANEND)

    type DsSystem with
        member s._on     = getSM(s).GetSysBitTag(SysBitTag.ON)
        member s._off    = getSM(s).GetSysBitTag(SysBitTag.OFF)
        member s._auto   = getSM(s).GetSysBitTag(SysBitTag.AUTO)
        member s._manual = getSM(s).GetSysBitTag(SysBitTag.MANUAL)
        member s._drive  = getSM(s).GetSysBitTag(SysBitTag.DRIVE)
        member s._stop   = getSM(s).GetSysBitTag(SysBitTag.STOP)
        member s._emg    = getSM(s).GetSysBitTag(SysBitTag.EMG)
        member s._test   = getSM(s).GetSysBitTag(SysBitTag.TEST )
        member s._ready  = getSM(s).GetSysBitTag(SysBitTag.READY)
        member s._clear  = getSM(s).GetSysBitTag(SysBitTag.CLEAR)
        member s._home   = getSM(s).GetSysBitTag(SysBitTag.HOME)
        member s._dtimeyy  = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_YY)
        member s._dtimemm  = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_MM)
        member s._dtimedd  = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_DD)
        member s._dtimeh   = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_H )
        member s._dtimem   = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_M )
        member s._dtimes   = getSM(s).GetSysDateTag(SysDataTimeTag.DATET_S )
        member s._tout     = getSM(s).GetSysErrTag(SysErrorTag.TIMEOUT)
        member x.S = x |> getSM
        member x.Storages = x.TagManager.Storages

        member private x.GenerationLampIO() =
            for b in x.SystemLamps do
                b.OutTag  <- createBridgeTag(x.Storages, b.Name, b.OutAddress, Out , x)
        member private x.GenerationCondition() =
            for b in x.SystemConditions do
                b.InTag  <- createBridgeTag(x.Storages, b.Name, b.InAddress, In, x)

        member private x.GenerationButtonIO() =
            for b in x.SystemButtons do
                     b.InTag  <- createBridgeTag(x.Storages, b.Name, b.OutAddress, In, x)
                     b.OutTag <- createBridgeTag(x.Storages, b.Name, b.OutAddress, Out, x)

        member private x.GenerationTaskDevIO() =
            let taskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs)
            for dev in taskDevices do
                dev.InTag <- createBridgeTag(x.Storages, dev.ApiName, dev.InAddress, In, x)
                dev.OutTag <- createBridgeTag(x.Storages, dev.ApiName, dev.OutAddress, Out, x)

        member x.GenerationIO() =
            x.GenerationTaskDevIO()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()

        //자신이 사용된 API Plan Set Send
        member x.GetPSs(r:Real) =
            x.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)
        //자신이 사용된 API Plan Rst Send
        member x.GetPRs(r:Real) =
            x.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PR)

    let private getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool> seq =
            btns.Where(fun b -> b.SettingFlows.Contains(flow))
                .Select(fun b ->
                    let inTag = (b.InTag :?> Tag<bool>).Expr
                    if hasNot(b.Funcs)then !!inTag else inTag    )

    let private getBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool>  =
        let exprs = getButtonExpr(f, btns)
        if exprs.any()
        then exprs.ToOr()
        else f.System._off.Expr

    let private getSelectBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool> seq =
        getButtonExpr(f, btns)

    let getConditionInputs(flow:Flow, condis:ConditionDef seq) : Tag<bool> seq =
            condis.Where(fun b -> b.SettingFlows.Contains(flow))
                 .Select(fun b -> b.InTag)
                 .Cast<Tag<bool>>()


//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with

        member f.rop    = getFM(f).GetFlowTag(FlowTag.READY_OP    ) // Ready Operation Mode
        member f.aop    = getFM(f).GetFlowTag(FlowTag.AUTO_OP     ) // Auto Operation Mode
        member f.mop    = getFM(f).GetFlowTag(FlowTag.MANUAL_OP   ) // Manual Operation Mode
        member f.dop    = getFM(f).GetFlowTag(FlowTag.DRIVE_OP    ) // Drive Operation Mode
        member f.top    = getFM(f).GetFlowTag(FlowTag.TEST_OP     ) //  Test  Operation Mode (시운전)
        member f.sop    = getFM(f).GetFlowTag(FlowTag.STOP_OP     ) // Stop State
        member f.eop    = getFM(f).GetFlowTag(FlowTag.EMERGENCY_OP) // Emergency State
        member f.auto   = getFM(f).GetFlowTag(FlowTag.AUTO_BIT    )
        member f.manual = getFM(f).GetFlowTag(FlowTag.MANUAL_BIT  )
        member f.drive  = getFM(f).GetFlowTag(FlowTag.DRIVE_BIT   )
        member f.stop   = getFM(f).GetFlowTag(FlowTag.STOP_BIT    )
        member f.ready  = getFM(f).GetFlowTag(FlowTag.READY_BIT   )
        member f.clear  = getFM(f).GetFlowTag(FlowTag.CLEAR_BIT   )
        member f.emg    = getFM(f).GetFlowTag(FlowTag.EMG_BIT     )
        member f.test   = getFM(f).GetFlowTag(FlowTag.TEST_BIT    )
        member f.home   = getFM(f).GetFlowTag(FlowTag.HOME_BIT    )
        member f.scr    = getFM(f).GetFlowTag(FlowTag.READYCONDI_BIT    )
        member f.scd    = getFM(f).GetFlowTag(FlowTag.DRIVECONDI_BIT    )
        member x.F = x |> getFM
        member f._on     = f.System._on
        member f._off    = f.System._off

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
            let auto     = if f.SelectAutoExpr.any()   then f.SelectAutoExpr.ToAnd()    else f._on.Expr
            let ableAuto = if f.SelectManualExpr.any() then !!f.SelectManualExpr.ToOr() else f._on.Expr
            auto <&&> ableAuto

        member f.ModeManualHwExpr =
            let manual     = if f.SelectManualExpr.any() then f.SelectManualExpr.ToAnd() else f._off.Expr
            let ableManual = if f.SelectAutoExpr.any()   then !!f.SelectAutoExpr.ToOr()  else f._on.Expr
            manual <&&> ableManual

        member f.ModeAutoSwHMIExpr   =    f.auto.Expr <&&> !!f.manual.Expr
        member f.ModeManualSwHMIExpr =  !!f.auto.Expr <&&>   f.manual.Expr


    type Call with
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

        member c.INs           = c.CallTargetJob.DeviceDefs.Select(fun j -> j.ActionIN)
        member c.OUTs          = c.CallTargetJob.DeviceDefs.Select(fun j -> j.ActionOut)


        member c.MutualResets =
            c.CallTargetJob.DeviceDefs
                .SelectMany(fun j -> j.ApiItem.System.GetMutualResetApis(j.ApiItem))
                .SelectMany(fun a -> c.System.DeviceDefs.Where(fun w-> w.ApiItem = a))

    type Real with
        member r.V = r.TagManager :?> VertexMReal
        member r.CoinRelays = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.CR)
        member r.ErrorTXs   = r.Graph.Vertices.Select(getVM    ).Select(fun f->f.E1)
        member r.ErrorRXs   = r.Graph.Vertices.Select(getVM    ).Select(fun f->f.E2)

    type Indirect with
        member a.V = a.TagManager :?> VertexMCoin

    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r._on  = r.Parent.GetSystem()._on
        member r._off  = r.Parent.GetSystem()._off

    type TaskDevice with
        member jd.ActionIN  = jd.InTag  :?> Tag<bool>
        member jd.ActionOut = jd.OutTag :?> Tag<bool>
        member jd.RXs       = jd.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.EP)

        member jd.MutualResets(x:DsSystem) =
                jd.ApiItem.System.GetMutualResetApis(jd.ApiItem)
                    .SelectMany(fun a -> x.DeviceDefs.Where(fun w-> w.ApiItem = a))


