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
    member __.``A1_ApiSet`` () = () //test ahn
        //for td, coins in t.TaskDevCallSet do
        //    if coins.Any() then
        //        getAM(td.ApiItem).A1_ApiSet(td) |> doChecks
    [<Test>]
    member __.``A2_ApiEnd`` () = () //test ahn
        //for api, coins in t.ApiCallSet do
        //    api.A2_ApiEnd(t.Sys) |> doCheck
    [<Test>]
    member __.``A3_SensorLinking`` () = () //test ahn
        //for api, coins in t.ApiCallSet do
        //    if coins.Any() then
        //        api.A3_SensorLinking(t.Sys, coins.OfType<Call>()) |> doCheck
    [<Test>]
    member __.``A4_SensorLinked`` () = () //test ahn
        //for api, coins in t.ApiCallSet do
        //    if coins.Any() then
        //        api.A4_SensorLinked(t.Sys, coins.OfType<Call>()) |> doCheck

