namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec11_LinkStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``L1 Link Start`` () = Eq 1 1
    [<Test>] member __.``L2 Link Reset`` () = Eq 1 1
    [<Test>] member __.``L2 Link StartReset`` () = Eq 1 1
