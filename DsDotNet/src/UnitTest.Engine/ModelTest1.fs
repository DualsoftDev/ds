namespace UnitTest.Engine


open Xunit
open System
open FsUnit.Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions

[<AutoOpen>]
module ModelTests1 =
    type DemoTests1(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parse Cylinder`` () =
            logInfo "============== Parse Cylinder"
            let text = sysP + """
[cpus] AllCpus = {
    [cpu] Cpu = {
        P.F;
    }
}
"""

            let builder = new EngineBuilder(text, "Cpu")
            let system = builder.Model.Systems |> Seq.exactlyOne
            let cpu = builder.Cpu
            cpu.Name === "Cpu"
            system.Name === "P"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let children = flow.Coins |> Enumerable.OfType<Segment>
            let childrenNames = children |> Seq.map(fun (seg:Segment) -> seg.Name)
            (childrenNames, ["Vp"; "Pp"; "Sp"; "Vm"; "Pm"; "Sm"]) |> seqEq

            children
            |> Seq.forall(fun f ->
                f.Edges.length() = 0 && f.ChildVertices.Count() = 0)
            |> ShouldBeTrue

            flow.Cpu === cpu
            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            flow === rootFlow


        [<Fact>]
        member __.``Parse Task`` () =
            logInfo "============== Parse Task"
            let mutable text = """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
        C00 = { _ ~ _ }
        C01 = { _ ~ P.F.Sm }
        C02 = { _ ~ P.F.Sp, P.F.Sm}
        C10 = {P.F.Vm ~ _ }
        C20 = {P.F.Vp, P.F.Vm ~ _ }
        C21 = {P.F.Vp, P.F.Vm ~ P.F.Sm }
        C22 = {P.F.Vp, P.F.Vm ~ P.F.Sp, P.F.Sm }
    }
    [flow] F = {
        Main = { T.Cp > T.Cm > T.C22; }
    }
}
"""
            text <- text + sysP + cpus
            let builder = new EngineBuilder(text, "Cpu")
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu
            cpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue
            cpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue

            let fakeCpu = builder.Model.Cpus |> Seq.find(fun c -> not c.IsActive)
            fakeCpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue
            fakeCpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue


            let task = system.Tasks |> Seq.exactlyOne
            let callProtos = task.CallPrototypes |> Seq.map(fun c -> c.Name, c) |> Tuple.toDictionary
            (callProtos.Keys, ["Cp"; "Cm"; "C00"; "C01"; "C02"; "C10"; "C20"; "C21"; "C22"; ])
            |> seqEq

            let checkC22Proto_ =
                let c22 = callProtos.["C22"]
                let txs = c22.TXs.OfType<Segment>()
                (txs |> Seq.map(fun tx -> tx.Name), ["Vp"; "Vm"]) |> setEq


            cpu.Name === "Cpu"
            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let main = flow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let childrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (childrenNames, ["T.Cp"; "T.Cm"; "T.C22"]) |> setEq

            let checkC22Instance_ =
                let c22 = main.Children |> Seq.find(fun child -> child.Name = "T.C22")
                c22.QualifiedName === "L_F_Main_T.C22"
                (c22.GetStartTags().Select(fun t -> t.Name), ["L_F_Main_T.C22_P_F_Vp_Start"; "L_F_Main_T.C22_P_F_Vm_Start"]) |> setEq
                (c22.GetEndTags().  Select(fun t -> t.Name), ["L_F_Main_T.C22_P_F_Sp_End";   "L_F_Main_T.C22_P_F_Sm_End"]) |> setEq


            flow.Cpu === cpu
            let rootFlow = cpu.RootFlows |> Seq.exactlyOne
            flow === rootFlow




        [<Fact>]
        member __.``Parse Real Child`` () =
            logInfo "============== Parse Real Child"
            let mutable text = """
[sys] L = {
    [flow] F = {
        Main = { P.F.Vp > P.F.Vm; }
    }
}
[sys] P = {
    [flow] F = {
        Vp > Vm;
    }
}
"""
            text <- text + cpus;
            let builder = new EngineBuilder(text, "Cpu")
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu

            cpu.Name === "Cpu"
            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let main = flow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let childrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (childrenNames, ["P.F.Vp"; "P.F.Vm";]) |> setEq
            ()


        [<Fact>]
        member __.``Parse Alias`` () =
            let mutable text = """
[sys] L = {
    [alias] = {
        P.F.Vp = { Vp1; Vp2; Vp3; }
        P.F.Vm = { Vm1; Vm2; Vm3; }
        L.T.A = {A1; A2; A3;}
    }
    [task] T = {
        A = {P.F.Vp ~ P.F.Sp}
    }

    [flow] F = {
        Main = { Vp1 > Vp2 > A1; }
    }
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text, "Cpu")
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu

            cpu.Name === "Cpu"
            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let main = flow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let childrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (childrenNames, ["Vp1"; "Vp2"; "A1"]) |> setEq

            let externalReals = collectExternalRealSegment main
            ( externalReals |> Seq.map(fun seg -> seg.Name), ["Vp1"; "Vp2";]) |> setEq
            (collectAlises main |> Seq.map(fun seg -> seg.Name), ["Vp1"; "Vp2"; "A1"]) |> setEq
            ()
