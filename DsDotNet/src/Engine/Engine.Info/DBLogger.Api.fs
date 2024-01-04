namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices

[<AutoOpen>]
module DBLoggerApi =
    let private random = Random()
    [<Obsolete("Remove random test data....")>]
    let private updateInfoBase (x:InfoBase, fqdn:string, kindDrive:int,  kindError:int,  kindPause:int) = 

        if DBLoggerImpl.logSet.IsNull()
        then failwithf "do InitializeLogDbOnDemandAsync"

        x.DriveSpan <- DBLogger.Sum(fqdn, kindDrive)
        x.DriveAverage <- DBLogger.Average(fqdn, kindDrive)
        x.ErrorSpan <- DBLogger.Sum(fqdn, kindDrive)
        x.ErrorAverage <- DBLogger.Average(fqdn, kindDrive)
        x.ErrorCount <- DBLogger.Count(fqdn, kindError)
        x.PauseCount <- DBLogger.Count(fqdn, kindPause)
        if (x.DriveSpan + x.ErrorSpan > 0.0) then
            x.Efficiency <- x.DriveSpan / (x.DriveSpan + x.ErrorSpan)

        //// todo: remove random test data
        //x.DriveSpan    <- random.Next(0, 1000)
        //x.DriveAverage <- random.Next(0, 1000)
        //x.ErrorSpan    <- random.Next(0, 1000)
        //x.ErrorAverage <- random.Next(0, 1000)
        //x.ErrorCount   <- random.Next(0, 1000)
        //x.PauseCount   <- random.Next(0, 1000)
        //x.Efficiency   <- random.Next(0, 1000)

    let getInfoDevices (xs:Device seq) : InfoDevice seq = 
        if xs.isEmpty()
        then Enumerable.Empty<InfoDevice>()
        else 
            let sys  = (xs.First():>LoadedSystem).ContainerSystem
            let jobs = sys.Jobs.SelectMany(fun j -> j.DeviceDefs) 
            xs.Select(fun x->
                let info = InfoDevice.Create(x)
                let apis = jobs
                            |> Seq.filter (fun s -> s.ApiItem.System = x.ReferenceSystem)
                            |> Seq.map (fun d -> d.ApiItem)
        
       
                apis |> Seq.iter (fun api ->
                    let rxErrOpen = DBLogger.GetLastValue(api.QualifiedName, int ApiItemTag.rxErrOpen)
                    let rxErrShort = DBLogger.GetLastValue(api.QualifiedName, int ApiItemTag.rxErrShort)
                    let txErrTimeOver = DBLogger.GetLastValue(api.QualifiedName, int ApiItemTag.txErrTimeOver)
                    let txErrTrendOut = DBLogger.GetLastValue(api.QualifiedName, int ApiItemTag.txErrTrendOut)
        
                    let err1 = if rxErrOpen.HasValue && rxErrOpen.Value         then $"{api.Name} 동작편차 이상" else ""
                    let err2 = if rxErrShort.HasValue && rxErrShort.Value       then $"{api.Name} 동작시간 이상" else ""
                    let err3 = if txErrTimeOver.HasValue && txErrTimeOver.Value then $"{api.Name} 센서감지 이상" else ""
                    let err4 = if txErrTrendOut.HasValue && txErrTrendOut.Value then $"{api.Name} 센서오프 이상" else ""
                    let errs =[err1;err2;err3;err4]|> Seq.where(fun f->f <> "")

                    info.ErrorMessages.AddRange errs
                    )

                let errInfos =
                    apis
                    |> Seq.map (fun s ->
                        let count = DBLogger.Count(s.QualifiedName, int ApiItemTag.trxErr)
                        let duration = DBLogger.Average(s.QualifiedName, int ApiItemTag.trxErr)
                        (count, duration))
                    |> Seq.toArray

                    //해당 디바이스가 전체 시스템에서 going된 횟수를 구한다
                let fqdns = sys.Flows.SelectMany(fun f -> f.GetVerticesOfFlow().OfType<Call>())
                                    |> Seq.filter (fun w -> w.TargetJob.DeviceDefs |> Seq.exists (fun c -> c.ApiItem.System = x.ReferenceSystem))
                                    |> Seq.map (fun call -> call.QualifiedName)

                info.GoingCount <-  DBLogger.Count(fqdns, [| int VertexTag.going |])
                info.ErrorCount <- errInfos |> Seq.sumBy (fun s -> fst s)
                if info.ErrorCount > 0 then
                    info.RepairAverage <- (errInfos |> Seq.sumBy (fun s -> (fst s|>float) * snd s)) / Convert.ToDouble(info.ErrorCount)
                info
            )

    let getInfoDevice (x:Device) : InfoDevice =  getInfoDevices([x]) |> Seq.head
    
    let getInfoCall (x:Call) : InfoCall = 
        let info = InfoCall.Create(x)
        let loadedDevices = x.Parent.GetSystem().Devices
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        let infoDevices = x.TargetJob.DeviceDefs.Select(fun d->loadedDevices.First(fun f->f.Name = d.DeviceName))
        info.InfoDevices.AddRange(getInfoDevices(infoDevices)) |>ignore
        info

    let getInfoReal (x:Real) : InfoReal = 
        let info = InfoReal.Create(x)
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        let infoCalls = x.Graph.Vertices.OfType<Call>().Select(getInfoCall)
        info.InfoCalls.AddRange(infoCalls) |>ignore
        info

    let getInfoFlow (x:Flow) : InfoFlow = 
        let info = InfoFlow.Create(x)
        updateInfoBase (info, x.QualifiedName, FlowTag.drive_mode|>int,  FlowTag.flowStopError|>int, FlowTag.flowStopPause|>int)
        let infoReals = x.GetVerticesOfFlow().OfType<Real>().Select(getInfoReal)
        info.InfoReals.AddRange(infoReals) |>ignore
        info

    let getInfoSystem (x:DsSystem) : InfoSystem = 
        let infoSys = InfoSystem.Create(x)
        updateInfoBase (infoSys, x.QualifiedName, SystemTag.driveState|>int,  SystemTag.sysStopError|>int, SystemTag.sysStopPause|>int)
        let infoFlows = x.Flows.Select(getInfoFlow)
        infoSys.InfoFlows.AddRange(infoFlows) |>ignore
        infoSys



[<Extension>]
type InfoPackageModuleExt = 
    [<Extension>] static member GetInfo (x:DsSystem): InfoSystem = getInfoSystem x    
    [<Extension>] static member GetInfos (xs:Device seq)  : InfoDevice seq = getInfoDevices xs    

