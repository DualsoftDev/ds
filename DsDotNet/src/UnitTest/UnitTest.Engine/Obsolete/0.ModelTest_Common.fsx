namespace T

open System.Linq
open Dual.Common.Core.FS
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


    let seqEq(a, b) = Enumerable.SequenceEqual(a, b) |> ShouldBeTrue
    let setEq(xs:'a seq, ys:'a seq) =
        (xs.Count() = ys.Count() && xs |> Seq.forall(fun x -> ys.Contains(x)) ) |> ShouldBeTrue

    let wait(cpu:Cpu) =
        while cpu.NeedWait do
            Thread.Sleep(50)

    let createDummyCpu() = new Cpu("dummy", new DsSystem("dummy", new Model()))
