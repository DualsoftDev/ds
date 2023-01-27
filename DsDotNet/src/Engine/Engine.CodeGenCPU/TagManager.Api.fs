namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System

[<AutoOpen>]
module ApiTagManagerModule =

    [<Flags>]
    type ApiTag =
    |PLANSET
    |PLANRST
    |PLANEND


    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (a:ApiItem)  =
        let sys = a.System
        let s =  sys.TagManager.Storages

        let ps    = createPlanVarBool s $"{a.Name}_PS_"
        let pr    = createPlanVarBool s $"{a.Name}_PR_"
        let pe    = createPlanVarBool s $"{a.Name}_PE_"

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

