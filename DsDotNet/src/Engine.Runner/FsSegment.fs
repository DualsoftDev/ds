namespace Engine.Runner

open Engine.Core
open System
open System.Reactive.Linq
open Dual.Common


[<AutoOpen>]
module FsSegmentModule =
    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> unit
    
    type FsSegment(cpu, n) =
        inherit Segment(cpu, n)

        let mutable oldStatus:Status4 option = None
    
        abstract member WireEvent:ChangeWriter*ExceptionHandler->IDisposable
        default x.WireEvent(writer, onError) =
            let tagMyFlowReset = x.TagsReset |> Seq.find(fun t -> t.Type.HasFlag(TagType.Flow))
            let write(bit, value, cause) =
                writer(BitChange(bit, value, cause, onError))

            Global.RawBitChangedSubject
                .Where(fun bc ->
                    [
                        //tagMyFlowReset :> IBit; x.Going; x.Ready;
                        x.PortS :> IBit; x.PortR; x.PortE
                    ] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let state = x.Status
                    let bit = bc.Bit
                    let value = bc.NewValue
                    let cause = $"by bit change {bit.GetName()}={value}"
                    //if bit = x.Going then
                    //    if value then   // going 시작
                    //        write(x.PortE, true, $"{n} GOING 끝")
                    //    ()
                    //elif bit = x.Ready then
                    //    ()
                    //elif bit = tagMyFlowReset then
                    //    if value then
                    //        ()
                    //    else if state = Status4.Homing then
                    //        if x.PortE.Value then
                    //            write(x.PortE, false, $"{n} HOMING")
                    //        else
                    //            write(x.Ready, true, $"{n} Ready")
                    //    ()
                    //else
                    if oldStatus = Some state then
                        logDebug $"\t\tSkipping duplicate status: [{n}] status : {state} {cause}"
                    else
                        logDebug $"[{n}] Segment status : {state} {cause}"
                        if x.Going.Value && state <> Status4.Going then
                            write(x.Going, false, $"{n} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            write(x.Ready, false, $"{n} ready off by status {state}")

                        match state with
                        //| None, Status4.Going ->
                        //    // 준비가 되지 않아 reset 먼저 수행
                        //    write(tagMyFlowReset, true, $"{n} GOING 시작을 위한 reset")
                        //| Some Status4.Going, Status4.Homing ->
                        //    if tagMyFlowReset.Value then
                        //        write(tagMyFlowReset, false, $"{n} GOING 시작을 위한 reset 완료")
                        //    else
                        //        ()


                        | Status4.Ready -> doReady(write, x)
                        | Status4.Going -> doGoing(write, x)
                        | Status4.Finished -> doFinish(write, x)
                        | Status4.Homing -> doHoming(write, x)


                        | _ ->
                            failwith "Unexpected"
                        oldStatus <- Some state
                )


        //member x.Status = //with get() =
        //    match x.PortS.Value, x.PortR.Value, x.PortE.Value with
        //    | false, false, false -> Status4.Ready  //??
        //    | true, false, false  -> Status4.Going
        //    | _, false, true      -> Status4.Finished
        //    | _, true, _          -> Status4.Homing


