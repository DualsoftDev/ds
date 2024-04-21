namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuDsSystem =
    let emptyAddressCheck(address:string) (name:string) = 
        if address.IsNullOrEmpty() || address = TextAddrEmpty || address = TextSkip
            then
                failwithf $"{name} 해당 주소가 없습니다."

    type DsSystem with
        member private s.GetPv<'T when 'T:equality >(st:SystemTag) =
            getSM(s).GetSystemTag(st) :?> PlanVar<'T>

        member s._on          = s.GetPv<bool>(SystemTag.on)
        member s._off         = s.GetPv<bool>(SystemTag.off)
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

        member s._pause       = s.GetPv<bool>(SystemTag.pauseMonitor)
        member s._autoMonitor   = s.GetPv<bool>(SystemTag.autoMonitor   )
        member s._manualMonitor = s.GetPv<bool>(SystemTag.manualMonitor )
        member s._driveMonitor  = s.GetPv<bool>(SystemTag.driveMonitor  )
        member s._errorMonitor  = s.GetPv<bool>(SystemTag.errorMonitor   )
        member s._emgState    = s.GetPv<bool>(SystemTag.emergencyMonitor  )
        member s._testMonitor   = s.GetPv<bool>(SystemTag.testMonitor   )
        member s._readyMonitor  = s.GetPv<bool>(SystemTag.readyMonitor  )
        member s._idleMonitor   = s.GetPv<bool>(SystemTag.idleMonitor  )
        member s._originMonitor = s.GetPv<bool>(SystemTag.originMonitor  )
        member s._homingMonitor = s.GetPv<bool>(SystemTag.homingMonitor  )
        member s._goingMonitor  = s.GetPv<bool>(SystemTag.goingMonitor  )

        member s._tout        = s.GetPv<uint32>(SystemTag.timeout)
        member s._flicker20msec = s.GetPv<bool>(SystemTag.flicker20ms)
        member s._flicker100msec = s.GetPv<bool>(SystemTag.flicker100ms)
        member s._flicker200msec = s.GetPv<bool>(SystemTag.flicker200ms)
        member s._flicker1sec = s.GetPv<bool>(SystemTag.flicker1s)
        member s._flicker2sec = s.GetPv<bool>(SystemTag.flicker2s)
        
        member s._homeHW  =  
                    let homes = s.HomeHWButtons.Where(fun s-> s.InTag.IsNonNull())
                    if homes.any()
                        then homes.Select(fun s->s.ActionINFunc).ToOrElseOn()
                        else s._off.Expr    

        member s.S = s |> getSM
        member s.Storages = s.TagManager.Storages

        member s.GetTempTag(x:TaskDev) = 
            emptyAddressCheck x.InAddress x.Name
            let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempBoolTag(name, x.InAddress, x)

        member s.GetTempTimer(x:HwSystemDef) = 
            emptyAddressCheck x.InAddress x.Name
            //let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempTimerTag(x.Name)
    
        member private x.GenerationButtonIO()   = x.HWButtons.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationLampIO()     = x.HWLamps.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationCondition()  = x.HWConditions.Iter(fun f-> createHwApiBridgeTag(f, x))  
        

        member private x.GenerationCallManualMemory()  = 
            DsAddressModule.setMemoryIndex(DsAddressModule.memoryCnt + BufferAlramSize)
            
            for call in x.GetVerticesOfCoins().OfType<Call>() |> Seq.sortBy (fun c -> c.Name) do
                let cv =  call.TagManager :?> VertexMCall
                cv.SF.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory, getTarget(x))
                call.ExternalTags.Add(ManualTag, cv.SF :> IStorage) |>ignore

        member private x.GenerationCallConditionMemory()  = 
            for condi in x.HWConditions do
                condi.ErrorCondition <- createPlanVar  x.Storages  $"{condi.Name}_err" DuBOOL false condi (int FlowTag.flowStopConditionErrLamp) x
                condi.ErrorCondition.Address <- getValidAddress(TextAddrEmpty, condi.Name, false, IOType.Memory, getTarget(x))

        member private x.GenerationButtonEmergencyMemory()  = 
            for emg in x.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                emg.ErrorEmergency <- createPlanVar  x.Storages  $"{emg.Name}_err" DuBOOL false emg (int FlowTag.flowStopEmergencyErrLamp) x
                emg.ErrorEmergency.Address <- getValidAddress(TextAddrEmpty, emg.Name, false, IOType.Memory, getTarget(x))

        member private x.GenerationEmulationMemory()  = 
            x._emulation.Address <- getValidAddress(TextAddrEmpty, x._emulation.Name, false, IOType.Memory  , getTarget(x))
            RuntimeDS.EmulationAddress <- x._emulation.Address 
            
         
        member private x.GenerationCallAlarmMemory()  = 
            for call in x.GetVerticesOfJobCalls()
                            .Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)   
                            |> Seq.sortBy (fun c -> c.Name) do

                let cv =  call.TagManager :?> VertexMCall
                cv.ErrShort.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory, getTarget(x))
                cv.ErrOpen.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory, getTarget(x))
                cv.ErrTimeOver.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory, getTarget(x))
                cv.ErrTimeShortage.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory, getTarget(x))
                call.ExternalTags.Add(ManualTag, cv.SF :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorSensorOn, cv.ErrShort:> IStorage) |>ignore
                call.ExternalTags.Add(ErrorSensorOff, cv.ErrOpen  :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorTimeOver, cv.ErrTimeOver :> IStorage) |>ignore
                call.ExternalTags.Add(ErrorTimeShortage, cv.ErrTimeShortage :> IStorage) |>ignore

        member private x.GenerationRealAlarmMemory()  = 
            for real in x.GetVertices().OfType<Real>() |> Seq.sortBy (fun c -> c.Name) do
                let rm =  real.TagManager :?> VertexMReal
                rm.ErrGoingOrigin.Address <- getValidAddress(TextAddrEmpty, rm.Name, false, IOType.Memory, getTarget(x))
                real.ExternalTags.Add(ErrGoingOrigin, rm.ErrGoingOrigin :> IStorage) |>ignore


        member private x.GenerationTaskDevIO() =
            let TaskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs) |> Seq.sortBy(fun d-> d.QualifiedName) 
            let calls = x.GetVerticesOfJobCalls()
            for dev in TaskDevices do
                if calls.Where(fun f->f.TargetJob.DeviceDefs.Contains(dev)).any() //외부입력 전용 확인
                then
                    if  dev.InAddress <> TextSkip then
                        let inT = createBridgeTag(x.Storages, dev.ApiName, dev.InAddress, (int)ActionTag.ActionIn , BridgeType.Device, x , dev).Value
                        dev.InTag <- inT  ; dev.InAddress <- inT.Address
                      
                    if  dev.OutAddress <> TextSkip then
                        let outT = createBridgeTag(x.Storages, dev.ApiName, dev.OutAddress, (int)ActionTag.ActionOut , BridgeType.Device, x , dev).Value
                        dev.OutTag <- outT; dev.OutAddress <- outT.Address
                else 
                    if  dev.InAddress <> TextSkip then
                        let inT = createBridgeTag(x.Storages, dev.ApiName, dev.InAddress, (int)ActionTag.ActionIn , BridgeType.Device, x , dev).Value
                        dev.InTag <- inT  ; dev.InAddress <- inT.Address
                      

        member x.GenerationIO() =

            x.GenerationTaskDevIO()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()


        member x.GenerationMemory() =

            x.GenerationEmulationMemory()
            x.GenerationCallAlarmMemory()
            x.GenerationRealAlarmMemory()
            
            x.GenerationButtonEmergencyMemory()
            x.GenerationCallConditionMemory()
            x.GenerationCallManualMemory()
                                    
    
         
        member x.GenerationOrigins() =
            let getOriginInfos(sys:DsSystem) =
                let reals = sys.GetVertices().OfType<Real>()
                reals.Select(fun r->
                       let info = OriginHelper.GetOriginInfo r
                       r, info)
                       |> Tuple.toDictionary
            let origins = getOriginInfos x
            for (rv: VertexMReal) in x.GetVertices().OfType<Real>().Select(fun f->f.TagManager :?> VertexMReal) do
                rv.OriginInfo <- origins[rv.Vertex :?> Real]

        //자신이 사용된 API Plan Set Send
        member x.GetPSs(r:Real) = x.ApiItems.Where(fun api-> api.TXs.Contains(r)).Select(fun api -> api.PS)
        member x.GetASs(r:Real) = x.ApiItems.Where(fun api-> api.TXs.Contains(r)).Select(fun api -> api.SL1)

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
            SystemTag.GetValues(typeof<SystemTag>).Cast<SystemTag>()
                     .Where(fun typ -> writeAble.Contains(typ))
                     .Select(sm.GetSystemTag)
