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
        member val PortS:PortInfoStart = sp with get, set
        member val PortR:PortInfoReset = rp with get, set
        member val PortE:PortInfoEnd = ep with get, set
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
            seg.PortS <- PortInfoStart(cpu, seg, $"spex{n}_default", st, null)
            seg.PortR <- PortInfoReset(cpu, seg, $"rpex{n}_default", rt, null)
            seg.PortE <- PortInfoEnd.Create(cpu, seg, $"epex{n}_default", null)

            seg, (st, rt)


        override x.WireEvent() =
            Global.BitChangedSubject
                .Where(fun bc ->
                    [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let newSegmentState = x.GetSegmentStatus()
                    if newSegmentState = oldStatus then
                        logDebug $"\t\tSkipping duplicate status: [{n}] status : {newSegmentState} by bit change {bc.Bit.GetName()}={bc.NewValue}"
                    else
                        oldStatus <- newSegmentState
                        logDebug $"[{n}] Segment status : {newSegmentState}"
                        if x.Going.Value && newSegmentState <> Status4.Going then
                            cpu.Enqueue(x.Going, false, $"{n} going off by status {newSegmentState}")
                        if x.Ready.Value && newSegmentState <> Status4.Ready then
                            cpu.Enqueue(x.Ready, false, $"{n} ready off by status {newSegmentState}")

                        match newSegmentState with
                        | Status4.Ready    ->
                            cpu.Enqueue(x.Ready, true)
                            ()
                        | Status4.Going    ->
                            cpu.Enqueue(x.Going, true, $"{n} GOING 시작")
                            cpu.Enqueue(x.PortE, true, $"{n} GOING 끝")
                        | Status4.Finished ->
                            cpu.Enqueue(x.Going, false, $"{n} FINISH")   //! 순서 민감
                            x.FinishCount <- x.FinishCount + 1
                            logDebug $"[{x.Name}] Segment FinishCounter = {x.FinishCount}"
                        | Status4.Homing   ->
                            cpu.Enqueue(x.PortE, false, $"{n} HOMING")

                        | _ ->
                            failwith "Unexpected"
                )

    type MuCpu(n) =
        inherit Cpu(n, new Model())
        member val MuQueue = new ConcurrentQueue<BitChange>()

