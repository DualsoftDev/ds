namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module ApiTagManagerModule =

    [<AutoOpen>]
    type ApiTag =
    |PLANSET
    |PLANRST
    |PLANEND


    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (a:ApiItem)  =
        let sys = a.System
        let s =  sys.TagManager.Storages

        let ps    = planTag s $"{a.Name}(PS)" sys
        let pr    = planTag s $"{a.Name}(PR)" sys
        let pe    = planTag s $"{a.Name}(PE)" sys

        interface ITagManager with
            member x.Target = a
            member x.Storages = s

        member f.GetApiTag(at:ApiTag)     =
            let t =
                match at with
                |PLANSET        -> ps
                |PLANRST        -> pr
                |PLANEND        -> pe

            t

