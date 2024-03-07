namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuDsSystem =


    type DsSystem with
        member private s.GetPv<'T when 'T:equality >(st:SystemTag) =
            getSM(s).GetSystemTag(st) :?> PlanVar<'T>
        member s._on          = s.GetPv<bool>(SystemTag.on)
        member s._off         = s.GetPv<bool>(SystemTag.off)
        member s._sim         = s.GetPv<bool>(SystemTag.sim)
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

        member s._tout        = s.GetPv<uint16>(SystemTag.timeout)
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
            let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempBoolTag(name, x.InAddress, x)

        member s.GetTempTimer(x:HwSystemDef) = 
            let name = x.InAddress.Replace("%", "_").Replace(".", "_")
            getSM(s).GetTempTimerTag(name)
    
        member private x.GenerationButtonIO()   = x.HWButtons.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationLampIO()     = x.HWLamps.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationCondition()  = x.HWConditions.Iter(fun f-> createHwApiBridgeTag(f, x))   
        member private x.GenerationCallManualMemory()  = 
            for call in x.GetVerticesOfCoins().OfType<Call>() |> Seq.sortBy (fun c -> c.Name) do
                let cv =  call.TagManager :?> VertexMCoin
                cv.SF.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory)
                call.ManualTag  <- cv.SF :> IStorage
                                    

        member private x.GenerationCallAlarmMemory()  = 
            for call in x.GetVerticesOfCoins().OfType<Call>() |> Seq.sortBy (fun c -> c.Name) do
                let cv =  call.TagManager :?> VertexMCoin
                cv.ErrOpen.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory)
                cv.ErrShort.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory)
                cv.ErrTimeOver.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory)
                cv.ErrTrendOut.Address <- getValidAddress(TextAddrEmpty, call.Name, false, IOType.Memory)
                call.ErrorSensorOn   <- cv.ErrOpen :> IStorage
                call.ErrorSensorOff  <- cv.ErrShort :> IStorage
                call.ErrorTimeOver   <- cv.ErrTimeOver :> IStorage
                call.ErrorTrendOut   <- cv.ErrTrendOut :> IStorage


        member private x.GenerationTaskDevIO() =
            let TaskDevices = x.Jobs |> Seq.collect(fun j -> j.DeviceDefs) |> Seq.sortBy(fun d-> d.QualifiedName) 
            for b in TaskDevices do
                if b.ApiItem.RXs.length() = 0 && b.ApiItem.TXs.length() = 0
                then failwith $"Error {getFuncName()}"

                if  b.InAddress <> TextSkip then
                    let inT = createBridgeTag(x.Storages, b.ApiName, b.InAddress, (int)ActionTag.ActionIn , BridgeType.Device, x , b).Value
                    b.InTag <- inT
                    b.InAddress <- inT.Address
                      
                if  b.OutAddress <> TextSkip then
                    let outT = createBridgeTag(x.Storages, b.ApiName, b.OutAddress, (int)ActionTag.ActionOut , BridgeType.Device, x , b).Value
                    b.OutTag <- outT
                    b.OutAddress <- outT.Address


        member x.GenerationIO() =

            x.GenerationTaskDevIO()
            x.GenerationButtonIO()
            x.GenerationLampIO()
            x.GenerationCondition()
            x.GenerationCallManualMemory()
            x.GenerationCallAlarmMemory()
            
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
