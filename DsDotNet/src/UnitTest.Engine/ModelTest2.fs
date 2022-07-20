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


