namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.IO
open System.ComponentModel

module DBLog =


    type ValueLog(time: DateTime, tag: TagDS) =
        inherit DsLog(time, tag.GetStorage())

        let logData = tag.GetTagToText().Split(';')
        let _time = time

        member val Time = _time.ToString("HH:mm:ss.fff") with get
        member val Name = logData.[0] with get, set
        member val Value = logData.[1] with get, set
        member val System = logData.[2] with get, set
        member val TagKind = logData.[3] with get, set

        [<Browsable(false)>]
        member x.Storage = (x :> DsLog).Storage.Name

        member x.GapTime(t: DateTime) =
            _time.Subtract(t)

        member x.GetTime() =
            _time

    let InsertValueLog (time: DateTime) (tag: TagDS) : ValueLog =
        let vlog = ValueLog(time, tag)
        if tag.IsNeedSaveDBLog() then
            DBLogger.EnqueLogForInsert(vlog)
        vlog
