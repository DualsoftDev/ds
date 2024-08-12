namespace Engine.Info

open Engine.Core
open System
open System.ComponentModel

module DBLog =
    type ValueLog(time: DateTime, tagEvent: TagEvent, tokenId:TokenIdType) =
        inherit DsLog(time, tagEvent.GetStorage(), tokenId)

        let tagName, value, objName, kind = tagEvent.GetTagContents()
        let _time = time

        member val Time    = _time.ToString("HH:mm:ss.fff") with get
        member val Name    = tagName with get, set
        member val Value   = value   with get, set
        member val System  = objName with get, set
        member val TagKind = kind    with get, set

        [<Browsable(false)>]
        member x.Storage = (x :> DsLog).Storage.Name

        member x.GapTime(t: DateTime) =
            _time.Subtract(t)

        member x.GetTime() =
            _time
