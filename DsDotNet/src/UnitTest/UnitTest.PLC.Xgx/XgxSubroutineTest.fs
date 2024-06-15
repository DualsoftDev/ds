namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS



type XgxSubroutineTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let code = """
int sum(int a, int b) => 2 * ($a + $b);
int nn1 = sum(1, 2);
$nn1 = sum(1, 2);
"""

    member x.``Subroutine test`` () =
        code |> x.TestCode (getFuncName()) |> ignore
            

type XgiSubroutineTest() =
    inherit XgxSubroutineTest(XGI)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()

type XgkSubroutineTest() =
    inherit XgxSubroutineTest(XGK)
    [<Test>] member __.``Subroutine test`` () = base.``Subroutine test``()
