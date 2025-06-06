namespace T

open Engine.Core
open System.Linq


[<AutoOpen>]
module ModelExt =
    let collectExternalRealSegment(childFlow:ChildFlow) =
        childFlow.Children
        |> Seq.map(fun (c:Child) -> c.Coin)
        |> Enumerable.OfType<ExSegment>
        ;

    let collectAlises(childFlow:ChildFlow) =
        childFlow.Children
        |> Seq.filter(fun (c:Child) -> c.IsAlias)
        ;
