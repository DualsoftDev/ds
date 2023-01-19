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


    /// ApiItem Manager Manager : ApiItem Tag  를 관리하는 컨테이어
    type ApiItemManager (a:ApiItem)  =    
        let s =  a.System.TagManager.Storages

        let ps    = dsTag s $"{a.Name}(PS)" DuBOOL   
        let pr    = dsTag s $"{a.Name}(PR)" DuBOOL   
                
        interface ITagManager with
            member x.Target = a
            member x.Storages = s

        member f.GetApiTag(at:ApiTag)     = 
            let t = 
                match at with
                |PLANSET        -> ps   
                |PLANRST        -> pr   

            t :?> DsTag<bool>

