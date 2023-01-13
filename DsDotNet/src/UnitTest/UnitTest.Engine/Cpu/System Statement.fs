namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec13_SystemStatement() =
    do Fixtures.SetUpTest()

    let t = CpuTestSample()
    [<Test>]
    member __.``Y1 System Bit Set Flow`` () = 
        t.Sys.Y1_SystemBitSetFlow() |> doChecks
        
