namespace Engine.Common.FS

open System.Linq
open System.Collections.Generic

[<RequireQualifiedAccess>]
module ResizeArray =
    let ofSeq (x:_ seq) = ResizeArray(x)

