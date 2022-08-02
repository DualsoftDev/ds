namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Reactive.Linq
open System.Threading
open System.Threading.Tasks
open Engine.Core
open UnitTest.Engine
open System.Collections.Concurrent

[<AutoOpen>]
module MockUp =
    type Segment(cpu, n, sp, rp, ep) =
        inherit Engine.Core.Segment(n)
        let mutable oldStatus = Status4.Homing
        new(cpu, n) = Segment(cpu, n, null, null, null)
        member val FinishCount = 0 with get, set
        member val PortS:PortExpressionStart = sp with get, set
        member val PortR:PortExpressionReset = rp with get, set
        member val PortE:PortExpressionEnd = ep with get, set
        member val Going = new Tag(cpu, null, $"{n}_Going")
        member x.GetSegmentStatus() =
            match x.PortS.Value, x.PortR.Value, x.PortE.Value with
            | false, false, false -> Status4.Ready  //??
            | true, false, false  -> Status4.Going
            | _, false, true      -> Status4.Finished
            | _, true, _          -> Status4.Homing

        member x.WireEvent() =
            Global.BitChangedSubject
                //.Select(fun bc -> bc.Bit)
                .Where(fun bc ->
                    [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let newSegmentState = x.GetSegmentStatus()
                    if newSegmentState = oldStatus then
                        logDebug $"\t\tSkipping duplicate status: [{x.Name}] status : {newSegmentState}"
                    else
                        oldStatus <- newSegmentState
                        logDebug $"[{x.Name}] Segment status : {newSegmentState}"

                        //x.Going.Value <- (newSegmentState = Status4.Going)
                        //assert(x.GetSegmentStatus() = newSegmentState)

                        //Task.Run(fun () ->
                        match newSegmentState with
                        | Status4.Ready    ->
                            ()
                        | Status4.Going    ->
                            //Task.Run(fun () ->
                                x.Going.Value <- true
                                Thread.Sleep(100)
                                assert(x.GetSegmentStatus() = Status4.Going)
                                x.Going.Value <- false
                                x.PortE.Value <- true
                                if not x.PortE.Value then
                                    ()
                                //assert(x.PortE.Value)
                                //assert(x.GetSegmentStatus() = Status4.Finished)
                            //) |> ignore
                        | Status4.Finished ->
                            x.FinishCount <- x.FinishCount + 1
                            assert(x.PortE.Value)
                            ()
                        | Status4.Homing   ->
                            //assert(not x.PortS.Value)

                            if x.PortE.Value then
                                x.PortE.Value <- false
                                assert(not x.PortE.Value)
                            else
                                logDebug $"\tSkipping [{x.Name}] Segment status : {newSegmentState} : already homing by bit change {bc.Bit}={bc.NewValue}"
                                ()

                            assert(not x.PortE.Value)

                        | _ ->
                            failwith "Unexpected"
                        //    ) |> ignore

                        //logDebug $"[{x.Name}] New Segment status : {x.GetSegmentStatus()}"
                )

    type MuCpu(n) =
        inherit Cpu(n, new Model())
        member val MuQueue = new ConcurrentQueue<BitChange>()


[<AutoOpen>]
module ToyMockupTest =
    type ToyMockupTests1(output1:ITestOutputHelper) =
        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``ToyMockup repeating triangle test`` () =
            // todo : check execution order/counting

            let cpu = new MuCpu("dummy")
            let b = Segment(cpu, "B")
            let g = Segment(cpu, "G")
            let r = Segment(cpu, "R")
            let stB = new Flag(cpu, "stB")


            [b; g; r;] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.Cause then "" else $" caused by [{bc.Cause}={bc.Cause.Value}]"
                    logDebug $"\tBit changed: [{bit}] = {bc.NewValue}{cause}"
                    match bit with
                    | :? PortExpressionEnd as portE ->
                        let seg = portE.Segment :?> Segment
                        let status = seg.GetSegmentStatus()
                        //logDebug $"Segment [{seg.Name}] Status : {status} inferred by port [{bit}]={bit.Value} change"
                        if bit = b.PortE && b.PortE.Value then
                            if stB.Value then
                                logDebug "초기 시작 button OFF"
                                stB.Value <- false

                            if seg.FinishCount % 10 = 0 then
                                logDebug $"COUNTER: B={b.FinishCount}, G={g.FinishCount}, R={r.FinishCount}"

                        ()
                    | _ ->
                        ()
                )
            |> ignore


            (*
                {s, r, e}{t, l, pex}{R, G, B}: {Start, Reset, End} {Tag, Latch, Port Expression} for seg {R, G, B}
            *)

            let stR = new Flag(cpu, "stR")     // rvst: R 노드의 Virtual Start Tag
            let stG = new Flag(cpu, "stG")

            let rtR = new Flag(cpu, "rtR")
            let rtG = new Flag(cpu, "rtG")
            let rtB = new Flag(cpu, "rtB")

            r.PortE <- PortExpressionEnd.Create(cpu, r, "epexR", null)
            g.PortE <- PortExpressionEnd.Create(cpu, g, "epexG", null)
            b.PortE <- PortExpressionEnd.Create(cpu, b, "epexB", null)


            let B = b.PortE
            let R = r.PortE
            let G = g.PortE


            let slB = Latch(cpu, "slB", G, B)
            let slG = Latch(cpu, "slG", R, G)
            let slR = Latch(cpu, "slR", B, R)

            let rlB = Latch(cpu, "rlB", r.Going, Not(B))
            let rlG = Latch(cpu, "rlG", b.Going, Not(G))
            let rlR = Latch(cpu, "rlR", g.Going, Not(R))


            // bvspe: B 노드의 Virtual Start Port Expression
            let spexB = Or(cpu, "speB(OR)", slB, stB)
            let spexG = Or(cpu, "speG(OR)", slG, stG)
            let spexR = Or(cpu, "speR(OR)", slR, stR)

            let rpexB = Or(cpu, "rpeB(OR)", rlB, rtB)
            let rpexG = Or(cpu, "rpeG(OR)", rlG, rtG)
            let rpexR = Or(cpu, "rpeR(OR)", rlR, rtR)

            r.PortS <- new PortExpressionStart(cpu, r, "spexR", spexR, null)
            g.PortS <- new PortExpressionStart(cpu, g, "spexG", spexG, null)
            b.PortS <- new PortExpressionStart(cpu, b, "spexB", spexB, null)

            r.PortR <- new PortExpressionReset(cpu, r, "rpexR", rpexR, null)
            g.PortR <- new PortExpressionReset(cpu, g, "rpexG", rpexG, null)
            b.PortR <- new PortExpressionReset(cpu, b, "rpexB", rpexB, null)




            stB.Value <- true

            // give enough time to wait...
            while BitChange.PendingTasks.Count > 0 do
                Thread.Sleep(500)

            b.PortE.Value === true
            let gStatus = g.GetSegmentStatus()
            let gPortS = g.PortS.Value

            r.PortS.Plan.Value === true
            ()

        [<Fact>]
        member __.``ToyMockup with 1 segment test`` () =
            let init (cpu:MuCpu) =
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        //cpu.MuQueue.Enqueue(bc)
                        let bit = bc.Bit
                        logDebug $"\tBit changed: [{bit}] = {bc.NewValue}"
                    )
                |> ignore

            let cpu = new MuCpu("dummy")
            init cpu
            let b = Segment(cpu, "B")
            let st = new Flag(cpu, "VStartB")
            let rt = new Flag(cpu, "VResetB")
            b.PortE <- PortExpressionEnd.Create(cpu, b, "BVEP", null)


            let startLatch = Latch(cpu, "시작 래치", st, b.PortE)
            // bvspe: B 노드의 Virtual Start Port Expression
            let bvspe = Or(cpu, "OR(BVSPE)", startLatch, st)

            let notPortE = Not(cpu, "^B", b.PortE)
            let resetLatch = Latch(cpu, "BVSP_Latch", rt, notPortE)

            // bvrpe: B 노드의 Virtual Reset Port Expression
            let bvrpe = Or(cpu, "OR(BVRPE)", resetLatch, rt)


            b.PortS <- new PortExpressionStart(cpu, b, "BVSP", bvspe, null)
            b.PortR <- new PortExpressionReset(cpu, b, "BVRP", bvrpe, null)

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    if bit = b.PortE then
                        if bit.Value then
                            logDebug $"Endport ON 감지로 인한 start button 끄기"
                            st.Value <- false // 종료 감지시 -> Start button 끄기
                        else
                            logDebug $"Endport OFF 감지로 인한 reset button 끄기"
                            rt.Value <- false
                )
            |> ignore

            b.WireEvent() |> ignore

            st.Value <- true
            // ... going 진행 후, end port 까지 ON
            st.Value === false
            b.GetSegmentStatus() === Status4.Finished

            b.PortS.Value === false
            b.PortE.Value === true
            b.PortR.Value === false

            notPortE.Value === false
            startLatch.Value === false
            resetLatch.Value === false

            // reset 시작
            rt.Value <- true
            b.PortE.Value === false
            notPortE.Value === true
            b.GetSegmentStatus() === Status4.Ready
            ()

