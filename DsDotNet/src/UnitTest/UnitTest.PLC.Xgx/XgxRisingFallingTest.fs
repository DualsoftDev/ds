namespace T
open Dual.UnitTest.Common.FS

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS


type XgxRisingFallingTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Normal, Negation, Rising, Falling contact test`` () =
        let storages = Storages()
        let code = """
            bool ix = createTag("%IX0.0.0", false);
            bool qx = createTag("%QX0.1.0", false);
            $qx := $ix && ! $ix && rising($ix) && falling($ix);
"""

        let statements = parseCodeForWindows storages code
        statements.Length === 1
        statements[0].ToText() === "$qx := $ix && !($ix) && rising($ix) && falling($ix)"

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

 
type XgiRisingFallingTest() =
    inherit XgxRisingFallingTest(XGI)
    [<Test>] member x.``Normal, Negation, Rising, Falling contact test`` () = base.``Normal, Negation, Rising, Falling contact test`` ()

     