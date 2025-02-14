namespace Dual.Common.Core.FS

[<AutoOpen>]
module CollectionModule =
    let toArray xs = xs |> Array.ofSeq
    let toFSharpList xs = xs |> List.ofSeq
    let toList = toFSharpList
    let toResizeArray (xs:'a seq) = xs |> ResizeArray


