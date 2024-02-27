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

        member s._pause       = s.GetPv<bool>(SystemTag.sysPause)
        member s._autoState   = s.GetPv<bool>(SystemTag.autoState   )
        member s._manualState = s.GetPv<bool>(SystemTag.manualState )
        member s._driveState  = s.GetPv<bool>(SystemTag.driveState  )
        member s._errorState  = s.GetPv<bool>(SystemTag.errorState   )
        member s._emgState    = s.GetPv<bool>(SystemTag.emgState    )
        member s._testState   = s.GetPv<bool>(SystemTag.testState   )
        member s._readyState  = s.GetPv<bool>(SystemTag.readyState  )
        member s._idleState   = s.GetPv<bool>(SystemTag.idleState  )
        member s._originState = s.GetPv<bool>(SystemTag.originState  )
        member s._homingState = s.GetPv<bool>(SystemTag.homingState  )
        member s._goingState  = s.GetPv<bool>(SystemTag.goingState  )

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
