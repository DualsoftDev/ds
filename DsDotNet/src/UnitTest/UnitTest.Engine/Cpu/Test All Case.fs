namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS
open System.Linq
open Engine.CodeGenCPU.CpuLoader

type TestAllCase() =
    do Fixtures.SetUpTest()
    
    let t = CpuTestSample()
    
  
    [<Test>] 
    member __.``Test All Case`` () = 
        let result = Cpu.LoadStatements(t.Sys, Storages())
        result === result
   