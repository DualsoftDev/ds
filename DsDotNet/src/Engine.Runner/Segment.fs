
namespace Engine.Runner

open Engine.Core
open System
open System.Linq
open System.Reactive.Linq
open Engine.Common.FS
open Engine.Common
open System.Collections.Generic


[<AutoOpen>]
module FsSegmentModule =    
    /// Common base class for *Real* root segment and Virtual Parent Segment
    [<AbstractClass>]
    type FsSegmentBase(cpu, segmentName) =
        inherit SegmentBase(cpu, segmentName)

        abstract member WireEvent:ChangeWriter*ExceptionHandler->IDisposable


        //member x.Status = //with get() =
        //    match x.PortS.Value, x.PortR.Value, x.PortE.Value with
        //    | false, false, false -> Status4.Ready  //??
        //    | true, false, false  -> Status4.Going
        //    | _, false, true      -> Status4.Finished
        //    | _, true, _          -> Status4.Homing

    /// Real Root Segment
    type Segment(cpu, segmentName) as this =
        inherit FsSegmentBase(cpu, segmentName)
        let name = segmentName
        do
            let getName() = $"{name}_{EmLinq.UniqueId()}"
            this.Going <- TagE(cpu, this, $"Going_{getName()}", TagType.Going, InternalName = "Going")
            this.Ready <- TagE(cpu, this, $"Ready_{getName()}", TagType.Ready, InternalName = "Ready")

            let ns = $"StartPlan_{getName()}"
            let nr = $"ResetPlan_{getName()}"
            let ne = $"EndPlan_{getName()}"
            this.TagPStart <- TagP(cpu, this, ns, TagType.Plan ||| TagType.Q ||| TagType.Start, InternalName = "StartPlan")
            this.TagPReset <- TagP(cpu, this, nr, TagType.Plan ||| TagType.Q ||| TagType.Reset, InternalName = "ResetPlan")
            this.TagPEnd   <- TagP(cpu, this, ne, TagType.Plan ||| TagType.I ||| TagType.End  , InternalName = "EndPlan")

        member val Inits:Child array = null with get, set
        member val Lasts:Child array = null with get, set
        member val ChildrenOrigin:IVertex array = null with get, set
        member val TraverseOrder:VertexAndOutgoingEdges array = null with get, set

        override x.Epilogue() =
            base.Epilogue()
            
            let n = $"{name}_{x.QualifiedName}"
            let (s, r, e) = x.Addresses
            if s <> null then
                this.TagAStart <- TagA(cpu, this, $"StartActual_{n}", s, TagType.External ||| TagType.Q ||| TagType.Start, InternalName = "Start")
            if r <> null then
                this.TagAReset <- TagA(cpu, this, $"ResetActual_{n}", r, TagType.External ||| TagType.Q ||| TagType.Reset, InternalName = "Reset")
            if e <> null then
                this.TagAEnd   <- TagA(cpu, this, $"EndActual_{n}",   e, TagType.External ||| TagType.I ||| TagType.End  , InternalName = "End")


            x.PortS <- PortInfoStart(cpu, x, $"StartPort_{n}", x.TagPStart, x.TagAStart, InternalName = "StartPort")
            x.PortR <- PortInfoReset(cpu, x, $"ResetPort_{n}", x.TagPReset, x.TagAReset, InternalName = "ResetPort")
            x.PortE <- PortInfoEnd  (cpu, x, $"EndPort_{n}",   x.TagPEnd,   x.TagAEnd,   InternalName = "EndPort")

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
                        assert(not bit.Value)
                        let msg = $"{n} status {state} duplicated on port {bit.GetName()} OFF by {cause}"
                        // case1 : Reset port 켜지는 시점
                        // case2 : EndPort 꺼지는 시점에 : Reset port 는 아직 살아 있으므로 homing 
                        if bit = x.PortS then   // finish 도중에 start port 꺼져서 finish 완료되려는 시점
                            logDebug $"\t\tFinished homing: {msg}"
                        elif bit = x.PortR then
                            ()
                        elif bit = x.PortE then // reset 중에 end port 꺼져서 reset 완료 되려는 시점.  상태는 아직 homing 중
                            logDebug $"\t\tAbout to finished homing: [{n}] status : {state} {cause}"
                            assert(not x.TagPStart.Value)
                            //write(x.TagStart, false, $"{n} homing completed")
                        else
                            failwith "ERROR"
                    else
                        logInfo $"[{n}] Segment status : {state} {cause}"
                        Global.SegmentStatusChangedSubject.OnNext(SegmentStatusChange(x, state))

                        if x.Going.Value && state <> Status4.Going then
                            write(x.Going, false, $"{n} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            write(x.Ready, false, $"{n} ready off by status {state}")

                        // { debug
                        if n = "A_F_Pp" && state = Status4.Homing then
                            noop()
                        match state with
                        | Status4.Ready ->  assert( [x.TagPStart; x.TagPEnd].ForAll(fun t -> not t.Value))
                        | Status4.Going -> ()
                        | Status4.Finished -> ()
                        | Status4.Homing -> ()
                        | _ -> ()
                        // } debug


                        match state with
                        | Status4.Ready -> doReady(x, writer, onError)
                        | Status4.Going -> doGoing(x, writer, onError)
                        | Status4.Finished -> doFinish(x, writer, onError)
                        | Status4.Homing -> doHoming(x, writer, onError)
                        | _ ->
                            failwith "Unexpected"
                        oldStatus <- Some state
                )

        //member val ProgressInfo:GraphProgressSupportUtil.ProgressInfo = null with get, set
