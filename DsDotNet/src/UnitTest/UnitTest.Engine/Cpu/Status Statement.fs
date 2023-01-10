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
            rgfh.First() |> doCheck                 //Ready
            rgfh.Skip(1).Take(1).First() |> doCheck //Going
            rgfh.Skip(2).Take(1).First() |> doCheck //Finish
            rgfh.Last() |> doCheck                  //Homing

    [<Test>]
    member __.``S2 CoinRGFH`` () =
        for coin in t.Coins do
            let rgfh = coin.S1_RealRGFH() 
            rgfh.First() |> doCheck                 //Ready
            rgfh.Skip(1).Take(1).First() |> doCheck //Going
            rgfh.Skip(2).Take(1).First() |> doCheck //Finish
            rgfh.Last() |> doCheck                  //Homing
      