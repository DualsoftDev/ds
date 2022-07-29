namespace UnitTest.Engine


open Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions
open System.Reactive.Linq
open Akka.Actor

[<AutoOpen>]
module MockUp =
    type Segment(cpu, n, sp, rp, ep) =
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
                    let newSegmentState = x.GetSegmentStatus()
                    logDebug $"Segment [{x.Name}] status : {newSegmentState}"
                    x.Going.Value <- (newSegmentState = Status4.Going)
                    match newSegmentState with
                    | Status4.Ready -> ()
                    | Status4.Going ->
                        x.PortE.Value <- true
                    | Status4.Finished ->
                        ()//x.PortS.Plan.Value <- false
                    | Status4.Homing ->
                        let xs = x.PortS.Value
                        let xr = x.PortR.Value
                        let xe = x.PortE.Value
                        x.PortE.Value <- false
                        let xx = x.GetSegmentStatus()
                        ()
                        //x.PortR.Plan.Value <- false
                    | _ -> failwith "Unexpected"
                )

    let buildBackToBack() =
        let cpu = new Cpu("dummy", new Model())
        let b = Segment(cpu, "B")
        let g = Segment(cpu, "G")
        let r = Segment(cpu, "R")

        let rvst = new Flag(cpu, "VStartR")     // rvst: R 노드의 Virtual Start Tag
        let gvst = new Flag(cpu, "VStartG")
        let bvst = new Flag(cpu, "VStartB")

        let rvrt = new Flag(cpu, "VResetR")
        let gvrt = new Flag(cpu, "VResetG")
        let bvrt = new Flag(cpu, "VResetB")

        r.PortE <- PortExpressionEnd.Create(cpu, "RVEP", null)
        g.PortE <- PortExpressionEnd.Create(cpu, "GVEP", null)
        b.PortE <- PortExpressionEnd.Create(cpu, "BVEP", null)


        let bvspe =      // bvspe: B 노드의 Virtual Start Port Expression
            let ``g↑`` = Rising(cpu, "G↑", g.PortE)
            let ``b↑`` = Rising(cpu, "B↑", b.PortE)
            let latch = Latch(cpu, "BVSP_latch", ``g↑``, ``b↑``)
            Or(cpu, "BVSP", latch, bvst)

        let bvrpe =     // bvrpe: B 노드의 Virtual Reset Port Expression
            let ``rG↑`` = Rising(cpu, "↑g(R)", r.Going)
            let ``b↓`` = Falling(cpu, "B↓", b.PortE)
            let latch = Latch(cpu, "BVSP", ``rG↑``, ``b↓``)
            Or(cpu, "BVRP", latch, bvrt)



        let gvspe =
            let ``r↑`` = Rising(cpu, "R↑", r.PortE)
            let ``g↑`` = Rising(cpu, "G↑", g.PortE)
            let latch = Latch(cpu, "GVSP_latch", ``r↑``, ``g↑``)
            Or(cpu, "GVSP", latch, gvst)

        let gvrpe =
            let ``bG↑`` = Rising(cpu, "↑g(B)", b.Going)
            let ``g↓`` = Falling(cpu, "G↓", g.PortE)
            let latch = Latch(cpu, "GVSP", ``bG↑``, ``g↓``)
            Or(cpu, "GVRP", latch, gvrt)




        let rvspe =
            let ``b↑`` = Rising(cpu, "B↑", b.PortE)
            let ``r↑`` = Rising(cpu, "R↑", r.PortE)
            let latch = Latch(cpu, "RVSP_latch", ``b↑``, ``r↑``)
            Or(cpu, "RVSP", latch, rvst)

        let rvrpe =
            let ``gG↑`` = Rising(cpu, "↑g(G)", g.Going)
            let ``r↓`` = Falling(cpu, "R↓", r.PortE)
            let latch = Latch(cpu, "RVSP", ``gG↑``, ``r↓``)
            Or(cpu, "RVRP", latch, rvrt)


        r.PortS <- new PortExpressionStart(cpu, "RVSP", rvspe, null)
        g.PortS <- new PortExpressionStart(cpu, "GVSP", gvspe, null)
        b.PortS <- new PortExpressionStart(cpu, "BVSP", bvspe, null)

        r.PortR <- new PortExpressionReset(cpu, "RVRP", rvrpe, null)
        g.PortR <- new PortExpressionReset(cpu, "GVRP", gvrpe, null)
        b.PortR <- new PortExpressionReset(cpu, "BVRP", bvrpe, null)


        {|  Segments=[b; g; r;]
            Cpu=cpu
            VStarts=[bvst; gvst; rvst;]
            VResets=[bvrt; gvrt; rvrt;]
            VEnds=[b.PortE.Plan; g.PortE.Plan; r.PortE.Plan;]
            |}

[<AutoOpen>]
module ToyMockupTest =
    type ToyMockupTests1(output1:ITestOutputHelper) =
        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``ToyMockup with 1 segment test`` () =
            init()
            //let cpu = new Cpu("dummy", new Model())
            //let b = Segment(cpu, "B")
            //let bvst = new Flag(cpu, "VStartB")
            //let bvrt = new Flag(cpu, "VResetB")
            //b.PortE <- PortExpressionEnd.Create(cpu, "BVEP", null)


            //let ``bvst↑`` = Rising(cpu, "시작버튼 눌림감지↑", bvst)
            //let ``finish↑`` = Rising(cpu, "종료 감지↑", b.PortE)
            //let startLatch = Latch(cpu, "시작 래치", ``bvst↑``, ``finish↑``)
            //// bvspe: B 노드의 Virtual Start Port Expression
            //let bvspe = Or(cpu, "OR(BVSPE)", startLatch, bvst)

            //let ``bvrt↑`` = Rising(cpu, "bvrt↑", bvrt)
            //let ``resetFinished↓`` = Falling(cpu, "B↓", b.PortE)
            //let resetLatch = Latch(cpu, "BVSP_Latch", ``bvrt↑``, ``resetFinished↓``)

            //// bvrpe: B 노드의 Virtual Reset Port Expression
            //let bvrpe = Or(cpu, "OR(BVRPE)", resetLatch, bvrt)


            //b.PortS <- new PortExpressionStart(cpu, "BVSP", bvspe, null)
            //b.PortR <- new PortExpressionReset(cpu, "BVRP", bvrpe, null)

            //Global.BitChangedSubject
            //    .Where(fun bc -> bc.Bit = b.PortE && bc.Bit.Value)
            //    .Subscribe(fun bc ->
            //        logDebug $"Endport 감지로 인한 start button 끄기"
            //        bvst.Value <- false // 종료 감지시 -> Start button 끄기
            //    )
            //|> ignore

            //b.WireEvent()

            //bvst.Value <- true
            //b.PortS.Value === true
            //b.PortE.Value === true
            //b.PortR.Value === false

            ()

        [<Fact>]
        member __.``ToyMockup test`` () =
            // todo : 현재 무한 루프

            //init()
            //let toySystem = buildBackToBack()
            //let [b; g; r;] = toySystem.Segments
            //[b; g; r;] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)
            //let [bvst; gvst; rvst;] = toySystem.VStarts
            //let [bvrt; gvrt; rvrt;] = toySystem.VResets
            //let [bvet; gvet; rvet;] = toySystem.VEnds

            //bvst.Value <- true

            //b.PortE.Value === true
            //r.PortS.Plan.Value === true
            ()