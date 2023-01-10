namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec07_CallStatement() =
    do Fixtures.SetUpTest()
    let t = CpuTestSample()

    [<Test>]
    member __.``C1 Call Action Out`` () = 
        for call in t.Calls do
            call.C1_CallActionOut() |> doChecks

    [<Test>]
    member __.``C2 Call Tx`` () = 
        for call in t.Calls do
            call.C2_CallTx() |> doChecks

    [<Test>]
    member __.``C3 Call Rx`` () =
        for call in t.Calls do
            call.C3_CallRx() |> doCheck

