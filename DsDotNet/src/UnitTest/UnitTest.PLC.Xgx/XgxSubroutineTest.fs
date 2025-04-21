namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open Engine.Parser.FS


type XgxSubroutineTest(xgx:HwCPU) =
    inherit XgxTestBaseClass(xgx)

    let code = """
int sum(int a, int b) => ($a + $b);
int nn1 = sum(1, 2);
$nn1 = sum(1, 2);
"""
    member x.``Subroutine lambda test`` () =
        if enableSubroutine then
            code |> x.TestCode (getFuncName()) |> ignore
            
    member x.``Subroutine lambda test1`` () =
        if enableSubroutine then
            let code = """
        double pi() => (3.0 + 0.14);
        double pp = pi();
    """
            let storages, statements = code |> x.TestCode (getFuncName())
            storages["pp"].BoxedValue === 3.14

    member x.``Subroutine lambda test2`` () =
        if enableSubroutine then
            let code = """
        int sum(int a, int b) => ($a + $b);
        int nn1 = sum(1, 2);
    """
            let storages, statements = code |> x.TestCode (getFuncName())
            storages["nn1"].BoxedValue === 3

    member x.``Subroutine lambda test3`` () =
        if enableSubroutine then
            let code = """
    int sum(int a, int b) => ($a + $b);
    int nn1 = sum(sum(1, 2), 3);
    """
            let storages, statements = code |> x.TestCode (getFuncName())
            statements[1].Do()
            storages["nn1"].BoxedValue === 6



    member x.``Subroutine proc test1`` () =
        if enableSubroutine then
            let code = """
        int nn1 = 1;
        void doit() {
            $nn1 = 2;
        }
        doit();
    """
            let storages, statements = code |> x.TestCode (getFuncName())
            storages["pp"].BoxedValue === 3.14


type XgiSubroutineTest() =
    inherit XgxSubroutineTest(XGI)
    [<Test>] member __.``Subroutine lambda test`` () = base.``Subroutine lambda test``()
    [<Test>] member __.``Subroutine lambda test1`` () = base.``Subroutine lambda test1``()
    [<Test>] member __.``Subroutine lambda test2`` () = base.``Subroutine lambda test2``()
    [<Test>] member __.``Subroutine lambda test3`` () = base.``Subroutine lambda test3``()
    [<Test>] member __.``Subroutine proc test1`` () = base.``Subroutine proc test1``()

type XgkSubroutineTest() =
    inherit XgxSubroutineTest(XGK)
    [<Test>] member __.``Subroutine lambda test`` () = base.``Subroutine lambda test``()
    [<Test>] member __.``Subroutine lambda test1`` () = base.``Subroutine lambda test1``()
    [<Test>] member __.``Subroutine lambda test2`` () = base.``Subroutine lambda test2``()
    [<Test>] member __.``Subroutine lambda test3`` () = base.``Subroutine lambda test3``()
    [<Test>] member __.``Subroutine proc test1`` () = base.``Subroutine proc test1``()
