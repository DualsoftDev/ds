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
    member __.``A1_ApiSet`` () =
        let apiDevSet = t.Sys.GetDistinctApisWithDeviceCall()
        for (api, td, calls) in apiDevSet do
            getAM(api).A1_ApiSet(td, calls) |> doChecks

    [<Test>]
    member __.``A2_ApiEnd`` () = 
        let apiDevSet = t.Sys.GetDistinctApisWithDeviceCall()
        for (api, td, calls) in apiDevSet do
            getAM(api).A2_ApiEnd() |> doCheck

    [<Test>]
    member __.``TD1 SensorLinking`` () =
         let devCallSet =  t.Sys.GetTaskDevsCoin()
         for (td, call) in devCallSet do
                getDM(td).TD1_SensorLinking(call) |> doChecks
    [<Test>]
    member __.``TD2 SensorLinked`` () = 
         let devCallSet =  t.Sys.GetTaskDevsCoin()
         for (td, call) in devCallSet do
                getDM(td).TD2_SensorLinked(call) |> doChecks
