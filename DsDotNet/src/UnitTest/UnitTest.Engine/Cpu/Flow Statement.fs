namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq


type Spec02_FlowStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()

    [<Test>]
    member __.``F1 Root Start`` () =
        for real in t.Reals do
            real.F1_RootStart() |> doChecks

    [<Test>]
    member __.``F2 Root Reset`` () =
        for real in t.Reals do
            real.F2_RootReset() |> doChecks

    [<Test>]
    member __.``F3 VertexEnd WithOutReal`` () =
        for real in t.VertexInFlows do
            real.V.F3_VertexEndWithOutReal() |> doCheck

            