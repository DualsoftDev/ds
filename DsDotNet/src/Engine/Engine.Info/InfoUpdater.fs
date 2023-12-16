namespace Engine.Info.Func

open System.Linq
open System.Collections.Generic 
open Engine.Core
open Dual.Common.Core.FS
open Engine.Info
open System
open Engine.CodeGenCPU


[<AutoOpen>]
module InfoUpdaterM =
    let updateDevices(sys:DsSystem) = 
        let taskDevs = sys.Jobs.SelectMany(fun j -> j.DeviceDefs);
        let calls = sys.Flows.SelectMany(fun f -> f.GetVerticesOfFlow().OfType<Call>());
        
        for d in sys.Devices do
            let apis = taskDevs |> Seq.filter (fun s -> s.ApiItem.System = d.ReferenceSystem) |> Seq.map (fun d -> d.ApiItem)
            let myDevCalls = calls |> Seq.filter (fun w -> w.TargetJob.DeviceDefs |> Seq.exists (fun c -> c.ApiItem.System = d.ReferenceSystem))
            let fqdns = myDevCalls |> Seq.map (fun dev -> dev.QualifiedName)

            apis |> Seq.iter (fun s ->
                let rxErrOpen = DBLogger.GetLastValue(s.QualifiedName, int ApiItemTag.rxErrOpen)
                let rxErrShort = DBLogger.GetLastValue(s.QualifiedName, int ApiItemTag.rxErrShort)
                let txErrTimeOver = DBLogger.GetLastValue(s.QualifiedName, int ApiItemTag.txErrTimeOver)
                let txErrTrendOut = DBLogger.GetLastValue(s.QualifiedName, int ApiItemTag.txErrTrendOut)
                let trxErr = DBLogger.GetLastValue(s.QualifiedName, int ApiItemTag.trxErr)

                if rxErrOpen.HasValue then ((s.TagManager :?> ApiItemManager).RXErrOpen.Value <- rxErrOpen.Value)
                if rxErrShort.HasValue then ((s.TagManager :?> ApiItemManager).RXErrShort.Value <- rxErrShort.Value)
                if txErrTimeOver.HasValue then ((s.TagManager :?> ApiItemManager).TXErrOverTime.Value <- txErrTimeOver.Value)
                if txErrTrendOut.HasValue then ((s.TagManager :?> ApiItemManager).TXErrTrendOut.Value <- txErrTrendOut.Value)
                if trxErr.HasValue then ((s.TagManager :?> ApiItemManager).TRxErr.Value <- trxErr.Value))

            let errs =
                apis
                |> Seq.map (fun api -> api.TagManager:?> ApiItemManager)
                |> Seq.choose (fun tagManager -> if tagManager.ErrorText.IsNullOrEmpty() then None else Some(tagManager.ErrorText))

            //errs |> Seq.iter (fun e -> messages.Add(e, e))  //에러 디스플레이 

            let errInfos =
                apis
                |> Seq.map (fun s ->
                    let count = DBLogger.Count(s.QualifiedName, int ApiItemTag.trxErr)
                    let duration = DBLogger.Average(s.QualifiedName, int ApiItemTag.trxErr)
                    (count, duration))
                |> Seq.toArray

            let goingCount = DBLogger.Count(fqdns, [| int VertexTag.going |])

            d.GoingCount <- goingCount
            d.ErrorMsg <- String.Join(", ", errs)
            d.ErrorCount <- errInfos |> Seq.sumBy (fun s -> fst s)
            d.ErrorAvgTime <- (errInfos |> Seq.sumBy (fun s -> (fst s|>float) * snd s)) / float d.ErrorCount 

