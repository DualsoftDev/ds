namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS


type XgxSubroutineTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let code = """
int sum(int a, int b) => 2 * ($a + $b);
int nn1 = sum(1, 2);
$nn1 = sum(1, 2);
"""
    member x.``Subroutine test`` () =
        code |> x.TestCode (getFuncName()) |> ignore
            
    member x.``Subroutine test2`` () =
        let code = code + """
int nn2 = sum(sum(1, 2), 3);
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        statements[1].Do()
        storages["nn1"].BoxedValue === 6
        statements[3].Do()
        storages["nn2"].BoxedValue === 18

type XgiSubroutineTest() =
    inherit XgxSubroutineTest(XGI)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()
    [<Test>] member __.``Subroutine test2`` () = base.``Subroutine test2``()

type XgkSubroutineTest() =
    inherit XgxSubroutineTest(XGK)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()
    [<Test>] member __.``Subroutine test2`` () = base.``Subroutine test2``()
