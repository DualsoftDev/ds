namespace T


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI


type XgiRisingFallingTest() =
    inherit XgiTestClass()

    [<Test>]
    member __.``Normal, Negation, Rising, Falling contact test`` () =
        let storages = Storages()
        let code = """
            bool ix = createTag("%IX0.0.0", false);
            bool qx = createTag("%QX0.1.0", false);
            $qx := $ix && not($ix) && rising($ix) && falling($ix);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


