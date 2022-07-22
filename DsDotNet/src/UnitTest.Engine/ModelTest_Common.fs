namespace UnitTest.Engine

open System.Linq
open Dual.Common


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
    let cpuL = """
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
}
"""


    let seqEq(a, b) = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue
    let setEq(xs:'a seq, ys:'a seq) =
        (xs.Count() = ys.Count() && xs |> Seq.forall(fun x -> ys.Contains(x)) ) |> ShouldBeTrue