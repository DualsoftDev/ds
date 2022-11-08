namespace Engine.Common.FS

open System
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
    [<Extension>] static member AllEqual(xs:'T seq, x:'T) = xs |> Seq.forall((=) x)
    //[<Extension>] static member Any(xs:'a seq) = not <| Seq.isEmpty xs   //System.Collections.Generic 혼동
    [<Extension>] static member GetLength(xs:'a seq) = Seq.length xs
    [<Extension>] static member IsEmpty(xs:'a seq) = Seq.isEmpty xs
    [<Extension>] static member IsOneOf(x:'x, [<ParamArray>] (xs:'x array)) = xs |> Seq.contains x
    [<Extension>] static member GroupByToDictionary<'V, 'K when 'K: equality>(xs:'V seq, keySelector:'V->'K) = groupByToDictionary xs keySelector
    [<Extension>] static member ToFSharpList(xs:'a seq)  = List.ofSeq xs

    [<Extension>] static member Collect(xs:'a seq, f)    = Seq.collect f xs
    [<Extension>] static member Choose(xs:'a seq, f)     = Seq.choose f xs
    [<Extension>] static member Map(xs:'a seq, f)        = Seq.map f xs
    [<Extension>] static member Filter(xs:'a seq, f)     = Seq.filter f xs
    [<Extension>] static member Find(xs:'a seq, f)       = Seq.find f xs
    [<Extension>] static member Indexed(xs:'a seq)       = Seq.indexed xs
    [<Extension>] static member Reduce(xs:'a seq, f)     = Seq.reduce f xs

    (*  List.fold (+) 0 [1; 2; 3] = ((0 + 1) + 2) + 3
        List.foldBack (+) [1; 2; 3] 0 = 1 + (2 + (3 + 0))   *)
    /// [x] -> (acc -> x -> acc) -> acc -> acc
    [<Extension>] static member FoldLeft(xs:'a seq, f, seed)   = Seq.fold f seed xs
    /// [x] -> (x -> acc -> acc) -> acc -> acc
    [<Extension>] static member FoldRight(xs:'a seq, f, seed)  = Seq.foldBack f xs seed

    [<Extension>] static member TryFind(xs:'a seq, f)    = Seq.tryFind f xs
    [<Extension>] static member TryHead(xs:'a seq)       = Seq.tryHead xs
    [<Extension>] static member Head(xs:'a seq)          = Seq.head xs
    [<Extension>] static member ForEach(xs:'a seq, f)    = Seq.iter f xs
    [<Extension>] static member Iter(xs:'a seq, f)       = Seq.iter f xs
    [<Extension>] static member ForAll(xs:'a seq, f)     = Seq.forall f xs
    [<Extension>] static member IsNullOrEmpty(xs:'a seq) = xs = null || Seq.isEmpty xs
    [<Extension>] static member NonNullAny(xs:'a seq)    = xs <> null && xs.Any()
    [<Extension>] static member Pairwise(xs:'a seq)      = Seq.pairwise xs
    [<Extension>] static member TapWhole(xs:'a seq, f)   = f xs; xs
    [<Extension>] static member TapInner(xs:'a seq, f)   = Seq.iter f xs; xs
    [<Extension>] static member Tap(xs:'a seq, f)        = Seq.iter f xs; xs


