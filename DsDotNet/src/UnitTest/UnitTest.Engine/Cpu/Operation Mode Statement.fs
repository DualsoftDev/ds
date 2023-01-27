namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec04_OperationModeStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()

    [<Test>]
    member __.``O1 Ready Operation Mode`` () =
        for flow in t.Flows do
            flow.O1_ReadyMode() |> doCheck

    [<Test>]
    member __.``O2 Auto Operation Mode`` () =
        for flow in t.Flows do
            flow.O2_AutoOperationMode() |> doCheck

    [<Test>]
    member __.``O3 Manual Operation Mode`` () =
        for flow in t.Flows do
            flow.O3_ManualOperationMode() |> doCheck

    [<Test>]
    member __.``O4 DriveOperation Operation Mode`` () =
        for flow in t.Flows do
            flow.O4_DriveOperationMode() |> doCheck

    [<Test>]
    member __.``O5 TestRunOperation Mode`` () =
        for flow in t.Flows do
            flow.O5_TestRunOperationMode()  |> doCheck

    [<Test>]
    member __.``O6 Emergency Mode`` () =
        for flow in t.Flows do
            flow.O6_EmergencyMode()  |> doCheck

    [<Test>]
    member __.``O7 Stop Mode`` () =
        for flow in t.Flows do
            flow.O7_StopMode()  |> doCheck

