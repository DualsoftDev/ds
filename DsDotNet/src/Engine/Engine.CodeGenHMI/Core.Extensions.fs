namespace Engine.CodeGenHMI

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System.Collections.Generic
open System.Reflection
open Engine.CodeGenCPU


[<AutoOpen>]
module ConvertHMI =
      
    let kindDescriptions = TagKindExt.GetAllTagKinds() |> Tuple.toDictionary

    let getWebTag (tm:ITagManager) (kind:int) =
        let tag = 
            match tm with
            | :? SystemManager  as m -> m.GetSystemTag (DU.tryGetEnumValue<SystemTag>(kind).Value)
            | :? FlowManager    as m -> m.GetFlowTag   (DU.tryGetEnumValue<FlowTag>(kind).Value)
            | :? VertexManager  as m -> m.GetVertexTag (DU.tryGetEnumValue<VertexTag>(kind).Value)
            | :? ApiItemManager as m -> m.GetApiTag    (DU.tryGetEnumValue<ApiItemTag>(kind).Value)
            | _ -> failwithf "getPushWebTag error"

        TagWebExt.GetWebTag(tag, kindDescriptions) 
        
    let getPush          (tm:ITagManager) (kind:int) = getWebTag tm kind
    let getLamp          (tm:ITagManager) (kind:int) = getWebTag tm kind
    let getSelectLamp    (tm:ITagManager) (kindPushA:int)  (kindLampA:int) (kindPushB:int)  (kindLampB:int) = 
                         ((getWebTag tm  kindPushA), (getWebTag tm kindLampA)), ((getWebTag tm  kindPushB), (getWebTag tm kindLampB))
    let getPushLampMode  (tm:ITagManager) (kindPush :int) (kindLamp :int) (kindMode:int) = ((getWebTag tm  kindPush), (getWebTag tm kindLamp)), (getWebTag tm kindMode)
    let getSelectLampMode(tm:ITagManager) (kindPushA:int)  (kindLampA:int) (kindModeA:int) (kindPushB:int)  (kindLampB:int) (kindModeB:int) = 
                         (getPushLampMode tm  kindPushA  kindLampA kindModeA),(getPushLampMode tm  kindPushB  kindLampB kindModeB)

    let getPushLamp      (tm:ITagManager) (kindPush :int) (kindLamp :int) = (getWebTag tm  kindPush), (getWebTag tm kindLamp)

    let getPushMultiLamp (tm:ITagManager) (pushKind:int) (lampTags:ITag seq) =
        getWebTag tm pushKind, lampTags.Select(fun f-> TagWebExt.GetWebTag(f, kindDescriptions))
        

    type Call with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> VertexMCall
            {
                Name = x.Name
                TimeOnShortageErrorLamp  = getLamp  tm (VertexTag.txErrOnTimeShortage |>int)
                TimeOnOverErrorLamp      = getLamp  tm (VertexTag.txErrOnTimeOver |>int)
                TimeOffShortageErrorLamp = getLamp  tm (VertexTag.txErrOffTimeShortage |>int)
                TimeOffOverErrorLamp     = getLamp  tm (VertexTag.txErrOffTimeOver |>int)

                ShortErrorLamp           = getLamp  tm (VertexTag.rxErrShort |>int)
                OpenErrorLamp            = getLamp  tm (VertexTag.rxErrOpen |>int)
                ErrorTotalLamp           = getLamp  tm (VertexTag.errorTRx |>int)
            }

    type LoadedSystem with
        member private x.GetHMI()   =
            let containerCalls = x.ContainerSystem.GetVerticesOfJobCalls()
            {
                Name        = x.Name
                Calls    = containerCalls
                                .Where(fun c->c.TargetJob.DeviceDefs.any(fun d->d.ApiItem.ApiSystem = x.ReferenceSystem))
                                .Select(fun c->c.GetHMI()).ToArray()
            }

    type Job with
        member private x.GetHMI()   =
            let actionInTags  = x.DeviceDefs.Where(fun d->d.InTag.IsNonNull()).Select(fun d->d.InTag) //todo  ActionINFunc func $not 적용 검토     
            let apiTagManager = x.DeviceDefs.First().ApiItem.TagManager :?> ApiItemManager
            {
                Name = x.QualifiedName
                JobPushMutiLamp = getPushMultiLamp  apiTagManager (ApiItemTag.planSet |>int) (actionInTags)
            }

    type Real with
        member private x.GetHMI()   =

            let getLoadedName (api:ApiItem) = x.Parent.GetSystem().GetLoadedSys(api.ApiSystem).Value
            let calls = x.Graph.Vertices.OfType<Call>()
            let tm = x.TagManager :?> VertexManager
            {
                Name = x.Name
                StartPush    = getPush tm (VertexTag.startTag |>int)   
                ResetPush    = getPush tm (VertexTag.resetTag |>int)  
                ONPush       = getPush tm (VertexTag.forceOn |>int)  
                OFFPush      = getPush tm (VertexTag.forceOff |>int)  
                ReadyLamp    = getLamp tm (VertexTag.ready |>int)  
                GoingLamp    = getLamp tm (VertexTag.going |>int)  
                FinishLamp   = getLamp tm (VertexTag.finish |>int)  
                HomingLamp   = getLamp tm (VertexTag.homing |>int)  
                OriginLamp   = getLamp tm (VertexTag.origin |>int)  
                PauseLamp    = getLamp tm (VertexTag.pause |>int)  
                Error        = getLamp tm (VertexTag.errorTRx |>int)  
                
                Devices      = calls
                                    .Where(fun c->c.IsJob)
                                    .SelectMany(fun c->
                                      c.TargetJob.DeviceDefs.Select(fun d-> getLoadedName d.ApiItem).Distinct()
                                                            .Select(fun d->d.GetHMI())
                                       ).ToArray()
                               
                Jobs         = calls 
                                    .Where(fun c->c.IsJob)
                                    .Select(fun c->c.TargetJob.GetHMI()).ToArray()
            }

    type Flow with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> FlowManager
            {
                Name = x.Name
                AutoManualSelectLampMode = getSelectLampMode tm (FlowTag.auto_btn  |>int) (FlowTag.auto_lamp  |>int)  (FlowTag.auto_mode  |>int) (FlowTag.manual_btn |>int) (FlowTag.manual_lamp |>int) (FlowTag.manual_mode |>int)
                DrivePushLampMode        = getPushLampMode   tm (FlowTag.drive_btn |>int) (FlowTag.drive_lamp |>int)  (FlowTag.drive_state |>int)
                EmergencyPushLampMode    = getPushLampMode   tm (FlowTag.emg_btn   |>int) (FlowTag.emg_lamp   |>int)  (FlowTag.emergency_state   |>int)
                TestPushLampMode         = getPushLampMode   tm (FlowTag.test_btn  |>int) (FlowTag.test_lamp  |>int)  (FlowTag.test_state  |>int)
                ReadyPushLampMode        = getPushLampMode   tm (FlowTag.ready_btn |>int) (FlowTag.ready_lamp |>int)  (FlowTag.ready_state |>int)
                ClearPushLamp            = getPushLamp       tm (FlowTag.clear_btn |>int) (FlowTag.clear_lamp |>int)
                PausePushLamp            = getPushLamp       tm (FlowTag.pause_btn |>int) (FlowTag.pause_lamp  |>int)  
          
                IdleLampMode        = getLamp   tm (FlowTag.idle_mode    |>int)
                OriginLampMode      = getLamp   tm (FlowTag.origin_state    |>int)
                ErrorLampMode       = getLamp   tm (FlowTag.error_state    |>int)
                
                Reals            = x.Graph.Vertices.OfType<Real>().Select(fun r->r.GetHMI()).ToArray()
            }


    type DsSystem with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> SystemManager
            {
                Name  = x.Name
                AutoManualSelectLamp =  getSelectLamp tm (SystemTag.auto_btn  |>int) (SystemTag.auto_lamp  |>int) (SystemTag.manual_btn|>int) (SystemTag.manual_lamp|>int)
                DrivePushLamp        =  getPushLamp   tm (SystemTag.drive_btn |>int) (SystemTag.drive_lamp |>int)
                PausePushLamp        =  getPushLamp   tm (SystemTag.pause_btn |>int) (SystemTag.pause_lamp  |>int)
                ClearPushLamp        =  getPushLamp   tm (SystemTag.clear_btn |>int) (SystemTag.clear_lamp |>int)
                EmergencyPushLamp    =  getPushLamp   tm (SystemTag.emg_btn   |>int) (SystemTag.emg_lamp   |>int)
                TestPushLamp         =  getPushLamp   tm (SystemTag.test_btn  |>int) (SystemTag.test_lamp  |>int)
                HomePushLamp         =  getPushLamp   tm (SystemTag.home_btn  |>int) (SystemTag.home_lamp  |>int)
                ReadyPushLamp        =  getPushLamp   tm (SystemTag.ready_btn |>int) (SystemTag.ready_lamp |>int)

                Flows         = x.Flows.Select(fun f->f.GetHMI()).ToArray()
            }

    [<AutoOpen>]
    [<Extension>]
    type ConvertHMIExt =
        [<Extension>]
        static member GetHMIPackage (sys:DsSystem) : HMIPackage = 
            let ip        = RuntimeDS.IP
            let versionDS = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            let system    = sys.GetHMI()
            let devices   = sys.Devices.Select(fun d -> d.GetHMI()).ToArray()
            HMIPackage(ip, versionDS, system, devices) |> tee (fun x-> x.BuildTagMap())

        [<Extension>]
        static member GetHMIPackageTags (package:HMIPackage)  = 
            package.System.CollectTags() |> Seq.map(fun k->  k.Name, k) |> dict


