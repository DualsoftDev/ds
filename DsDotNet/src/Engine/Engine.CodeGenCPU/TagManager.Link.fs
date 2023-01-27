namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Collections.Generic
open System

[<AutoOpen>]
module LinkTagManagerModule =

    [<Flags>]
    type LinkTag =
    |LinkStart
    |LintReset

