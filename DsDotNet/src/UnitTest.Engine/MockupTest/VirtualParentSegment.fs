namespace UnitTest.Mockup.Engine


open Engine.Core
open Dual.Common
open Engine.Runner


[<AutoOpen>]
module VirtualParentSegment =

    /// Virtual Parent Segment
    /// endport 는 target child 의 endport 공유
    /// startPort 는 공정 auto
    /// resetPort = { auto &&
    ///     #latch( (Self
    ///                 && #latch(#g(Previous), #r(Self)  <--- reset 조건 1
    ///                 && #latch(#g(Next), #r(Self)),    <--- reset 조건 2 ...
    ///             #r(Self))
    type Vps(name, target:MockupSegment, causalSourceSegments:MockupSegment seq
        , startPort, resetPort, endPort
        , goingTag, readyTag
        , targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
    ) =
        inherit MockupSegmentBase(target.Cpu, name, startPort, resetPort, endPort, goingTag, readyTag)
        let cpu = target.Cpu
        let mutable oldStatus:Status4 option = None

        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get
        member val TargetResetTag = targetResetTag with get

        static member Create(target:MockupSegment, auto:IBit
            , (targetStartTag:IBit, targetResetTag:IBit)
            , causalSourceSegments:MockupSegment seq
            , resetSourceSegments:MockupSegment seq
        ) =
            let cpu = target.Cpu
            let n = $"VPS_{target.Name}"

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
                    let vrp =
                        [|
                            yield auto
                            yield target.PortE  //ep
                            let set =
                                let andItems = [|
                                    for rsseg in resetSourceSegments do
                                        yield FlipFlop(cpu, $"InnerResetSourceFF_{n}_{rsseg.Name}", rsseg.Going, readyTag)  :> IBit
                                |]
                                And(cpu, $"InnerResetSourceAnd_{n}", andItems)
                            yield FlipFlop(cpu, $"ResetFF_{n}", set, readyTag)
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
                    let vsp =
                        [|
                            yield auto
                            yield readyTag
                            for csseg in causalSourceSegments do
                                yield FlipFlop(cpu, $"InnerStartSourceFF_{n}_{csseg.Name}", csseg.PortE, ep)// :> IBit
                        |]
                    And(cpu, $"StartAnd_{n}", vsp)

                PortInfoStart(cpu, null, $"Start_{n}", startPortInfoPlan, null)


            let vps = Vps(n, target, causalSourceSegments, sp, rp, ep, null, readyTag, targetStartTag, targetResetTag)
            sp.Segment <- vps
            rp.Segment <- vps
            ep.Segment <- vps

            vps

        override x.WireEvent() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit :?> Bit
                    let on = bc.Bit.Value

                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                    let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                    let state = x.GetSegmentStatus()

                    //if notiVpsPortChange || notiTargetEndPortChange then
                    //    noop()

                    if notiTargetEndPortChange then
                        if x.Going.Value && state <> Status4.Going then
                            cpu.Enqueue(x.Going, false, $"{x.Name} going off by status {state}")
                        if x.Ready.Value && state <> Status4.Ready then
                            cpu.Enqueue(x.Ready, false, $"{x.Name} ready off by status {state}")

                        let cause = $"${x.Target.Name} End Port={x.Target.PortE.Value}"


                        match state, on with
                        | Status4.Going, true ->
                            cpu.Enqueue(targetStartTag, false, $"{x.Name} going 끝내기 by{cause}")
                            cpu.Enqueue(x.Going, false, $"{x.Name} going 끝내기 by{cause}")
                            cpu.Enqueue(x.PortE, true, $"{x.Name} FINISH 끝내기 by{cause}")


                        | Status4.Homing, false ->
                            assert(x.Going.Value = false)
                            cpu.Enqueue(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            cpu.Enqueue(x.Ready, true)
                            cpu.Enqueue(x.PortE, false)

                        | Status4.Ready, true ->
                            logInfo $"외부에서 내부 target {x.Target.Name} 실행 감지"

                        | _ ->
                            failwithlog $"Unknown: [{x.Name}]{state}: Target endport => {x.Target.Name}={on}"


                    if notiVpsPortChange then
                        if oldStatus = Some state then
                            logDebug $"\t\tVPS Skipping duplicate status: [{x.Name}] status : {state}"
                        else
                            oldStatus <- Some state
                            logDebug $"[{x.Name}] Segment status : {state} by {bit.Name}={bit.Value}"

                            match state with
                            | Status4.Ready    ->
                                ()
                            | Status4.Going    ->
                                let childStatus = x.Target.GetSegmentStatus()
                                cpu.Enqueue(x.Going, true, $"{name} GOING 시작")

                                assert(childStatus = Status4.Ready || cpu.ProcessingQueue);
                                if childStatus = Status4.Ready then
                                    cpu.Enqueue(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                else
                                    async {
                                        // wait while target child available
                                        let mutable childStatus:Status4 option = None
                                        while childStatus <> Some Status4.Ready do
                                            // re-evaluate child status
                                            childStatus <- Some <| x.Target.GetSegmentStatus()
                                            logWarn $"Waiting target child [{x.Target.Name}] ready..from {childStatus.Value}"
                                            do! Async.Sleep(10);

                                        cpu.Enqueue(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                    } |> Async.Start

                            | Status4.Finished ->
                                cpu.Enqueue(targetStartTag, false, $"{x.Name} FINISH 로 인한 {x.Target.Name} start 끄기")
                                cpu.Enqueue(x.Going, false)
                                x.FinishCount <- x.FinishCount + 1
                                assert(x.PortE.Value)
                                assert(x.PortR.Value = false)

                            | Status4.Homing   ->
                                assert(x.Target.GetSegmentStatus() = Status4.Finished)
                                assert(x.Target.PortE.Value)
                                cpu.Enqueue(targetResetTag, true, $"{x.Name} HOMING 으로 인한 {x.Target.Name} reset 켜기")
                            | _ ->
                                failwith "Unexpected"
                        ()
                )
