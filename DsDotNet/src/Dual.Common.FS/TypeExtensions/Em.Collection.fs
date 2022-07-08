namespace Dual.Common

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module EnumuerableExt =
    // instead Seq<'T>
    type IEnumerable<'T> with
        member x.isEmpty() = Seq.isEmpty x
        member x.length() = Seq.length x
        member x.map f = Seq.map f x
        member x.any() = Seq.isEmpty x |> not 
        member x.any f = Seq.tryFind f x |> Option.isSome

        member x.select f = Seq.map f x
        member x.selectMany f = Seq.collect f x
        member x.where f = Seq.filter f x
        member x.realize() = Array.ofSeq x |> ignore
        member x.nonNullAny() = x <> null && x.Any()
        member x.isNullOrEmpty() = x = null || Seq.isEmpty x

[<Extension>] // type SeqExt =
type SeqExt =
    /// test if sequence contains the value
    [<Extension>] static member Contains(xs:'T seq, x:'T) = xs |> Seq.contains x
    [<Extension>] static member ContainsAnyOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAnyOf x
    [<Extension>] static member ContainsAllOf(xs:'T seq, x:'T seq) = xs |> Seq.containsAllOf x
    [<Extension>] static member AllSame(xs:'T seq, x:'T) = (xs |> Seq.filter (fun s -> s = x)).length() = xs.length()
    [<Extension>] static member Any(xs:'a seq) = not <| Seq.isEmpty xs
    [<Extension>] static member GetLength(xs:'a seq) = Seq.length xs
    [<Extension>] static member IsEmpty(xs:'a seq) = Seq.isEmpty xs
