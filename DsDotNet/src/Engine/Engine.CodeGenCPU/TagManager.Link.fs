namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module LinkTagManagerModule =

    [<AutoOpen>]
    type LinkTag =
    |LinkStart
    |LintReset

