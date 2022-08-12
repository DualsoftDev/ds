namespace UnitTest.Mockup.Engine


open System.Reactive.Linq
open System
open System.Reactive.Disposables
open Dual.Common
open Engine.Core

type ChangeWriter = IBit*bool*obj -> unit
type MockupSegmentBase(cpu, n, sp, rp, ep, goingTag, readyTag) as this =
    inherit Segment(cpu, n, sp, rp, ep, goingTag, readyTag)

    member val FinishCount = 0 with get, set
    abstract member WireEvent:ChangeWriter->IDisposable
    default x.WireEvent(writer:ChangeWriter) = Disposable.Empty

    static member val WithThreadOnPortEnd = false with get, set
    static member val WithThreadOnPortReset = false with get, set

type MockupSegment(cpu, n) =
    inherit MockupSegmentBase(cpu, n, null, null, null, null, null)
    let mutable oldStatus:Status4 option = None

    static member CreateWithDefaultTags(cpu, n) =
        let seg = MockupSegment(cpu, n)
        let st = Tag(cpu, seg, $"st_default_{n}", TagType.Start)
        let rt = Tag(cpu, seg, $"rt_default_{n}", TagType.Reset)
        seg.PortS <- PortInfoStart(cpu, seg, $"spex{n}_default", st, null)
        seg.PortR <- PortInfoReset(cpu, seg, $"rpex{n}_default", rt, null)
        seg.PortE <- PortInfoEnd.Create(cpu, seg, $"epex{n}_default", null)

        seg, (st, rt)


    override x.WireEvent(writer) =
        Global.BitChangedSubject
            .Where(fun bc ->
                [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
            )
            .Subscribe(fun bc ->
                let state = x.Status
                if oldStatus = Some state then
                    logDebug $"\t\tSkipping duplicate status: [{n}] status : {state} by bit change {bc.Bit.GetName()}={bc.NewValue}"
                else
                    oldStatus <- Some state
                    logDebug $"[{n}] Segment status : {state}"
                    if x.Going.Value && state <> Status4.Going then
                        writer(x.Going, false, $"{n} going off by status {state}")
                    if x.Ready.Value && state <> Status4.Ready then
                        cpu.Enqueue(x.Ready, false, $"{n} ready off by status {state}")

                    match state with
                    | Status4.Ready    ->
                        writer(x.Ready, true, null)
                        ()
                    | Status4.Going    ->
                        writer(x.Going, true, $"{n} GOING 시작")
                        if MockupSegmentBase.WithThreadOnPortEnd then
                            async { writer(x.PortE, true, $"{n} GOING 끝") } |> Async.Start
                        else
                            writer(x.PortE, true, $"{n} GOING 끝")
                    | Status4.Finished ->
                        writer(x.Going, false, $"{n} FINISH")   //! 순서 민감
                        x.FinishCount <- x.FinishCount + 1
                        logDebug $"[{x.Name}] Segment FinishCounter = {x.FinishCount}"
                    | Status4.Homing   ->
                        if MockupSegmentBase.WithThreadOnPortReset then
                            async { writer(x.PortE, false, $"{n} HOMING") } |> Async.Start
                        else
                            writer(x.PortE, false, $"{n} HOMING")

                    | _ ->
                        failwith "Unexpected"
            )

//[<AutoOpen>]
[<RequireQualifiedAccess>]
module MockUpCpu =
    let create(name:string) = Cpu(name, new Model())

