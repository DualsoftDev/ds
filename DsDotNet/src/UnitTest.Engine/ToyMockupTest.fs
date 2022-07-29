namespace UnitTest.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Reactive.Linq
open System.Threading

[<AutoOpen>]
module MockUp =
    type Segment(cpu, n, sp, rp, ep) =
        inherit Engine.Core.Segment(n)
        new(cpu, n) = Segment(cpu, n, null, null, null)
        member x.Cpu:Cpu = cpu
        member x.Name:string = n
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
                .Where(fun bc -> [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit))
                .Subscribe(fun bc ->
                    Thread.Sleep(10);
                    let newSegmentState = x.GetSegmentStatus()
                    logDebug $"Segment [{x.Name}] status : {newSegmentState}"
                    x.Going.Value <- (newSegmentState = Status4.Going)
                    match newSegmentState with
                    | Status4.Ready    -> ()
                    | Status4.Going    -> x.PortE.Value <- true
                    | Status4.Finished -> ()
                    | Status4.Homing   -> x.PortE.Value <- false
                    | _ -> failwith "Unexpected"
                    logDebug $"New Segment [{x.Name}] status : {x.GetSegmentStatus()}"
                )

[<AutoOpen>]
module ToyMockupTest =
    type ToyMockupTests1(output1:ITestOutputHelper) =
        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"\tBit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``ToyMockup with 1 segment test`` () =
            init()
            let cpu = new Cpu("dummy", new Model())
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

        [<Fact>]
        member __.``ToyMockup test`` () =
            // todo : 현재 무한 루프

            let cpu = new Cpu("dummy", new Model())
            let b = Segment(cpu, "B")
            let g = Segment(cpu, "G")
            let r = Segment(cpu, "R")

            let stR = new Flag(cpu, "stR")     // rvst: R 노드의 Virtual Start Tag
            let stG = new Flag(cpu, "stG")
            let stB = new Flag(cpu, "stB")

            let rtR = new Flag(cpu, "rtR")
            let rtG = new Flag(cpu, "rtG")
            let rtB = new Flag(cpu, "rtB")

            r.PortE <- PortExpressionEnd.Create(cpu, r, "epeR", null)
            g.PortE <- PortExpressionEnd.Create(cpu, g, "epeG", null)
            b.PortE <- PortExpressionEnd.Create(cpu, b, "epeB", null)


            let speB =      // bvspe: B 노드의 Virtual Start Port Expression
                let slB = Latch(cpu, "slB", g.PortE, b.PortE)
                Or(cpu, "speB(OR)", slB, stB)

            let rpeB =     // bvrpe: B 노드의 Virtual Reset Port Expression
                let notB = Not(cpu, "^B", b.PortE)
                let rlB = Latch(cpu, "rlB", r.Going, notB)
                Or(cpu, "rpeB(OR)", rlB, rtB)



            let speG =
                let slG = Latch(cpu, "slG", r.PortE, g.PortE)
                Or(cpu, "speG(OR)", slG, stG)

            let rpeG =
                let notG = Not(cpu, "^G", g.PortE)
                let rlG = Latch(cpu, "rlG", b.Going, notG)
                Or(cpu, "rpeG(OR)", rlG, rtG)




            let speR =
                let slR = Latch(cpu, "slR", b.PortE, r.PortE)
                Or(cpu, "speR(OR)", slR, stR)

            let rpeR =
                let notR = Not(cpu, "^R", r.PortE)
                let rlR = Latch(cpu, "rlR", g.Going, notR)
                Or(cpu, "rpeR(OR)", rlR, rtR)


            r.PortS <- new PortExpressionStart(cpu, r, "speR", speR, null)
            g.PortS <- new PortExpressionStart(cpu, g, "speG", speG, null)
            b.PortS <- new PortExpressionStart(cpu, b, "speB", speB, null)

            r.PortR <- new PortExpressionReset(cpu, r, "rpeR", rpeR, null)
            g.PortR <- new PortExpressionReset(cpu, g, "rpeG", rpeG, null)
            b.PortR <- new PortExpressionReset(cpu, b, "rpeB", rpeB, null)



            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"\tBit changed: [{bit}] = {bc.NewValue}"
                    match bit with
                    | :? PortExpressionEnd as portE ->
                        let seg = portE.Segment :?> Segment
                        let status = seg.GetSegmentStatus()
                        logDebug $"Segment [{seg.Name}] Status : {status} inferred by port [{bit}]={bit.Value} change"
                        ()
                    | _ ->
                        ()
                )
            |> ignore

            [b; g; r;] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)


            stB.Value <- true

            b.PortE.Value === true
            r.PortS.Plan.Value === true
            ()