namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuDsSystem =
    let emptyAddressCheck(address:string) (name:string) = 
        if address.IsNullOrEmpty() || address = TextAddrEmpty || address = TextSkip then
            failwithf $"{name} 해당 주소가 없습니다."

    let getMemory (tag:IStorage) (target:PlatformTarget) = 
        getValidAddress(TextAddrEmpty, DuBOOL, tag.Name, false, IOType.Memory, target)

    type DsSystem with
        member private s.GetPv<'T when 'T:equality >(st:SystemTag) =
            getSM(s).GetSystemTag(st) :?> PlanVar<'T>

        member s._on          = s.GetPv<bool>(SystemTag._ON)
        member s._off         = s.GetPv<bool>(SystemTag._OFF)
        member s._sim         = s.GetPv<bool>(SystemTag.sim)
        member s._emulation   = s.GetPv<bool>(SystemTag.emulation)
        member s._auto_btn    = s.GetPv<bool>(SystemTag.auto_btn)
        member s._manual_btn  = s.GetPv<bool>(SystemTag.manual_btn)
        member s._drive_btn   = s.GetPv<bool>(SystemTag.drive_btn)
        member s._pause_btn   = s.GetPv<bool>(SystemTag.pause_btn)
        member s._emg_btn     = s.GetPv<bool>(SystemTag.emg_btn)
        member s._test_btn    = s.GetPv<bool>(SystemTag.test_btn )
        member s._ready_btn   = s.GetPv<bool>(SystemTag.ready_btn)
        member s._clear_btn   = s.GetPv<bool>(SystemTag.clear_btn)
        member s._home_btn    = s.GetPv<bool>(SystemTag.home_btn)

        member s._auto_lamp    = s.GetPv<bool>(SystemTag.auto_lamp)
        member s._manual_lamp  = s.GetPv<bool>(SystemTag.manual_lamp)
        member s._drive_lamp   = s.GetPv<bool>(SystemTag.drive_lamp)
        member s._pause_lamp   = s.GetPv<bool>(SystemTag.pause_lamp)
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

        member s._pause            = s.GetPv<bool>(SystemTag.pauseMonitor)
        member s._autoMonitor      = s.GetPv<bool>(SystemTag.autoMonitor   )
        member s._manualMonitor    = s.GetPv<bool>(SystemTag.manualMonitor )
        member s._driveMonitor     = s.GetPv<bool>(SystemTag.driveMonitor  )
        member s._errorMonitor     = s.GetPv<bool>(SystemTag.errorMonitor   )
        member s._emergencyMonitor = s.GetPv<bool>(SystemTag.emergencyMonitor  )
        member s._testMonitor      = s.GetPv<bool>(SystemTag.testMonitor   )
        member s._readyMonitor     = s.GetPv<bool>(SystemTag.readyMonitor  )
        member s._idleMonitor      = s.GetPv<bool>(SystemTag.idleMonitor  )
        member s._originMonitor    = s.GetPv<bool>(SystemTag.originMonitor  )
        member s._goingMonitor     = s.GetPv<bool>(SystemTag.goingMonitor  )

        


        member s._tout        = s.GetPv<uint32>(SystemTag.timeout)
        member s._flicker20msec = s.GetPv<bool>(SystemTag._T20MS)
        member s._flicker100msec = s.GetPv<bool>(SystemTag._T100MS)
        member s._flicker200msec = s.GetPv<bool>(SystemTag._T200MS)
        member s._flicker1sec = s.GetPv<bool>(SystemTag._T1S)
        member s._flicker2sec = s.GetPv<bool>(SystemTag._T2S)
        
        member s._homeHW  =  
            let homes = s.HomeHWButtons.Where(fun s-> s.InTag.IsNonNull())
            if homes.any() then
                homes.Select(fun s->s.ActionINFunc).ToOrElseOn()
            else
                s._off.Expr    

        member s.S = s |> getSM
        member s.Storages = s.TagManager.Storages

       
        member s.GetTempTimer(x:HwSystemDef) = 
            getSM(s).GetTempTimerTag(x.Name)
        member s.GetTempBoolTag(name:string) : PlanVar<bool>=
            getSM(s).GetTempBoolTag(name)

        member private x.GenerationButtonIO()   = x.HWButtons.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationLampIO()     = x.HWLamps.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationCondition()  = x.HWConditions.Iter(fun f-> createHwApiBridgeTag(f, x))  
        
        member private x.GenerationCallConditionMemory()  = 
            for condi in x.HWConditions do
                condi.ErrorCondition <- createPlanVar  x.Storages  $"{condi.Name}_err" DuBOOL true condi (int HwSysTag.HwStopConditionErrLamp) x
                condi.ErrorCondition.Address <- getValidAddress(TextAddrEmpty,DuBOOL, condi.Name, false, IOType.Memory, getTarget(x))

        member private x.GenerationButtonEmergencyMemory()  = 
            for emg in x.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                emg.ErrorEmergency <- createPlanVar  x.Storages  $"{emg.Name}_err" DuBOOL true emg (int HwSysTag.HwStopEmergencyErrLamp) x
                emg.ErrorEmergency.Address <- getValidAddress(TextAddrEmpty, DuBOOL, emg.Name, false, IOType.Memory, getTarget(x))

        member private x.GenerationEmulationMemory()  = 
            x._emulation.Address <- getValidAddress(TextAddrEmpty,DuBOOL, x._emulation.Name, false, IOType.Memory  , getTarget(x))
            RuntimeDS.EmulationAddress <- x._emulation.Address 
            
         
        member private x.GenerationCallAlarmMemory()  = 
            let calls = x.GetAlarmCalls().Distinct() 

            for call in calls do

                let cv =  call.TagManager :?> CallVertexTagManager
                let target = getTarget(x)
                cv.ErrShort.Address             <- getMemory  cv.ErrShort target 
                cv.ErrOpen.Address              <- getMemory  cv.ErrOpen  target 
                cv.ErrOnTimeOver.Address        <- getMemory  cv.ErrOnTimeOver  target 
                cv.ErrOnTimeShortage.Address    <- getMemory  cv.ErrOnTimeShortage target 
                cv.ErrOffTimeOver.Address       <- getMemory  cv.ErrOffTimeOver target 
                cv.ErrOffTimeShortage.Address   <- getMemory  cv.ErrOffTimeShortage target 
                call.ExternalTags.Add(ErrorSensorOn,        cv.ErrShort           :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorSensorOff,       cv.ErrOpen            :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorOnTimeOver,      cv.ErrOnTimeOver      :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorOnTimeShortage,  cv.ErrOnTimeShortage  :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorOffTimeOver,     cv.ErrOffTimeOver     :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorOffTimeShortage, cv.ErrOffTimeShortage :> IStorage) |>ignore
         
         
        member private x.GenerationRealAlarmMemory()  = 
            for real in x.GetRealVertices().Distinct()  |> Seq.sortBy (fun c -> c.Name) do
                let rm =  real.TagManager :?> RealVertexTagManager
                rm.ErrGoingOrigin.Address <- getMemory rm.ErrGoingOrigin (getTarget(x))
                real.ExternalTags.Add(ErrGoingOrigin, rm.ErrGoingOrigin :> IStorage) |>ignore

        member  x.GenerationRealActionMemory()  = 
            for real in x.GetRealVertices().Distinct() |> Seq.sortBy (fun c -> c.Name) do
                let rm =  real.TagManager :?> RealVertexTagManager
                  
                rm.ScriptStart.Address  <- getMemory  rm.ScriptStart (getTarget(x))
                rm.MotionStart.Address  <- getMemory  rm.MotionStart (getTarget(x))

                rm.ScriptEnd.Address    <- getMemory  rm.ScriptEnd  (getTarget(x))
                rm.MotionEnd.Address    <- getMemory  rm.MotionEnd  (getTarget(x))

                real.ExternalTags.Add(ScriptStart,  rm.ScriptStart :> IStorage) |>ignore
                real.ExternalTags.Add(MotionStart,  rm.MotionStart :> IStorage) |>ignore

                real.ExternalTags.Add(ScriptEnd,    rm.ScriptEnd   :> IStorage) |>ignore
                real.ExternalTags.Add(MotionEnd,    rm.MotionEnd   :> IStorage) |>ignore

    
        member private x.GenerationFlowHMIMemory()  = 
            for flow in x.GetFlowsOrderByName() do
                let fm =  flow.TagManager :?> FlowManager
                let target = getTarget(x)
                let tag = fm.GetFlowTag(FlowTag.auto_btn)   in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.auto_mode)  in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.manual_btn) in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.manual_mode)in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.drive_btn)  in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.drive_state)in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.pause_btn)  in tag.Address  <- getMemory tag target
                let tag = fm.GetFlowTag(FlowTag.pause_state)in tag.Address  <- getMemory tag target

        member private x.GenerationRealHMIMemory()  = 
            for real in x.GetVerticesOfRealOrderByName().Distinct() do
                let rm =  real.TagManager :?> RealVertexTagManager
                let target = getTarget(x)
                rm.ON.Address     <- getMemory rm.ON target
                rm.RF.Address     <- getMemory rm.RF target
                rm.SF.Address     <- getMemory rm.SF target
                rm.OB.Address     <- getMemory rm.OB target
                rm.ErrTRX.Address <- getMemory rm.ErrTRX target
                rm.R.Address      <- getMemory rm.R target
                rm.G.Address      <- getMemory rm.G target
                rm.F.Address      <- getMemory rm.F target
                rm.H.Address      <- getMemory rm.H target

        member private x.GenerationTaskDevIOM() =

            let jobDevices =x.GetTaskDevsSkipEmptyAddress().Distinct() 
            
            for dev, job in jobDevices do
                let apiStgName = dev.GetApiStgName(job)
                if  dev.InAddress <> TextSkip then
                    let inT = createBridgeTag(x.Storages, apiStgName, dev.InAddress, (int)TaskDevTag.actionIn , BridgeType.Device, x , dev, dev.GetInParam(job).Type).Value
                    dev.InTag <- inT  ; dev.InAddress <- (inT.Address)

                  //외부입력 전용 확인하여 출력 생성하지 않는다.
                if not(dev.IsRootOnlyDevice) then
                    if dev.OutAddress <> TextSkip then
                        let outT = createBridgeTag(x.Storages, apiStgName, dev.OutAddress, (int)TaskDevTag.actionOut , BridgeType.Device, x , dev, dev.GetOutParam(job).Type).Value
                        dev.OutTag <- outT; dev.OutAddress <- (outT.Address)

        member x.GenerationIO() =
            x.GenerationTaskDevIOM()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()
            
        member private x.GenerationCallManualMemory()  = 
            let devCalls = x.GetDevicesForHMI() 
            for (dev, call) in devCalls do
                let cv =  call.TagManager :?> CallVertexTagManager
                if call.TargetJob.JobTaskDevInfo.TaskDevCount = 1
                    ||( dev.OutAddress <> TextSkip  &&  cv.SF.Address = TextAddrEmpty)   
                then
                    cv.SF.Address    <- getMemory  cv.SF (getTarget(x))
                    dev.MaunualAddress  <- cv.SF.Address
                else 
                    dev.MaunualAddress  <- TextSkip  //다중 작업은 수동 작업을 사용하지 않는다.

        member x.GenerationMemory() =
            //Step1)Emulation base + 1 bit 
            x.GenerationEmulationMemory()

            let startAlarm = DsAddressModule.getCurrentMemoryIndex() 
            //Step2)Alarm base + (2 ~ N) bit 

            x.GenerationCallAlarmMemory()
            x.GenerationRealAlarmMemory()
            x.GenerationButtonEmergencyMemory()
            x.GenerationCallConditionMemory()

            if (DsAddressModule.getCurrentMemoryIndex()-startAlarm < BufferAlramSize) //9999개 HMI 리미트    
            then  DsAddressModule.setMemoryIndex(startAlarm+BufferAlramSize) //9999개 HMI 리미트

            //Step3)Flow Real HMI base + (N+1 ~ M)bit 
            x.GenerationCallManualMemory()
            x.GenerationFlowHMIMemory()
            x.GenerationRealHMIMemory()
    
         
        member x.GenerationOrigins() =
            let getOriginInfos(sys:DsSystem) =
                let reals = sys.GetRealVertices()
                reals.Select(fun r->
                    let info = OriginHelper.GetOriginInfo r
                    r, info
                ) |> Tuple.toDictionary
            let origins = getOriginInfos x

            let rvms = x.GetRealVertices().Select(fun f -> f.TagManager :?> RealVertexTagManager)
            for (rv: RealVertexTagManager) in rvms do
                rv.OriginInfo <- origins[rv.Vertex :?> Real]

        //자신이 사용된 API Plan Set Send
        member x.GetApiSets(r:Real) = x.ApiItems.Where(fun api-> api.TX = r).Select(fun api -> api.ApiItemSet)
        member x.GetApiSensorLinks(r:Real) = x.ApiItems.Where(fun api-> api.TX = r).Select(fun api -> api.SL1)

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
                    SystemTag.pause_btn
                    SystemTag.emg_btn
                    SystemTag.test_btn
                    SystemTag.ready_btn
                    SystemTag.clear_btn
                    SystemTag.home_btn
                ]
            let sm = getSM(x)
            SystemTag.GetValues(typeof<SystemTag>)
                .Cast<SystemTag>()
                .Where(fun typ -> writeAble.Contains(typ))
                .Select(sm.GetSystemTag)
