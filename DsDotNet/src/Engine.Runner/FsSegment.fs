namespace Engine.Runner

open Engine.Core
open System
open System.Reactive.Linq
open Dual.Common


[<AutoOpen>]
module FsSegmentModule =
    /// Bit * New Value * Change reason
    type ChangeWriter = IBit*bool*obj -> unit

    type FsSegment(cpu, n) =
        inherit Segment(cpu, n)

        let mutable oldStatus:Status4 option = None
    
        abstract member WireEvent:ChangeWriter->IDisposable
        default x.WireEvent(writer) =
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
                            writer(x.Ready, false, $"{n} ready off by status {state}")

                        match state with
                        | Status4.Ready    ->
                            writer(x.Ready, true, null)
                            ()
                        | Status4.Going    ->
                            writer(x.Going, true, $"{n} GOING 시작")
                            //if MockupSegmentBase.WithThreadOnPortEnd then
                            //    async { writer(x.PortE, true, $"{n} GOING 끝") } |> Async.Start
                            //else
                            writer(x.PortE, true, $"{n} GOING 끝")

                        | Status4.Finished ->
                            writer(x.Going, false, $"{n} FINISH")   //! 순서 민감

                        | Status4.Homing   ->
                            //if MockupSegmentBase.WithThreadOnPortReset then
                            //    async { writer(x.PortE, false, $"{n} HOMING") } |> Async.Start
                            //else
                            writer(x.PortE, false, $"{n} HOMING")

                        | _ ->
                            failwith "Unexpected"
                )


        member x.Status = //with get() =
            match x.PortS.Value, x.PortR.Value, x.PortE.Value with
            | false, false, false -> Status4.Ready  //??
            | true, false, false  -> Status4.Going
            | _, false, true      -> Status4.Finished
            | _, true, _          -> Status4.Homing


    let Initialize() =
        Segment.Create <-
            new Func<string, RootFlow, Segment>(
                fun name (rootFlow:RootFlow) ->
                    let seg = FsSegment(rootFlow.Cpu, name)
                    seg.ContainerFlow <- rootFlow
                    rootFlow.AddChildVertex(seg)
                    seg)
    ()
