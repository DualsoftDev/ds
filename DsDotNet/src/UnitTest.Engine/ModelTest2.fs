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
module ModelTest2 =
    type DemoTests2(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Parse Alias & Task`` () =
            logInfo "============== Parse Cylinder"
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
            text <- text + sysP + cpuL

            let engine = new Engine(text, "Cpu")
            ( engine.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = engine.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = engine.Cpu

            cpu.Name === "Cpu"
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

            (main.CollectAlises() |> Seq.map(fun seg -> seg.Name), ["Cp2"; "Cm2"]) |> setEq
            ()


        [<Fact>]
        member __.``Tag/Edge with two main`` () =
            logInfo "============== Parse Cylinder"
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
            text <- text + sysP + cpuL

            let engine = new Engine(text, "Cpu")
            ( engine.Model.Systems |> Seq.map(fun s -> s.Name), ["L"; "P"] ) |> setEq
            let system = engine.Model.Systems |> Seq.find(fun s -> s.Name = "L")
            let cpu = engine.Cpu

            cpu.Name === "Cpu"
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
            let mutable main1CpProto:CallPrototype = null
            let mutable main2CpProto:CallPrototype = null
            
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
                
            let ``check main edges`` =
                let edge = flow.Edges |> Seq.exactlyOne
                edge.ToText() === "L_F_Main1 > L_F_Main2"
                edge.Sources |> Seq.exactlyOne === main1
                edge.Target === main2
                
            let ``check sub edges`` =
                let edge1 = main1.Edges |> Seq.exactlyOne
                let edge2 = main2.Edges |> Seq.exactlyOne
                edge1.ToText() === "L_F_Main1_T.Cp > L_F_Main1_T.Cm"
                edge2.ToText() === "L_F_Main2_T.Cm |> L_F_Main2_T.Cp"
                edge1 :? WeakSetEdge |> ShouldBeTrue
                edge2 :? WeakResetEdge |> ShouldBeTrue

                let s1 = edge1.Sources|> Seq.exactlyOne
                
                ()

            let ``check call tag with real segment`` =
                let systemP = engine.Model.Systems |> Seq.find(fun s -> s.Name = "P")
                let flowP = systemP.RootFlows |> Seq.exactlyOne
                let vp = flowP.ChildVertices |> Seq.ofType<Segment> |> Seq.find(fun s -> s.Name = "Vp")
                let cpStart = main1Cp.TagsStart |> Seq.exactlyOne
                cpStart.Name === "L_F_Main1_T.Cp_P_F_Vp_Start"
                let vpStart = vp.TagsStart |> Seq.find(fun t -> t.Name = cpStart.Name)
                cpStart.Name === vpStart.Name
                cpStart =!= vpStart

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

                let cpEnd = main1Cp.TagsEnd |> Seq.exactlyOne
                cpEnd.Name === "L_F_Main1_T.Cp_P_F_Sp_End"
                let sp = flowP.ChildVertices |> Seq.ofType<Segment> |> Seq.find(fun s -> s.Name = "Sp")

                let spEnd = sp.TagsEnd |> Seq.find(fun t -> t.Name = cpEnd.Name)
                cpEnd.Name === spEnd.Name
                cpEnd =!= spEnd

                ()
            
            ()


