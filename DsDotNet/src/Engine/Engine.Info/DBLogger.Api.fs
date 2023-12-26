namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Runtime.CompilerServices

[<AutoOpen>]
module DBLoggerApi =

    let private updateInfoBase (x:InfoBase, fqdn:string, kindDrive:int,  kindError:int,  kindPause:int) = 

        if DBLoggerImpl.logSet.IsNull()
        then failwithf "do InitializeLogDbOnDemandAsync"

        x.DriveSpan <- DBLogger.Sum(fqdn, kindDrive)
        x.DriveAverage <- DBLogger.Average(fqdn, kindDrive)
        x.ErrorSpan <- DBLogger.Sum(fqdn, kindDrive)
        x.ErrorAverage <- DBLogger.Average(fqdn, kindDrive)
        x.ErrorCount <- DBLogger.Count(fqdn, kindError)
        x.PauseCount <- DBLogger.Count(fqdn, kindPause)
        x.Efficiency <- x.DriveSpan / (x.DriveSpan + x.ErrorSpan) 


    let getInfoSystem (x:DsSystem) : InfoSystem = 
        let info = InfoSystem(x.Name)
        updateInfoBase (info, x.QualifiedName, SystemTag.sysDrive|>int,  SystemTag.sysStopError|>int, SystemTag.sysStopPause|>int)
        info

    let getInfoFlow (x:Flow) : InfoFlow = 
        let info = InfoFlow(x.Name)
        updateInfoBase (info, x.QualifiedName, FlowTag.drive_mode|>int,  FlowTag.flowStopError|>int, FlowTag.flowStopPause|>int)
        info

    let getInfoReal (x:Real) : InfoReal = 
        let info = InfoReal(x.Name)
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        info

    let getInfoCall (x:Call) : InfoCall = 
        let info = InfoCall(x.Name)
        updateInfoBase (info, x.QualifiedName, VertexTag.going|>int,  VertexTag.errorTRx|>int, VertexTag.pause|>int)
        info


    let getInfoDevice (x:Device) : InfoDevice = 
        let info = InfoDevice(x.Name)
        let sys  = (x:>LoadedSystem).ContainerSystem
        let apis = sys.Jobs.SelectMany(fun j -> j.DeviceDefs) 
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

            //해당 디바이스가 전체 시스템에서 사용된 횟수를 구한다
        let fqdns = sys.Flows.SelectMany(fun f -> f.GetVerticesOfFlow().OfType<Call>()) 
                            |> Seq.filter (fun w -> w.TargetJob.DeviceDefs |> Seq.exists (fun c -> c.ApiItem.System = x.ReferenceSystem))
                            |> Seq.map (fun call -> call.QualifiedName)

        info.GoingCount <-  DBLogger.Count(fqdns, [| int VertexTag.going |])
        info.ErrorCount <- errInfos |> Seq.sumBy (fun s -> fst s)
        info.RepairAverage <- (errInfos |> Seq.sumBy (fun s -> (fst s|>float) * snd s)) / Convert.ToDouble(info.ErrorCount)
        info


[<Extension>]
type InfoPackageModuleExt = 
    [<Extension>] static member GetInfo (x:DsSystem)  : InfoSystem = getInfoSystem x    
    [<Extension>] static member GetInfo (x:Flow)  : InfoFlow = getInfoFlow x    
    [<Extension>] static member GetInfo (x:Real)  : InfoReal = getInfoReal x    
    [<Extension>] static member GetInfo (x:Call)  : InfoCall = getInfoCall x    
    [<Extension>] static member GetInfo (x:Device)  : InfoDevice = getInfoDevice x    

