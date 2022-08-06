namespace UnitTest.Mockup.Engine


open Engine.Core
open Dual.Common
open System.Reactive.Linq
open System.Collections.Concurrent
open System
open System.Reactive.Disposables

[<AutoOpen>]
module MockUpClasses =
    type MuSegmentBase(cpu, n, sp, rp, ep, goingTag, readyTag) =
        inherit Segment(n)

        member val Cpu:Cpu = cpu
        member val PortS:PortExpressionStart = sp with get, set
        member val PortR:PortExpressionReset = rp with get, set
        member val PortE:PortExpressionEnd = ep with get, set
        member val Going = if isNull goingTag then new Tag(cpu, null, $"{n}_Going") else goingTag
        member val Ready = if isNull readyTag then new Tag(cpu, null, $"{n}_Ready") else readyTag
        member val FinishCount = 0 with get, set
        member x.GetSegmentStatus() =
            match x.PortS.Value, x.PortR.Value, x.PortE.Value with
            | false, false, false -> Status4.Ready  //??
            | true, false, false  -> Status4.Going
            | _, false, true      -> Status4.Finished
            | _, true, _          -> Status4.Homing
        abstract member WireEvent:unit->IDisposable
        default x.WireEvent() = Disposable.Empty

    type MuSegment(cpu, n, sp, rp, ep, goingTag, readyTag) =
        inherit MuSegmentBase(cpu, n, sp, rp, ep, goingTag, readyTag)
        let mutable oldStatus = Status4.Homing

        static member CreateWithDefaultTags(cpu, n) =
            let seg = MuSegment(cpu, n, null, null, null, null, null)
            let st = Tag(cpu, seg, $"st_default_{n}", TagType.Start)
            let rt = Tag(cpu, seg, $"rt_default_{n}", TagType.Reset)
            seg.PortS <- PortExpressionStart(cpu, seg, $"spex{n}_default", st, null)
            seg.PortR <- PortExpressionReset(cpu, seg, $"rpex{n}_default", rt, null)
            seg.PortE <- PortExpressionEnd.Create(cpu, seg, $"epex{n}_default", null)

            seg, (st, rt)


        override x.WireEvent() =
            Global.BitChangedSubject
                .Where(fun bc ->
                    [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let newSegmentState = x.GetSegmentStatus()
                    if newSegmentState = oldStatus then
                        logDebug $"\t\tSkipping duplicate status: [{x.Name}] status : {newSegmentState} by bit change {bc.Bit.GetName()}={bc.NewValue}"
                    else
                        oldStatus <- newSegmentState
                        logDebug $"[{x.Name}] Segment status : {newSegmentState}"
                        if x.Going.Value && newSegmentState <> Status4.Going then
                            cpu.Enqueue(x.Going, false)
                        if x.Ready.Value && newSegmentState <> Status4.Ready then
                            cpu.Enqueue(x.Ready, false)

                        match newSegmentState with
                        | Status4.Ready    ->
                            cpu.Enqueue(x.Ready, true)
                            ()
                        | Status4.Going    ->
                            cpu.Enqueue(x.Going, true)
                            cpu.Enqueue(x.PortE, true)
                        | Status4.Finished ->
                            cpu.Enqueue(x.Going, false)   //! 순서 민감
                            x.FinishCount <- x.FinishCount + 1
                            logDebug $"[{x.Name}] Segment FinishCounter = {x.FinishCount}"
                        | Status4.Homing   ->
                            cpu.Enqueue(x.PortE, false)

                        | _ ->
                            failwith "Unexpected"
                )

    type MuCpu(n) =
        inherit Cpu(n, new Model())
        member val MuQueue = new ConcurrentQueue<BitChange>()

