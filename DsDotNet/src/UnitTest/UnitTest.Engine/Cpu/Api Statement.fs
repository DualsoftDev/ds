namespace T.CPU

open System.Linq
open Engine.Core
open NUnit.Framework

open T
open Engine.CodeGenCPU

type Spec01_PortStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample(WINDOWS)

    [<Test>]
    member __.``A1_PlanSend`` () =
        for api, coins in t.ApiCoinsSet do
            if coins.Any() then
                api.A1_PlanSend(t.Sys, coins) |> doCheck
    [<Test>]
    member __.``A2_PlanReceive`` () =
        for api, coins in t.ApiCoinsSet do
            api.A2_PlanReceive(t.Sys) |> doCheck
    [<Test>]
    member __.``A3_SensorLinking`` () =
        for api, coins in t.ApiCoinsSet do
            if coins.Any() then
                api.A3_SensorLinking(t.Sys, coins.OfType<Call>()) |> doCheck
    [<Test>]
    member __.``A4_SensorLinked`` () =
        for api, coins in t.ApiCoinsSet do
            if coins.Any() then
                api.A4_SensorLinked(t.Sys, coins.OfType<Call>()) |> doCheck

