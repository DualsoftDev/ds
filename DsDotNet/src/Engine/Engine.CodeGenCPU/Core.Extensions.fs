namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCodeCoreExt =
    
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


    let errText (x:Call)  = getVMCoin(x).ErrorText

    let createHwApiBridgeTag (x:HwSystemDef, sys:DsSystem)  = 
        let hwApi =   sys.HWSystemItems.First(fun f->f.Name = x.Name)
        let bridgeType = 
            match x with
            | :? ButtonDef -> BridgeType.Button
            | :? LampDef -> BridgeType.Lamp
            | :? ConditionDef -> BridgeType.Condition
            | _ -> 
                failwithf "bridgeType err"

        createBridgeTag(sys.Storages, x.Name, x.InAddress, (int)HwSysTag.HwSysIn, bridgeType , sys, hwApi)
        |> iter (fun t -> x.InTag   <- t)
        createBridgeTag(sys.Storages, x.Name, x.OutAddress,(int)HwSysTag.HwSysOut ,bridgeType ,sys, hwApi)
        |> iter (fun t -> x.OutTag  <- t)


    type ApiItem with
        member a.PS     = getAM(a).PS
        member a.PE     = getAM(a).PE
    
        member a.RXTags       = a.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.ET)
        member a.TXTags       = a.TXs |> Seq.map getVMReal |> Seq.map(fun f->f.ST)


    type HwSystemDef with
        member s.ActionINFunc = 
                let inTag = (s.InTag :?> Tag<bool>).Expr
                if hasNot(s.Funcs)then !!inTag else inTag  

    type DsSystem with
        member private s.GetPv<'T when 'T:equality >(st:SystemTag) =
            getSM(s).GetSystemTag(st) :?> PlanVar<'T>
        member s._on          = s.GetPv<bool>(SystemTag.on)
        member s._off         = s.GetPv<bool>(SystemTag.off)
        member s._sim         = s.GetPv<bool>(SystemTag.sim)
        member s._auto_btn    = s.GetPv<bool>(SystemTag.auto_btn)
        member s._manual_btn  = s.GetPv<bool>(SystemTag.manual_btn)
        member s._drive_btn   = s.GetPv<bool>(SystemTag.drive_btn)
        member s._stop_btn    = s.GetPv<bool>(SystemTag.stop_btn)
        member s._emg_btn     = s.GetPv<bool>(SystemTag.emg_btn)
        member s._test_btn    = s.GetPv<bool>(SystemTag.test_btn )
        member s._ready_btn   = s.GetPv<bool>(SystemTag.ready_btn)
        member s._clear_btn   = s.GetPv<bool>(SystemTag.clear_btn)
        member s._home_btn    = s.GetPv<bool>(SystemTag.home_btn)

        member s._auto_lamp    = s.GetPv<bool>(SystemTag.auto_lamp)
        member s._manual_lamp  = s.GetPv<bool>(SystemTag.manual_lamp)
        member s._drive_lamp   = s.GetPv<bool>(SystemTag.drive_lamp)
        member s._stop_lamp    = s.GetPv<bool>(SystemTag.stop_lamp)
        member s._emg_lamp     = s.GetPv<bool>(SystemTag.emg_lamp)
        member s._test_lamp    = s.GetPv<bool>(SystemTag.test_lamp )
        member s._ready_lamp   = s.GetPv<bool>(SystemTag.ready_lamp)
        member s._clear_lamp   = s.GetPv<bool>(SystemTag.clear_lamp)
        member s._home_lamp    = s.GetPv<bool>(SystemTag.home_lamp)


        member s._dtimeyy     = s.GetPv<uint8>(SystemTag.datet_yy)
        member s._dtimemm     = s.GetPv<uint8>(SystemTag.datet_mm)
        member s._dtimedd     = s.GetPv<uint8>(SystemTag.datet_dd)
        member s._dtimeh      = s.GetPv<uint8>(SystemTag.datet_h )
        member s._dtimem      = s.GetPv<uint8>(SystemTag.datet_m )
        member s._dtimes      = s.GetPv<uint8>(SystemTag.datet_s )


        member s._stopPause   = s.GetPv<bool>(SystemTag.sysStopPause)
        member s._stopErr     = s.GetPv<bool>(SystemTag.sysStopError)
        member s._autoState   = s.GetPv<bool>(SystemTag.autoState   )
        member s._manualState = s.GetPv<bool>(SystemTag.manualState )
        member s._driveState  = s.GetPv<bool>(SystemTag.driveState  )
        member s._stopState   = s.GetPv<bool>(SystemTag.stopState   )
        member s._emgState    = s.GetPv<bool>(SystemTag.emgState    )
        member s._testState   = s.GetPv<bool>(SystemTag.testState   )
        member s._readyState  = s.GetPv<bool>(SystemTag.readyState  )
        member s._idleState   = s.GetPv<bool>(SystemTag.idleState  )


        member s._tout        = s.GetPv<uint16>(SystemTag.timeout)
        member s._flicker200msec = s.GetPv<bool>(SystemTag.flicker200ms)
        member s._flicker1sec = s.GetPv<bool>(SystemTag.flicker1s)
        member s._flicker2sec = s.GetPv<bool>(SystemTag.flicker2s)
        member s.GetTempTag(x:TaskDev) = 
            let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempBoolTag(name, x.InAddress, x)

        member x.S = x |> getSM
        member x.Storages = x.TagManager.Storages

        member s._homeHW  =  
                    let homes = s.HomeHWButtons.Where(fun s-> s.InTag.IsNonNull())
                    if homes.any()
                        then homes.Select(fun s->s.ActionINFunc).ToOrElseOn()
                        else s._off.Expr    

        member private x.GenerationButtonIO()   = x.HWButtons.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationLampIO()     = x.HWLamps.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationCondition()  = x.HWConditions.Iter(fun f-> createHwApiBridgeTag(f, x))   

        member private x.GenerationTaskDevIO() =
            let TaskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs) |> Seq.sortBy(fun d-> d.QualifiedName) 
            for b in TaskDevices do
                if b.ApiItem.RXs.length() = 0 && b.ApiItem.TXs.length() = 0
                then failwith $"Error {getFuncName()}"


                //if b.ApiItem.RXs.any() then
                let inT = createBridgeTag(x.Storages, b.ApiName, b.InAddress, (int)ActionTag.ActionIn , BridgeType.Device, x , b).Value
                b.InTag <- inT
                b.InAddress <- inT.Address
                      
                //if b.ApiItem.TXs.any() then
                let outT = createBridgeTag(x.Storages, b.ApiName, b.OutAddress, (int)ActionTag.ActionOut , BridgeType.Device, x , b).Value
                b.OutTag <- outT
                b.OutAddress <- outT.Address


        member x.GenerationIO() =

            x.GenerationTaskDevIO()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()

        member x.GenerationOrigins() =
            let getOriginInfos(sys:DsSystem) =
                let reals = sys.GetVertices().OfType<Real>()
                reals.Select(fun r->
                       let info = OriginHelper.GetOriginInfo r
                       r, info)
                       |> Tuple.toDictionary
            let origins = getOriginInfos x
            for (rv: VertexMReal) in x.GetVertices().OfType<Real>().Select(fun f->f.V) do
                rv.OriginInfo <- origins[rv.Vertex :?> Real]

        //자신이 사용된 API Plan Set Send
        member x.GetPSs(r:Real) =
            x.ApiItems.Where(fun api-> api.TXs.Contains(r))
                      .Select(fun api -> api.PS)
    

        member x.GetReadAbleTags() =
            SystemTag.GetValues(typeof<SystemTag>)
                     .Cast<SystemTag>()
                     .Select(getSM(x).GetSystemTag)


        member x.GetWriteAbleTags() =
            let writeAble =
                [
                    SystemTag.auto_btn
                    SystemTag.manual_btn
                    SystemTag.drive_btn
                    SystemTag.stop_btn
                    SystemTag.emg_btn
                    SystemTag.test_btn
                    SystemTag.ready_btn
                    SystemTag.clear_btn
                    SystemTag.home_btn
                ]
            let sm = getSM(x)
            SystemTag.GetValues(typeof<SystemTag>).Cast<SystemTag>()
                     .Where(fun typ -> writeAble.Contains(typ))
                     .Select(sm.GetSystemTag)
    
    let private getButtonExpr(flow:Flow, btns:ButtonDef seq) : Expression<bool>  =
        let tags = btns.Where(fun b -> b.SettingFlows.Contains(flow))
                       .Select(fun b ->b.ActionINFunc)
        if tags.any() then tags.ToOrElseOn() else flow.System._off.Expr

    let private getConditionExpr(flow:Flow, condis:ConditionDef seq) : Expression<bool>  =
        let tags = condis
                    .Where(fun c -> c.SettingFlows.Contains(flow))
                    .Select(fun c ->c.ActionINFunc)
        if tags.any() then tags.ToOrElseOn() else flow.System._off.Expr


//운영 모드 는 Flow 별로 제공된 모드 On/Off 상태 나타낸다.
    type Flow with

        /// READY operation mode
        member f.rop    = getFM(f).GetFlowTag(FlowTag.ready_mode    )
        /// AUTO operation mode
        member f.aop    = getFM(f).GetFlowTag(FlowTag.auto_mode     )
        /// MANUAL operation mode
        member f.mop    = getFM(f).GetFlowTag(FlowTag.manual_mode   )
        /// DRIVE operation mode
        member f.dop    = getFM(f).GetFlowTag(FlowTag.drive_mode    )
        /// TEST  operation mode (시운전)
        member f.top    = getFM(f).GetFlowTag(FlowTag.test_mode     )
        /// STOP state
        member f.sop    = getFM(f).GetFlowTag(FlowTag.stop_mode     )
        /// EMERGENCY State
        member f.eop    = getFM(f).GetFlowTag(FlowTag.emg_mode)
        /// IDLE state
        member f.iop    = getFM(f).GetFlowTag(FlowTag.idle_mode)
        member f.auto_btn   = getFM(f).GetFlowTag(FlowTag.auto_btn    )
        member f.manual_btn = getFM(f).GetFlowTag(FlowTag.manual_btn  )
        member f.drive_btn  = getFM(f).GetFlowTag(FlowTag.drive_btn   )
        member f.stop_btn   = getFM(f).GetFlowTag(FlowTag.stop_btn    )
        member f.ready_btn  = getFM(f).GetFlowTag(FlowTag.ready_btn   )
        member f.clear_btn  = getFM(f).GetFlowTag(FlowTag.clear_btn   )
        member f.emg_btn    = getFM(f).GetFlowTag(FlowTag.emg_btn     )
        member f.test_btn   = getFM(f).GetFlowTag(FlowTag.test_btn    )
        member f.home_btn   = getFM(f).GetFlowTag(FlowTag.home_btn    )

        member f.auto_lamp   = getFM(f).GetFlowTag(FlowTag.auto_lamp    )
        member f.manual_lamp = getFM(f).GetFlowTag(FlowTag.manual_lamp  )
        member f.drive_lamp  = getFM(f).GetFlowTag(FlowTag.drive_lamp   )
        member f.stop_lamp   = getFM(f).GetFlowTag(FlowTag.stop_lamp    )
        member f.ready_lamp  = getFM(f).GetFlowTag(FlowTag.ready_lamp   )
        member f.clear_lamp  = getFM(f).GetFlowTag(FlowTag.clear_lamp   )
        member f.emg_lamp    = getFM(f).GetFlowTag(FlowTag.emg_lamp     )
        member f.test_lamp   = getFM(f).GetFlowTag(FlowTag.test_lamp    )
        member f.home_lamp   = getFM(f).GetFlowTag(FlowTag.home_lamp    )

        member f.stopError  = getFM(f).GetFlowTag(FlowTag.flowStopError    )
        member f.stopPause    = getFM(f).GetFlowTag(FlowTag.flowStopPause    )
        member f.F = f |> getFM
        member f._on     = f.System._on
        member f._off    = f.System._off
        member f._sim    = f.System._sim
        //select 버튼은 없을경우 항상 _on
        member f.HwAutoSelects =  f.System.AutoHWButtons.Where(fun b->b.SettingFlows.Contains(f))
        member f.HwManuSelects =  f.System.ManualHWButtons.Where(fun b->b.SettingFlows.Contains(f))
        member f.HwAutoExpr = getButtonExpr(f, f.System.AutoHWButtons  )
        member f.HwManuExpr = getButtonExpr(f, f.System.ManualHWButtons)

        //push 버튼은 없을경우 항상 _off
        member f.HWBtnDriveExpr = getButtonExpr(f, f.System.DriveHWButtons    ) (*<||> f._sim.Expr*)
        member f.HWBtnStopExpr  = getButtonExpr(f, f.System.StopHWButtons     )
        member f.HWBtnEmgExpr   = getButtonExpr(f, f.System.EmergencyHWButtons)
        member f.HWBtnTestExpr  = getButtonExpr(f, f.System.TestHWButtons     )
        member f.HWBtnReadyExpr = getButtonExpr(f, f.System.ReadyHWButtons    ) (*<||> f._sim.Expr*)
        member f.HWBtnClearExpr = getButtonExpr(f, f.System.ClearHWButtons    )
        member f.HWBtnHomeExpr  = getButtonExpr(f, f.System.HomeHWButtons     )


        member f.HWConditionsExpr = getConditionExpr(f, f.System.HWConditions    ) 

        member f.ModeAutoHwHMIExpr   =  if f.HwManuSelects.any() 
                                        then f.HwAutoExpr <&&> !!f.HwManuExpr <||> f._sim.Expr
                                        else f.HwAutoExpr <&&> !!f._off.Expr  <||> f._sim.Expr

        member f.ModeManualHwHMIExpr =  if f.HwManuSelects.any() 
                                        then f.HwManuExpr <&&> !!f.HwAutoExpr 
                                        else f.HwManuExpr <&&> !!f._off.Expr  
        member f.ModeAutoSwHMIExpr   =    f.auto_btn.Expr <&&> !!f.manual_btn.Expr
        member f.ModeManualSwHMIExpr =  !!f.auto_btn.Expr <&&>   f.manual_btn.Expr

        member f.AutoExpr   =  
                if f.HwAutoSelects.any() //hw 있으면 hw만 보는것으로
                then f.ModeAutoHwHMIExpr
                else f.ModeAutoSwHMIExpr

        member f.ManuExpr   =  
                if f.HwManuSelects.any() //hw 있으면 hw만 보는것으로
                then f.ModeManualHwHMIExpr
                else f.ModeManualSwHMIExpr

                    

        member f.GetReadAbleTags() =
            FlowTag.GetValues(typeof<FlowTag>).Cast<FlowTag>()
                  .Select(getFM(f).GetFlowTag)

        member f.GetWriteAbleTags() =
            let writeAble =
                [   FlowTag.auto_btn
                    FlowTag.manual_btn
                    FlowTag.drive_btn
                    FlowTag.stop_btn
                    FlowTag.ready_btn
                    FlowTag.clear_btn
                    FlowTag.emg_btn
                    FlowTag.test_btn
                    FlowTag.home_btn
                ]
            writeAble |> map (getFM(f).GetFlowTag)

    type TaskDev with
        member td.ExistIn  = td.ApiItem.RXs.any()
        member td.ActionINFunc  = 
                            if(td.InTag.IsNull()) then failwithf $"{td.QualifiedName} Input 주소 할당이 없습니다."

                            if hasNot td.Funcs 
                            then !!(td.InTag  :?> Tag<bool>).Expr 
                            else (td.InTag  :?> Tag<bool>).Expr

        member td.ActionOut = 
                            if(td.OutTag.IsNull()) then failwithf $"{td.QualifiedName} Output 주소 할당이 없습니다."
                            td.OutTag :?> Tag<bool>

        member td.RXTags       = td.ApiItem.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.ET)
        member td.TXTags       = td.ApiItem.TXs |> Seq.map getVMReal |> Seq.map(fun f->f.ST)

        member td.MutualReset(x:DsSystem) =
            let exMutualApis = td.ApiItem.System.GetMutualResetApis(td.ApiItem)
            let myMutualDevs = 
                    exMutualApis.SelectMany(fun api -> 
                                x.DeviceDefs.Where(fun dev-> dev.ApiItem = api))
            myMutualDevs

        member td.MutualResetExpr(x:DsSystem) =
            let myMutualDevs =  td.MutualReset(x).Where(fun d->d.ExistIn).Select(fun d->d.ActionINFunc)
            if myMutualDevs.any() then myMutualDevs.ToAndElseOff() else x._on.Expr

    type Call with
       
                                    
        member c.UsingTon  = c.TargetJob.Funcs |> hasTime
        member c.UsingCtr  = c.TargetJob.Funcs |> hasCount
        member c.UsingNot  = c.TargetJob.Funcs |> hasNot
        member c.UsingMove = c.TargetJob.Funcs |> hasMove
        member c._on     = c.System._on
        member c._off     = c.System._off
    
      
        member c.PresetTime =   if c.UsingTon
                                then c.TargetJob.Funcs.First(fun f->f.Name = TextOnDelayTimer).GetDelayTime()
                                else failwith $"{c.Name} not use timer" 

        member c.PresetCounter = if c.UsingCtr
                                 then c.TargetJob.Funcs.First(fun f->f.Name = TextRingCounter).GetRingCount()
                                 else failwith $"{c.Name} not use counter"
        
        member c.PSs           = c.TargetJob.DeviceDefs.Where(fun j -> j.ApiItem.TXs.any()).Select(fun f->f.ApiItem.PS)
        member c.PEs           = c.TargetJob.DeviceDefs.Where(fun j -> j.ApiItem.RXs.any()).Select(fun f->f.ApiItem.PE)
        member c.TXs           = c.TargetJob.DeviceDefs|>Seq.collect(fun j -> j.ApiItem.TXs)
        member c.RXs           = c.TargetJob.DeviceDefs|>Seq.collect(fun j -> j.ApiItem.RXs)
        member c.ActionINFuncs = c.TargetJob.DeviceDefs.Where(fun f->f.ExistIn).Select(fun d->d.ActionINFunc).ToAndElseOff()       
        member c.Errors       = 
                                [
                                    getVMCoin(c).ErrTimeOver
                                    getVMCoin(c).ErrTrendOut 
                                    getVMCoin(c).ErrShort 
                                    getVMCoin(c).ErrOpen 
                                ]
        

        //개별 부정의 AND  <안전하게 전부 확인>
        member c.INsFuns  = let ins = c.TargetJob.DeviceDefs
                                        .Where(fun j -> j.ApiItem.RXs.any())
                                        .Select(fun j -> j.ActionINFunc)
                            if ins.any() then ins.ToAndElseOn() else c._on.Expr
                         
        member c.MutualResets =
            c.TargetJob.DeviceDefs
                .SelectMany(fun j -> j.ApiItem.System.GetMutualResetApis(j.ApiItem))
                .SelectMany(fun a -> c.System.DeviceDefs.Where(fun w-> w.ApiItem = a))

        member c.StartPointExpr =
            match c.Parent.GetCore() with
            | :? Real as r ->
                let tasks = r.V.OriginInfo.Tasks
                let flow = c.Parent.GetFlow()
                let homeManuAct = flow.mop.Expr <&&> (c.Parent.GetSystem()._homeHW <||> r.V.H.Expr)
                let homeAutoAct = flow.dop.Expr <&&> r.V.RO.Expr

                if tasks.Where(fun (_,ty) -> ty = InitialType.On) //NeedCheck 처리 필요 test ahn
                        .Select(fun (t,_)->t).ContainsAllOf(c.TargetJob.DeviceDefs)
                    then (homeManuAct <||> homeAutoAct) <&&> !!c.ActionINFuncs      
                    else c._off.Expr
            | _ -> 
                c._off.Expr

    type Real with
        member r.V = r.TagManager :?> VertexMReal
        member r.CoinRelays = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ET)
        member r.ErrTimeOvers   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTimeOver) 
        member r.ErrTrendOuts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrTrendOut) 
        member r.ErrOpens   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrOpen) 
        member r.ErrShorts   = r.Graph.Vertices.Select(getVMCoin).Select(fun f->f.ErrShort) 
        member r.Errors     = r.ErrTimeOvers @ r.ErrTrendOuts @ r.ErrOpens @ r.ErrShorts 

    type Indirect with
        member a.V = a.TagManager :?> VertexMCoin

    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off



    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)  = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)    = getFM(x).GetFlowTag(typ )
