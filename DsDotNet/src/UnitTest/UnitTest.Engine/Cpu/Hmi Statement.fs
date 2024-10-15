namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

type Spec16_HmiStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample(WINDOWS)

   
    [<Test>]
    member __.``H1 HmiPulse`` () =
         for v in t.Sys.GetVertices() do
            let vm = v.TagManager :?> VertexTagManager
            vm.H1_HmiPulse() |> doChecks
