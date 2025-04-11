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
    member __.``C1 CallPlanStart`` () =
        for call in t.Sys.GetVerticesHasJobInReal().Select(getVM) do
            call.CallPlanStartActive() |> doCheck
            call.CallPlanStartPassive() |> doCheck
    [<Test>]
    member __.``C2 CallPlanEnd`` () =
        for call in t.Sys.GetVerticesHasJobInReal().Select(getVM) do
            call.C2_CallPlanEnd() |> doCheck

    [<Test>]
    member __.`` J1 JobActionOuts`` () =
        let devCallSet =  t.Sys.GetTaskDevCalls()
        for (td, coins) in devCallSet do
            let tm = td.TagManager :?> TaskDevManager
            tm.J1_JobActionOuts(coins, true)  |> doChecks
