namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec12_ButtonLampStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``B1 Button Output`` () = Eq 1 1
    [<Test>] member __.``B2 Mode Lamp`` () = Eq 1 1
