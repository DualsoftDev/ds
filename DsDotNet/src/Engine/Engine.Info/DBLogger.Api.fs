namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices

[<AutoOpen>]
module DBLoggerApi =
    let private random = Random()
    let private updateInfoBase (x:InfoBase, fqdn:string, kindDrive:int,  kindError:int,  kindPause:int) =

        if DbWriter.TheDbWriter.LogSet.IsNull() then
            failwithf "do InitializeLogDbOnDemandAsync"

        debugfn $"updateInfoBase for fqdn: {fqdn}"

        x.DriveSpan    <- DBLogger.Sum(fqdn, kindDrive)
        x.DriveAverage <- DBLogger.Average(fqdn, kindDrive)
        x.ErrorSpan    <- DBLogger.Sum(fqdn, kindError)
        x.ErrorAverage <- DBLogger.Average(fqdn, kindError)
        x.ErrorCount   <- DBLogger.Count(fqdn, kindError)
        x.PauseCount   <- DBLogger.Count(fqdn, kindPause)
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

    let getInfoDevices (devices:Device seq) : InfoDevice seq =
        let sys  = (devices.First():>LoadedSystem).ContainerSystem
        let calls = sys.GetVerticesOfJobCalls()
        devices.Select(fun x->
            let info = InfoDevice.Create(x)
            let callUseds =
                calls.Where(fun c-> c.TargetJob.TaskDefs.any(fun d->d.FirstApi.ApiSystem = x.ReferenceSystem))


            callUseds
            |> Seq.iter (fun call ->
                let errOpen            = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.rxErrOpen)
                let errShort           = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.rxErrShort)
                let errOnTimeOver      = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.txErrOnTimeOver)
                let errOnTimeShortage  = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.txErrOnTimeShortage)
                let errOffTimeOver     = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.txErrOffTimeOver)
                let errOffTimeShortage = DBLogger.TryGetLastValue(call.QualifiedName, int VertexTag.txErrOffTimeShortage)
                let isTrue(opt:bool Option) = opt.IsSome && opt.Value

                let errs =
                    [|
                        if isTrue(errOpen)            then yield $"{call.Name} 센서오프이상"
                        if isTrue(errShort)           then yield $"{call.Name} 센서감지이상"
                        if isTrue(errOnTimeOver)      then yield $"{call.Name} 감지시간초과 이상"
                        if isTrue(errOnTimeShortage)  then yield $"{call.Name} 감지시간부족 이상"
                        if isTrue(errOffTimeOver)     then yield $"{call.Name} 해지시간초과 이상"
                        if isTrue(errOffTimeShortage) then yield $"{call.Name} 해지시간부족 이상"
                    |]

                info.ErrorMessages.AddRange errs
            )

            let errInfos =
                callUseds
                |> Seq.map (fun c ->
                    let count = DBLogger.Count(c.QualifiedName, int VertexTag.errorTRx)
                    let duration = DBLogger.Average(c.QualifiedName, int VertexTag.errorTRx)
                    (count, duration))
                |> Seq.toArray

                //해당 디바이스가 전체 시스템에서 going된 횟수를 구한다
            let fqdns = callUseds |> Seq.map (fun call -> call.QualifiedName)
            info.GoingCount <- DBLogger.Count(fqdns, [| int VertexTag.going |])
            info.ErrorCount <- errInfos |> Seq.sumBy fst
            if info.ErrorCount > 0 then
                info.RepairAverage <- (errInfos |> Seq.sumBy (fun s -> (fst s|>float) * snd s)) / Convert.ToDouble(info.ErrorCount)
            info
        )

    let getInfoDevice (x:Device) : InfoDevice =  getInfoDevices([x]) |> Seq.head

    let getInfoCall (x:Call) : InfoCall =
        let info = InfoCall.Create(x)
        let loadedDevices = x.Parent.GetSystem().Devices
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        let infoDevices = x.TargetJob.TaskDefs.Select(fun d->loadedDevices.First(fun f->f.Name = d.DeviceName))
        info.InfoDevices.AddRange(getInfoDevices(infoDevices)) |> ignore
        info

    let getInfoReal (x:Real) : InfoReal =
        let info = InfoReal.Create(x)
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        let infoCalls = x.Graph.Vertices.OfType<Call>().Select(getInfoCall)
        info.InfoCalls.AddRange(infoCalls) |> ignore
        info

    let getInfoFlow (x:Flow) : InfoFlow =
        let info = InfoFlow.Create(x)
        updateInfoBase (info, x.QualifiedName, FlowTag.drive_state|>int,  FlowTag.flowStopError|>int, FlowTag.pause_state|>int)
        let infoReals = x.GetVerticesOfFlow().OfType<Real>().Select(getInfoReal)
        info.InfoReals.AddRange(infoReals) |> ignore
        info

    let getInfoSystem (x:DsSystem) : InfoSystem =
        let infoSys = InfoSystem.Create(x)
        updateInfoBase (infoSys, x.QualifiedName, SystemTag.driveMonitor|>int,  SystemTag.errorMonitor|>int, SystemTag.pauseMonitor|>int)
        let infoFlows = x.Flows.Select(getInfoFlow)
        infoSys.InfoFlows.AddRange(infoFlows) |> ignore
        infoSys



[<Extension>]
type InfoPackageModuleExt =
    [<Extension>] static member GetInfo (x:DsSystem): InfoSystem = getInfoSystem x
    [<Extension>] static member GetInfos (xs:Device seq)  : InfoDevice seq = getInfoDevices xs

