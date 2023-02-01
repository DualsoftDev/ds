namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec08_DAGStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    do
            t.GenerationIO()
    [<Test>]
    member __.``D1 DAG Head Start`` () =
        for real in t.Reals do
            real.D1_DAGHeadStart() |> doChecks

    [<Test>]
    member __.``D2 DAG Tail Start`` () =
        for real in t.Reals do
            real.D2_DAGTailStart() |> doChecks

    [<Test>]
    member __.``D3 DAG Coin Complete`` () =
        for real in t.Reals do
            real.D3_DAGCoinComplete() |> doChecks

