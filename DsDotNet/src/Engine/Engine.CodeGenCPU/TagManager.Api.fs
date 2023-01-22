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
        let s =  a.System.TagManager.Storages

        let ps    = planTag s $"{a.Name}(PS)"
        let pr    = planTag s $"{a.Name}(PR)"
        let pe    = planTag s $"{a.Name}(PE)"

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

