namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC
open Dual.UnitTest.Common.FS



type XgxBitwiseTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Bitwise AND test`` () =
        let storages = Storages()
        let code = """
bool truth = 8 &&& 255 == 8;
bool falsy = 8 &&& 255 == 3;
uint nn1 = 0u;
uint nn2 = 0u;
$nn1 = 8u &&& 255u;
$nn2 = 8u ||| 255u;
"""
        let f = getFuncName()
        let statements = parseCodeForWindows storages code
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


type XgiBitwiseTest() =
    inherit XgxBitwiseTest(XGI)
    [<Test>] member __.``Bitwise AND test`` () = base.``Bitwise AND test``()


type XgkBitwiseTest() =
    inherit XgxBitwiseTest(XGK)
    [<Test>] member __.``Bitwise AND test`` () = base.``Bitwise AND test``()
