namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec07_CallStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample()

    [<Test>]
    member __.``C1 Call Plan Send`` () =
        for call in t.Calls do
            call.C1_CallPlanSend() |> doChecks

    [<Test>]
    member __.``C2 Call Action Out`` () =
        for call in t.Calls do
            call.C2_CallActionOut() |> doChecks

    [<Test>]
    member __.``C3 Call Plan Receive`` () =
        for call in t.Calls do
            call.C3_CallPlanReceive() |> doChecks


    //[<Test>]
    //member __.``C4 Call Action In`` () =
    //    for call in t.Calls do
    //        call.C4_CallActionIn() |> doChecks

