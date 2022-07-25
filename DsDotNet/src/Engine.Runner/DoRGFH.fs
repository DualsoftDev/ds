namespace Engine.Runner


open Dual.Common
open Engine.Core
open System.Reactive.Linq
open System.Linq
open System.Threading.Tasks
open System
open System.Threading
open System.Reactive.Disposables

[<AutoOpen>]
module DoRGFH =
    let noop() = ()

    ///<summary> 모든 Children 을 origin 상태로 이동</summary>
    let moveChildrenToOrigin (seg:Segment) =
        // todo: children 이 origin 상태에 있는지 검사!!!
        logDebug $"Moving segment[{seg.QualifiedName}] children to origin."
        //while not <| seg.MovingCancellationTokenSource.IsCancellationRequested do
        //    ()
        true


    //let rec goingChild (child:Child) =
    //    let parent = child.Parent
    //    assert (parent.Status = Status4.Going)
    //    child.Status <- Status4.Going

    //    match child.Coin with
    //    | :? ExSegmentCall as extSeg ->
    //        //goingSegment extSeg
    //        ()
    //    | :? SubCall as call->
    //        //goingCall call
    //        ()
    //    | _ ->
    //        failwith "ERROR"

    //    let tcs = new TaskCompletionSource<bool>();     // https://rehansaeed.com/reactive-extensions-part5-awaiting-observables/
    //    let mutable subs:IDisposable = null
    //    subs <-
    //        Global.BitChangedSubject
    //            .Subscribe( fun bc ->
    //                if parent.MovingCancellationTokenSource.IsCancellationRequested then
    //                    subs.Dispose()
    //                    tcs.SetResult(false)
    //                else
    //                    match bc.Bit with
    //                    | :? Tag as tag ->
    //                        if child.TagsEnd |> Seq.contains(tag)
    //                            && child.TagsEnd |> Seq.forall(fun et -> et.Value)
    //                        then
    //                            subs.Dispose();
    //                            tcs.SetResult(true)
    //                    | _ ->
    //                        ()
    //            )
    //    child.TagsStart |> Seq.iter(fun t -> t.Value <- true)
    //    tcs.Task

    let private goingSegment (seg:Segment) =
        assert seg.PortS.Value
        assert (seg.Status = Status4.Going) // PortS ON 시, 이미 Going 상태
        assert isNull seg.MovingCancellationTokenSource

        logDebug $"Segment Going: {seg.QualifiedName}"

        let goingCancel = new CancellationTokenSource()
        seg.MovingCancellationTokenSource <- goingCancel
        use _goingCancelSubscription =
            // going 중에 start port 꺼지거나, reset port 켜질 때에는 going 중단.
            Global.BitChangedSubject
                .Where(fun bc ->
                    (bc.Bit = seg.PortS && bc.NewValue = false)
                    || (bc.Bit = seg.PortR && bc.NewValue = true))
                .Subscribe(fun _ ->
                    seg.CancelGoing())
                ;

        // 1. Ready 상태에서의 clean start
        // 2. Going pause (==> Ready 로 해석) 상태에서의 resume start

        let allFinished = seg.IsChildrenStatusAllWith(Status4.Finished)
        if allFinished then
            seg.PortE.Value <- true
            assert(seg.Status = Status4.Finished)
        else
            let anyHoming = seg.IsChildrenStatusAnyWith(Status4.Homing)
            if anyHoming then
                assert(seg.IsChildrenStatusAllWith(Status4.Homing))      // 하나라도 homing 이면, 모두 homing
                if moveChildrenToOrigin seg then
                    let map = seg.ChildStatusMap
                    let keys = map.Keys |> Array.ofSeq
                    for key in keys do
                        map[key] <- Status4.Ready
                else
                    noop()

            let allReady = seg.IsChildrenStatusAllWith(Status4.Ready)
            let anyGoing = seg.IsChildrenStatusAnyWith(Status4.Going)
            if (allReady || anyGoing) then
                if allReady then
                    // do origin check
                    ()

                let endTag2ChildMap =
                    seg.Children
                    |> Seq.bind(fun c -> c.TagsEnd.Select(fun et -> et, c))
                    |> Tuple.toDictionary

                let keepGoingFrom (child:Child) =
                    let goingChild (child:Child) =
                        logDebug $"Child [{child.QualifiedName}] going.."
                        //! child call 을 "잘" 시켜야 한다.
                        let parent = child.Parent
                        assert (parent.Status = Status4.Going)
                        child.Status <- Status4.Going
                        child.TagsStart |> Seq.iter(fun t -> t.Value <- true)

                    let getNextChildren (seed:Child) =
                        if isNull seed then
                            seg.GraphInfo.Inits
                        else
                            seg.GraphInfo.Graph.OutEdges(seed) |> Seq.map(fun e -> e.Target) |> Array.ofSeq
                        |> Enumerable.OfType<Child>


                    for next in getNextChildren child do
                        goingChild next


                use _childFinishedSubscription =
                    // child 들의 end tag 들 중에 하나라도 ON 으로 변경되면...
                    let xx =
                        Global.BitChangedSubject
                            .Where(fun bc ->
                                match bc.Bit with
                                | :? Tag as tag ->
                                    endTag2ChildMap.ContainsKey(tag)
                                | _ -> false )
                            .Subscribe(fun bc ->
                                let child = endTag2ChildMap[bc.Bit :?> Tag]

                                assert (child.Status = Status4.Going)
                                if child.Status = Status4.Going then
                                    logDebug $"Detected child [{child.QualifiedName}] finished"
                                    child.Status <- Status4.Finished
                                    keepGoingFrom child )
                    Disposable.Empty

                keepGoingFrom null


                //let pickVictims (seed:Child) =
                //    let gi = seg.GraphInfo
                //    let g = gi.Graph
                //    match seed with
                //    | null ->
                //        gi.Inits
                //    | seed ->
                //        g.OutEdges(seed)
                //        |> Seq.map(fun e -> e.Target)
                //        |> Array.ofSeq
                //    |> Enumerable.OfType<Child>
                //    |> Seq.filter(fun child ->
                //        match child.Status with
                //        | Status4.Going
                //        | Status4.Homing
                //            -> failwith "Check me"
                //        | _ -> ()
                //        true)
                //    |> Seq.filter(fun child -> child.Status = Status4.Ready)
                //    |> Array.ofSeq

                //let mutable victims = pickVictims null


                //let rec makeAllChildGo (seeds:Child seq) : Task<bool> =
                //    task {
                //        let mutable result = true
                //        if goingCancel.IsCancellationRequested then
                //            result <- false
                //        else
                //            let xxx =
                //            seeds
                //            |> Seq.map(fun seed -> goingChild seed)
                //            |> Task.WhenAll()
                //            ;
                //            |>
                //            for seed in seeds do
                //                if result then
                //                    let! res = goingChild seed
                //                    result <- res
                //                    if result then
                //                        let! res = pickVictims seed |> makeAllChildGo
                //                        result <- res
                //        return result
                //    }


                //pickVictims null |> makeAllChildGo

                //    //seeds |> Seq.map goingChild |> Task.WhenAll



                //let v_oes = seg.TraverseOrder
                //for ve in v_oes do
                //    let child = ve.Vertex :?> Child
                //    let es = ve.OutgoingEdges
                //    match child.Status with
                //    //! child call 을 "잘" 시켜야 한다.
                //    | Status4.Ready ->
                //        goingChild child
                //    | Status4.Going
                //    | Status4.Finished ->
                //        ()
                //    | _ ->
                //        failwith "ERROR"


            if (seg.IsChildrenStatusAnyWith(Status4.Homing)) then
                ()


    let private homing() = ()
    let private pauseSegment (seg:Segment) = ()
    let private finish() = ()
    let private ready() = ()

    /// Port 값 변경에 따른 작업 수행
    let evaluatePort (port:Port) (newValue:bool) =
        if port.Value <> newValue then
            let seg = port.OwnerSegment
            let rf = seg.IsResetFirst
            let st = seg.Status

            logDebug $"Evaluating port [{port.QualifiedName}]={newValue} with {st}"

            // start port 와 reset port 동시 눌림
            let duplicate =
                newValue &&
                    match port with
                    | :? PortS when seg.PortR.Value -> true
                    | :? PortR when seg.PortS.Value -> true
                    | _ -> false

            // 동시 눌림을 고려한, 실제 동작해야 할 port
            let mutable effectivePort = port
            if duplicate then
                effectivePort <- if rf then seg.PortR :> Port else seg.PortS

            effectivePort.Value <- newValue
            match effectivePort, newValue, st with
            | :? PortS, true , Status4.Ready ->
                goingSegment seg
            | :? PortS, false, Status4.Ready -> pauseSegment seg
            | :? PortS, true,  Status4.Finished -> noop()
            | :? PortS, false, Status4.Finished ->
                    if seg.PortR.Value then
                        homing()
            | :? PortR, true , Status4.Finished -> homing()
            | :? PortR, false, Status4.Finished -> pauseSegment seg
            | :? PortR, true , Status4.Going -> homing()
            | :? PortR, false, Status4.Going -> pauseSegment seg
            | :? PortR, true , Status4.Ready -> noop()
            | :? PortR, false, Status4.Ready ->
                    if seg.PortS.Value then
                        goingSegment seg

            | :? PortE, true , Status4.Going -> finish()
            | :? PortE, false, Status4.Homing -> ready()

            | _ ->
                failwith "ERROR"

