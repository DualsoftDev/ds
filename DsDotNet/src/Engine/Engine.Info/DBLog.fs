namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.IO
open System.ComponentModel
open System.Runtime.CompilerServices

module DBLog =
    type ValueLog(time: DateTime, tag: TagDS, tokenId:TokenIdType) =
        inherit DsLog(time, tag.GetStorage(), tokenId)

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


[<Extension>]
type DbWriterExtension =
    [<Extension>]
    static member InsertValueLog(dbWriter:DbWriter, time: DateTime, tag: TagDS, tokenId:TokenIdType) =
        let vlog = DBLog.ValueLog(time, tag, tokenId)
        if tag.IsNeedSaveDBLog() then
            dbWriter.EnqueLog(vlog)
        vlog
