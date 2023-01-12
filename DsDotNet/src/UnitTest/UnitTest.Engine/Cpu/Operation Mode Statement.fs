namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec04_OperationModeStatement() =
    do Fixtures.SetUpTest()

    let t = CpuTestSample()

    [<Test>] 
    member __.``O1 Emergency Operation Mode`` () = 
        for flow in t.Flows do
            flow.O1_EmergencyOperationMode() |> doCheck

    [<Test>]
    member __.``O2 Stop Operation Mode`` () = 
        for flow in t.Flows do
            flow.O2_StopOperationMode() |> doCheck

    [<Test>]
    member __.``O3 Manual Operation Mode `` () = 
        for flow in t.Flows do
            flow.O3_ManualOperationMode() |> doCheck

    [<Test>]
    member __.``O4 Run Operation Mode `` () =   
        for flow in t.Flows do
            flow.O4_RunOperationMode() |> doCheck

    [<Test>] 
    member __.``O5 Dry Run Operation Mode `` () =
        for flow in t.Flows do
            flow.O5_DryRunOperationMode()  |> doCheck

