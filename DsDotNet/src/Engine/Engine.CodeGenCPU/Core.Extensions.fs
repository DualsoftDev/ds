namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
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
        member private s.GetPv<'T when 'T:equality >(st:SystemTag) =
            getSM(s).GetSystemTag(st) :?> PlanVar<'T>
        member s._on       = s.GetPv<bool>(SystemTag.on)
        member s._off      = s.GetPv<bool>(SystemTag.off)
        member s._auto     = s.GetPv<bool>(SystemTag.auto)
        member s._manual   = s.GetPv<bool>(SystemTag.manual)
        member s._drive    = s.GetPv<bool>(SystemTag.drive)
        member s._stop     = s.GetPv<bool>(SystemTag.stop)
        member s._emg      = s.GetPv<bool>(SystemTag.emg)
        member s._test     = s.GetPv<bool>(SystemTag.test )
        member s._ready    = s.GetPv<bool>(SystemTag.ready)
        member s._clear    = s.GetPv<bool>(SystemTag.clear)
        member s._home     = s.GetPv<bool>(SystemTag.home)
        member s._dtimeyy  = s.GetPv<uint8>(SystemTag.datet_yy)
        member s._dtimemm  = s.GetPv<uint8>(SystemTag.datet_mm)
        member s._dtimedd  = s.GetPv<uint8>(SystemTag.datet_dd)
        member s._dtimeh   = s.GetPv<uint8>(SystemTag.datet_h )
        member s._dtimem   = s.GetPv<uint8>(SystemTag.datet_m )
        member s._dtimes   = s.GetPv<uint8>(SystemTag.datet_s )
        member s._tout     = s.GetPv<uint16>(SystemTag.timeout)
        member x.S = x |> getSM
        member x.Storages = x.TagManager.Storages

        member private x.GenerationLampIO() =
            for lamp in x.SystemLamps do
                match createBridgeTag(x.Storages, lamp.Name, lamp.OutAddress, Out ,BridgeType.Lamp, x) with
                | Some t ->  lamp.OutTag  <- t
                | None -> ()

        member private x.GenerationCondition() =
            for sc in x.SystemConditions do
                match createBridgeTag(x.Storages, sc.Name, sc.InAddress, In ,BridgeType.Condition, x) with
                | Some t ->  sc.InTag  <- t
                | None -> ()

        member private x.GenerationButtonIO() =
            for b in x.SystemButtons do
                createBridgeTag(x.Storages, b.Name, b.InAddress, In ,BridgeType.Button, x)
                |> iter (fun t -> b.InTag   <- t)
                createBridgeTag(x.Storages, b.Name, b.OutAddress, Out ,BridgeType.Button, x)
                |> iter (fun t -> b.OutTag  <- t)

        member private x.GenerationTaskDevIO() =
            let taskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs)
            for b in taskDevices do
                if b.ApiItem.RXs.length() = 0 && b.ApiItem.TXs.length() = 0
                then failwithlog $"Error {getFuncName()}"

                if b.ApiItem.RXs.any() then
                    createBridgeTag(x.Storages, b.ApiName, b.InAddress, In ,BridgeType.Device, x)
                    |> iter (fun t -> b.InTag <- t)
                if b.ApiItem.TXs.any() then
                    createBridgeTag(x.Storages, b.ApiName, b.OutAddress, Out ,BridgeType.Device, x)
                    |> iter (fun t -> b.OutTag <- t)

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

    let private getButtonExprWrtRuntimePackage(f:Flow, btns:ButtonDef seq) : Expression<bool> =
        match Runtime.Package with
        | (StandardPC | StandardPLC) ->
            let exprs = getButtonExpr(f, btns)
            if exprs.any() then exprs.ToOr() else f.System._off.Expr

        | (LightPC | LightPLC) ->
            f.System._off.Expr


    let private getSelectBtnExpr(f:Flow, btns:ButtonDef seq) : Expression<bool> seq =
        getButtonExpr(f, btns)

    let getConditionInputs(flow:Flow, condis:ConditionDef seq) : Tag<bool> seq =
        condis
            .Where(fun b -> b.SettingFlows.Contains(flow))
            .Select(fun b -> b.InTag)
            .Cast<Tag<bool>>()


//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with

        /// READY operation mode
        member f.rop    = getFM(f).GetFlowTag(FlowTag.ready_op    )
        /// AUTO operation mode
        member f.aop    = getFM(f).GetFlowTag(FlowTag.auto_op     )
        /// MANUAL operation mode
        member f.mop    = getFM(f).GetFlowTag(FlowTag.manual_op   )
        /// DRIVE operation mode
        member f.dop    = getFM(f).GetFlowTag(FlowTag.drive_op    )
        /// TEST  operation mode (시운전)
        member f.top    = getFM(f).GetFlowTag(FlowTag.test_op     )
        /// STOP state
        member f.sop    = getFM(f).GetFlowTag(FlowTag.stop_op     )
        /// EMERGENCY State
        member f.eop    = getFM(f).GetFlowTag(FlowTag.emergency_op)
        /// IDLE state
        member f.iop    = getFM(f).GetFlowTag(FlowTag.idle_op)
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
        member f.BtnDriveExpr = getButtonExprWrtRuntimePackage(f, f.System.DriveButtons    )
        member f.BtnStopExpr  = getButtonExprWrtRuntimePackage(f, f.System.StopButtons     )
        member f.BtnEmgExpr   = getButtonExprWrtRuntimePackage(f, f.System.EmergencyButtons)
        member f.BtnTestExpr  = getButtonExprWrtRuntimePackage(f, f.System.TestButtons     )
        member f.BtnReadyExpr = getButtonExprWrtRuntimePackage(f, f.System.ReadyButtons    )
        member f.BtnClearExpr = getButtonExprWrtRuntimePackage(f, f.System.ClearButtons    )
        member f.BtnHomeExpr  = getButtonExprWrtRuntimePackage(f, f.System.HomeButtons     )

        member f.ModeAutoHwExpr =
            let auto = if f.SelectAutoExpr.any()   then f.SelectAutoExpr.ToAnd()    else f._on.Expr
          //  let ableAuto = if f.SelectManualExpr.any() then !!f.SelectManualExpr.ToOr() else f._on.Expr
            auto// <&&> ableAuto  반대조건 봐야하나 ?

        member f.ModeManualHwExpr =
            let manual = if f.SelectManualExpr.any() then f.SelectManualExpr.ToAnd() else f._off.Expr
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
            writeAble |> map (getFM(f).GetFlowTag)

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
        member r._off = r.Parent.GetSystem()._off


    type TaskDev with
        member td.ActionIN  = td.InTag  :?> Tag<bool>
        member td.ActionOut = td.OutTag :?> Tag<bool>
        member td.RXs       = td.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.EP)

        member td.MutualResets(x:DsSystem) =
            td.ApiItem.System.GetMutualResetApis(td.ApiItem)
                .SelectMany(fun a -> x.DeviceDefs.Where(fun w-> w.ApiItem = a))

    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)  = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)    = getFM(x).GetFlowTag(typ )
        [<Extension>] static member GetTagApi  (x:ApiItem  ,typ:ApiItemTag) = getAM(x).GetApiTag(typ)
