namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC
open Dual.Common.UnitTest.FS
open System



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
            let code = udtBaseCode + """
$hong.name = "Hong";
$hong.age = 20;
copyStructIf(true, $hong, $people[0]);
copyStructIf(true, $people[0], $people[1]);
copyStructIf(true, $people[1], $hong);
"""
            let statements = parseCodeForWindows storages code
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        match xgx with
        | XGI -> doit()
        | XGK -> doit |> ShouldFailWithSubstringT "UDT declaration is not supported in XGK"
        | _ -> failwith "Not supported runtime target"

    member x.``UDT copy test2`` () =
        let f = getFuncName()
        let doit() =
            let storages = Storages()
            let code = udtBaseCode + """
copyStructIf(2 > 3, $hong, $people[0]);
copyStructIf(2 + 3 > 10, $people[0], $people[1]);
copyStructIf(true, $people[1], $hong);
"""
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

        doit |> ShouldFailWithSubstringT "Type mismatch"

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

    member x.``UDT member reference test`` () =
        let f = getFuncName()
        let storages = Storages()
        let code = udtBaseCode + """
        $people[0].age = 13;
        int ages = $people[0].age + $people[1].age;
        $ages = 1 + $people[0].age * $people[1].age;
        """

        let statements = parseCodeForWindows storages code
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``UDT decl plc gen`` () =
        let result = exportXMLforLSPLC(XGI, sys, "XXXXXXXXX", None,  0, 0, true, None)
        ()



type XgiUdtTest() =
    inherit XgxUdtTest(XGI)
    [<Test>] member __.``UDT copy test`` () = base.``UDT copy test``()
    [<Test>] member __.``UDT copy test2`` () = base.``UDT copy test2``()
    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
    [<Test>] member __.``UDT invalid member assign test`` () = base.``UDT invalid member assign test``()
    [<Test>] member __.``UDT member reference test`` () = base.``UDT member reference test``()
    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()

[<Obsolete("XGK 는 UDT 지원 안함")>]
type XgkUdtTest() =
    inherit XgxUdtTest(XGK)
//    [<Test>] member __.``UDT copy test`` () = base.``UDT copy test``()
//    [<Test>] member __.``UDT copy test2`` () = base.``UDT copy test2``()
//    [<Test>] member __.``UDT decl test`` () = base.``UDT decl test``()
//    [<Test>] member __.``UDT decl nonexisting test`` () = base.``UDT decl nonexisting test``()
//    [<Test>] member __.``UDT invalid member assign test`` () = base.``UDT invalid member assign test``()
//    [<Test>] member __.``UDT member assign test`` () = base.``UDT member assign test``()
//    [<Test>] member __.``UDT decl plc gen`` () = base.``UDT decl plc gen``()
