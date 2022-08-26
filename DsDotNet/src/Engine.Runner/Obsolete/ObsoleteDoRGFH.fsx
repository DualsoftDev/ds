namespace Engine.Runner


//open Engine.Common.FS
//open Engine.Core
//open System.Reactive.Linq
//open System.Linq
//open System
//open System.Threading

//[<AutoOpen>]
//module DoRGFH =
//    ///<summary> 모든 Children 을 origin 상태로 이동</summary>
//    let moveChildrenToOrigin (seg:SegmentBase) =
//        // to do: children 이 origin 상태에 있는지 검사!!!
//        logDebug $"Moving segment[{seg.QualifiedName}] children to origin."
//        //while not <| seg.MovingCancellationTokenSource.IsCancellationRequested do
//        //    ()
//        true


//    //let rec goingChild (child:Child) =
//    //    let parent = child.Parent
//    //    assert (parent.Status = Status4.Going)
//    //    child.Status <- Status4.Going

//    //    match child.Coin with
//    //    | :? ExSegmentCall as extSeg ->
//    //        //goingSegment extSeg
//    //        ()
//    //    | :? SubCall as call->
//    //        //goingCall call
//    //        ()
//    //    | _ ->
//    //        failwith "ERROR"

//    //    let tcs = new TaskCompletionSource<bool>();     // https://rehansaeed.com/reactive-extensions-part5-awaiting-observables/
//    //    let mutable subs:IDisposable = null
//    //    subs <-
//    //        Global.BitChangedSubject
//    //            .Subscribe( fun bc ->
//    //                if parent.MovingCancellationTokenSource.IsCancellationRequested then
//    //                    subs.Dispose()
//    //                    tcs.SetResult(false)
//    //                else
//    //                    match bc.Bit with
//    //                    | :? Tag as tag ->
//    //                        if child.TagsEnd |> Seq.contains(tag)
//    //                            && child.TagsEnd |> Seq.forall(fun et -> et.Value)
//    //                        then
//    //                            subs.Dispose();
//    //                            tcs.SetResult(true)
//    //                    | _ ->
//    //                        ()
//    //            )
//    //    child.TagsStart |> Seq.iter(fun t -> t.Value <- true)
//    //    tcs.Task

//#if false
//    let private goingSegment (seg:Segment) =
//        assert false
//        assert seg.PortS.Value
//        assert (seg.Status = Status4.Going) // PortS ON 시, 이미 Going 상태
//        assert isNull seg.MovingCancellationTokenSource

//        logDebug $"GOING segment: {seg.QualifiedName}"

//        let goingCancel = new CancellationTokenSource()
//        seg.MovingCancellationTokenSource <- goingCancel
//        use _goingCancelSubscription =
//            // going 중에 start port 꺼지거나, reset port 켜질 때에는 going 중단.
//            Global.BitChangedSubject
//                .Where(fun bc ->
//                    (bc.Bit = seg.PortS && bc.NewValue = false)
//                    || (bc.Bit = seg.PortR && bc.NewValue = true))
//                .Subscribe(fun _ ->
//                    seg.CancelGoing())
//                ;

//        let checkAllChildrenFinished() =
//            let allFinished = seg.IsChildrenStatusAllWith(Status4.Finished)
//            if allFinished then
//                logDebug $"FINISHING segment [{seg.QualifiedName}]."
//                seg.PortE.SetValue(true)
//                assert(seg.Status = Status4.Finished)
//            allFinished

//        // 1. Ready 상태에서의 clean start
//        // 2. Going pause (==> Ready 로 해석) 상태에서의 resume start

//        if seg.Children.IsEmpty() then
//            seg.Going.SetValue(true)
//            assert(seg.Status = Status4.Going)
//            seg.PortE.SetValue(true)
//            //assert(seg.Status = Status4.Finished) || Status4.Ready???
//            ()
//        elif not <| checkAllChildrenFinished() then
//            let anyHoming = seg.IsChildrenStatusAnyWith(Status4.Homing)
//            if anyHoming then
//                assert(seg.IsChildrenStatusAllWith(Status4.Homing))      // 하나라도 homing 이면, 모두 homing
//                if moveChildrenToOrigin seg then
//                    let map = seg.ChildStatusMap
//                    let keys = map.Keys |> Array.ofSeq
//                    for key in keys do
//                        map[key] <- (false, Status4.Ready)
//                else
//                    noop()

//            let allReady = seg.IsChildrenStatusAllWith(Status4.Ready)
//            let anyGoing = seg.IsChildrenStatusAnyWith(Status4.Going)
//            if (allReady || anyGoing) then
//                if allReady then
//                    // do origin check
//                    ()

//                let endTag2ChildMap =
//                    seg.Children
//                    |> Seq.bind(fun c -> c.TagsEnd.Select(fun et -> et, c))
//                    |> Tuple.toDictionary

//                let keepGoingFrom (child:Child) =
//                    let goingChild (child:Child) =
//                        logDebug $"Child Going: [{child.QualifiedName}]"
//                        //! child call 을 "잘" 시켜야 한다.
//                        let parent = child.Parent
//                        match parent.Status with
//                        | Status4.Going ->
//                            child.Status <- Status4.Going
//                            child.TagsStart |> Seq.iter(fun t -> t.SetValue(true))
//                        | Status4.Finished ->
//                            assert (child.Status = Status4.Finished)
//                        | _ ->
//                            failwith "Unexpected"

//                    let getNextChildren (seed:Child) =
//                        if isNull seed then
//                            seg.GraphInfo.Inits
//                        else
//                            seg.GraphInfo.Graph.OutEdges(seed) |> Seq.map(fun e -> e.Target) |> Array.ofSeq
//                        |> Enumerable.OfType<Child>


//                    for next in getNextChildren child do
//                        goingChild next

//                    checkAllChildrenFinished()


//                use _childFinishedSubscription =
//                    // child 들의 end tag 들 중에 하나라도 ON 으로 변경되면...
//                    Global.BitChangedSubject
//                        .Where(fun bc ->
//                            match bc.Bit with
//                            | :? Tag as tag ->
//                                endTag2ChildMap.ContainsKey(tag)
//                            | _ -> false )
//                        .Subscribe(fun bc ->
//                            let child = endTag2ChildMap[bc.Bit :?> Tag]
//                            assert (child.Status = Status4.Going || child.Status = Status4.Finished)
//                            if child.Status = Status4.Finished
//                                || (child.Status = Status4.Going && child.TagsEnd.All(fun t -> t.Value))
//                            then
//                                logDebug $"FINISHING child [{child.QualifiedName}] detected"
//                                child.Status <- Status4.Finished
//                                keepGoingFrom child |> ignore)


//                seg.Going.SetValue(true)
//                keepGoingFrom null |> ignore


//                //let pickVictims (seed:Child) =
//                //    let gi = seg.GraphInfo
//                //    let g = gi.Graph
//                //    match seed with
//                //    | null ->
//                //        gi.Inits
//                //    | seed ->
//                //        g.OutEdges(seed)
//                //        |> Seq.map(fun e -> e.Target)
//                //        |> Array.ofSeq
//                //    |> Enumerable.OfType<Child>
//                //    |> Seq.filter(fun child ->
//                //        match child.Status with
//                //        | Status4.Going
//                //        | Status4.Homing
//                //            -> failwith "Check me"
//                //        | _ -> ()
//                //        true)
//                //    |> Seq.filter(fun child -> child.Status = Status4.Ready)
//                //    |> Array.ofSeq

//                //let mutable victims = pickVictims null


//                //let rec makeAllChildGo (seeds:Child seq) : Task<bool> =
//                //    task {
//                //        let mutable result = true
//                //        if goingCancel.IsCancellationRequested then
//                //            result <- false
//                //        else
//                //            let xxx =
//                //            seeds
//                //            |> Seq.map(fun seed -> goingChild seed)
//                //            |> Task.WhenAll()
//                //            ;
//                //            |>
//                //            for seed in seeds do
//                //                if result then
//                //                    let! res = goingChild seed
//                //                    result <- res
//                //                    if result then
//                //                        let! res = pickVictims seed |> makeAllChildGo
//                //                        result <- res
//                //        return result
//                //    }


//                //pickVictims null |> makeAllChildGo

//                //    //seeds |> Seq.map goingChild |> Task.WhenAll



//                //let v_oes = seg.TraverseOrder
//                //for ve in v_oes do
//                //    let child = ve.Vertex :?> Child
//                //    let es = ve.OutgoingEdges
//                //    match child.Status with
//                //    //! child call 을 "잘" 시켜야 한다.
//                //    | Status4.Ready ->
//                //        goingChild child
//                //    | Status4.Going
//                //    | Status4.Finished ->
//                //        ()
//                //    | _ ->
//                //        failwith "ERROR"


//            if (seg.IsChildrenStatusAnyWith(Status4.Homing)) then
//                ()


//    let private homing (seg:Segment) =
//        logDebug $"HOMING segment [{seg.QualifiedName}]."
//        seg.PortE.SetValue(false)

//    let private pauseSegment (seg:Segment) =
//        logDebug $"Pausing segment [{seg.QualifiedName}]."
//        ()
//    let private finish (seg:Segment) =
//        logDebug $"FINISHING segment [{seg.QualifiedName}]."
//        seg.PortS.SetValue(false)
//    let private ready (seg:Segment) =
//        logDebug $"READY segment [{seg.QualifiedName}]."
//        ()

//    /// Port 값 변경에 따른 작업 수행
//    let evaluatePort (port:PortInfo) (newValue:bool) =
//        assert false
//        if port.Value <> newValue then
//            let seg = port.Segment
//            let rf = seg.IsResetFirst
//            let st = seg.Status

//            if port :? PortInfoReset then
//                noop()

//            logDebug $"\tEvaluating port [{port.QualifiedName}]={newValue} with {st}"

//            // start port 와 reset port 동시 눌림
//            let duplicate =
//                newValue &&
//                    match port with
//                    | :? PortInfoStart when seg.PortR.Value -> true
//                    | :? PortInfoReset when seg.PortS.Value -> true
//                    | _ -> false

//            // 동시 눌림을 고려한, 실제 동작해야 할 port
//            let mutable effectivePort = port
//            if duplicate then
//                effectivePort <- if rf then seg.PortR :> PortInfo else seg.PortS

//            effectivePort.SetValue(newValue)
//            match effectivePort, newValue, st with
//            | :? PortInfoStart, true , Status4.Ready ->
//                goingSegment seg
//            | :? PortInfoStart, false, Status4.Ready -> pauseSegment seg
//            | :? PortInfoStart, true,  Status4.Finished ->
//                seg.PortS.SetValue(false)
//            | :? PortInfoStart, false, Status4.Finished ->
//                    if seg.PortR.Value then
//                        homing seg
//            | :? PortInfoReset, true , Status4.Finished -> homing seg
//            | :? PortInfoReset, false, Status4.Finished -> pauseSegment seg
//            | :? PortInfoReset, true , Status4.Going -> homing seg
//            | :? PortInfoReset, false, Status4.Going -> pauseSegment seg
//            | :? PortInfoReset, true , Status4.Ready ->
//                // if seg is in origin state, then, turn off reset port
//                logDebug $"\tSkip homing due to segment [{seg.QualifiedName}] already ready state."
//                seg.PortR.SetValue(false)
//            | :? PortInfoReset, false, Status4.Ready ->
//                    if seg.PortS.Value then
//                        goingSegment seg

//            | :? PortInfoEnd, true , Status4.Going -> finish seg
//            | :? PortInfoEnd, false, Status4.Homing -> ready seg

//            | _ ->
//                failwith "ERROR"

//#endif