namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCodeCoreExt =
    
    let hasTime (x:Func option) = x.IsSome && x.Value.Name = TextOnDelayTimer
    let hasCount(x:Func option) = x.IsSome && x.Value.Name = TextRingCounter
    let hasMove (x:Func option) = x.IsSome && x.Value.Name = TextMove
    let hasNot  (x:Func option) = x.IsSome && x.Value.Name = TextNot 

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
        member a.AS     = getAM(a).AS
        member a.AL     = getAM(a).AL
    
        member a.RxETs       = a.RXs |> Seq.map getVMReal |> Seq.map(fun f->f.ET)
        member a.TxSTs       = a.TXs |> Seq.map getVMReal |> Seq.map(fun f->f.ST)


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
        member s.GetTempTimer(x:HwSystemDef) = 
            let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempTimerTag(name)

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
        member x.GetPSs(r:Real) = x.ApiItems.Where(fun api-> api.TXs.Contains(r)).Select(fun api -> api.PS)
        member x.GetASs(r:Real) = x.ApiItems.Where(fun api-> api.TXs.Contains(r)).Select(fun api -> api.AS)
    

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

        member f.AutoExpr   =  
                let hmiAuto = f.auto_btn.Expr <&&> !!f.manual_btn.Expr
                let hwAuto  = f.HwAutoExpr <&&> !!f.HwManuExpr
                if f.HwAutoSelects.any() //반드시 a/m 쌍으로 존재함  checkErrHWItem 체크중
                then hwAuto <&&> hmiAuto //HW, HMI Select and 처리
                else hmiAuto

        member f.ManuExpr   =  
                let hmiManu = !!f.auto_btn.Expr <&&> f.manual_btn.Expr
                let hwManu  = !!f.HwAutoExpr <&&> f.HwManuExpr
                if f.HwManuSelects.any() 
                then hwManu <||> hmiManu //HW, HMI Select or 처리
                else hmiManu

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

        member td.ActionOut = td.OutTag :?> Tag<bool>



    type Vertex with
        member r.V = r.TagManager :?> VertexManager
        member r.VC = r.TagManager :?> VertexMCoin
        member r.VR = r.TagManager :?> VertexMReal
        member r._on  = r.Parent.GetSystem()._on
        member r._off = r.Parent.GetSystem()._off

        
    type HwSystemDef with
        member s.ActionINFunc = 
            let inTag = (s.InTag :?> Tag<bool>).Expr
            if hasNot (s.Func)
            then !!inTag else inTag  


    type Call with
        member c._on     = c.System._on
        member c._off     = c.System._off

        member c.InTags  = c.TaskDevs.Where(fun d->d.ApiItem.RXs.any())
                                                 .Select(fun d->d.InTag :?> Tag<bool>)

        member c.UsingTon  = c.TargetJob.Func |> hasTime
        member c.UsingCtr  = c.TargetJob.Func |> hasCount
        member c.UsingNot  = c.TargetJob.Func |> hasNot
        member c.UsingMove = c.TargetJob.Func |> hasMove
      
        member c.EndPlan = c.TargetJob.ApiDefs.Select(fun f->f.PE).ToAndElseOff()
        member c.EndAction = 
                if c.UsingMove   then c._on.Expr  //todo : Move 처리 완료시 End
                elif c.UsingCtr  then c.VC.CTR.DN.Expr 
                elif c.UsingTon  then c.VC.TDON.DN.Expr
                elif c.UsingNot  then 
                                 if c.InTags.any() 
                                 then !!c.InTags.ToOrElseOff() 
                                 else failwithf $"'Not function' requires an InTag. {c.Name} input error"   

                else c.InTags.ToAndElseOn() 


        member c.GetEndAction(x:ApiItem) =
            let inTag = c.TaskDevs.First(fun d->d.ApiItem = x).InTag :?> Tag<bool>
            if c.UsingNot  then !!inTag.Expr
                           else inTag.Expr
      
        member c.PresetTime =   if c.UsingTon
                                then c.TargetJob.Func.Value.GetDelayTime()
                                else failwith $"{c.Name} not use timer" 

        member c.PresetCounter = if c.UsingCtr
                                 then c.TargetJob.Func.Value.GetRingCount()
                                 else failwith $"{c.Name} not use counter"
        
        member c.PSs           = c.TaskDevs.Where(fun j -> j.ApiItem.TXs.any()).Select(fun f->f.ApiItem.PS)
        member c.PEs           = c.TaskDevs.Where(fun j -> j.ApiItem.RXs.any()).Select(fun f->f.ApiItem.PE)
        member c.TXs           = c.TaskDevs|>Seq.collect(fun j -> j.ApiItem.TXs)
        member c.RXs           = c.TaskDevs|>Seq.collect(fun j -> j.ApiItem.RXs)
        member c.Errors       = 
                                [
                                    getVMCoin(c).ErrTimeOver
                                    getVMCoin(c).ErrTrendOut 
                                    getVMCoin(c).ErrShort 
                                    getVMCoin(c).ErrOpen 
                                ]
    
                         
        member c.MutualResetCalls =  c.System.S.MutualCalls[c].Cast<Call>()
          
        member c.StartPointExpr =
            let f = c.Parent.GetFlow()
            match c.Parent.GetCore() with
            | :? Real as r ->
                let initOnCalls  = r.V.OriginInfo.CallInitials
                                     .Where(fun (_,ty) -> ty = InitialType.On)
                                     .Select(fun (call,_)->call)
               
                if initOnCalls.Contains(c)
                    then 
                        let homeManuAct = f.mop.Expr <&&> (c.Parent.GetSystem()._homeHW <||> r.V.H.Expr)
                        let homeAutoAct = f.dop.Expr <&&> r.V.RO.Expr
                        let homeAct =  homeManuAct <||> homeAutoAct
                        homeAct <&&> (!!c.EndAction <&&> !!c.System._sim.Expr)    
                                     <||>
                                     (!!c.EndPlan <&&> c.System._sim.Expr)     

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





    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)  = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)    = getFM(x).GetFlowTag(typ )
