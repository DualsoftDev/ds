namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS
open System.Linq

type Spec01_PortStatement() =
    do Fixtures.SetUpTest()
    
    let t = CpuTestSample()
    
  
    [<Test>] 
    member __.``P1 Real Start Port`` () = 
        for real in t.Reals do
            real.P1_RealStartPort() |> doCheck

    [<Test>] 
    member __.``P2 Real Reset Port`` () = 
        for real in t.Reals do
            real.P2_RealResetPort()  |> doCheck

    [<Test>] 
    member __.``P3 Real End Port`` () = 
        for real in t.Reals do
            real.P3_RealEndPort()   |> doCheck

    //[<Test>] 
    //member __.``P4 Call Start Port`` () = 
    //    for real in t.Calls do
    //        real.P4_CallStartPort()   |> doCheck
    
    //[<Test>] 
    //member __.``P5 Call Reset Port`` () = 
    //    for real in t.Calls do
    //        real.P5_CallResetPort()  |> doCheck 

    //[<Test>] 
    //member __.``P6 Call End Port`` () = 
    //    for real in t.Calls do
    //        real.P6_CallEndPort()   |> doCheck 

        