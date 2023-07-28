namespace T


open System.Linq
open Engine
open Engine.Core
open Engine.Runner
open Dual.Common.Core.FS
open NUnit.Framework

[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() = 
        inherit EngineTestBaseClass()

        [<Test>]
        member __.``Parse Cylinder`` () =
            logInfo "============== Parse Cylinder"
            let text = sysP

            let builder = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu"))
            let system = builder.Model.Systems |> Seq.exactlyOne
            let cpu = builder.Cpu
            system.Name === "P"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let children = flow.Coins |> Enumerable.OfType<SegmentBase>
            let childrenNames = children |> Seq.map(fun (seg:SegmentBase) -> seg.Name)
            (childrenNames, ["Vp"; "Pp"; "Sp"; "Vm"; "Pm"; "Sm"]) |> seqEq

            children
            |> Seq.forall(fun f ->
                f.Edges.length() = 0 && f.ChildVertices.Count() = 0)
            |> ShouldBeTrue

            flow.Cpu === cpu


        [<Test>]
        member __.``Parse Task`` () =
            logInfo "============== Parse Task"
            let mutable text = """
[sys] L = {
    [flow] F = {
        Main = { Cp > Cm > C22; }
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
}
"""
            text <- text + sysP
            let builder = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu"))
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu
            cpu.BuildBitDependencies()
            cpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue
            cpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue

            let fakeCpu = builder.Model.Cpus |> Seq.find(fun c -> not c.IsActive)
            fakeCpu.BuildBitDependencies()
            fakeCpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue
            fakeCpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue


            let flow = system.RootFlows |> Seq.exactlyOne
            let callProtos = flow.CallPrototypes |> Seq.map(fun c -> c.Name, c) |> Tuple.toDictionary
            (callProtos.Keys, ["Cp"; "Cm"; "C00"; "C01"; "C02"; "C10"; "C20"; "C21"; "C22"; ])
            |> seqEq

            let checkC22Proto_ =
                let c22 = callProtos.["C22"]
                let txs = c22.TXs.OfType<SegmentBase>()
                (txs |> Seq.map(fun tx -> tx.Name), ["Vp"; "Vm"]) |> setEq


            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let main = flow.Coins |> Enumerable.OfType<SegmentBase> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let childrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (childrenNames, ["Cp"; "Cm"; "C22"]) |> setEq

            let checkC22Instance_ =
                let c22 = main.Children |> Seq.find(fun child -> child.Name = "C22")
                c22.QualifiedName === "L.F.Main.C22"
                (c22.TagsStart.Select(fun t -> t.Name), ["StartPlan_P.F.Vp"; "StartPlan_P.F.Vm"]) |> setEq
                (c22.TagsEnd.Select(fun t -> t.Name), ["EndPlan_P.F.Sp"; "EndPlan_P.F.Sm"]) |> setEq


            flow.Cpu === cpu

        [<Test>]
        member __.``XParse Alias`` () =
            let mutable text = """
[sys] L = {
    [flow] F = {
        Main = {
            // 정보로서의 CallDev 상호 리셋
            Ap <||> Am;
            Bp <||> Bm;
            Ap > Am, Bp > Bm > Ap1 > Am1, Bp1 > Bm1;
        }
        Ap = {A.F.Vp ~ A.F.Sp}
        Am = {A.F.Vm ~ A.F.Sm}
        Bp = {B.F.Vp ~ B.F.Sp}
        Bm = {B.F.Vm ~ B.F.Sm}
        [alias] = {
            Ap = { Ap1; Ap2; }
            Am = { Am1; Am2; }
            Bp = { Bp1; Bp2; }
            Bm = { Bm1; Bm2; }
        }

    }
}
"""
            text <- text + Tester.CreateCylinder("A") + Tester.CreateCylinder("B")

            let builder = new EngineBuilder(text, ParserOptions.Create4Simulation("Cpu"))
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "A"; "B"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu

            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"
            let main = flow.Coins |> Enumerable.OfType<SegmentBase> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let childrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (childrenNames, ["Vp1"; "Vp2"; "A1"]) |> setEq

            let externalReals = collectExternalRealSegment main
            ( externalReals |> Seq.map(fun seg -> seg.Name), ["Vp1"; "Vp2";]) |> setEq
            (collectAlises main |> Seq.map(fun seg -> seg.Name), ["Vp1"; "Vp2"; "A1"]) |> setEq
            ()
