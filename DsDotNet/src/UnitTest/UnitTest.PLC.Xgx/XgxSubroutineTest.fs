namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS


type XgxSubroutineTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let code = """
int sum(int a, int b) => ($a + $b);
int nn1 = sum(1, 2);
$nn1 = sum(1, 2);
"""
    member x.``Subroutine test`` () =
        code |> x.TestCode (getFuncName()) |> ignore
            
    member x.``Subroutine test1`` () =
        let code = """
    double pi() => (3.0 + 0.14);
    double pp = pi();
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        storages["pp"].BoxedValue === 3.14

    member x.``Subroutine test2`` () =
        let code = """
    int sum(int a, int b) => ($a + $b);
    int nn1 = sum(1, 2);
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        storages["nn1"].BoxedValue === 3

    member x.``Subroutine test3`` () =
        let code = """
int sum(int a, int b) => ($a + $b);
int nn1 = sum(sum(1, 2), 3);
"""
        let storages, statements = code |> x.TestCode (getFuncName())
        statements[1].Do()
        storages["nn1"].BoxedValue === 6


type XgiSubroutineTest() =
    inherit XgxSubroutineTest(XGI)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()
    [<Test>] member __.``Subroutine test1`` () = base.``Subroutine test1``()
    [<Test>] member __.``Subroutine test2`` () = base.``Subroutine test2``()
    [<Test>] member __.``Subroutine test3`` () = base.``Subroutine test3``()

type XgkSubroutineTest() =
    inherit XgxSubroutineTest(XGK)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()
    [<Test>] member __.``Subroutine test1`` () = base.``Subroutine test1``()
    [<Test>] member __.``Subroutine test2`` () = base.``Subroutine test2``()
    [<Test>] member __.``Subroutine test3`` () = base.``Subroutine test3``()
