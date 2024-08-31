namespace T.CPU
open Dual.Common.UnitTest.FS

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec14_ExtensionStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample(WINDOWS)
    [<Test>] member __.``E1`` () = Eq 1 1
