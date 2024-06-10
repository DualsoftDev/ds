namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
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
    member x.``UDT copy test`` () =
        let f = getFuncName()
        let doit() =
            let storages = Storages()
            let code = udtBaseCode + "copyStructIf(true, $people[0], $people[1]);"
            let statements = parseCodeForWindows storages code
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> doit |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"


    member x.``UDT decl test`` () =
        let f = getFuncName()
        let doit() =
            let storages = Storages()
            let statements = parseCodeForWindows storages udtBaseCode
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> doit |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"

    member x.``UDT decl nonexisting test`` () =
        let f = getFuncName()
        let doit = fun () ->
            let storages = Storages()
            let code = udtBaseCode + "NonExistingType nonExisting;"
            let statements = parseCodeForWindows storages code
            x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
        doit |> ShouldFailWithSubstringT "ERROR: UDT type NonExistingType is not declared"


    member x.``UDT invalid member assign test`` () =
        let f = getFuncName()
        let doit = fun () ->
            let storages = Storages()
            let code = udtBaseCode + "$hong.name = 1;"
            let statements = parseCodeForWindows storages code
            x.generateXmlForTest f storages (map withNoComment statements) |> ignore
            
        doit |> ShouldFailWithSubstringT "ERROR: Type mismatch in member variable assignment"

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
        | XGK -> doit |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"

    member x.``UDT decl plc gen`` () =
        let result = exportXMLforLSPLC(XGI, sys, "XXXXXXXXX", None, 0, 0, 0)
        ()



type XgiUdtTest() =
    inherit XgxUdtTest(XGI)
    [<Test>] member __.``UDT copy test`` () = base.``UDT copy test``()
    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
    [<Test>] member __.``UDT invalid member assign test`` () = base.``UDT invalid member assign test``()
    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()


type XgkUdtTest() =
    inherit XgxUdtTest(XGK)
    [<Test>] member __.``UDT copy test`` () = base.``UDT copy test``()
    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
    [<Test>] member __.``UDT invalid member assign test`` () = base.``UDT invalid member assign test``()
    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()
