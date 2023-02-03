namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCoreExt =



    let hasTime (xs:Func seq) = xs.Any(fun f->f.Name = TextOnDelayTimer)
    let hasCount(xs:Func seq) = xs.Any(fun f->f.Name = TextRingCounter)
    let hasMove (xs:Func seq) = xs.Any(fun f->f.Name = TextMove)
    let hasNot  (xs:Func seq) = xs.Any(fun f->f.Name = TextNot )

    let getVM(v:Vertex)     = v.TagManager :?> VertexManager
    let getVMReal(v:Vertex) = v.TagManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.TagManager :?> VertexMCoin

    let getSM (x:DsSystem) = x.TagManager :?> SystemManager
    let getFM (x:Flow)     = x.TagManager :?> FlowManager
    let getAM (x:ApiItem)  = x.TagManager :?> ApiItemManager

    let getOriginInfos(sys:DsSystem) =
        let reals = sys.GetVertices().OfType<Real>()
        reals.Select(fun r->
               let t, rs = OriginHelper.GetOriginsWithDeviceDefs r.Graph
               r, {Real = r; Tasks = t; ResetChains = rs})
               |> Tuple.toDictionary

    type ApiItem with
        member a.PS     = getAM(a).GetApiTag(ApiItemTag.planSet)
        member a.PR     = getAM(a).GetApiTag(ApiItemTag.planRst)
        member a.PE     = getAM(a).GetApiTag(ApiItemTag.planEnd)

    type DsSystem with
        member s._on     = getSM(s).GetSystemTag(SystemTag.on)         :?> PlanVar<bool>
        member s._off    = getSM(s).GetSystemTag(SystemTag.off)        :?> PlanVar<bool>
        member s._auto   = getSM(s).GetSystemTag(SystemTag.auto)       :?> PlanVar<bool>
        member s._manual = getSM(s).GetSystemTag(SystemTag.manual)     :?> PlanVar<bool>
        member s._drive  = getSM(s).GetSystemTag(SystemTag.drive)      :?> PlanVar<bool>
        member s._stop   = getSM(s).GetSystemTag(SystemTag.stop)       :?> PlanVar<bool>
        member s._emg    = getSM(s).GetSystemTag(SystemTag.emg)        :?> PlanVar<bool>
        member s._test   = getSM(s).GetSystemTag(SystemTag.test )      :?> PlanVar<bool>
        member s._ready  = getSM(s).GetSystemTag(SystemTag.ready)      :?> PlanVar<bool>
        member s._clear  = getSM(s).GetSystemTag(SystemTag.clear)      :?> PlanVar<bool>
        member s._home   = getSM(s).GetSystemTag(SystemTag.home)       :?> PlanVar<bool>
        member s._dtimeyy  = getSM(s).GetSystemTag(SystemTag.datet_yy) :?> PlanVar<uint8>
        member s._dtimemm  = getSM(s).GetSystemTag(SystemTag.datet_mm) :?> PlanVar<uint8>
        member s._dtimedd  = getSM(s).GetSystemTag(SystemTag.datet_dd) :?> PlanVar<uint8>
        member s._dtimeh   = getSM(s).GetSystemTag(SystemTag.datet_h ) :?> PlanVar<uint8>
        member s._dtimem   = getSM(s).GetSystemTag(SystemTag.datet_m ) :?> PlanVar<uint8>
        member s._dtimes   = getSM(s).GetSystemTag(SystemTag.datet_s ) :?> PlanVar<uint8>
        member s._tout     = getSM(s).GetSystemTag(SystemTag.timeout)  :?> PlanVar<uint16>
        member x.S = x |> getSM
        member x.Storages = x.TagManager.Storages

        member private x.GenerationLampIO() =
            for b in x.SystemLamps do
                match createBridgeTag(x.Storages, b.Name, b.OutAddress, Out ,BridgeType.Lamp, x) with
                |Some t ->  b.OutTag  <- t
                |None -> ()

        member private x.GenerationCondition() =
            for b in x.SystemConditions do
                match createBridgeTag(x.Storages, b.Name, b.InAddress, In ,BridgeType.Condition, x) with
                |Some t ->  b.InTag  <- t
                |None -> ()

        member private x.GenerationButtonIO() =
            for b in x.SystemButtons do
                match createBridgeTag(x.Storages, b.Name, b.InAddress, In ,BridgeType.Button, x) with
                |Some t ->  b.InTag   <- t  |None -> ()
                match createBridgeTag(x.Storages, b.Name, b.OutAddress, Out ,BridgeType.Button, x) with
                |Some t ->  b.OutTag  <- t  |None -> ()

        member private x.GenerationTaskDevIO() =
            let taskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs)
            for b in taskDevices do
                if b.ApiItem.RXs.any()
                then
                    match createBridgeTag(x.Storages, b.ApiName, b.InAddress, In ,BridgeType.Device, x) with
                    |Some t ->  b.InTag   <- t  |None -> ()
                if b.ApiItem.TXs.any()
                then
                    match createBridgeTag(x.Storages, b.ApiName, b.OutAddress, Out ,BridgeType.Device, x) with
                    |Some t ->  b.OutTag  <- t  |None -> ()

        member x.GenerationIO() =
            x.GenerationTaskDevIO()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()

        member x.GenerationOrigins() =
            let origins = getOriginInfos x
            for (rv: VertexMReal) in x.GetVertices().OfType<Real>().Select(fun f->f.V) do
                rv.OriginInfo <- origins[rv.Vertex :?> Real]

        //자신이 사용된 API Plan Set Send
        member x.GetPSs(r:Real) =
            x.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)
        //자신이 사용된 API Plan Rst Send
        member x.GetPRs(r:Real) =
            x.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PR)

        member x.GetReadAbleTags() =
            SystemTag.GetValues(typeof<SystemTag>)
                     .Cast<SystemTag>()
                     .Select(getSM(x).GetSystemTag)


        member x.GetWriteAbleTags() =
            let writeAble =
                [
                    SystemTag.auto
                    SystemTag.manual
                    SystemTag.drive
                    SystemTag.stop
                    SystemTag.emg
                    SystemTag.test
                    SystemTag.ready
                    SystemTag.clear
                    SystemTag.home
                ]
            let sm = getSM(x)
            SystemTag.GetValues(typeof<SystemTag>).Cast<SystemTag>()
                     .Where(fun typ -> writeAble.Contains(typ))
                     .Select(sm.GetSystemTag)

    let private getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool> seq =
        btns.Where(fun b -> b.SettingFlows.Contains(flow))
            .Where(fun b -> b.InAddress <> "")
            .Select(fun b ->
                let inTag = (b.InTag :?> Tag<bool>).Expr
                if hasNot(b.Funcs)then !!inTag else inTag    )

    let private getBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool>  =

        match Runtime.Package with
        | StandardPC | StandardPLC -> let exprs = getButtonExpr(f, btns)
                                      if exprs.any() then exprs.ToOr() else f.System._off.Expr

        | LightPC    | LightPLC    -> f.System._off.Expr


    let private getSelectBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool> seq =
        getButtonExpr(f, btns)

    let getConditionInputs(flow:Flow, condis:ConditionDef seq) : Tag<bool> seq =
            condis.Where(fun b -> b.SettingFlows.Contains(flow))
                 .Select(fun b -> b.InTag)
                 .Cast<Tag<bool>>()


//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with

        member f.rop    = getFM(f).GetFlowTag(FlowTag.ready_op    ) // ready Operation Mode
        member f.aop    = getFM(f).GetFlowTag(FlowTag.auto_op     ) // auto operation Mode
        member f.mop    = getFM(f).GetFlowTag(FlowTag.manual_op   ) // manual Operation Mode
        member f.dop    = getFM(f).GetFlowTag(FlowTag.drive_op    ) // drive Operation Mode
        member f.top    = getFM(f).GetFlowTag(FlowTag.test_op     ) //  test  Operation Mode (시운전)
        member f.sop    = getFM(f).GetFlowTag(FlowTag.stop_op     ) // stop state
        member f.eop    = getFM(f).GetFlowTag(FlowTag.emergency_op) // emergency State
        member f.iop    = getFM(f).GetFlowTag(FlowTag.idle_op)      // emergency state
        member f.auto   = getFM(f).GetFlowTag(FlowTag.auto_bit    )
        member f.manual = getFM(f).GetFlowTag(FlowTag.manual_bit  )
        member f.drive  = getFM(f).GetFlowTag(FlowTag.drive_bit   )
        member f.stop   = getFM(f).GetFlowTag(FlowTag.stop_bit    )
        member f.ready  = getFM(f).GetFlowTag(FlowTag.ready_bit   )
        member f.clear  = getFM(f).GetFlowTag(FlowTag.clear_bit   )
        member f.emg    = getFM(f).GetFlowTag(FlowTag.emg_bit     )
        member f.test   = getFM(f).GetFlowTag(FlowTag.test_bit    )
        member f.home   = getFM(f).GetFlowTag(FlowTag.home_bit    )
        member f.scr    = getFM(f).GetFlowTag(FlowTag.readycondi_bit    )
        member f.scd    = getFM(f).GetFlowTag(FlowTag.drivecondi_bit    )
        member f.F = f |> getFM
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
          //  let ableAuto = if f.SelectManualExpr.any() then !!f.SelectManualExpr.ToOr() else f._on.Expr
            auto// <&&> ableAuto  반대조건 봐야하나 ?

        member f.ModeManualHwExpr =
            let manual     = if f.SelectManualExpr.any() then f.SelectManualExpr.ToAnd() else f._off.Expr
          //  let ableManual = if f.SelectAutoExpr.any()   then !!f.SelectAutoExpr.ToOr()  else f._on.Expr
            manual// <&&> ableManual 반대조건 봐야하나 ?

        member f.ModeAutoSwHMIExpr   =    f.auto.Expr <&&> !!f.manual.Expr
        member f.ModeManualSwHMIExpr =  !!f.auto.Expr <&&>   f.manual.Expr

        member f.GetReadAbleTags() =
            FlowTag.GetValues(typeof<FlowTag>).Cast<FlowTag>()
                  .Select(getFM(f).GetFlowTag)

        member f.GetWriteAbleTags() =
            let writeAble =
                [   FlowTag.auto_bit
                    FlowTag.manual_bit
                    FlowTag.drive_bit
                    FlowTag.stop_bit
                    FlowTag.ready_bit
                    FlowTag.clear_bit
                    FlowTag.emg_bit
                    FlowTag.test_bit
                    FlowTag.home_bit
                ]
            let fm = getFM(f)
            FlowTag.GetValues(typeof<FlowTag>).Cast<FlowTag>()
                  .Where(fun typ -> writeAble.Contains(typ))
                  .Select(fm.GetFlowTag)

    type CallDev with
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

        member c.INs           = c.CallTargetJob.DeviceDefs.Where(fun j -> j.ApiItem.RXs.any()).Select(fun j -> j.ActionIN)
        member c.OUTs          = c.CallTargetJob.DeviceDefs.Where(fun j -> j.ApiItem.TXs.any()).Select(fun j -> j.ActionOut)


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


    type TaskDev with
        member jd.ActionIN  = jd.InTag  :?> Tag<bool>
        member jd.ActionOut = jd.OutTag :?> Tag<bool>
        member jd.RXs       = jd.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.EP)

        member jd.MutualResets(x:DsSystem) =
                jd.ApiItem.System.GetMutualResetApis(jd.ApiItem)
                    .SelectMany(fun a -> x.DeviceDefs.Where(fun w-> w.ApiItem = a))

    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)   = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)     = getFM(x).GetFlowTag(typ )
        [<Extension>] static member GetTagApi  (x:ApiItem  ,typ:ApiItemTag)  = getAM(x).GetApiTag(typ)
