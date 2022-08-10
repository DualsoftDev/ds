namespace UnitTest.Engine

open System.Linq
open Dual.Common
open Engine.Core
open System.Threading


[<AutoOpen>]
module ModelTest_Common =

    let sysP = """
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Pp |> Sm;
        Pm |> Sp;
        Vp <||> Vm;
    }
}
"""
    let cpus = """
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
    [cpu] FakeCpu = {
        P.F;
    }
}
"""


    let seqEq(a, b) = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue
    let setEq(xs:'a seq, ys:'a seq) =
        (xs.Count() = ys.Count() && xs |> Seq.forall(fun x -> ys.Contains(x)) ) |> ShouldBeTrue

    let wait(cpu:Cpu) =
        while cpu.Running && (cpu.ProcessingQueue || cpu.Queue.Count > 0) do
            Thread.Sleep(50)
