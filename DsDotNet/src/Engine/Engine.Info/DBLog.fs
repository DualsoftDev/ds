namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.IO
open System.ComponentModel

module DBLog =


    type ValueLog(time: DateTime, tag: TagDS, token:Nullable<uint64>) =
        inherit DsLog(time, tag.GetStorage(), token)

        let tagName, value, objName, kind = tag.GetTagContents()     
        let _time = time

        member val Time = _time.ToString("HH:mm:ss.fff") with get
        member val Name = tagName with get, set
        member val Value = value with get, set
        member val System = objName with get, set
        member val TagKind = kind with get, set

        [<Browsable(false)>]
        member x.Storage = (x :> DsLog).Storage.Name

        member x.GapTime(t: DateTime) =
            _time.Subtract(t)

        member x.GetTime() =
            _time

    let InsertValueLog (time: DateTime) (tag: TagDS) (token:Nullable<uint64>) : ValueLog =
        let vlog = ValueLog(time, tag, token)
        if tag.IsNeedSaveDBLog() then
            DBLogger.EnqueLog(vlog)
        vlog
