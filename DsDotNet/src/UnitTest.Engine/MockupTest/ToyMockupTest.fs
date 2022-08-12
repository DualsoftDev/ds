namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open UnitTest.Engine

[<AutoOpen>]
module ToyMockupTest =
    type ToyMockupTests1(output1:ITestOutputHelper) =
        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``ToyMockup repeating triangle test`` () =
            let cpu = MockUpCpu.create("dummy")
            let b, (stB, rtB) = MockupSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MockupSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MockupSegment.CreateWithDefaultTags(cpu, "R")
            let stB = new Flag(cpu, "stB")


            [b; g; r;] |> Seq.iter(fun seg -> seg.WireEvent(cpu.Enqueue) |> ignore)

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.CauseRepr then "" else $" caused by [{bc.CauseRepr}]"
                    logDebug $"\tBit changed: [{bit}] = {bc.NewValue}{cause}"
                    match bit with
                    | :? PortInfoEnd as portE ->
                        let seg = portE.Segment :?> MockupSegment
                        let status = seg.GetSegmentStatus()
                        //logDebug $"Segment [{seg.Name}] Status : {status} inferred by port [{bit}]={bit.Value} change"
                        if bit = b.PortE && b.PortE.Value then
                            if stB.Value then
                                logDebug "초기 시작 button OFF"
                                cpu.Enqueue(stB, false)

                        if bit = g.PortE && g.PortE.Value then
                            if seg.FinishCount % 10 = 0 then
                                logDebug $"COUNTER: B={b.FinishCount}, G={g.FinishCount}, R={r.FinishCount}"
                        ()
                    | _ ->
                        ()
                ) |> ignore


            (*
                {s, r, e}{t, l, pex}{R, G, B}: {Start, Reset, End} {Tag, Latch, Port Expression} for seg {R, G, B}
            *)

            let stR = new Flag(cpu, "stR")     // rvst: R 노드의 Virtual Start Tag
            let stG = new Flag(cpu, "stG")

            let rtR = new Flag(cpu, "rtR")
            let rtG = new Flag(cpu, "rtG")
            let rtB = new Flag(cpu, "rtB")


            let B = b.PortE
            let R = r.PortE
            let G = g.PortE


            let slB = Latch(cpu, "slB", G, B)
            let slG = Latch(cpu, "slG", R, G)
            let slR = Latch(cpu, "slR", B, R)

            let rlB = Latch(cpu, "rlB", r.Going, Not(B))
            let rlG = Latch(cpu, "rlG", b.Going, Not(G))
            let rlR = Latch(cpu, "rlR", g.Going, Not(R))



            r.PortS.Plan <- Or(cpu, "speR(OR)", slR, stR)
            g.PortS.Plan <- Or(cpu, "speG(OR)", slG, stG)
            b.PortS.Plan <- Or(cpu, "speB(OR)", slB, stB)

            r.PortR.Plan <- Or(cpu, "rpeR(OR)", rlR, rtR)
            g.PortR.Plan <- Or(cpu, "rpeG(OR)", rlG, rtG)
            b.PortR.Plan <- Or(cpu, "rpeB(OR)", rlB, rtB)


            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()


            cpu.Enqueue(stB, true)

            // give enough time to wait...
            wait(cpu)

            b.PortE.Value === true
            let gStatus = g.GetSegmentStatus()
            let gPortS = g.PortS.Value

            r.PortS.Plan.Value === true
            ()

        [<Fact>]
        member __.``ToyMockup with 1 segment test`` () =
            let cpu = MockUpCpu.create("dummy")
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"\tBit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

            let b, (stB, rtB) = MockupSegment.CreateWithDefaultTags(cpu, "B")
            let st = new Flag(cpu, "VStartB")
            let rt = new Flag(cpu, "VResetB")


            let startLatch = Latch(cpu, "시작 래치", st, b.PortE)
            // bvspe: B 노드의 Virtual Start Port Expression
            let bvspe = Or(cpu, "OR(BVSPE)", startLatch, st)

            let notPortE = Not(cpu, "^B", b.PortE)
            let resetLatch = Latch(cpu, "BVSP_Latch", rt, notPortE)

            // bvrpe: B 노드의 Virtual Reset Port Expression
            let bvrpe = Or(cpu, "OR(BVRPE)", resetLatch, rt)


            b.PortS <- new PortInfoStart(cpu, b, "BVSP", bvspe, null)
            b.PortR <- new PortInfoReset(cpu, b, "BVRP", bvrpe, null)

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    if bit = b.PortE then
                        if bit.Value then
                            logDebug $"Endport ON 감지로 인한 start button 끄기"
                            cpu.Enqueue(st, false) // 종료 감지시 -> Start button 끄기
                        else
                            logDebug $"Endport OFF 감지로 인한 reset button 끄기"
                            cpu.Enqueue(rt, false)
                )
            |> ignore

            b.WireEvent(cpu.Enqueue) |> ignore

            cpu.Enqueue(st, true)
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
            cpu.Enqueue(rt, true)
            b.PortE.Value === false
            notPortE.Value === true
            b.GetSegmentStatus() === Status4.Ready
            ()

