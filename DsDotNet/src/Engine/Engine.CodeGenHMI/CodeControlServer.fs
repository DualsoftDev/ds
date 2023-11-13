namespace Engine.CodeGenHMI

open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module ControlServerHmiMap =
    type HmiMap(__: Model) =
        member x.res = ""
