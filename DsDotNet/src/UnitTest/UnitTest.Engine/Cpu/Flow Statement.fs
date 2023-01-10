namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq


type Spec02_FlowStatement() =
    do Fixtures.SetUpTest()

    let t = CpuTestSample()
  
    [<Test>] 
    member __.``F1 Root Start Real`` () = 
        for real in t.Reals do
            real.F1_RootStartReal() |> doChecks

    [<Test>] 
    member __.``F2 Root Reset Real`` () = 
        for real in t.Reals do
            real.F2_RootResetReal() |> doChecks

    [<Test>]
    member __.``F3 Root Start Coin`` () = 
        for real in t.Reals do
            real.F3_RootStartCoin() |> doChecks

    [<Test>]
    member __.``F4 Root Reset Coin`` () = 
        for coin in t.Coins do
            coin.F4_RootCoinRelay()   |> doCheck
