namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Linq

open Engine.Core
open Engine.Common.FS
open Engine.Common


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

        member val Inits:Child array = null with get, set
        member val Lasts:Child array = null with get, set
        member val ChildrenOrigin:IVertex array = null with get, set
        member val TraverseOrder:VertexAndOutgoingEdges array = null with get, set

        override x.Epilogue() =
            base.Epilogue()
            
            let n = x.QualifiedName
            this.Going <- TagE(cpu, this, $"Going_{n}", TagType.Going)
            this.Ready <- TagE(cpu, this, $"Ready_{n}", TagType.Ready)

            let ns = $"StartPlan_{n}"
            let nr = $"ResetPlan_{n}"
            let ne = $"EndPlan_{n}"
            this.TagPStart <- TagP(cpu, this, ns, TagType.Plan ||| TagType.Q ||| TagType.Start)
            this.TagPReset <- TagP(cpu, this, nr, TagType.Plan ||| TagType.Q ||| TagType.Reset)
            this.TagPEnd   <- TagP(cpu, this, ne, TagType.Plan ||| TagType.I ||| TagType.End)

            let (s, r, e) = x.Addresses
            if s <> null then
                this.TagAStart <- TagA(cpu, this, $"StartActual_{n}", s, TagType.External ||| TagType.Q ||| TagType.Start)
            if r <> null then
                this.TagAReset <- TagA(cpu, this, $"ResetActual_{n}", r, TagType.External ||| TagType.Q ||| TagType.Reset)
            if e <> null then
                this.TagAEnd   <- TagA(cpu, this, $"EndActual_{n}",   e, TagType.External ||| TagType.I ||| TagType.End)


            x.PortS <- PortInfoStart(cpu, x, $"StartPort_{n}", x.TagPStart, x.TagAStart)
            x.PortR <- PortInfoReset(cpu, x, $"ResetPort_{n}", x.TagPReset, x.TagAReset)
            x.PortE <- PortInfoEnd  (cpu, x, $"EndPort_{n}",   x.TagPEnd,   x.TagAEnd)

            // Graph 정보 추출 & 저장
            let gi = x.GraphInfo;
            x.Inits <- gi.Inits.OfType<Child>().ToArray();
            x.Lasts <- gi.Lasts.OfType<Child>().ToArray();
            x.TraverseOrder <- gi.TraverseOrders;

            x.PrintPortPlanTags();


        default x.WireEvent(writer, onError) =
            let mutable oldStatus:Status4 option = None
            let n = x.QualifiedName
            let write(bit, value, cause) =
                writer(BitChange(bit, value, cause, onError))

            Global.RawBitChangedSubject
                .Where(fun bc -> bc.Bit.IsOneOf(x.PortS, x.PortR, x.PortE))
                .Subscribe(fun bc ->
                    let state = x.Status
                    let bit = bc.Bit
                    let value = bc.NewValue
                    let cause = $"by bit change {bit.GetName()}={value}"
                    if oldStatus = Some state then
                        let msg = $"{n} status {state} duplicated on port {bit.GetName()}={value} by {cause}"
                        //assert(not bit.Value) // todo
                        // case1 : Reset port 켜지는 시점
                        // case2 : EndPort 꺼지는 시점에 : Reset port 는 아직 살아 있으므로 homing 
                        if bit = x.PortS then   // finish 도중에 start port 꺼져서 finish 완료되려는 시점
                            logDebug $"\t\tFinished homing: {msg}"
                        elif bit = x.PortR then
                            ()
                        elif bit = x.PortE then // reset 중에 end port 꺼져서 reset 완료 되려는 시점.  상태는 아직 homing 중
                            if state = Status4.Homing then
                                logDebug $"\t\tAbout to finished homing: [{n}] status : {state} {cause}"
                                assert(not x.TagPStart.Value)
                            elif state = Status4.Going then
                                logDebug $"\t\tAbout to finished going: [{n}] status : {state} {cause}"
                                assert(value || x.IsOriginating)
                                if x.IsOriginating then
                                    x.IsOriginating <- false
                            else
                                assert(value)
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
