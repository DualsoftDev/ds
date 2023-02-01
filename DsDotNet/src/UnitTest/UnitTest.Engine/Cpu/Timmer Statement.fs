namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec09_TimmerStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    do
        t.GenerationIO()
    [<Test>]
    member __.``T1 Delay Call`` () =
        t.Sys.T1_DelayCall() |> doChecks
