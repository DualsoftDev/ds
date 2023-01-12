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
    member __.``O1 Auto Operation Mode`` () = 
        for flow in t.Flows do
            flow.O1_AutoOperationMode() |> doCheck

    [<Test>]
    member __.``O2 Manual Operation Mode`` () = 
        for flow in t.Flows do
            flow.O2_ManualOperationMode() |> doCheck

    [<Test>]
    member __.``O3 Drive Operation Mode`` () = 
        for flow in t.Flows do
            flow.O3_DriveOperationMode() |> doCheck

    [<Test>]
    member __.``O4 TestRun OperationM ode`` () =   
        for flow in t.Flows do
            flow.O4_TestRunOperationMode() |> doCheck

    [<Test>] 
    member __.``O5 Emergency Mode`` () =
        for flow in t.Flows do
            flow.O5_EmergencyMode()  |> doCheck

    [<Test>] 
    member __.``O6 Stop Mode`` () =
        for flow in t.Flows do
            flow.O6_StopMode()  |> doCheck

    [<Test>] 
    member __.``O7 Ready Mode`` () =
        for flow in t.Flows do
            flow.O7_ReadyMode()  |> doCheck

