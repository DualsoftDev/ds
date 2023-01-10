namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec06_RealStatement() =
    do Fixtures.SetUpTest()
    let t = CpuTestSample()

    [<Test>]
    member __.``R1 Real Initial Start`` () = 
            for real in t.Reals do
                real.R1_RealInitialStart() |> doCheck

    [<Test>]
    member __.``R2 Real Job Complete`` () = 
            for real in t.Reals do
                real.R2_RealJobComplete() |> doCheck

