namespace UnitTest.Engine

open Engine.Core
open System.Linq


[<AutoOpen>]
module ModelExt =
    let collectExternalRealSegment(childFlow:ChildFlow) =
        childFlow.Children
        |> Seq.map(fun (c:Child) -> c.Coin)
        |> Enumerable.OfType<ExSegmentCall>
        ;

    let collectAlises(childFlow:ChildFlow) =
        childFlow.Children
        |> Seq.filter(fun (c:Child) -> c.IsAlias)
        ;
