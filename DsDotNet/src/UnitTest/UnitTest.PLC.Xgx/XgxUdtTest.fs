namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common
open Engine.CodeGenPLC
open Dual.UnitTest.Common.FS



type XgxUdtTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let udtBaseCode = """
struct Person {
    string name;
    int age;
};
Person hong;
Person people[10];
"""

    member x.``timer struct member assign test`` () =
        let storages = Storages()
        let code = """
ton myTon = createXgkTON(20u, true);
$myTon.PRE = 30u;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
    member x.``UDT decl test`` () =
        let f = getFuncName()
        let doit() =
            let storages = Storages()
            let statements = parseCodeForWindows storages udtBaseCode
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> (fun () -> doit()) |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"

    member x.``UDT decl nonexisting test`` () =
        let f = getFuncName()
        let doit = fun () ->
            let storages = Storages()
            let code = udtBaseCode + "NonExistingType nonExisting;"
            let statements = parseCodeForWindows storages code
            x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
        doit |> ShouldFailWithSubstringT "ERROR: UDT type NonExistingType is not declared"

    member x.``UDT member assign test`` () =
        let f = getFuncName()
        let doit() =
            let storages = Storages()
            let code = udtBaseCode + """
            $hong.name = "Hong";
            $hong.age = 20;
            $people[0].name = "Kim";
            $people[0].age = 30;
            """

            let statements = parseCodeForWindows storages code
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> (fun () -> doit()) |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"

    member x.``UDT decl plc gen`` () =
        let result = exportXMLforLSPLC(XGI, sys, "XXXXXXXXX", None, 0, 0, 0)
        ()



type XgiUdtTest() =
    inherit XgxUdtTest(XGI)
    [<Test>] member __.``timer struct member assign test`` () = base.``timer struct member assign test``()
    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()


type XgkUdtTest() =
    inherit XgxUdtTest(XGK)
    [<Test>] member __.``timer struct member assign test`` () = base.``timer struct member assign test``()
    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()
