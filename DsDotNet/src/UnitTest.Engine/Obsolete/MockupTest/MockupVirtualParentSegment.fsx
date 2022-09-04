namespace UnitTest.Mockup.Engine


open Engine.Core
open Engine.Common
open Engine.Common.FS
open Engine.Runner
open System


/// Virtual Parent Segment
/// endport 는 target child 의 endport 공유
/// startPort 는 공정 auto
/// resetPort = { auto &&
///     #latch( (Self
///                 && #latch(#g(Previous), #r(Self)  <--- reset 조건 1
///                 && #latch(#g(Next), #r(Self)),    <--- reset 조건 2 ...
///             #r(Self))
type MockupVirtualParentSegment(name, target:MockupSegment, causalSourceSegments:MockupSegment seq
    , startPort, resetPort, endPort
    , goingTag, readyTag
    , targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
) as this =
    inherit MockupSegmentBase(target.Cpu, name)

    let cpu = target.Cpu
    do
        let ns = $"VPS_Start_{name}"
        let nr = $"VPS_Reset_{name}"
        let ne = $"VPS_End_{name}"
        this.BitPStart <- TagP(cpu, this, ns, TagType.Q ||| TagType.Start)
        this.BitPReset <- TagP(cpu, this, nr, TagType.Q ||| TagType.Reset)
        this.TagPEnd   <- TagP(cpu, this, ne, TagType.I ||| TagType.End  )

        this.PortS <- startPort
        this.PortR <- resetPort
        this.PortE <- endPort

        this.Going <- if isNull goingTag then TagE(cpu, this, $"Going_{name}", TagType.Going) else goingTag
        this.Ready <- if isNull readyTag then TagE(cpu, this, $"Ready_{name}", TagType.Ready) else readyTag

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

        let readyTag = new TagE(cpu, null, $"{n}_Ready")

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
                        //yield target.PortE  //ep
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
                        //yield readyTag
                        for csseg in causalSourceSegments do
                            yield FlipFlop(cpu, $"InnerStartSourceFF_{n}_{csseg.Name}", csseg.PortE, ep)// :> IBit
                    |]
                And(cpu, $"StartAnd_{n}", vsp)

            PortInfoStart(cpu, null, $"Start_{n}", startPortInfoPlan, null)


        let vps = MockupVirtualParentSegment(n, target, causalSourceSegments, sp, rp, ep, null, readyTag, targetStartTag, targetResetTag)
        sp.Segment <- vps
        rp.Segment <- vps
        ep.Segment <- vps

        vps

    override x.WireEvent(writer:ChangeWriter) =
        let write(bit, value, cause) =
            writer(BitChange(bit, value, cause))

        let onError =
            fun (ex:Exception) ->
                cpu.Running <- false

        Global.BitChangedSubject
            .Subscribe(fun bc ->
                let bit = bc.Bit :?> Bit
                let on = bc.Bit.Value

                let notiVpsPortChange = bc.Bit.IsOneOf(x.PortS, x.PortR, x.PortE)
                let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                let state = x.Status

                //if notiVpsPortChange || notiTargetEndPortChange then
                //    noop()

                if notiTargetEndPortChange then
                    if x.Going.Value && state <> Status4.Going then
                        write(x.Going, false, $"{x.Name} going off by status {state}")
                    if x.Ready.Value && state <> Status4.Ready then
                        write(x.Ready, false, $"{x.Name} ready off by status {state}")

                    let cause = $"${x.Target.Name} End Port={x.Target.PortE.Value}"


                    match state, on with
                    | Status4.Going, true ->
                        write(targetStartTag, false, $"{x.Name} going 끝내기 by{cause}")
                        write(x.Going, false, $"{x.Name} going 끝내기 by{cause}")
                        if MockupSegmentBase.WithThreadOnPortEnd then
                            async { write(x.PortE, true, $"{x.Name} FINISH 끝내기 by{cause}") } |> Async.Start
                        else
                            write(x.PortE, true, $"{x.Name} FINISH 끝내기 by{cause}")
                            


                    | Status4.Homing, false ->
                        assert(x.Going.Value = false)
                        let homing() =
                            write(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            write(x.Ready, true, $"{x.Target.Name} homing 완료")
                            write(x.PortE, false, null)

                        if MockupSegmentBase.WithThreadOnPortReset then
                            async { homing() } |> Async.Start
                        else
                            homing()

                    | Status4.Ready, true ->
                        logInfo $"외부에서 내부 target {x.Target.Name} 실행 감지"

                    | _ ->
                        failwithlog $"Unexpected VPS & child status : [{x.Name}]{state}: Target endport => {x.Target.Name}={on}"


                if notiVpsPortChange then
                    if oldStatus = Some state then
                        logDebug $"\t\tVPS Skipping duplicate status: [{x.Name}] status : {state}"
                    else
                        oldStatus <- Some state
                        logDebug $"[{x.Name}] Segment status : {state} by {bit.Name}={bit.Value}"
                        let childStatus = x.Target.Status

                        match state with
                        | Status4.Ready    ->
                            ()
                        | Status4.Going    ->
                            write(x.Going, true, $"{name} GOING 시작")

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
                            write(targetStartTag, false, $"{x.Name} FINISH 로 인한 {x.Target.Name} start 끄기")
                            write(x.Going, false, "${x.Name} FINISH")
                            x.FinishCount <- x.FinishCount + 1
                            if not MockupSegmentBase.WithThreadOnPortReset then
                                assert(x.PortE.Value)
                                assert(x.PortR.Value = false)

                        | Status4.Homing ->
                            if childStatus = Status4.Going then
                                failwithlog $"Something bad happend?  trying to reset child while {x.Target.Name}={childStatus}"

                            write(targetResetTag, true, $"{x.Name} HOMING 으로 인한 {x.Target.Name} reset 켜기")


                        | _ ->
                            failwith "Unexpected"
                    ()
            )

type Vps = MockupVirtualParentSegment

