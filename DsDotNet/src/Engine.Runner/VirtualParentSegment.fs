namespace Engine.Runner


open Engine.Common.FS
open Engine.Core
open System
open System.Linq
open System.Web.Configuration

[<AutoOpen>]
module VirtualParentSegmentModule =
    type VirtualParentSegment(target:Segment
        , causalSourceSegments:Segment seq
        , resetSourceSegments:Segment seq
        , startPort, resetPort, endPort
        , goingTag, readyTag
    ) as this =
        inherit FsSegmentBase(target.Cpu, $"VPS_{target.QualifiedName}"
            , $"VPS_{target.TagStart.Name}"
            , $"VPS_{target.TagReset.Name}"
            , $"VPS_{target.TagEnd.Name}"
            )

        let cpu = target.Cpu
        let mutable oldStatus:Status4 option = None
        let triggerTargetStart = causalSourceSegments.Any()
        let triggerTargetReset = resetSourceSegments.Any()
        let targetStartTag = target.TagStart
        let targetResetTag = target.TagReset

        do
            this.CreateSREGR(cpu, startPort, resetPort, endPort, goingTag, readyTag)

        member val Target = target;

        /// target 에 대한 가상 부모 생성
        /// 가상부모 StartPort:
        ///     ON: target 으로 들어오는 모든 set incoming 에 대해서 finish rising 이 되었을 때
        ///     OFF: 가상부모가 finish 되었을 때
        /// 가상부모 ResetPort:
        ///     ON: 가상 부모가 ready 상태에서 target 으로 들어오는 모든 reset incoming 에 대해서,  going rising 되었을 때
        ///     OFF: 가상부모가 ready 되었을 때
        /// - 가상 부모는 plan 만 존재하고, actual 이 없음
        static member Create(target:Segment, auto:IBit
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
                        ,FlipFlop(     // $"ResetFF_{X}"
                            And(    // $"InnerResetSourceAnd_{X}"
                                #(__X)
                                //, FlipFlop(#g(ResetSource1), #r(__X))
                                , FlipFlop(#g(ResetSource2), #r(__X)) )        // $"InnerResetSourceFF_{X}_{rsseg.Name}"
                            ,#r(__X)))
                *)
                let resetPortInfoPlan =
                    let vrp =
                        [|
                            yield auto
                            if resetSourceSegments.Any() then
                                for rsseg in resetSourceSegments do
                                    yield FlipFlop(cpu, $"InnerResetSourceFF_{n}_{rsseg.Name}", rsseg.Going, readyTag)  :> IBit
                            else
                                // self reset.  실제 reset 시 알맹이의 reset 은 수행하지 말아야 한다.
                                yield ep
                        |]
                    And(cpu, $"ResetAnd_{n}", vrp)
                PortInfoReset(cpu, null, $"Reset_{n}", resetPortInfoPlan, null)

            let sp =
                (*
                    And(            // $"StartAnd_{X}"
                        _auto
                        ,FlipFlop(     // $"StartFF_{X}"
                                #(__Prev)
                                ,#(__X.RsetPort)))
                *)
                let startPortInfoPlan =
                    //assert causalSourceSegments.Any()
                    let vsp =
                        [|
                            yield auto
                            for csseg in causalSourceSegments do
                                yield FlipFlop(cpu, $"InnerStartSourceFF_{n}_{csseg.Name}", csseg.PortE, ep)// :> IBit
                        |]
                    And(cpu, $"StartAnd_{n}", vsp)

                PortInfoStart(cpu, null, $"Start_{n}", startPortInfoPlan, null)


            let vps = VirtualParentSegment(target, causalSourceSegments, resetSourceSegments, sp, rp, ep, null, readyTag)
            sp.Segment <- vps
            rp.Segment <- vps
            ep.Segment <- vps

            vps

        override x.WireEvent(writer, onError) =
            let write:BitWriter = getBitWriter writer onError
            let writeEndPort = getEndPortPlanWriter writer onError

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
                            writeEndPort(x.PortE, true, $"{n} FINISH 끝내기 by{cause}")
                            


                        | Status4.Homing, false ->
                            assert(not targetStartTag.Value)
                            //assert(x.Going.Value = false) // 아직 write 안되었을 수도 있음
                            write(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            write(x.Ready, true, $"{x.Target.Name} homing 완료")
                            writeEndPort(x.PortE, false, null)

                        | Status4.Ready, true ->
                            logInfo $"외부에서 내부 target {x.Target.Name} 실행 감지"

                        | _ ->
                            failwithlog $"Unknown: [{n}]{state}: Target endport => {x.Target.Name}={on}"


                    if notiVpsPortChange then
                        if oldStatus = Some state then
                            assert(not bit.Value)
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
                                if triggerTargetStart then
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
                                write(targetStartTag, false, $"{n} FINISH 로 인한 {x.Target.Name} start {targetStartTag.GetName()} 끄기")
                                write(x.Going, false, "${n} FINISH")
                                assert(x.PortE.Value)
                                assert(x.PortR.Value = false)

                            | Status4.Homing ->
                                if triggerTargetReset then
                                    assert(not targetStartTag.Value)        // 일반적으로... 
                                    if childStatus = Status4.Going then
                                        failwith $"Something bad happend?  trying to reset child while {x.Target.Name}={childStatus}"

                                    write(targetResetTag, true, $"{n} HOMING 으로 인한 {x.Target.Name} reset 켜기")


                            | _ ->
                                failwith "Unexpected"
                        ()
                )

    let CreateVirtualParentSegmentsFromRootFlow(rootFlow: RootFlow) =
        let auto = rootFlow.Auto
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

                yield VirtualParentSegment.Create(target, auto, causalSources, resetSources)
        |]
