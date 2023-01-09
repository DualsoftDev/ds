namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU

type Spec03_StatusStatement() =
    do Fixtures.SetUpTest()

    [<Test>]
    member __.``S1 RealRGFH`` () =
        Eq 1 1

    [<Test>]
    member __.``S2 CoinRGFH`` () =
        Eq 1 1
      