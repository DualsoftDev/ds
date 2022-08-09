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
    type Vps(name, target:MuSegment, causalSourceSegments:MuSegment seq
        , startPort, resetPort, endPort
        , goingTag, readyTag
        , targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
    ) =
        inherit MuSegmentBase(target.Cpu, name, startPort, resetPort, endPort, goingTag, readyTag)
        let cpu = target.Cpu
        let mutable oldStatus:Status4 option = None

        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get
        member val TargetResetTag = targetResetTag with get

        static member Create(target:MuSegment, auto:IBit
            , (targetStartTag:IBit, targetResetTag:IBit)
            , causalSourceSegments:MuSegment seq
            , resetSourceSegments:MuSegment seq
        ) =
            let cpu = target.Cpu
            let n = $"VPS_{target.Name}"

            let readyTag = new Tag(cpu, null, $"{n}_Ready")

            let ep = PortInfoEnd.Create(cpu, null, $"End_{n}", null)

            let rp =
                (*
                    And(            // $"ResetPortInfo_{X}"
                        _auto
                        ,Latch(     // $"ResetLatch_{X}"
                            And(    // $"InnerResetSourceAnd_{X}"
                                #(__X)
                                //, Latch(#g(ResetSource1), #r(__X))
                                , latch(#g(ResetSource2), #r(__X)) )        // $"InnerResetSourceLatch_{X}_{rsseg.Name}"
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
                                        yield FlipFlop(cpu, $"InnerResetSourceLatch_{n}_{rsseg.Name}", rsseg.Going, readyTag)  :> IBit
                                |]
                                And(cpu, $"InnerResetSourceAnd_{n}", andItems)
                            yield FlipFlop(cpu, $"ResetLatch_{n}", set, readyTag)
                        |]
                    And(cpu, $"ResetPortInfo_{n}", vrp)
                PortInfoReset(cpu, null, $"Reset_{n}", resetPortInfoPlan, null)

            let sp =
                (*
                    And(            // $"StartPortInfo_{X}"
                        _auto
                        ,Latch(     // $"StartLatch_{X}"
                                #(__Prev)
                                ,#(__X.RsetPort)))
                *)
                let startPortInfoPlan =
                    let vsp =
                        [|
                            yield auto
                            yield readyTag :> IBit
                            for csseg in causalSourceSegments do
                                yield FlipFlop(cpu, $"InnerStartSourceLatch_{n}_{csseg.Name}", csseg.PortE, ep)// :> IBit
                        |]
                    And(cpu, $"StartPortInfo_{n}", vsp)

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
                    let ep = if bc.Bit :? PortInfoEnd then bc.Bit :?> PortInfoEnd else null
                    let on = bc.Bit.Value

                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                    let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                    let newVpsState = x.GetSegmentStatus()

                    //if notiVpsPortChange || notiTargetEndPortChange then
                    //    noop()

                    if notiTargetEndPortChange then
                        if x.Going.Value && newVpsState <> Status4.Going then
                            cpu.Enqueue(x.Going, false, $"{x.Name} going off by status {newVpsState}")
                        if x.Ready.Value && newVpsState <> Status4.Ready then
                            cpu.Enqueue(x.Ready, false, $"{x.Name} ready off by status {newVpsState}")

                        let cause = $"${x.Target.Name} End Port={x.Target.PortE.Value}"


                        match newVpsState, on with
                        | Status4.Finished, true ->
                            assert(false)
                        | Status4.Going, true ->
                            cpu.Enqueue(targetStartTag, false, $"{x.Name} going 끝내기 by{cause}")
                            cpu.Enqueue(x.Going, false, $"{x.Name} going 끝내기 by{cause}")
                            cpu.Enqueue(x.PortE, true, $"{x.Name} FINISH 끝내기 by{cause}")

                        | Status4.Ready, false ->
                            assert(false)
                        | Status4.Ready, true ->    // 외부에서 내부 target 을 실행한 경우
                            ()
                        | Status4.Homing, false ->
                            assert(x.Going.Value = false)
                            cpu.Enqueue(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            cpu.Enqueue(x.Ready, true)
                            cpu.Enqueue(x.PortE, false)
                        | _ ->
                            failwithlog $"Unknown: [{x.Name}]{newVpsState}: Target endport => {x.Target.Name}={on}"


                    if notiVpsPortChange then
                        if x.Name = "VPS_B" then
                            noop()
                        if oldStatus = Some newVpsState then
                            logDebug $"\t\tVPS Skipping duplicate status: [{x.Name}] status : {newVpsState}"
                        else
                            oldStatus <- Some newVpsState
                            logDebug $"[{x.Name}] Segment status : {newVpsState} by {bit.Name}={bit.Value}"

                            match newVpsState with
                            | Status4.Ready    ->
                                ()
                            | Status4.Going    ->
                                let targetChildStatus = x.Target.GetSegmentStatus()
                                cpu.Enqueue(x.Going, true, $"{name} GOING 시작")

                                assert(targetChildStatus = Status4.Ready || cpu.ProcessingQueue);
                                if targetChildStatus = Status4.Ready then
                                    cpu.Enqueue(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                else
                                    async {
                                        // wait while target child available
                                        let mutable targetChildStatus:Status4 option = None
                                        while targetChildStatus <> Some Status4.Ready do
                                            targetChildStatus <- Some <| x.Target.GetSegmentStatus()
                                            logWarn $"Waiting target child [{x.Target.Name}] ready..from {targetChildStatus.Value}"
                                            do! Async.Sleep(10);

                                        cpu.Enqueue(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                    } |> Async.StartAsTask |> ignore

                                ()
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
