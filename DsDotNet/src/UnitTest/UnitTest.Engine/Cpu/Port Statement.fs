namespace T.CPU

open NUnit.Framework

open T
open Engine.CodeGenCPU

type Spec01_PortStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    do
        t.GenerationIO()


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

