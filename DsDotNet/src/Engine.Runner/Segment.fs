namespace Engine.Runner

open Engine.Core
open System
open System.Linq
open System.Reactive.Linq
open Dual.Common
open Engine.Common
open System.Collections.Generic


[<AutoOpen>]
module FsSegmentModule =
    /// Bit * New Value * Change reason
    type ChangeWriter = BitChange -> unit
    
    /// Common base class for *Real* root segment and Virtual Parent Segment
    [<AbstractClass>]
    type FsSegmentBase(cpu, segmentName, startTagName, resetTagName, endTagName) =
        inherit SegmentBase(cpu, segmentName, startTagName, resetTagName, endTagName)
    
        //new(cpu, segmentName) = FsSegment(cpu, segmentName, true, null, null, null)
        abstract member WireEvent:ChangeWriter*ExceptionHandler->IDisposable


        //member x.Status = //with get() =
        //    match x.PortS.Value, x.PortR.Value, x.PortE.Value with
        //    | false, false, false -> Status4.Ready  //??
        //    | true, false, false  -> Status4.Going
        //    | _, false, true      -> Status4.Finished
        //    | _, true, _          -> Status4.Homing

    /// Real Root Segment
    type Segment(cpu, segmentName) as this =
        inherit FsSegmentBase(cpu, segmentName, null, null, null)
        do
            let uid = EmLinq.UniqueId;
            this.Going <- Tag(cpu, this, $"Going_{segmentName}_{uid()}", TagType.Going, InternalName = "Going")
            this.Ready <- Tag(cpu, this, $"Ready_{segmentName}_{uid()}", TagType.Ready, InternalName = "Ready")

            this.PortE <- PortInfoEnd.Create(cpu, this, $"EndPort_{segmentName}_{uid()}", null)
            this.PortE.InternalName <- "EndPort"
            this.PortS <- new PortInfoStart(cpu, this, $"StartPort_{segmentName}_{uid()}", this.TagStart, null, InternalName = "StartPort")
            this.PortR <- new PortInfoReset(cpu, this, $"ResetPort_{segmentName}_{uid()}", this.TagReset, null, InternalName = "ResetPort")

        member val Inits:Child array = null with get, set
        member val Lasts:Child array = null with get, set
        member val ChildrenOrigin:IVertex array = null with get, set
        member val TraverseOrder:VertexAndOutgoingEdges array = null with get, set

        override x.Epilogue() =
            base.Epilogue()

            // Graph 정보 추출 & 저장
            let gi = x.GraphInfo;
            x.Inits <- gi.Inits.OfType<Child>().ToArray();
            x.Lasts <- gi.Lasts.OfType<Child>().ToArray();
            x.TraverseOrder <- gi.TraverseOrders;

            x.PrintPortInfos();


        default x.WireEvent(writer, onError) =
            let mutable oldStatus:Status4 option = None
            let n = x.QualifiedName
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
                    if oldStatus = Some state then
                        logDebug $"\t\tSkipping duplicate status: [{n}] status : {state} {cause}"
                    else
                        logInfo $"[{n}] Segment status : {state} {cause}"
                        Global.SegmentStatusChangedSubject.OnNext(SegmentStatusChange(x, state))

                        if x.Going.Value && state <> Status4.Going then
                            write(x.Going, false, $"{n} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            write(x.Ready, false, $"{n} ready off by status {state}")

                        match state with
                        | Status4.Ready -> doReady(write, x)
                        | Status4.Going -> doGoing(write, x)
                        | Status4.Finished -> doFinish(write, x)
                        | Status4.Homing -> doHoming(write, x)
                        | _ ->
                            failwith "Unexpected"
                        oldStatus <- Some state
                )

        //member val ProgressInfo:GraphProgressSupportUtil.ProgressInfo = null with get, set
