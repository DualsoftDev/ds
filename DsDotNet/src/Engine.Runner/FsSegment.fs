namespace Engine.Runner

open Engine.Core
open System
open System.Reactive.Linq
open Dual.Common
open log4net.Core


[<AutoOpen>]
module FsSegmentModule =
    /// Bit * New Value * Change reason
    //type ChangeWriter = IBit*bool*obj -> unit
    type ChangeWriter = BitChange -> unit
    
    type FsSegment(cpu, n) =
        inherit Segment(cpu, n)

        let mutable oldStatus:Status4 option = None
    
        abstract member WireEvent:ChangeWriter*ExceptionHandler->IDisposable
        default x.WireEvent(writer, onError) =
            let write(bit, value, cause) =
                writer(BitChange(bit, value, cause, onError))
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
                            write(x.Going, false, $"{n} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            write(x.Ready, false, $"{n} ready off by status {state}")

                        match state with
                        | Status4.Ready    ->
                            write(x.Ready, true, null)
                            ()
                        | Status4.Going    ->
                            write(x.Going, true, $"{n} GOING 시작")
                            //if MockupSegmentBase.WithThreadOnPortEnd then
                            //    async { writer(x.PortE, true, $"{n} GOING 끝") } |> Async.Start
                            //else
                            write(x.PortE, true, $"{n} GOING 끝")

                        | Status4.Finished ->
                            write(x.Going, false, $"{n} FINISH")   //! 순서 민감

                        | Status4.Homing   ->
                            //if MockupSegmentBase.WithThreadOnPortReset then
                            //    async { writer(x.PortE, false, $"{n} HOMING") } |> Async.Start
                            //else
                            write(x.PortE, false, $"{n} HOMING")

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
