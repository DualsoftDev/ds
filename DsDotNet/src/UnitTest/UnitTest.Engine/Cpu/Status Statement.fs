namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

type Spec03_StatusStatement() =
    do Fixtures.SetUpTest()

    let t = CpuTestSample()

    [<Test>]
    member __.``S1 RealRGFH`` () =
        for real in t.Reals do
            let rgfh = real.S1_RealRGFH() 
            rgfh[0] |> doCheck     //Ready
            rgfh[1] |> doCheck     //Going
            rgfh[2] |> doCheck     //Finish
            rgfh[3] |> doCheck     //Homing

    [<Test>]
    member __.``S2 CoinRGFH`` () =
        for coin in t.Coins do
            let rgfh = coin.S2_CoinRGFH() 
            rgfh[0] |> doCheck     //Ready
            rgfh[1] |> doCheck     //Going
            rgfh[2] |> doCheck     //Finish
            rgfh[3] |> doCheck     //Homing
      