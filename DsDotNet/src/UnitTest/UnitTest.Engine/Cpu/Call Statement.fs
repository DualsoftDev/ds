namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS
open System.Linq

type Spec07_CallStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample(WINDOWS)


    [<Test>]
    member __.``C1 CallMemo`` () =
        for call in t.Sys.GetVerticesHasJobInReal().Select(getVM) do
            call.C1_CallMemo() |> doCheck


    [<Test>]
    member __.`` J1 JobActionOuts`` () =
        for c in t.Sys.GetVerticesOfJobCalls() do
            c.TargetJob.J1_JobActionOuts(c) |> doChecks
