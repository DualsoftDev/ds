namespace T
open Dual.UnitTest.Common.FS

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS


type XgiRisingFallingTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Normal, Negation, Rising, Falling contact test`` () =
        let storages = Storages()
        let code = """
            bool ix = createTag("%IX0.0.0", false);
            bool qx = createTag("%QX0.1.0", false);
            $qx := $ix && ! $ix && rising($ix) && falling($ix);
"""

        let statements = parseCode storages code
        statements.Length === 1
        statements[0].ToText() === "$qx := $ix && !($ix) && rising($ix) && falling($ix)"

        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

   