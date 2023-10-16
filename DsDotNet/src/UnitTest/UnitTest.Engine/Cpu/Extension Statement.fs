namespace T.CPU
open Dual.UnitTest.Common.FS

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec14_ExtensionStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    [<Test>] member __.``E1`` () = Eq 1 1
