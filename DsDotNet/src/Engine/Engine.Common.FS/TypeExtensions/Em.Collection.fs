namespace Engine.Common.FS

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module EnumuerableExt =
    // instead Seq<'T>
    type IEnumerable<'T> with
        member x.isEmpty() = Seq.isEmpty x
        member x.length() = Seq.length x
        member x.any() = Seq.isEmpty x |> not
        member x.any f = Seq.tryFind f x |> Option.isSome
        member x.realize() = Array.ofSeq x |> ignore

    let groupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq) (keySelector:'V->'K) =
        xs.GroupBy(keySelector)
            .Select(fun g -> g.Key, g.ToArray())
            |> dict
            |> Dictionary

[<Extension>] // type SeqExt =
type SeqExt =
    /// test if sequence contains the value
    //[<Extension>] static member Contains(xs:'T seq, x:'T) = xs |> Seq.contains x  //System.Collections.Generic 혼동
    [<Extension>] static member ContainsAnyOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAnyOf x
    [<Extension>] static member ContainsAllOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAllOf x
    [<Extension>] static member AllSame(xs:'T seq, x:'T) = (xs |> Seq.filter (fun s -> s = x)).length() = xs.length()
    //[<Extension>] static member Any(xs:'a seq) = not <| Seq.isEmpty xs   //System.Collections.Generic 혼동
    [<Extension>] static member GetLength(xs:'a seq) = Seq.length xs
    [<Extension>] static member IsEmpty(xs:'a seq) = Seq.isEmpty xs
    [<Extension>] static member GroupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq, keySelector:'V->'K) = groupByToDictionary xs keySelector

    [<Extension>] static member Collect(xs:'a seq, f)    = Seq.collect f xs
    [<Extension>] static member Choose(xs:'a seq, f)     = Seq.choose f xs
    [<Extension>] static member Map(xs:'a seq, f)        = Seq.map f xs
    [<Extension>] static member Filter(xs:'a seq, f)     = Seq.filter f xs
    [<Extension>] static member Find(xs:'a seq, f)       = Seq.find f xs
    [<Extension>] static member Reduce(xs:'a seq, f)     = Seq.reduce f xs
    [<Extension>] static member Foldr(xs:'a seq, f)      = Seq.fold f xs
    [<Extension>] static member TryFind(xs:'a seq, f)    = Seq.tryFind f xs
    [<Extension>] static member TryHead(xs:'a seq)       = Seq.tryHead xs
    [<Extension>] static member Head(xs:'a seq)          = Seq.head xs
    [<Extension>] static member ForEach(xs:'a seq, f)    = Seq.iter f xs
    [<Extension>] static member Iter(xs:'a seq, f)       = Seq.iter f xs
    [<Extension>] static member IsNullOrEmpty(xs:'a seq) = xs = null || Seq.isEmpty xs
    [<Extension>] static member NonNullAny(xs:'a seq)    = xs <> null && xs.Any()

    [<Extension>] static member Tap(xs:'a seq, f)        = Seq.iter f xs; xs

