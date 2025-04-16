namespace Engine.CodeGenHMI

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Base.FS
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
            | :? SystemManager  as m -> m.GetSystemTag  (DU.tryGetEnumValue<SystemTag>(kind).Value)
            | :? FlowManager    as m -> m.GetFlowTag    (DU.tryGetEnumValue<FlowTag>(kind).Value)
            | :? VertexTagManager  as m -> m.GetVertexTag  (DU.tryGetEnumValue<VertexTag>(kind).Value)
            | :? ApiItemManager as m -> m.GetApiTag     (DU.tryGetEnumValue<ApiItemTag>(kind).Value)
            | :? TaskDevManager as m -> m.GetTaskDevTag (DU.tryGetEnumValue<TaskDevTag>(kind).Value)
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

    let getDeiveHMIs(call:Call) =
        call.TaskDefs.Select(fun td->
        {
            Name =  td.DeviceName
            ActionIN  = if td.InTag.IsNonNull()  then Some (getLamp (td.TagManager) (TaskDevTag.actionIn |> int)) else None
            ActionOUT = if td.OutTag.IsNonNull() then Some (getLamp (td.TagManager) (TaskDevTag.actionOut |> int)) else None
        })

    /// int 로 변환
    let inline private i t = t |> int

    type Call with
        member private x.GetHMI(): HMICall =
            let tm = x.TagManager :?> CoinVertexTagManager
            {
                Name = x.Name
                TimeOnShortageErrorLamp  = getLamp  tm (i VertexTag.txErrOnTimeUnder)
                TimeOnOverErrorLamp      = getLamp  tm (i VertexTag.txErrOnTimeOver)
                TimeOffShortageErrorLamp = getLamp  tm (i VertexTag.txErrOffTimeUnder)
                TimeOffOverErrorLamp     = getLamp  tm (i VertexTag.txErrOffTimeOver)

                ShortErrorLamp           = getLamp  tm (i VertexTag.rxErrShort)
                OpenErrorLamp            = getLamp  tm (i VertexTag.rxErrOpen)
                InterlockErrLamp         = getLamp  tm (i VertexTag.rxErrInterlock)
                
                ErrorTotalLamp           = getLamp  tm (i VertexTag.errorAction)
            }

    type LoadedSystem with
        member private x.GetHMIs() : HMIDevice seq =
            let containerCalls = x.ContainerSystem.GetVerticesOfJobCalls()
            containerCalls.Where(fun c->c.TaskDefs
                                         .Any(fun d->d.ApiItem.ApiSystem = x.ReferenceSystem))
                          .SelectMany(getDeiveHMIs)

    type Job with
        member private x.GetHMI(call:Call): HMIJob =
            let actionInTags  = x.TaskDefs.Where(fun d->d.InTag.IsNonNull()).Select(fun d->d.InTag) //todo  ActionINFunc func $not 적용 검토
            {
                Name = x.QualifiedName
                JobPushMutiLamp = getPushMultiLamp call.TagManager (VertexTag.forceStart |>int) (actionInTags)
            }

    type Real with
        member private x.GetHMI(): HMIReal =

            let calls = x.Graph.Vertices.OfType<Call>()
            let tm = x.TagManager :?> VertexTagManager
            {
                Name = x.Name
                StartPush    = getPush tm (i VertexTag.startTag)
                ResetPush    = getPush tm (i VertexTag.resetTag)
                ONPush       = getPush tm (i VertexTag.forceOn)
                OFFPush      = getPush tm (i VertexTag.forceReset)
                ReadyLamp    = getLamp tm (i VertexTag.ready)
                GoingLamp    = getLamp tm (i VertexTag.going)
                FinishLamp   = getLamp tm (i VertexTag.finish)
                HomingLamp   = getLamp tm (i VertexTag.homing)
                OriginLamp   = getLamp tm (i VertexTag.origin)
                PauseLamp    = getLamp tm (i VertexTag.pause)
                Error        = getLamp tm (i VertexTag.errorWork)

                Devices      = calls.SelectMany(fun c-> getDeiveHMIs(c)).ToArray()

                Jobs         = calls
                                    .Where(fun c->c.IsJob)
                                    .DistinctBy(fun c->c.TargetJob)
                                    .Select(fun c->c.TargetJob.GetHMI(c)).ToArray()
            }

    type Flow with
        member private x.GetHMI(): HMIFlow =
            let tm = x.TagManager :?> FlowManager
            {
                Name = x.Name
                AutoManualSelectLampMode = getSelectLampMode tm (i FlowTag.auto_btn ) (i FlowTag.auto_lamp )  (i FlowTag.auto_mode   ) (i FlowTag.manual_btn) (i FlowTag.manual_lamp) (i FlowTag.manual_mode)
                DrivePushLampMode        = getPushLampMode   tm (i FlowTag.drive_btn) (i FlowTag.drive_lamp)  (i FlowTag.drive_state )
                EmergencyPushLampMode    = getPushLampMode   tm (i FlowTag.emg_btn  ) (i FlowTag.emg_lamp  )  (i FlowTag.emergency_state )
                TestPushLampMode         = getPushLampMode   tm (i FlowTag.test_btn ) (i FlowTag.test_lamp )  (i FlowTag.test_state  )
                ReadyPushLampMode        = getPushLampMode   tm (i FlowTag.ready_btn) (i FlowTag.ready_lamp)  (i FlowTag.ready_state )
                ClearPushLamp            = getPushLamp       tm (i FlowTag.clear_btn) (i FlowTag.clear_lamp)
                HomePushLamp             = getPushLamp       tm (i FlowTag.home_btn)  (i FlowTag.home_lamp)
                PausePushLamp            = getPushLamp       tm (i FlowTag.pause_btn) (i FlowTag.pause_lamp)

                IdleLampMode        = getLamp   tm (i FlowTag.idle_mode   )
                OriginLampMode      = getLamp   tm (i FlowTag.origin_state)
                ErrorLampMode       = getLamp   tm (i FlowTag.error_state )

                Reals            = x.Graph.Vertices.OfType<Real>().Select(fun r->r.GetHMI()).ToArray()
            }


    type DsSystem with
        member private x.GetHMI(): HMISystem =
            let tm = x.TagManager :?> SystemManager
            {
                Name  = x.Name
                AutoManualSelectLamp =  getSelectLamp tm (i SystemTag.auto_btn ) (i SystemTag.auto_lamp ) (i SystemTag.manual_btn) (i SystemTag.manual_lamp)
                DrivePushLamp        =  getPushLamp   tm (i SystemTag.drive_btn) (i SystemTag.drive_lamp)
                PausePushLamp        =  getPushLamp   tm (i SystemTag.pause_btn) (i SystemTag.pause_lamp)
                ClearPushLamp        =  getPushLamp   tm (i SystemTag.clear_btn) (i SystemTag.clear_lamp)
                EmergencyPushLamp    =  getPushLamp   tm (i SystemTag.emg_btn  ) (i SystemTag.emg_lamp  )
                TestPushLamp         =  getPushLamp   tm (i SystemTag.test_btn ) (i SystemTag.test_lamp )
                HomePushLamp         =  getPushLamp   tm (i SystemTag.home_btn ) (i SystemTag.home_lamp )
                ReadyPushLamp        =  getPushLamp   tm (i SystemTag.ready_btn) (i SystemTag.ready_lamp)

                OriginLampMode       = getLamp   tm (i SystemTag.originMonitor)

                Flows = x.Flows|> map (fun f -> f.GetHMI()) |> toArray

                // 개별 call 과 무관하게 DsSystem.Jobs 를 HMIJob[] 으로 가져갈 필요는 없는가?
                Jobs = [||]
                //Jobs  = x.Jobs |> map (fun j -> j.GetHMI()) |> toArray
            }

    [<AutoOpen>]
    [<Extension>]
    type ConvertHMIExt =
        [<Extension>]
        static member GetHMIPackage (sys:DsSystem, hwIP:string) : HMIPackage =
            let versionDS = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            let system    = sys.GetHMI()
            let devices   = sys.Devices.SelectMany(fun d -> d.GetHMIs()).ToArray()
            HMIPackage(hwIP, versionDS, system, devices) |> tee (fun x-> x.BuildTagMap())

        [<Extension>]
        static member GetHMIPackageTags (package:HMIPackage)  =
            package.System.CollectTags() |> Seq.map(fun k->  k.Name, k) |> dict


