namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec06_RealStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample(WINDOWS)

    [<Test>]
    member __.``R1 Real Initial Start`` () =
            for real in t.Reals do
                real.R1_RealInitialStart() |> doCheck

    [<Test>]
    member __.``R2 Real Job Complete`` () =
            for real in t.Reals do
                real.RealEndActive() |> doChecks
                real.RealEndPassive() |> doChecks

    [<Test>]
    member __.``R3 Real Start Point`` () =
            for real in t.Reals do
                real.R3_RealStartPoint() |> doCheck

