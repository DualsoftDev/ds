namespace Engine.CodeGenHMI

open System.Collections.Generic
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module ControlServerHmiMap =
    type HmiMap (model:Model) =
        member x.res = ""