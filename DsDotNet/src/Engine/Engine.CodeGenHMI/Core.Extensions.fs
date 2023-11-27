namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System
open System.Collections.Generic
open System.Reflection
open Engine.Info
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
        
    let getPush     (tm:ITagManager) (kind:int) = getWebTag tm kind
    let getLamp     (tm:ITagManager) (kind:int) = getWebTag tm kind
    let getPushLamp (tm:ITagManager) (pushKind:int) (lampTag:ITag) = (getWebTag tm pushKind), TagWebExt.GetWebTag(lampTag, kindDescriptions) 
    let getSelect   (tm:ITagManager) (kindA:int) (kindB:int) = (getWebTag tm  kindA), (getWebTag tm kindB)

   

    type ApiItem with
        member x.GetHMI(containerSys:DsSystem)   =
            let getDevTask = containerSys.DeviceDefs.First(fun w->w.ApiItem = x)
            let tm = x.TagManager :?> ApiItemManager
            {
                Name = x.Name
                ApiPushLamp        = getPushLamp  tm (ApiItemTag.planSet |>int) (getDevTask.InTag)
                TrendOutErrorLamp  = getLamp      tm (ApiItemTag.txErrTrendOut |>int)
                TimeOverErrorLamp  = getLamp      tm (ApiItemTag.txErrTimeOver |>int)
                ShortErrorLamp     = getLamp      tm (ApiItemTag.rxErrShort |>int)
                OpenErrorLamp      = getLamp      tm (ApiItemTag.rxErrOpen |>int)
                ErrorTotalLamp     = getLamp      tm (ApiItemTag.trxErr |>int)
            }

    type Device with
        member x.GetHMI()   =
            {
                Name        = x.Name
                ApiItems    = x.ReferenceSystem.ApiItems.Select(fun s->s.GetHMI(x.ContainerSystem)).ToArray()
            }
    //작업중
    type Call with
        member x.GetHMI()   =
            {
                Name = x.Name
                JobPush        = null
                SensorLamps         = null
            }

    //작업중
    type Real with
        member x.GetHMI()   =
            {
                Name = x.Name
                JobPush        = null
                SensorLamps         = null
            }

    //작업중
    type Flow with
        member x.GetHMI()   =
            {
                Name = x.Name
                JobPush        = null
                SensorLamps         = null
            }


    //작업중
    type DsSystem with
        member x.GetHMI()   =
            let tm = x.TagManager :?> SystemManager
            {
                Name  = x.Name
                AutoManualSelect =  getSelect   tm (SystemTag.auto  |>int) (SystemTag.manual |>int)
                DrivePush        =  getPush     tm (SystemTag.drive |>int)
                StopPush         =  getPush     tm (SystemTag.stop  |>int)
                ClearPush        =  getPush     tm (SystemTag.clear |>int)
                EmergencyPush    =  getPush     tm (SystemTag.emg   |>int)
                TestPush         =  getPush     tm (SystemTag.test  |>int)
                HomePush         =  getPush     tm (SystemTag.home  |>int)
                ReadyPush        =  getPush     tm (SystemTag.ready |>int)

                Flows         = [].ToArray()
            }

    [<AutoOpen>]
    [<Extension>]
    type ConvertHMIExt =
        [<Extension>]
            static member GetHMIPackage(sys:DsSystem) = 
                {
                    IP          = RuntimeDS.IP
                    VersionDS   = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                    System      = sys.GetHMI()
                    Devices     = sys.Devices.Select(fun d->d.GetHMI()).ToArray()
                }
        