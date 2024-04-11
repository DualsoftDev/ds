namespace T


open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS




[<Collection("XgiCounterTest")>]
type XgiCounterTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Counter CTU simple test`` () =
        let storages = Storages()
        let code = """
            bool cu = createTag("%IX0.0.0", false);
            bool res = createTag("%IX0.0.1", false);
            ctu myCTU = createXgiCTU(2000u, $cu, $res);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTD simple test`` () =
        use _ = setRuntimeTarget XGI
        let storages = Storages()
        let code = """
            bool cd = createTag("%IX0.0.0", false);
            bool load = createTag("%IX0.0.1", false);
            ctd myCTD = createXgiCTD(2000u, $cd, $load);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTUD simple test`` () =
        let storages = Storages()
        let code = """
            bool cu = createTag("%IX0.0.0", false);
            bool cd = createTag("%IX0.0.1", false);
            bool r  = createTag("%IX0.0.2", false);
            bool ld = createTag("%IX0.0.3", false);
            ctud myCTUD = createXgiCTUD(2000u, $cu, $cd, $r, $ld);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTR simple test`` () =
        let storages = Storages()
        let code = """
            bool cd = createTag("%IX0.0.0", false);
            bool res = createTag("%IX0.0.1", false);
            ctr myCTR = createXgiCTR(2000u, $cd, $res);
            //int x7 = createTag("%QX0.1", 0);
            //$x7 := $myCTR.CV;
            $myCTR.RST := $cd;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml


    [<Test>]
    member __.``Counter CTU with conditional test`` () =
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
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTD with conditional test`` () =
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
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTR with conditional test`` () =
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
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTR with conditional test2`` () =
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
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``Counter CTUD with conditional test`` () =
        let storages = Storages()
        let code = """
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
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

[<Collection("SerialXgiFunctionTest")>]
type XgiFunctionTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``ADD simple test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 sum = 0s;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``ADD int32 test`` () =
        let storages = Storages()
        let code = """
            int nn1 = 1;
            int nn2 = 2;
            int sum = 0;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``ADD int64 test`` () =
        let storages = Storages()
        let code = """
            int64 nn1 = 1L;
            int64 nn2 = 2L;
            int64 sum = 0L;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``ADD double test`` () =
        let storages = Storages()
        let code = """
            double nn1 = 1.1;
            double nn2 = 2.2;
            double sum = 0.0;
            $sum := $nn1 + $nn2;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml


    [<Test>]
    member __.``ADD 3 items test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``ADD 7 items test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 nn4 = 4s;
            int16 nn5 = 5s;
            int16 nn6 = 6s;
            int16 nn7 = 7s;
            int16 nn8 = 8s;

            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member __.``ADD 8 items test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 nn4 = 4s;
            int16 nn5 = 5s;
            int16 nn6 = 6s;
            int16 nn7 = 7s;
            int16 nn8 = 8s;

            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
        saveTestResult f xml

    [<Test>]
    member x.``ADD 10 items test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                int16 nn1 = 1s;
                int16 nn2 = 2s;
                int16 nn3 = 3s;
                int16 nn4 = 4s;
                int16 nn5 = 5s;
                int16 nn6 = 6s;
                int16 nn7 = 7s;
                int16 nn8 = 8s;
                int16 nn9 = 9s;
                int16 nn10 = 10s;

                int16 sum = 0s;
                $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8 + $nn9 + $nn10;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
            saveTestResult f xml )

    [<Test>]
    member x.``DIV 3 items test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                int16 nn1 = 1s;
                int16 nn2 = 2s;
                int16 nn3 = 3s;

                int16 quotient = 0s;
                $quotient := $nn1 / $nn2 / $nn3;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
            saveTestResult f xml )
    [<Test>]
    member x.``ADD MUL 3 items test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                int16 nn1 = 1s;
                int16 nn2 = 2s;
                int16 nn3 = 3s;
                int16 nn4 = 4s;
                int16 nn5 = 5s;
                int16 nn6 = 6s;
                int16 nn7 = 7s;
                int16 nn8 = 8s;
                int16 sum = 0s;
                $sum := $nn1 + $nn2 * $nn3 + $nn4 + $nn5 * $nn6 / $nn7 - $nn8;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
            saveTestResult f xml
        )

    [<Test>]
    member x.``Comparision, Arithmatic, AND test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                int16 nn1 = 1s;
                int16 nn2 = 2s;
                int16 nn3 = 3s;
                int16 nn4 = 4s;
                int16 nn5 = 5s;
                int16 nn6 = 6s;
                int16 nn7 = 7s;
                int16 nn8 = 8s;
                int16 sum = 0s;
                bool result = false;

                $result := $nn1 + $nn2 * $nn3 > 2s && $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = XgiFixtures.generateXmlForTest f storages (map withNoComment statements)
            saveTestResult f xml
        )
