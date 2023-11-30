namespace Engine.CodeGenHMI

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System.Collections.Generic
open System.Reflection
open Engine.Info
open Engine.CodeGenCPU
open Engine.Cpu
[<AutoOpen>]
module ConvertHMI =
      
    let kindDescriptions = DBLoggerApi.GetAllTagKinds() |> Tuple.toDictionary

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
    let getSelect        (tm:ITagManager) (kindA:int) (kindB:int) = (getWebTag tm  kindA), (getWebTag tm kindB)
    let getPushMultiLamp (tm:ITagManager) (pushKind:int) (lampTags:ITag seq) =
        getWebTag tm pushKind, lampTags.Select(fun f-> TagWebExt.GetWebTag(f, kindDescriptions))


    type ApiItem with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> ApiItemManager
            {
                Name = x.Name
                TrendOutErrorLamp  = getLamp  tm (ApiItemTag.txErrTrendOut |>int)
                TimeOverErrorLamp  = getLamp  tm (ApiItemTag.txErrTimeOver |>int)
                ShortErrorLamp     = getLamp  tm (ApiItemTag.rxErrShort |>int)
                OpenErrorLamp      = getLamp  tm (ApiItemTag.rxErrOpen |>int)
                ErrorTotalLamp     = getLamp  tm (ApiItemTag.trxErr |>int)
            }

    type LoadedSystem with
        member private x.GetHMI()   =
            {
                Name        = x.Name
                ApiItems    = x.ReferenceSystem.ApiItems.Select(fun s->s.GetHMI()).ToArray()
            }

    type Job with
        member private x.GetHMI()   =
            let actionInTags  = x.DeviceDefs.Select(fun d->d.InTag)      
            let apiTagManager = x.DeviceDefs.First().ApiItem.TagManager :?> ApiItemManager
            {
                Name = x.Name
                JobPushMutiLamp = getPushMultiLamp  apiTagManager (ApiItemTag.planSet |>int) (actionInTags)
            }

    type Real with
        member private x.GetHMI()   =

            let getLoadedSystem (api:ApiItem) = x.Parent.GetSystem().GetLoadedSys(api.System.Name).Value 
            let tm = x.TagManager :?> VertexManager
            {
                Name = x.Name
                StartPush        = getPush tm (VertexTag.startTag |>int)   
                ResetPush        = getPush tm (VertexTag.resetTag |>int)  
                ONPush           = getPush tm (VertexTag.forceOn |>int)  
                OFFPush          = getPush tm (VertexTag.forceOff |>int)  
                ReadyLamp        = getPush tm (VertexTag.ready |>int)  
                GoingLamp        = getPush tm (VertexTag.going |>int)  
                FinishLamp       = getPush tm (VertexTag.finish |>int)  
                HomingLamp       = getPush tm (VertexTag.homing |>int)  
                OriginLamp       = getPush tm (VertexTag.origin |>int)  
                PauseLamp        = getPush tm (VertexTag.pause |>int)  
                ErrorTxLamp      = getPush tm (VertexTag.errorRx |>int)  
                ErrorRxLamp      = getPush tm (VertexTag.errorTx |>int)  
                
                Devices          = x.Graph.Vertices.OfType<Call>()
                                    .SelectMany(fun c->c.TargetJob.DeviceDefs
                                                        .Select(fun d->  getLoadedSystem  d.ApiItem)
                                                        .Select(fun d-> d.GetHMI())
                                                        ).ToArray()
                Jobs             = x.Graph.Vertices.OfType<Call>().Select(fun c->c.TargetJob.GetHMI()).ToArray()
            }

    type Flow with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> FlowManager
            {
                Name = x.Name
                AutoManualSelect = getSelect tm (FlowTag.auto_bit  |>int) (FlowTag.manual_bit |>int)
                DrivePush        = getPush   tm (FlowTag.drive_bit |>int)
                StopPush         = getPush   tm (FlowTag.stop_bit  |>int)
                ClearPush        = getPush   tm (FlowTag.clear_bit |>int)
                EmergencyPush    = getPush   tm (FlowTag.emg_bit   |>int)
                TestPush         = getPush   tm (FlowTag.test_bit  |>int)
                HomePush         = getPush   tm (FlowTag.home_bit  |>int)
                ReadyPush        = getPush   tm (FlowTag.ready_bit |>int)
                                             
                DriveLamp        = getLamp   tm (FlowTag.drive_op   |>int)
                AutoLamp         = getLamp   tm (FlowTag.auto_op    |>int)
                ManualLamp       = getLamp   tm (FlowTag.manual_op  |>int)
                StopLamp         = getLamp   tm (FlowTag.stop_op    |>int)
                EmergencyLamp    = getLamp   tm (FlowTag.emergency_op  |>int)
                TestLamp         = getLamp   tm (FlowTag.test_op    |>int)
                ReadyLamp        = getLamp   tm (FlowTag.ready_op   |>int)
                IdleLamp         = getLamp   tm (FlowTag.idle_op    |>int)

                Reals            = x.Graph.Vertices.OfType<Real>().Select(fun r->r.GetHMI()).ToArray()
            }


    type DsSystem with
        member private x.GetHMI()   =
            let tm = x.TagManager :?> SystemManager
            {
                Name  = x.Name
                AutoManualSelect =  getSelect tm (SystemTag.auto  |>int) (SystemTag.manual |>int)
                DrivePush        =  getPush   tm (SystemTag.drive |>int)
                StopPush         =  getPush   tm (SystemTag.stop  |>int)
                ClearPush        =  getPush   tm (SystemTag.clear |>int)
                EmergencyPush    =  getPush   tm (SystemTag.emg   |>int)
                TestPush         =  getPush   tm (SystemTag.test  |>int)
                HomePush         =  getPush   tm (SystemTag.home  |>int)
                ReadyPush        =  getPush   tm (SystemTag.ready |>int)

                Flows         = x.Flows.Select(fun f->f.GetHMI()).ToArray()
            }

    [<AutoOpen>]
    [<Extension>]
    type ConvertHMIExt =
        [<Extension>]
        static member GetHMIPackage (dsCpu:DsCPU) : HMIPackage = 
            let ip        = RuntimeDS.IP
            let versionDS = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            let system    = dsCpu.MySystem.GetHMI()
            let devices   = dsCpu.MySystem.Devices.Select(fun d -> d.GetHMI()).ToArray()
            HMIPackage(ip, versionDS, system, devices) |> tee (fun x-> x.BuildTagMap())
