namespace Dual.Common.Core.FS

open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module ResizeArray =
    let ofSeq (x:_ seq) = ResizeArray(x)

    /// ResizeArray 에서 조건에 맞는 요소를 제거하고, 반환
    let takeOut (pred:'x->bool) (xs: ResizeArray<'x>): ResizeArray<'x> =
        lock xs (fun () ->
            let itemsToRemove = xs |> Seq.filter pred |> ofSeq
            itemsToRemove |> iter (fun item -> xs.Remove(item) |> ignore)
            itemsToRemove
        )

    /// ResizeArray 에서 조건에 맞는 요소를 제거
    let clear (pred:'x->bool) (xs: ResizeArray<'x>) = takeOut pred xs |> ignore

type ResizeArrayExtension =
    /// ResizeArray 에서 조건에 맞는 요소를 제거
    [<Extension>] static member Clear(xs: ResizeArray<_>, pred: 'T -> bool) = ResizeArray.clear pred xs
    /// ResizeArray 에서 조건에 맞는 요소를 제거하고, 반환
    [<Extension>] static member Takeout(xs: ResizeArray<_>, pred: 'T -> bool) = ResizeArray.takeOut pred xs

