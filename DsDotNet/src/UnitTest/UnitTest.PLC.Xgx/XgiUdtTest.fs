namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common
open Engine.CodeGenPLC
open Dual.UnitTest.Common.FS


[<AutoOpen>]
module XgiUdtTestModule =

    type XgiUdtTest() =
        inherit XgxTestBaseClass(XGI)

        let udtBaseCode = """
    struct Person {
        string name;
        int age;
    };
    Person hong;
    Person people[10];
    """

        [<Test>]
        member x.``xgi udt decl test`` () =
            let storages = Storages()
            let statements = parseCodeForWindows storages udtBaseCode
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml

        [<Test>]
        member x.``xgi udt decl nonexisting test`` () =
            let f = fun () ->
                let storages = Storages()
                let code = udtBaseCode + "NonExistingType nonExisting;"
                let statements = parseCodeForWindows storages code
                let f = getFuncName()
                x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
            f |> ShouldFailWithSubstringT "ERROR: UDT type NonExistingType is not declared"

        [<Test>]
        member x.``timer struct member assign test`` () =
            let storages = Storages()
            let code = """
ton myTon = createXgkTON(20u, true);
$myTon.PRE = 30u;
"""
            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
        [<Test>]
        member x.``xgi udt member assign test`` () =
            let storages = Storages()
            let code = udtBaseCode + """
            $hong.name = "Hong";
            $hong.age = 20;
            $people[0].name = "Kim";
            $people[0].age = 30;
            """

            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml

        [<Test>]
        member x.``xgi udt decl plc gen`` () =
            let result = exportXMLforLSPLC(XGI, sys, "XXXXXXXXX", None, 0, 0, 0)
            ()

