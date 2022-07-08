namespace Dual.Common

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<Extension>]
type HashSetExt =
    /// hash 에 xs 를 추가한다.   중복되는 항목은 무시
    [<Extension>]
    static member AddRange(hash:HashSet<'a>, xs:'a seq) =
        for x in xs do
            hash.Add x |> ignore

    /// hash 에 xs 를 추가한다.   중복되는 항목이 존재하면 false 반환
    [<Extension>]
    static member AddRangeOrFail(hash:HashSet<'a>, xs:'a seq) =
        let xs = xs |> List.ofSeq
        if ( xs <> (xs |> List.distinct) || (xs |> List.exists hash.Contains)) then
            false
        else
            xs |> List.iter (hash.Add >> ignore)
            true

