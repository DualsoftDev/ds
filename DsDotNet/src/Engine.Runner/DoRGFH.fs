namespace Engine.Runner


open Dual.Common
open Engine.Core


[<AutoOpen>]
module DoRGFH =
    let rec goingChild (child:Child) =
        match child.Coin with
        | :? ExSegmentCall as extSeg ->
            //goingSegment extSeg
            ()
        | :? SubCall as call->
            //goingCall call
            ()
        | _ ->
            failwith "ERROR"

        child.TagsStart |> Seq.iter(fun t -> t.Value <- true)

    and private goingSegment (seg:Segment) =
        assert seg.PortS.Value

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
                if seg.IsChildrenOrigin() then
                    let map = seg.ChildStatusMap
                    let keys = map.Keys |> Array.ofSeq
                    for key in keys do
                        map[key] <- Status4.Ready

            let allReady = seg.IsChildrenStatusAllWith(Status4.Ready)
            let anyGoing = seg.IsChildrenStatusAnyWith(Status4.Going)
            if (allReady || anyGoing) then
                if allReady then
                    // do origin check
                    ()

                let v_oes = seg.TraverseOrder
                for ve in v_oes do
                    let child = ve.Vertex :?> Child
                    let es = ve.OutgoingEdges
                    match child.Status with
                    // child call 을 "잘" 시켜야 한다.
                    | Status4.Ready ->
                        goingChild child
                    | Status4.Going
                    | Status4.Finished ->
                        ()
                    | _ ->
                        failwith "ERROR"


            if (seg.IsChildrenStatusAnyWith(Status4.Homing)) then
                ()


    let private homing() = ()
    let private pause() = ()
    let private finish() = ()
    let private ready() = ()

    /// Port 값 변경에 따른 작업 수행
    let evaluatePort (port:Port) (newValue:bool) =
        if port.Value <> newValue then
            let seg = port.OwnerSegment
            let rf = seg.IsResetFirst
            let st = seg.Status

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
            | :? PortS, true , Status4.Ready -> goingSegment seg
            | :? PortS, false, Status4.Ready -> pause()
            | :? PortS, true,  Status4.Finished -> ()
            | :? PortS, false, Status4.Finished ->
                    if seg.PortR.Value then
                        homing()
            | :? PortR, true , Status4.Finished -> homing()
            | :? PortR, false, Status4.Finished -> pause()
            | :? PortR, true , Status4.Going -> homing()
            | :? PortR, false, Status4.Going -> pause()
            | :? PortR, true , Status4.Ready -> ()
            | :? PortR, false, Status4.Ready ->
                    if seg.PortS.Value then
                        goingSegment seg

            | :? PortE, true , Status4.Going -> finish()
            | :? PortE, false, Status4.Homing -> ready()

            | _ ->
                failwith "ERROR"

