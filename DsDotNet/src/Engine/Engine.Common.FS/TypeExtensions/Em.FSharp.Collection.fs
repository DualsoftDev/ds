namespace Engine.Common.FS

open System.Runtime.CompilerServices

[<AutoOpen>]
module EmFSharpCollectionModule =
    open Microsoft.FSharp.Collections

    (* F# list extension *)
    type List<'T> with
        member xs.OrElse(ys) = if xs |> List.isEmpty then ys else xs

    [<Extension>] // type SeqExt =
    type FSharpListExt =
        [<Extension>] static member Sort            (xs:List<'T>)             = xs |> List.sort
        [<Extension>] static member SortDescending  (xs:List<'T>)             = xs |> List.sortDescending
        [<Extension>] static member SortBy          (xs:List<'T>, projection) = xs |> List.sortBy projection
        [<Extension>] static member SortByDescending(xs:List<'T>, projection) = xs |> List.sortByDescending projection
        [<Extension>] static member SortWith        (xs:List<'T>, comparer)   = xs |> List.sortWith comparer
        [<Extension>] static member SplitAt         (xs:List<'T>, index)      = xs |> List.splitAt index
        [<Extension>] static member SplitInto       (xs:List<'T>, count)      = xs |> List.splitInto count


