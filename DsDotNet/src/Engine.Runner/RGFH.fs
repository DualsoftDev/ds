namespace Engine.Runner

open Engine.Core
open System
open System.Reactive.Linq
open Dual.Common


[<AutoOpen>]
module RGFHModule =

    type Writer = IBit*bool*obj -> unit

    let doReady(write:Writer, seg:Segment) =
        write(seg.Ready, true, null)

    let doGoing(write:Writer, seg:Segment, tagFlowReset:Tag) =
        write(seg.Going, true, $"{seg.QualifiedName} GOING 시작")

    let doFinish(write:Writer, seg:Segment) =
        write(seg.Going, false, $"{seg.QualifiedName} FINISH")

    let doHoming(write:Writer, seg:Segment) =
        //for child in seg.Children do
        //for orig in seg.ChildrenOrigin do
        //    seg.ChildStatusMap[orig]
        write(seg.PortE, false, $"{seg.QualifiedName} HOMING")

