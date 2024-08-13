namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

type Spec05_MonitorStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample(WINDOWS)

    [<Test>]
    member __.``M1 Origin Monitor`` () =
        for real in t.Reals do
            real.M1_OriginMonitor() |> doCheck

    [<Test>]
    member __.``M2 Pause Monitor`` () =
        for v in t.Reals do
            v.M2_PauseMonitor() |> doCheck

   