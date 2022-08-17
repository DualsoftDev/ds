namespace UnitTest.Engine


open Xunit
open Engine
open Engine.Core
open System.Linq
open Dual.Common
open Xunit.Abstractions

[<AutoOpen>]
module ModelTest2 =
    type DemoTests2(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parse Alias & Task`` () =
            logInfo "============== Parse Alias & Task"
            let mutable text = """
[sys] L = {
    [alias] = {
        P.F.Vp = { Vp1; Vp2; Vp3; }
        P.F.Vm = { Vm1; Vm2; Vm3; }
        L.T.Cp = {Cp1; Cp2; Cp3;}
        L.T.Cm = {Cm1; Cm2; Cm3;}
    }
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { Cp2 |> Cm2; }
        T.Cm > T.Cp;
    }
}
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text, "Cpu")
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = builder.Cpu
            cpu.Name === "Cpu"

            cpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue
            cpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue

            let fakeCpu = builder.Model.Cpus |> Seq.find(fun c -> not c.IsActive)
            fakeCpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue
            fakeCpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue


            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"

            flow.ChildVertices.Count() === 3
            flow.Coins.Count() === 3
            let flowCallChildrenNames = flow.ChildVertices |> Enumerable.OfType<RootCall> |> Seq.map(fun c -> c.Name)
            (flowCallChildrenNames, ["T.Cm"; "T.Cp"]) |> setEq

            let main = flow.Coins |> Enumerable.OfType<Segment> |> Seq.find(fun seg -> seg.Name = "Main")
            main.Name === "Main"
            let mainChildrenNames = main.ChildVertices |> Enumerable.OfType<Child> |> Seq.map(fun soc -> soc.Name)
            (mainChildrenNames, ["Cp2"; "Cm2"]) |> setEq

            (collectAlises main |> Seq.map(fun seg -> seg.Name), ["Cp2"; "Cm2"]) |> setEq
            ()


        [<Fact>]
        member __.``Tag/Edge with two main`` () =
            logInfo "============== Tag/Edge with two main"
            let mutable text = """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main1 = { T.Cp > T.Cm; }
        Main2 = { T.Cm |> T.Cp; }
        Main1 > Main2;
    }
}
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text, "Cpu")
            ( builder.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = builder.Model.Systems |> Seq.find(fun s -> s.Name = "L")


            let cpu = builder.Cpu
            cpu.Name === "Cpu"


            cpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue
            cpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) cpu) |> ShouldBeTrue

            let fakeCpu = builder.Model.Cpus |> Seq.find(fun c -> not c.IsActive)
            fakeCpu.ForwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue
            fakeCpu.BackwardDependancyMap.Keys |> Seq.map(fun k -> k.Cpu) |> Seq.forall( (=) fakeCpu) |> ShouldBeTrue



            system.Name === "L"
            let flow = system.RootFlows |> Seq.exactlyOne
            flow.Name === "F"

            flow.ChildVertices.Count() === 2
            flow.Coins.Count() === 2
            let mains = flow.ChildVertices |> Enumerable.OfType<Segment> |> Array.ofSeq
            let mainNames = mains |> Seq.map(fun c -> c.Name)
            (mainNames, ["Main1"; "Main2"]) |> setEq

            let main1 = mains |> Seq.find(fun seg -> seg.Name = "Main1")
            let main2 = mains |> Seq.find(fun seg -> seg.Name = "Main2")


            let mutable main1Cp:Child = null
            let mutable main2Cp:Child = null
            let mutable main1Cm:Child = null
            let mutable main2Cm:Child = null
            let mutable main1CpProto:CallPrototype = null
            let mutable main2CpProto:CallPrototype = null
            let mutable main1CmProto:CallPrototype = null
            let mutable main2CmProto:CallPrototype = null

            let ``check children`` =
                let main1Children = main1.ChildVertices |> Enumerable.OfType<Child> |> Array.ofSeq
                (main1Children |> Seq.map(fun ch -> ch.Name), ["T.Cp"; "T.Cm"]) |> setEq
                (main1Children |> Seq.map(fun ch -> ch.QualifiedName), ["L_F_Main1_T.Cp"; "L_F_Main1_T.Cm"]) |> setEq


                let main2Children = main2.ChildVertices |> Enumerable.OfType<Child> |> Array.ofSeq
                (main2Children |> Seq.map(fun ch -> ch.Name), ["T.Cp"; "T.Cm"]) |> setEq
                (main2Children |> Seq.map(fun ch -> ch.QualifiedName), ["L_F_Main2_T.Cp"; "L_F_Main2_T.Cm"]) |> setEq


                main1CpProto <-
                    main1Cp <- main1Children |> Seq.find(fun ch -> ch.Name = "T.Cp")
                    (main1Cp.Coin :?> SubCall).Prototype

                main2CpProto <-
                    main2Cp <- main1Children |> Seq.find(fun ch -> ch.Name = "T.Cp")
                    (main2Cp.Coin :?> SubCall).Prototype

                // main1/T.Cp 와 main2/T.Cp 는 동일한 Call prototype 이어야 한다.
                main1CpProto === main2CpProto


                main1CmProto <-
                    main1Cm <- main1Children |> Seq.find(fun ch -> ch.Name = "T.Cm")
                    (main1Cm.Coin :?> SubCall).Prototype

                main2CmProto <-
                    main2Cm <- main1Children |> Seq.find(fun ch -> ch.Name = "T.Cm")
                    (main2Cm.Coin :?> SubCall).Prototype

                // main1/T.Cm 와 main2/T.Cm 는 동일한 Call prototype 이어야 한다.
                main1CmProto === main2CmProto


            let ``check main edges`` =
                let edge = flow.Edges |> Seq.exactlyOne
                edge.ToText() === "L_F_Main1 > L_F_Main2[WeakSetEdge]"
                edge.Sources |> Seq.exactlyOne === main1
                edge.Target === main2

            let ``check sub edges`` =
                let edge1 = main1.Edges |> Seq.exactlyOne
                let edge2 = main2.Edges |> Seq.exactlyOne
                edge1.ToText() === "L_F_Main1_T.Cp > L_F_Main1_T.Cm[WeakSetEdge]"
                edge2.ToText() === "L_F_Main2_T.Cm |> L_F_Main2_T.Cp[WeakResetEdge]"
                edge1 :? WeakSetEdge |> ShouldBeTrue
                edge2 :? WeakResetEdge |> ShouldBeTrue

                let s1 = edge1.Sources|> Seq.exactlyOne
                ()

            let ``check call tag with real segment`` =
                let systemP = builder.Model.Systems |> Seq.find(fun s -> s.Name = "P")
                let flowP = systemP.RootFlows |> Seq.exactlyOne
                let vp = flowP.ChildVertices |> Seq.ofType<Segment> |> Seq.find(fun s -> s.Name = "Vp")
                let cpStart = main1Cp.GetStartTags() |> Seq.exactlyOne
                cpStart.Name === "L_F_Main1_T.Cp_P_F_Vp_Start"
                let vpStart = vp.TagsStart |> Seq.find(fun t -> t.Name = cpStart.Name)
                cpStart.Name === vpStart.Name
                //cpStart =!= vpStart

(*
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main1 = { T.Cp > T.Cm; }
        Main2 = { T.Cm |> T.Cp; }
        Main1 > Main2;
    }
}
*)

                let cpEnd = main1Cp.GetEndTags() |> Seq.exactlyOne
                cpEnd.Name === "L_F_Main1_T.Cp_P_F_Sp_End"
                let sp = flowP.ChildVertices |> Seq.ofType<Segment> |> Seq.find(fun s -> s.Name = "Sp")

                let spEnd = sp.TagsEnd |> Seq.find(fun t -> t.Name = cpEnd.Name)
                cpEnd.Name === spEnd.Name
                //cpEnd =!= spEnd


                let ``check main1`` =
                    // Call 이 실제 사용하는 외부 시스템의 real segment 를 찾아서, 해당 segment 의 tag 가 존재하는 지 검사.
                    let segMain1CpTx = (main1Cp.Coin :?> SubCall).Prototype.TXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne
                    let segMain1CpRx = (main1Cp.Coin :?> SubCall).Prototype.RXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne

                    segMain1CpTx.QualifiedName === "P_F_Vp"
                    segMain1CpRx.QualifiedName === "P_F_Sp"

                    let cpStart_ = segMain1CpTx.TagsStart |> Seq.filter(fun t -> t.Name = cpStart.Name) |> Seq.exactlyOne
                    let cpEnd_   = segMain1CpRx.TagsEnd   |> Seq.filter(fun t -> t.Name = cpEnd.Name)   |> Seq.exactlyOne

                    cpStart.Cpu =!= cpStart_.Cpu
                    cpEnd.Cpu =!= cpEnd_.Cpu


                    let segMain1CmTx = (main1Cm.Coin :?> SubCall).Prototype.TXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne
                    let segMain1CmRx = (main1Cm.Coin :?> SubCall).Prototype.RXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne

                    segMain1CmTx.QualifiedName === "P_F_Vm"
                    segMain1CmRx.QualifiedName === "P_F_Sm"

                    let cpStart_ = segMain1CpTx.TagsStart |> Seq.filter(fun t -> t.Name = cpStart.Name) |> Seq.exactlyOne
                    let cpEnd_   = segMain1CpRx.TagsEnd   |> Seq.filter(fun t -> t.Name = cpEnd.Name)   |> Seq.exactlyOne

                    cpStart.Cpu =!= cpStart_.Cpu
                    cpEnd.Cpu =!= cpEnd_.Cpu


                let ``check main2`` =
                    // Call 이 실제 사용하는 외부 시스템의 real segment 를 찾아서, 해당 segment 의 tag 가 존재하는 지 검사.
                    let segMain2CpTx = (main2Cp.Coin :?> SubCall).Prototype.TXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne
                    let segMain2CpRx = (main2Cp.Coin :?> SubCall).Prototype.RXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne

                    segMain2CpTx.QualifiedName === "P_F_Vp"
                    segMain2CpRx.QualifiedName === "P_F_Sp"

                    let cpStart_ = segMain2CpTx.TagsStart |> Seq.filter(fun t -> t.Name = cpStart.Name) |> Seq.exactlyOne
                    let cpEnd_   = segMain2CpRx.TagsEnd   |> Seq.filter(fun t -> t.Name = cpEnd.Name)   |> Seq.exactlyOne

                    cpStart.Cpu =!= cpStart_.Cpu
                    cpEnd.Cpu =!= cpEnd_.Cpu


                    let segMain2CmTx = (main2Cm.Coin :?> SubCall).Prototype.TXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne
                    let segMain2CmRx = (main2Cm.Coin :?> SubCall).Prototype.RXs |> Enumerable.OfType<Segment> |> Seq.exactlyOne

                    segMain2CmTx.QualifiedName === "P_F_Vm"
                    segMain2CmRx.QualifiedName === "P_F_Sm"

                    let cpStart_ = segMain2CpTx.TagsStart |> Seq.filter(fun t -> t.Name = cpStart.Name) |> Seq.exactlyOne
                    let cpEnd_   = segMain2CpRx.TagsEnd   |> Seq.filter(fun t -> t.Name = cpEnd.Name)   |> Seq.exactlyOne

                    cpStart.Cpu =!= cpStart_.Cpu
                    cpEnd.Cpu =!= cpEnd_.Cpu


                ()

            ()





        [<Fact>]
        member __.``Find object from model`` () =
            logInfo "============== Find object from model"
            let mutable text = """
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main1 = { T.Cp > T.Cm; }
        Main2 = { T.Cm |> T.Cp; }
        Main1 > Main2;
    }
}
"""
            text <- text + sysP + cpus

            let builder = new EngineBuilder(text, "Cpu")
            let model = builder.Model

            let ``model object inspection`` =
                let sysL = model.FindObject<DsSystem>("L");
                let sysP = model.FindObject<DsSystem>("P");
                sysL.Name === "L"
                sysP.Name === "P"

                let t = model.FindObject<DsTask>("L.T");
                t.Name === "T"
                let cp = model.FindObject<CallPrototype>("L.T.Cp");
                cp.Name === "Cp"
                let cm = model.FindObject<CallPrototype>("L.T.Cm");
                cm.Name === "Cm"

                let f = model.FindObject<RootFlow>("L.F");
                f.Name === "F"
                let main1 = model.FindObject<Segment>("L.F.Main1");
                main1.Name === "Main1"
                let main2 = model.FindObject<Segment>("L.F.Main2");
                main2.Name === "Main2"

                let main1CallInstanceCp = model.FindObject<Child>("L.F.Main1.T.Cp");
                main1CallInstanceCp.Name === "T.Cp"
                main1CallInstanceCp.QualifiedName === "L_F_Main1_T.Cp"

            let ``call site tag <--> real segment tag`` =
                let vp = model.FindObject<Segment>("P.F.Vp");
                vp.Name === "Vp"

                let vpTagsStart = vp.TagsStart |> Array.ofSeq
                let cp = model.FindObject<Child>("L.F.Main1.T.Cp");
                let cpStart = cp.GetStartTags() |> Seq.exactlyOne
                let vpStart = vpTagsStart |> Seq.filter(fun t -> t.Name = cpStart.Name) |> Seq.exactlyOne
                cpStart.Name === vpStart.Name
                cpStart =!= vpStart
                cpStart.Cpu =!= vpStart.Owner

                let pp = model.FindObject<Segment>("P.F.Pp");
                pp.Name === "Pp"


                let sp = model.FindObject<Segment>("P.F.Sp");
                sp.Name === "Sp"
                let spTagsEnd = sp.TagsEnd |> Array.ofSeq
                let cpEnd = cp.GetEndTags() |> Seq.exactlyOne
                let spEnd = spTagsEnd|> Seq.filter(fun t -> t.Name = cpEnd.Name) |> Seq.exactlyOne
                cpEnd.Name === spEnd.Name
                cpEnd =!= spEnd
                cpEnd.Cpu =!= spEnd.Owner

            ()