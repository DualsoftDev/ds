namespace UnitTest.Engine


open Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions


[<AutoOpen>]
module MockUp =
    type Segment(cpu, n, sp, rp, ep) =
        new(cpu, n) = Segment(cpu, n, null, null, null)
        member x.Cpu:Cpu = cpu
        member x.Name:string = n
        member val PortS:PortTagStart = sp with get, set
        member val PortR:PortTagReset = rp with get, set
        member val PortE:PortTagEnd = ep with get, set
        member val Going = new Tag(cpu, null, $"{n}_Going")

    let buildBackToBack() =
        let cpu = new Cpu("dummy", [||], new Model())
        let b = Segment(cpu, "B")
        let g = Segment(cpu, "G")
        let r = Segment(cpu, "R")

        let rvst = new Flag(cpu, "VStartR")     // rvst: R 노드의 Virtual Start Tag
        let gvst = new Flag(cpu, "VStartG")
        let bvst = new Flag(cpu, "VStartB")

        let rvrt = new Flag(cpu, "VResetR")
        let gvrt = new Flag(cpu, "VResetG")
        let bvrt = new Flag(cpu, "VResetB")

        let rvet = new Flag(cpu, "VEndR")
        let gvet = new Flag(cpu, "VEndG")
        let bvet = new Flag(cpu, "VEndB")


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


        r.PortS <- new PortTagStart(cpu, "RVSP", rvspe, null)
        g.PortS <- new PortTagStart(cpu, "GVSP", gvspe, null)
        b.PortS <- new PortTagStart(cpu, "BVSP", bvspe, null)

        r.PortR <- new PortTagReset(cpu, "RVRP", rvrpe, null)
        g.PortR <- new PortTagReset(cpu, "GVRP", gvrpe, null)
        b.PortR <- new PortTagReset(cpu, "BVRP", bvrpe, null)

        r.PortE <- new PortTagEnd(cpu, "RVEP", rvet, null)
        g.PortE <- new PortTagEnd(cpu, "GVEP", gvet, null)
        b.PortE <- new PortTagEnd(cpu, "BVEP", bvet, null)


        {|  Segments=[b; g; r;]
            Cpu=cpu
            VStarts=[bvst; gvst; rvst;]
            VResets=[bvrt; gvrt; rvrt;]
            VEnds=[bvet; gvet; rvet;]
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
        member __.``ToyMockup test`` () =
            init()
            let toySystem = buildBackToBack()
            let [b; g; r;] = toySystem.Segments
            let [bvst; gvst; rvst;] = toySystem.VStarts
            let [bvrt; gvrt; rvrt;] = toySystem.VResets
            let [bvet; gvet; rvet;] = toySystem.VEnds

            bvst.Value <- true
            ()