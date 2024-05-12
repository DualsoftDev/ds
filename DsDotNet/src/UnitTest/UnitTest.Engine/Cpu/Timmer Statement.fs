namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec09_TimmerStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    //[<Test>]
    //member __.``T1 Delay Call`` () =
    //    t.Sys.T1_DelayCall() |> doChecks
