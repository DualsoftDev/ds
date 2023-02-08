namespace Engine.Common.FS

open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module EmFSharpCollectionModule =
    open Microsoft.FSharp.Collections

    (* F# list extension *)
    type List<'T> with
        member xs.OrElse(ys) = if xs |> List.isEmpty then ys else xs

    (* Array<'T> extension *)       // https://stackoverflow.com/questions/18359825/f-how-to-extended-the-generic-array-type
    type 'T ``[]`` with
        member xs.OrElse(ys) = if xs |> Array.isEmpty then ys else xs

    (* Seq<'T> extension *)
    type IEnumerable<'T> with
        member xs.OrElse(ys) = if xs |> Seq.isEmpty then ys else xs

    [<Extension>] // type SeqExt =
    type FSharpListExt =
        // 일반 F# type extension 구현시, 다음 오류 발생해서 [<Extension>] 으로 사용
        // error FS0340: 형식 매개 변수 'T'의 선언을 사용하려면 'T: comparison 형식의 제약 조건이 필요하므로 시그니처와 구현이 호환되지 않습니다.
        [<Extension>] static member Sort            (xs:List<'T>) = xs |> List.sort
        [<Extension>] static member SortDescending  (xs:List<'T>) = xs |> List.sortDescending


    type List<'T> with
        member xs.SortBy          (projection) = xs |> List.sortBy projection
        member xs.SortByDescending(projection) = xs |> List.sortByDescending projection
        member xs.SortWith        (comparer)   = xs |> List.sortWith comparer
        member xs.SplitAt         (index)      = xs |> List.splitAt index
        member xs.SplitInto       (count)      = xs |> List.splitInto count
