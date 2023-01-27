namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec04_OperationStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()

    [<Test>]
    member __.``O1 Ready Operation State`` () =
        for flow in t.Flows do
            flow.O1_ReadyOperationState() |> doCheck

    [<Test>]
    member __.``O2 Auto Operation State`` () =
        for flow in t.Flows do
            flow.O2_AutoOperationState() |> doCheck

    [<Test>]
    member __.``O3 Manual Operation State`` () =
        for flow in t.Flows do
            flow.O3_ManualOperationState() |> doCheck

    [<Test>]
    member __.``O4 Emergency Operation State`` () =
        for flow in t.Flows do
            flow.O4_EmergencyOperationState()  |> doCheck

    [<Test>]
    member __.``O5 Stop Operation State`` () =
        for flow in t.Flows do
            flow.O5_StopOperationState()  |> doCheck

    [<Test>]
    member __.``O6 Drive Operation Mode`` () =
        for flow in t.Flows do
            flow.O6_DriveOperationMode() |> doCheck

    [<Test>]
    member __.``O7 Test Operation Mode`` () =
        for flow in t.Flows do
            flow.O7_TestOperationMode()  |> doCheck

    [<Test>]
    member __.``O8 Idle Operation Mode`` () =
        for flow in t.Flows do
            flow.O8_IdleOperationMode()  |> doCheck
