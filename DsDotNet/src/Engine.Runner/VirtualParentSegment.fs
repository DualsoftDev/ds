namespace Engine.Runner


open Engine.Common.FS
open Engine.Core
open System
open System.Reactive.Linq
open System.Linq
open System.Web.Configuration
open Engine.Common

[<AutoOpen>]
module VirtualParentSegmentModule =
    type VirtualParentSegment(target:Segment
        , causalSourceSegments:Segment seq
        , resetSourceSegments:Segment seq
    ) as this =
        inherit FsSegmentBase(target.Cpu, $"VPS_{target.QualifiedName}")

        let cpu = target.Cpu
        do
            let ne = $"VPS_{target.TagPEnd.Name}"
            this.TagPEnd   <- TagP(cpu, this, ne, TagType.I ||| TagType.End  )

            let n = $"VPS_{target.QualifiedName}"
            this.Going <- TagE(cpu, this, $"Going_{n}", TagType.Going)
            this.Ready <- TagE(cpu, this, $"Ready_{n}", TagType.Ready)


        let mutable oldStatus:Status4 option = None
        let triggerTargetStart = causalSourceSegments.Any()
        let triggerTargetReset = resetSourceSegments.Any()
        let targetStartTag = target.TagPStart
        let targetResetTag = target.TagPReset

        member val Target = target;

        member x.SetPorts(s, r, e) =
            x.PortS <- s
            x.PortR <- r
            x.PortE <- e

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
            let vps = VirtualParentSegment(target, causalSourceSegments, resetSourceSegments)

            let ep = PortInfoEnd(cpu, vps, $"End_{n}", vps.TagPEnd, null)

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
                                    yield FlipFlop(cpu, $"InnerResetSourceFF_{n}_{rsseg.Name}", rsseg.Going, vps.Ready)  :> IBit
                            else
                                // self reset.  실제 reset 시 알맹이의 reset 은 수행하지 말아야 한다.
                                yield ep
                        |]
                    And(cpu, $"ResetPlanAnd_{n}", vrp)
                vps.BitPReset <- resetPortInfoPlan
                PortInfoReset(cpu, vps, $"Reset_{n}", resetPortInfoPlan, null)

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
                    And(cpu, $"StartPlanAnd_{n}", vsp)

                vps.BitPStart <- startPortInfoPlan
                PortInfoStart(cpu, vps, $"Start_{n}", startPortInfoPlan, null)


            vps.SetPorts(sp, rp, ep)

            vps

        override x.WireEvent(writer, onError) =
            let write:BitWriter = getBitWriter writer onError

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit :?> Bit
                    let on = bc.Bit.Value

                    let notiVpsPortChange = bc.Bit.IsOneOf(x.PortS, x.PortR, x.PortE)
                    let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                    let state = x.Status
                    let n = x.QualifiedName
                    let cause = $"bit change {bit.GetName()}={on}"

                    if notiVpsPortChange || notiTargetEndPortChange then
                        noop()
                        if x.Name = "VPS_L_F_Main" then
                            noop()



                    if notiTargetEndPortChange then
                        let cause = $"{x.Target.Name} End Port={x.Target.PortE.Value}"

                        match state, on with
                        | Status4.Going, true ->
                            //[|
                            //    BitChange(targetStartTag, false, $"{n} going 끝내기 by {cause}")
                            //    BitChange(x.Going, false, $"{n} going 끝내기 by {cause}")
                            //|] |> writer
                            //write(x.PortE, true, $"{n} FINISH 끝내기 by {cause}")


                            //write(targetStartTag, false, $"{n} going 끝내기 by {cause}")
                            //write(x.Going, false, $"{n} going 끝내기 by {cause}")
                            write(x.PortE, true, $"{n} FINISH 끝내기 by {cause}")


                        | Status4.Homing, false ->
                            //assert(not targetStartTag.Value)    // homing 중에 end port 가 꺼졌다고, 반드시 start tag 가 꺼져 있어야 한다고 볼 수는 없다.  start tag ON 이면 바로 재시작
                            //assert(x.Going.Value = false) // 아직 write 안되었을 수도 있음
                            //[|
                            //    BitChange(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            //    BitChange(x.Ready, true, $"{x.Target.Name} homing 완료")
                            //    BitChange(x.PortE, false, null)
                            //|] |> writer  // <-- fail
                            write(targetResetTag, false, $"{x.Target.Name} homing 완료로 reset 끄기")
                            write(x.Ready, true, $"{x.Target.Name} homing 완료")
                            write(x.PortE, false, null)


                        | Status4.Ready, true ->
                            logInfo $"외부에서 내부 target {x.Target.Name} 실행 감지"


                        | Status4.Going, false ->
                            logInfo $"Children originated before going {x.Target.Name}"

                        | _ ->
                            failwithlog $"Unknown: [{n}]{state}: Target endport => {x.Target.Name}={on}"

                    if notiVpsPortChange then
                        if oldStatus = Some state then
                            logDebug $"\t\tVPS Skipping duplicate status: [{n}] status : {state} by {bit.Name}={on}"
                            let bitMatch =
                                if bit = x.PortS then 's'
                                else if bit = x.PortR then 'r'
                                else if bit = x.PortE then 'e'
                                else failwith "ERROR"
                            match bitMatch, state, on with
                            //| 's', Status4.Finished, false -> // finish 도중에 start port 꺼져서 finish 완료되려는 시점
                            //    ()
                            | 'e', Status4.Ready, false ->
                                ()
                            | 'e', Status4.Finished, _ ->
                                assert(on)
                                noop()
                            | 'e', Status4.Going, false ->  // going 중에 endport 가 꺼진다???
                                //assert(false)
                                noop()
                            | 's', Status4.Finished, false ->
                                ()
                            | _ ->
                                logWarn $"UNKNOWN: {n} status {state} duplicated on port {bit.GetName()}={on} by {cause}"
                                //assert(not on)    // todo
                                //assert(false)
                                noop()


                            noop()
                        else
                            oldStatus <- Some state
                            logInfo $"[{n}] VPS Segment status : {state} by {bit.Name}={on}"

                            if x.Going.Value && state <> Status4.Going then
                                write(x.Going, false, $"{n} going off by status {state}")
                            if x.Ready.Value && state <> Status4.Ready then
                                write(x.Ready, false, $"{n} ready off by status {state}")

                            Global.SegmentStatusChangedSubject.OnNext(SegmentStatusChange(x, state))


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
                                        logDebug $"Waiting target child [{x.Target.Name}] ready..from {x.Target.Status}"
                                        // wait while target child available
                                        let mutable subs:IDisposable = null
                                        subs <-
                                            Global.SegmentStatusChangedSubject.Where(fun ssc -> ssc.Segment = x.Target && ssc.Segment.Status = Status4.Ready)
                                                .Subscribe(fun ssc ->
                                                    write(targetStartTag, true, $"자식 {x.Target.Name} start tag ON")
                                                    subs.Dispose()
                                                )

                            | Status4.Finished ->
                                [|
                                    BitChange(targetStartTag, false, $"{n} FINISH 로 인한 {x.Target.Name} start {targetStartTag.GetName()} 끄기")
                                    BitChange(x.Going, false, $"{n} FINISH")
                                |] |> writer
                                //write(targetStartTag, false, $"{n} FINISH 로 인한 {x.Target.Name} start {targetStartTag.GetName()} 끄기")
                                //write(x.Going, false, $"{n} FINISH")

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

    let internal createVPSsFromRootFlow (rootFlow: RootFlow) =
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
                // todo : resetEdge.Length > 1 인 경우, OR 로 해석해서 구성해야 한다.

                let causalSources = setEdges.selectMany(fun e -> e.Sources).Cast<Segment>().ToArray()
                let resetSources = resetEdges.selectMany(fun e -> e.Sources).Cast<Segment>().ToArray()

                yield VirtualParentSegment.Create(target, auto, causalSources, resetSources)
        |]
