namespace Engine.Runner


open Engine.Common.FS
open Engine.Core
open System
open System.Linq

[<AutoOpen>]
module VirtualParentSegmentModule =
    type VirtualParentSegment(target:Segment, causalSourceSegments:Segment seq
        , startPort, resetPort, endPort
        , goingTag, readyTag
        , targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
    ) as this =
        inherit FsSegmentBase(target.Cpu, $"VPS_{target.QualifiedName}"
            , $"VPS_{target.TagStart.Name}"
            , $"VPS_{target.TagReset.Name}"
            , $"VPS_{target.TagEnd.Name}"
            ) //, startPort, resetPort, endPort, goingTag, readyTag)

        let cpu = target.Cpu
        let mutable oldStatus:Status4 option = None

        do
            this.CreateSREGR(cpu, startPort, resetPort, endPort, goingTag, readyTag)

        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get
        member val TargetResetTag = targetResetTag with get

        static member Create(target:Segment, auto:IBit
            , (targetStartTag:IBit, targetResetTag:IBit)
            , causalSourceSegments:Segment seq
            , resetSourceSegments:Segment seq
        ) =
            let cpu = target.Cpu
            let n = $"VPS_{target.QualifiedName}"
            let readyTag = new Tag(cpu, null, $"{n}_Ready")

            let ep = PortInfoEnd.Create(cpu, null, $"End_{n}", null)

            let rp =
                (*
                    And(            // $"ResetAnd_{X}"
                        _auto
                        , targetEnd
                        ,FlipFlop(     // $"ResetFF_{X}"
                            And(    // $"InnerResetSourceAnd_{X}"
                                #(__X)
                                //, FlipFlop(#g(ResetSource1), #r(__X))
                                , FlipFlop(#g(ResetSource2), #r(__X)) )        // $"InnerResetSourceFF_{X}_{rsseg.Name}"
                            ,#r(__X)))
                *)
                let resetPortInfoPlan =
                    assert resetSourceSegments.Any()
                    let vrp =
                        [|
                            yield auto
                            //yield target.PortE  //ep
                            for rsseg in resetSourceSegments do
                                yield FlipFlop(cpu, $"InnerResetSourceFF_{n}_{rsseg.Name}", rsseg.Going, readyTag)  :> IBit
                        |]
                    And(cpu, $"ResetAnd_{n}", vrp)
                PortInfoReset(cpu, null, $"Reset_{n}", resetPortInfoPlan, null)

            let sp =
                (*
                    And(            // $"StartAnd_{X}"
                        _auto
                        ,ready
                        ,FlipFlop(     // $"StartFF_{X}"
                                #(__Prev)
                                ,#(__X.RsetPort)))
                *)
                let startPortInfoPlan =
                    assert causalSourceSegments.Any()
                    let vsp =
                        [|
                            yield auto
                            //yield readyTag
                            for csseg in causalSourceSegments do
                                yield FlipFlop(cpu, $"InnerStartSourceFF_{n}_{csseg.Name}", csseg.PortE, ep)// :> IBit
                        |]
                    And(cpu, $"StartAnd_{n}", vsp)

                PortInfoStart(cpu, null, $"Start_{n}", startPortInfoPlan, null)


            let vps = VirtualParentSegment(target, causalSourceSegments, sp, rp, ep, null, readyTag, targetStartTag, targetResetTag)
            sp.Segment <- vps
            rp.Segment <- vps
            ep.Segment <- vps

            vps

        override x.WireEvent(writer, onError) =
            let write(bit, value, cause) =
                writer(BitChange(bit, value, cause, onError))
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit :?> Bit
                    let on = bc.Bit.Value

                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                    let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                    let state = x.Status
                    let n = x.QualifiedName

                    //if notiVpsPortChange || notiTargetEndPortChange then
                    //    noop()

                    if notiTargetEndPortChange then
                        if x.Going.Value && state <> Status4.Going then
                            write(x.Going, false, $"{n} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            write(x.Ready, false, $"{n} ready off by status {state}")

                        let cause = $"${x.Target.Name} End Port={x.Target.PortE.Value}"


                        match state, on with
                        | Status4.Going, true ->
                            write(targetStartTag, false, $"{n} going 끝내기 by{cause}")
                            write(x.Going, false, $"{n} going 끝내기 by{cause}")
                            write(x.PortE, true, $"{n} FINISH 끝내기 by{cause}")
                            


                        | Status4.Homing, false ->
                            assert(x.Going.Value = false)
                            write(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            write(x.Ready, true, $"{x.Target.Name} homing 완료")
                            write(x.PortE, false, null)

                        | Status4.Ready, true ->
                            logInfo $"외부에서 내부 target {x.Target.Name} 실행 감지"

                        | _ ->
                            failwithlog $"Unknown: [{n}]{state}: Target endport => {x.Target.Name}={on}"


                    if notiVpsPortChange then
                        if oldStatus = Some state then
                            logDebug $"\t\tVPS Skipping duplicate status: [{n}] status : {state}"
                        else
                            oldStatus <- Some state
                            logInfo $"[{n}] Segment status : {state} by {bit.Name}={bit.Value}"
                            let childStatus = x.Target.Status

                            match state with
                            | Status4.Ready    ->
                                ()
                            | Status4.Going    ->
                                write(x.Going, true, $"{n} GOING 시작")

                                assert(childStatus = Status4.Ready || cpu.ProcessingQueue);
                                if childStatus = Status4.Ready then
                                    write(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                else
                                    async {
                                        // wait while target child available
                                        let mutable childStatus:Status4 option = None
                                        while childStatus <> Some Status4.Ready do
                                            // re-evaluate child status
                                            childStatus <- Some <| x.Target.Status
                                            logWarn $"Waiting target child [{x.Target.Name}] ready..from {childStatus.Value}"
                                            do! Async.Sleep(10);

                                        write(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                    } |> Async.Start

                            | Status4.Finished ->
                                write(targetStartTag, false, $"{n} FINISH 로 인한 {x.Target.Name} start 끄기")
                                write(x.Going, false, "${n} FINISH")
                                assert(x.PortE.Value)
                                assert(x.PortR.Value = false)

                            | Status4.Homing ->
                                if childStatus = Status4.Going then
                                    failwith $"Something bad happend?  trying to reset child while {x.Target.Name}={childStatus}"

                                write(targetResetTag, true, $"{n} HOMING 으로 인한 {x.Target.Name} reset 켜기")


                            | _ ->
                                failwith "Unexpected"
                        ()
                )

    let CreateVirtualParentSegmentsFromRootFlow(rootFlow: RootFlow) =
        let autoStart = rootFlow.Auto
        let allEdges = rootFlow.Edges.ToArray()
        let segments = rootFlow.RootSegments.Cast<Segment>()
        [|
            for target in segments do
                let es = allEdges.Where(fun e -> e.Target = target).ToArray()
                let setEdges = es.Where(fun e -> box e :? ISetEdge).ToArray()
                let resetEdges = es.Where(fun e -> box e :? IResetEdge).ToArray()
                assert(setEdges.Length = 0 || setEdges.Length = 1)
                assert(resetEdges.Length = 0 || resetEdges.Length = 1)

                let causalSources = setEdges.selectMany(fun e -> e.Sources).Cast<Segment>().ToArray()
                let resetSources = resetEdges.selectMany(fun e -> e.Sources).Cast<Segment>().ToArray()
                if causalSources.Any() && resetSources.Any() then
                    let vps = VirtualParentSegment.Create(target, autoStart, (target.TagStart, target.TagReset), causalSources, resetSources)
                    yield vps
                else
                    logWarn $"Do not create VPS for {target.QualifiedName}"
        |]
