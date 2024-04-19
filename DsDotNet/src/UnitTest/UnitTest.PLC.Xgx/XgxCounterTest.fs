namespace T
open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS

type XgxCounterTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Counter CTU simple test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool cu = createTag("%IX0.0.0", false);
                bool res = createTag("%IX0.0.1", false);
                ctu myCTU = createXgiCTU(2000u, $cu, $res);
                """
            | XGK -> """
                bool cu = createTag("P00000", false);
                bool res = createTag("P00001", false);
                ctu myCTU = createXgiCTU(2000u, $cu, $res);
                """
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTD simple test`` () =
        use _ = setRuntimeTarget XGI
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool cd = createTag("%IX0.0.0", false);     // count down
                bool xload = createTag("%IX0.0.1", false);
                ctd myCTD = createXgiCTD(2000u, $cd, $xload);
                """
            | XGK -> """
                bool cd = createTag("P00000", false);
                bool xload = createTag("P00001", false);       // XGK 에서는 'load' 라는 변수명을 사용할 수 없다.
                ctd myCTD = createXgiCTD(2000u, $cd, $xload);
                ctd myCTD1 = createXgiCTD(2000u, $cd, $xload);
                ctd myCTD2 = createXgiCTD(2000u, $cd, $xload);
                """
            | _ -> failwith "Not supported plc type"


        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTUD simple test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool cu = createTag("%IX0.0.0", false);
                bool cd = createTag("%IX0.0.1", false);
                bool r  = createTag("%IX0.0.2", false);
                bool ld = createTag("%IX0.0.3", false);
                ctud myCTUD = createXgiCTUD(2000u, $cu, $cd, $r, $ld);
                """
            | XGK -> """
                bool cu = createTag("P00000", false);
                bool cd = createTag("P00001", false);
                bool r  = createTag("P00002", false);
                bool xld = createTag("P00003", false);
                ctud myCTUD = createXgkCTUD(2000u, $cu, $cd, $r, $xld);
                """
            | _ -> failwith "Not supported plc type"


        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTR simple test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool cd = createTag("%IX0.0.0", false);
                bool res = createTag("%IX0.0.1", false);
                ctr myCTR = createXgiCTR(2000u, $cd, $res);
                //int x7 = createTag("%QX0.1", 0);
                //$x7 := $myCTR.CV;
                $myCTR.RST := $cd;
                """
            | XGK -> """
                bool cd = createTag("P00000", false);
                bool res = createTag("P00001", false);
                ctr myCTR = createXgiCTR(2000u, $cd, $res);

                // XGK 에서는 다음 처럼 struct 변수를 접근하는 구문은 허용하지 않는다.
                // $myCTR.RST := $cd;
                """
            | _ -> failwith "Not supported plc type"


        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``Counter CTU with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cu1  = false;
            bool cu2  = false;
            bool cu3  = false;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;

            bool xx7 = false;
            ctu myCTU = createXgiCTU(2000u, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $xx7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTD with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cu1  = true;
            bool cu2  = false;
            bool cu3  = $cu1 || $cu2;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;

            bool xx7 = false;
            ctd myCTD = createXgiCTD(2000u, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $xx7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTR with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cd1  = false;
            bool cd2  = false;
            bool cd3  = false;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;

            bool xx7 = false;
            ctr myCTR = createXgiCTR(2000u, ($cd1 && $cd2) || $cd3, ($res0 || $res1) && $res2 );
            $xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTR with conditional test2`` () =
        let storages = Storages()
        let code = """
            bool cd1  = false;
            bool cd2  = false;
            bool cd3  = false;
            bool cd4  = false;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;

            bool xx7 = false;
            ctr myCTR = createXgiCTR(2000u, ($cd1 && $cd2 || $cd3 || $cd4) && $cd3, ($res0 || $res1) && $res2 );
            $xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Counter CTUD with conditional test`` () =
        let storages = Storages()
        let vars = """
            bool cu1  = false;
            bool cu2  = false;
            bool cu3  = false;
            bool cu4  = false;
            bool cd1  = false;
            bool cd2  = false;
            bool cd3  = false;
            bool cd4  = false;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;
            bool load1  = false;
            bool load2  = false;
            bool load3  = false;
            bool load4  = false;

            bool xx7 = false;
        """
        let code =
            vars +
                match xgx with
                | XGI -> """
                    ctud myCTUD =
                        createXgiCTUD(
                            2000u
                            , ($cu1 && $cu2) || $cu3 || $cu4
                            , $cd1 || $cd2 || $cd3 && $cd4
                            , $res0 || $res1 && $res2
                            , $load1 && $load2 ||$load3 || $load4
                            );
                    //$xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
                    """
                | XGK -> """
                    ctud myCTUD =
                        createXgkCTUD(
                            2000u
                            , ($cu1 && $cu2) || $cu3 || $cu4
                            , $cd1 || $cd2 || $cd3 && $cd4
                            , $res0 || $res1 && $res2
                            , $load1 && $load2 ||$load3 || $load4
                            );
                    """
                | _ -> failwith "Not supported plc type"
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

type XgxFunctionTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``ADD simple test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 sum = 0s;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD int32 test`` () =
        let storages = Storages()
        let code = """
            int nn1 = 1;
            int nn2 = 2;
            int sum = 0;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD int64 test`` () =
        let storages = Storages()
        let code = """
            int64 nn1 = 1L;
            int64 nn2 = 2L;
            int64 sum = 0L;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD double test`` () =
        let storages = Storages()
        let code = """
            double nn1 = 1.1;
            double nn2 = 2.2;
            double sum = 0.0;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``ADD 3 items test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD 7 items test`` () =
        let storages = Storages()
        let code =
            generateInt16VariableDeclarations 1 8 + """

            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD 8 items test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``ADD 10 items test`` () =
        lock x.Locker (fun () ->
            let storages = Storages()
            let code = generateInt16VariableDeclarations 1 10 + """

                int16 sum = 0s;
                $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8 + $nn9 + $nn10;
    """
            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml )

    member x.``DIV 3 items test`` () =
        lock x.Locker (fun () ->
            let storages = Storages()
            let code = """
                int16 nn1 = 1s;
                int16 nn2 = 2s;
                int16 nn3 = 3s;

                int16 quotient = 0s;
                $quotient := $nn1 / $nn2 / $nn3;
    """
            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml )
    member x.``ADD MUL 3 items test`` () =
        lock x.Locker (fun () ->
            let storages = Storages()
            let code = generateInt16VariableDeclarations 1 8 + """
                int16 sum = 0s;
                $sum := $nn1 + $nn2 * $nn3 + $nn4 + $nn5 * $nn6 / $nn7 - $nn8;
    """
            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``Comparision, Arithmatic, AND test`` () =
        lock x.Locker (fun () ->
            let storages = Storages()
            let code = generateInt16VariableDeclarations 1 8 + """
                int16 sum = 0s;
                bool result = false;

                $result := $nn1 + $nn2 * $nn3 > 2s && $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """
            let statements = parseCodeForWindows storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )







//[<Collection("XgxCounterTest")>]
type XgiCounterTest() =
    inherit XgxCounterTest(XGI)

    [<Test>] member __.``Counter CTU simple test`` () = base.``Counter CTU simple test``()
    [<Test>] member __.``Counter CTD simple test`` () = base.``Counter CTD simple test``()
    [<Test>] member __.``Counter CTUD simple test`` () = base.``Counter CTUD simple test``()
    [<Test>] member __.``Counter CTR simple test`` () = base.``Counter CTR simple test``()
    [<Test>] member __.``Counter CTU with conditional test`` () = base.``Counter CTU with conditional test``()
    [<Test>] member __.``Counter CTD with conditional test`` () = base.``Counter CTD with conditional test``()
    [<Test>] member __.``Counter CTR with conditional test`` () = base.``Counter CTR with conditional test``()
    [<Test>] member __.``Counter CTR with conditional test2`` () = base.``Counter CTR with conditional test2``()
    [<Test>] member __.``Counter CTUD with conditional test`` () = base.``Counter CTUD with conditional test``()


type XgkCounterTest() =
    inherit XgxCounterTest(XGK)

    [<Test>] member __.``Counter CTU simple test`` () = base.``Counter CTU simple test``()
    [<Test>] member __.``Counter CTD simple test`` () = base.``Counter CTD simple test``()
    [<Test>] member __.``Counter CTUD simple test`` () = base.``Counter CTUD simple test``()
    [<Test>] member __.``Counter CTR simple test`` () = base.``Counter CTR simple test``()
    [<Test>] member __.``Counter CTU with conditional test`` () = base.``Counter CTU with conditional test``()
    [<Test>] member __.``Counter CTD with conditional test`` () = base.``Counter CTD with conditional test``()
    [<Test>] member __.``Counter CTR with conditional test`` () = base.``Counter CTR with conditional test``()
    [<Test>] member __.``Counter CTR with conditional test2`` () = base.``Counter CTR with conditional test2``()

    /// TODO: XGK CTUD 에서 cu 나 cd 등의 조건이 단일 변수가 아닌 복합 expression 인 경우의 처리가 필요
    /// 복합 expression 들을 별도의 auto 변수에 할당한 후, auto 변수를 사용하는 방법으로 처리할 수 있음.
    [<Test>] member __.``X Counter CTUD with conditional test`` () = base.``Counter CTUD with conditional test``()


//[<Collection("SerialXgxFunctionTest")>]
type XgiFunctionTest() =
    inherit XgxFunctionTest(XGI)

    [<Test>] member __.``ADD simple test`` () = base.``ADD simple test``()
    [<Test>] member __.``ADD int32 test`` () = base.``ADD int32 test``()
    [<Test>] member __.``ADD int64 test`` () = base.``ADD int64 test``()
    [<Test>] member __.``ADD double test`` () = base.``ADD double test``()
    [<Test>] member __.``ADD 3 items test`` () = base.``ADD 3 items test``()
    [<Test>] member __.``ADD 7 items test`` () = base.``ADD 7 items test``()
    [<Test>] member __.``ADD 8 items test`` () = base.``ADD 8 items test``()
    [<Test>] member __.``ADD 10 items test`` () = base.``ADD 10 items test``()
    [<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()
    [<Test>] member __.``ADD MUL 3 items test`` () = base.``ADD MUL 3 items test``()
    [<Test>] member __.``Comparision, Arithmatic, AND test`` () = base.``Comparision, Arithmatic, AND test``()

type XgkFunctionTest() =
    inherit XgxFunctionTest(XGK)

    [<Test>] member __.``ADD simple test`` () = base.``ADD simple test``()
    [<Test>] member __.``ADD int32 test`` () = base.``ADD int32 test``()
    [<Test>] member __.``ADD int64 test`` () = base.``ADD int64 test``()
    [<Test>] member __.``ADD double test`` () = base.``ADD double test``()
    [<Test>] member __.``ADD 3 items test`` () = base.``ADD 3 items test``()
    [<Test>] member __.``ADD 7 items test`` () = base.``ADD 7 items test``()
    [<Test>] member __.``ADD 8 items test`` () = base.``ADD 8 items test``()
    [<Test>] member __.``ADD 10 items test`` () = base.``ADD 10 items test``()
    [<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()
    [<Test>] member __.``ADD MUL 3 items test`` () = base.``ADD MUL 3 items test``()
    [<Test>] member __.``Comparision, Arithmatic, AND test`` () = base.``Comparision, Arithmatic, AND test``()




