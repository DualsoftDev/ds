namespace T.Types
open T

open Dual.Common.UnitTest.FS
open NUnit.Framework
open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS


type XgxRisingFallingTest(xgx:HwCPU) =
    inherit XgxTestBaseClass(xgx)
    let baseCode =
            match xgx with
            | XGI -> """
                bool ix = createTag("%IX0.0.0", false);
                bool qx = createTag("%QX0.1.0", false);
                """
            | XGK -> """
                bool ix = createTag("P00000", false);
                bool qx = createTag("P00001", false);
                """
            | _ -> failwith "Not supported plc type"


    member x.``Negation, Rising, Falling contact test`` () =
        let storages = Storages()
        let testCode = "$qx = rising(!($ix)) && falling(!($ix));"
        let code = baseCode +  testCode

        let statements = parseCodeForTarget storages code xgx
        statements.Length === 1
        statements[0].ToText() === testCode

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Normal, Rising, Falling contact test`` () =
        let storages = Storages() //확인필요 !(!($ix)) ==> !($ix) PLC 내려갈때 처리되고 있습니다.
        let testCode =
            """$qx =       !$ix
                    &&    !!$ix
                    &&   !!!$ix
                    &&  !!!!$ix
                    && !!!!!$ix
                    && rising($ix) && falling($ix);"""
        let code = baseCode + testCode

        let statements = parseCodeForTarget storages code xgx
        statements.Length === 1
        let xxx = statements[0].ToText()
        //statements[0].ToText() === testCode.TrimEnd(';')

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``RisingAfter, FallingAfter contact test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool ix1 = createTag("%IX0.0.0", false);
                bool ix2 = createTag("%IX0.0.1", false);
                bool ix3 = createTag("%IX0.0.2", false);
                bool qx1 = createTag("%QX0.1.0", false);
                bool qx2 = createTag("%QX0.1.1", false);
                bool qx3 = createTag("%QX0.1.2", false);
                """
            | XGK -> """
                bool ix1 = createTag("P00000", false);
                bool ix2 = createTag("P00001", false);
                bool ix3 = createTag("P00002", false);
                bool qx1 = createTag("P00010", false);
                bool qx2 = createTag("P00012", false);
                bool qx3 = createTag("P00013", false);
                """
            | _ -> failwith "Not supported plc type"
            + """
                $qx1 = $ix1 && risingAfter($ix2 && !($ix3));
                $qx2 = $ix1 || fallingAfter($ix2 && !($ix3));
                $qx3 = (fallingAfter($ix1 || !($ix2)) && $ix3) || fallingAfter($ix1);
                """

        let statements = parseCodeForTarget storages code xgx
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Equality test1`` () =
        let storages = Storages()
        let code = """
        bool bTrue = true;
        bool bFalse = !true;            // todo: ERROR: declaration 임에도 assign 문으로 생성됨 @ XGK 
        bool b1 = $bTrue != true;
        bool b2 = $bTrue == true;
        """

        let statements = parseCodeForTarget storages code xgx
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

 
type XgiRisingFallingTest() =
    inherit XgxRisingFallingTest(XGI)
    [<Test>] member x.``Negation, Rising, Falling contact test`` () = base.``Negation, Rising, Falling contact test`` ()
    [<Test>] member x.``Normal, Rising, Falling contact test`` () = base.``Normal, Rising, Falling contact test`` ()
    //[<Test>] member x.``RisingAfter, FallingAfter contact test`` () = base.``RisingAfter, FallingAfter contact test`` ()
    [<Test>] member x.``XEquality test1`` () = base.``Equality test1`` ()

type XgkRisingFallingTest() =
    inherit XgxRisingFallingTest(XGK)
    [<Test>] member x.``Negation, Rising, Falling contact test`` () = base.``Negation, Rising, Falling contact test`` ()
    [<Test>] member x.``Normal, Rising, Falling contact test`` () = base.``Normal, Rising, Falling contact test`` ()
    //[<Test>] member x.``RisingAfter, FallingAfter contact test`` () = base.``RisingAfter, FallingAfter contact test`` ()
    [<Test>] member x.``XEquality test1`` () = base.``Equality test1`` ()

     