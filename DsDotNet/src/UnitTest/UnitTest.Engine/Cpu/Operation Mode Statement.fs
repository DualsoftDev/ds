namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU


type Spec04_OperationModeStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``O1 Emergency Operation Mode`` () = Eq 1 1
    [<Test>] member __.``O2 Stop Operation Mode`` () = Eq 1 1
    [<Test>] member __.``O3 Manual Operation Mode `` () = Eq 1 1
    [<Test>] member __.``O4 Run Operation Mode `` () = Eq 1 1
    [<Test>] member __.``O5 Dry Run Operation Mode `` () = Eq 1 1
