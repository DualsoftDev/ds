namespace UnitTest.Engine


open Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions

[<AutoOpen>]
module EdgeTest =
    type EdgeTests1(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parser detail test`` () =
            logInfo "============== Parser detail test"
            let mutable text = """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm; }
    }
}
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text)
            let model = builder.Model
            let activeCpuName = "Cpu"
            let cpu = model.Cpus.First(fun cpu -> cpu.Name = activeCpuName);
            cpu.ForwardDependancyMap.isNullOrEmpty() |> ShouldBeTrue
            cpu.BackwardDependancyMap |> ShouldBeNull
            cpu.IsActive <- true

            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            let main = rootFlow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")


            model.BuidGraphInfo();
            builder.InitializeAllFlows()

            model.Epilogue()


            let fwd = cpu.ForwardDependancyMap
            let bwd = cpu.BackwardDependancyMap

            // tag 기준으로 해당 port 와 연결되어 있는지 check
            main.TagsStart |> Seq.forall(fun s -> fwd[s].Contains(main.PortS)) |> ShouldBeTrue
            main.TagsReset |> Seq.forall(fun r -> fwd[r].Contains(main.PortR)) |> ShouldBeTrue
            main.TagsEnd   |> Seq.forall(fun e -> bwd[e].Contains(main.PortE)) |> ShouldBeTrue

            // port 기준으로 해당 tag 와 연결되어 있는지 check
            (bwd[main.PortS] |> Enumerable.OfType<Tag>, main.TagsStart) |> seqEq
            (bwd[main.PortR] |> Enumerable.OfType<Tag>, main.TagsReset) |> seqEq
            (fwd[main.PortE] |> Enumerable.OfType<Tag>, main.TagsEnd)   |> seqEq


            // subflow check
            let edge = main.Edges |> Seq.exactlyOne

            let vpStart = cpu.TagsMap["L_F_Main_T.Cp_P_F_Vp_Start"]
            let spEnd = cpu.TagsMap["L_F_Main_T.Cp_P_F_Sp_End"]
            cpu.TagsMap.ContainsKey(vpStart.Name) |> ShouldBeTrue
            cpu.TagsMap.ContainsKey(spEnd.Name) |> ShouldBeTrue
            cpu.ForwardDependancyMap[spEnd].Contains(edge) |> ShouldBeTrue



            let vmStart = cpu.TagsMap["L_F_Main_T.Cm_P_F_Vm_Start"]
            let smEnd = cpu.TagsMap["L_F_Main_T.Cm_P_F_Sm_End"]
            cpu.TagsMap.ContainsKey(vmStart.Name) |> ShouldBeTrue
            cpu.TagsMap.ContainsKey(smEnd.Name) |> ShouldBeTrue
            cpu.ForwardDependancyMap[edge].Contains(vmStart) |> ShouldBeTrue


            let otherCpu = model.Cpus.First(fun cpu -> not cpu.IsActive);
            [vpStart; spEnd; vmStart; smEnd]
            |> List.map (fun t -> t.Name)
            |> List.forall(otherCpu.TagsMap.ContainsKey)
            |> ShouldBeTrue

            let otherRootFlow = otherCpu.RootFlows |> Seq.exactlyOne
            let vp = otherRootFlow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Vp")
            let pp = otherRootFlow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Pp")
            let sp = otherRootFlow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Sp")
            let vpStart2 = otherCpu.TagsMap["L_F_Main_T.Cp_P_F_Vp_Start"]
            let spEnd2 = otherCpu.TagsMap["L_F_Main_T.Cp_P_F_Sp_End"]
            vp.TagsStart.Contains(vpStart2) |> ShouldBeTrue
            sp.TagsEnd.Contains(spEnd2) |> ShouldBeTrue
            vpStart =!= vpStart2
            spEnd =!= spEnd2


            otherCpu.ForwardDependancyMap[vpStart2].Contains(vp.PortS) |> ShouldBeTrue
            otherCpu.ForwardDependancyMap[sp.PortE].Contains(spEnd2) |> ShouldBeTrue


            let eVp2Pp = otherRootFlow.Edges |> Seq.find(fun e -> e.Sources.Contains(vp) && e.Target = pp)
            let ePp2Sp = otherRootFlow.Edges |> Seq.find(fun e -> e.Sources.Contains(pp) && e.Target = sp)

            let vpEnd2 = otherCpu.TagsMap["P_F_Vp_End"]
            let ppStart2 = otherCpu.TagsMap["P_F_Pp_Start"]
            let ppEndt2 = otherCpu.TagsMap["P_F_Pp_End"]
            let spStart2 = otherCpu.TagsMap["P_F_Sp_Start"]
            let spEnd2 = otherCpu.TagsMap["L_F_Main_T.Cp_P_F_Sp_End"]

            otherCpu.ForwardDependancyMap[vpEnd2].Contains(eVp2Pp) |> ShouldBeTrue
            otherCpu.ForwardDependancyMap[eVp2Pp].Contains(ppStart2) |> ShouldBeTrue



            ()
