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

    let t = CpuTestSample(WINDOWS)

    [<Test>]
    member __.``F1 Root Start`` () =
        for real in t.Reals do
            real.F1_RootStartActive() |> doCheck
            real.F1_RootStartPassive() |> doCheck

    [<Test>]
    member __.``F2 Root Reset`` () =
        for real in t.Reals do
            real.F2_RootResetActive() |> doCheck
            real.F2_RootResetPassive() |> doCheck

    [<Test>]
    member __.``F3 VertexEnd WithOutReal`` () =
        for v in t.AbleVertexInFlows do
            v.V.F3_RealEndInFlow() |> doCheck 
