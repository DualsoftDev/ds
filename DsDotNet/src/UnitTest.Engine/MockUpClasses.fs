namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Reactive.Linq
open System.Threading
open UnitTest.Engine
open System.Collections.Concurrent

[<AutoOpen>]
module MockUpClasses =
    type MuSegment(cpu, n, sp, rp, ep) =
        inherit Engine.Core.Segment(n)
        let mutable oldStatus = Status4.Homing
        new(cpu, n) = MuSegment(cpu, n, null, null, null)
        member val FinishCount = 0 with get, set
        member val PortS:PortExpressionStart = sp with get, set
        member val PortR:PortExpressionReset = rp with get, set
        member val PortE:PortExpressionEnd = ep with get, set
        member val Going = new Tag(cpu, null, $"{n}_Going")
        member x.GetSegmentStatus() =
            match x.PortS.Value, x.PortR.Value, x.PortE.Value with
            | false, false, false -> Status4.Ready  //??
            | true, false, false  -> Status4.Going
            | _, false, true      -> Status4.Finished
            | _, true, _          -> Status4.Homing

        member x.WireEvent() =
            Global.BitChangedSubject
                .Where(fun bc ->
                    [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let newSegmentState = x.GetSegmentStatus()
                    if newSegmentState = oldStatus then
                        logDebug $"\t\tSkipping duplicate status: [{x.Name}] status : {newSegmentState}"
                    else
                        oldStatus <- newSegmentState
                        logDebug $"[{x.Name}] Segment status : {newSegmentState}"

                        match newSegmentState with
                        | Status4.Ready    ->
                            ()
                        | Status4.Going    ->
                            x.Going.Value <- true
                            Thread.Sleep(100)
                            assert(x.GetSegmentStatus() = Status4.Going)
                            x.Going.Value <- false
                            x.PortE.Value <- true
                        | Status4.Finished ->
                            x.FinishCount <- x.FinishCount + 1
                            assert(x.PortE.Value)
                        | Status4.Homing   ->
                            if x.PortE.Value then
                                x.PortE.Value <- false
                                assert(not x.PortE.Value)
                            else
                                logDebug $"\tSkipping [{x.Name}] Segment status : {newSegmentState} : already homing by bit change {bc.Bit}={bc.NewValue}"
                                ()

                            assert(not x.PortE.Value)

                        | _ ->
                            failwith "Unexpected"
                )

    type MuCpu(n) =
        inherit Cpu(n, new Model())
        member val MuQueue = new ConcurrentQueue<BitChange>()


