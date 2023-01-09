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

    [<Test>] member __.``System Bit`` () = Eq 1 1
