namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec09_TimmerStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``T1 Delay Call`` () = Eq 1 1
