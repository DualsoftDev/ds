namespace UnitTest.Mockup.Engine


open System.Reactive.Linq
open System
open Engine.Common
open Engine.Common.FS
open Engine.Core
open Engine.Runner

[<AbstractClass>]
type MockupSegmentBase(cpu, n) =
    inherit FsSegmentBase(cpu, n)


    member val FinishCount = 0 with get, set

    static member val WithThreadOnPortEnd = false with get, set
    static member val WithThreadOnPortReset = false with get, set

type MockupSegment(cpu, n) =
    inherit MockupSegmentBase(cpu, n)
    let mutable oldStatus:Status4 option = None

    static member CreateWithDefaultTags(cpu, n) =
        let seg = MockupSegment(cpu, n)
        let ns = $"Start_{n}"
        let nr = $"Reset_{n}"
        let ne = $"End_{n}"
        seg.TagPStart <- TagP(cpu, seg, ns, TagType.Q ||| TagType.Start)
        seg.TagPReset <- TagP(cpu, seg, nr, TagType.Q ||| TagType.Reset)
        seg.TagPEnd   <- TagP(cpu, seg, ne, TagType.I ||| TagType.End  )
        seg.Going <- TagE(cpu, seg, $"Going_{n}", TagType.Going)
        seg.Ready <- TagE(cpu, seg, $"Ready_{n}", TagType.Ready)
        seg.PortS <- PortInfoStart(cpu, seg, $"spex{n}_default", seg.TagPStart, null)
        seg.PortR <- PortInfoReset(cpu, seg, $"rpex{n}_default", seg.TagPReset, null)
        seg.PortE <- PortInfoEnd.Create(cpu, seg, $"epex{n}_default", null)

        seg


    override x.WireEvent(writer, onError) =
        let write(bit, value, cause) =
            writer([| BitChange(bit, value, cause, onError) |])
        Global.BitChangedSubject
            .Where(fun bc -> bc.Bit.IsOneOf(x.PortS, x.PortR, x.PortE))
            .Subscribe(fun bc ->
                let state = x.Status
                if oldStatus = Some state then
                    logDebug $"\t\tSkipping duplicate status: [{n}] status : {state} by bit change {bc.Bit.GetName()}={bc.NewValue}"
                else
                    oldStatus <- Some state
                    logDebug $"[{n}] Segment status : {state}"
                    if x.Going.Value && state <> Status4.Going then
                        write(x.Going, false, $"{n} going off by status {state}")
                    if x.Ready.Value && state <> Status4.Ready then
                        cpu.Enqueue(x.Ready, false, $"{n} ready off by status {state}")

                    match state with
                    | Status4.Ready    ->
                        write(x.Ready, true, null)
                    | Status4.Going    ->
                        let go() =
                            write(x.Going, true, $"{n} GOING 시작")


                            write(x.PortE, true, $"{n} GOING 끝")
                        if MockupSegmentBase.WithThreadOnPortEnd then
                            async { go() } |> Async.Start
                        else
                            go()
                    | Status4.Finished ->
                        write(x.Going, false, $"{n} FINISH")   //! 순서 민감
                        x.FinishCount <- x.FinishCount + 1
                        logDebug $"[{x.Name}] Segment FinishCounter = {x.FinishCount}"
                    | Status4.Homing   ->
                        if MockupSegmentBase.WithThreadOnPortReset then
                            async { write(x.PortE, false, $"{n} HOMING") } |> Async.Start
                        else
                            write(x.PortE, false, $"{n} HOMING")

                    | _ ->
                        failwith "Unexpected"
            )

//[<AutoOpen>]
[<RequireQualifiedAccess>]
module MockUpCpu =
    let create(name:string) = Cpu(name, new Model())

