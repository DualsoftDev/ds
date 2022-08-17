namespace Engine.Runner

open Engine.Core
open System
open System.Linq
open System.Reactive.Linq
open Dual.Common
open System.Collections.Generic
open QuickGraph.Algorithms.Search


[<AutoOpen>]
module RGFHModule =

    type Writer = IBit*bool*obj -> unit

    let goingSubscriptions = Dictionary<Segment, IDisposable>()

    let doReady(write:Writer, seg:Segment) =
        write(seg.Ready, true, null)

    let doGoing(write:Writer, seg:Segment, tagFlowReset:Tag) =
        write(seg.Going, true, $"{seg.QualifiedName} GOING 시작")
        if seg.Children.Any() then
            assert(not <| goingSubscriptions.ContainsKey(seg))
            for init in seg.Inits do
                let st = init.TagsStart.First(fun t -> t.Type.HasFlag(TagType.Flow) || t.Type.HasFlag(TagType.TX))
                write(st, true, "Starting child")
            //let unflipped = seg.TraverseOrder |
            let startTasg = seg.Inits.SelectMany(fun ch -> ch.TagsStart)
            let childRxTags = seg.Children.Select(fun ch -> ch.Coin)
            let subs =
                Global.RawBitChangedSubject.Subscribe(fun bc -> noop())
            goingSubscriptions.Add(seg, subs)
        else
            write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")


    let doFinish(write:Writer, seg:Segment) =
        write(seg.Going, false, $"{seg.QualifiedName} FINISH")

    let doHoming(write:Writer, seg:Segment) =
        //for child in seg.Children do
        //for orig in seg.ChildrenOrigin do
        //    seg.ChildStatusMap[orig]
        write(seg.PortE, false, $"{seg.QualifiedName} HOMING")

