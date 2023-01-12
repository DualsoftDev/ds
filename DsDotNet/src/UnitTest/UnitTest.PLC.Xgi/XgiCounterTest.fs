namespace T


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI




type XgiCounterTest() =
    inherit XgiTestClass()

    [<Test>]
    member __.``Counter CTU simple test`` () =
        let storages = Storages()
        let code = """
            bool cu = createTag("%IX0.0.0", false);
            bool res = createTag("%IX0.0.1", false);
            ctu myCTU = createXgiCTU(2000us, $cu, $res);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTD simple test`` () =
        use _ = setRuntimeTarget XGI
        let storages = Storages()
        let code = """
            bool cd = createTag("%IX0.0.0", false);
            bool load = createTag("%IX0.0.1", false);
            ctd myCTD = createXgiCTD(2000us, $cd, $load);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTUD simple test`` () =
        let storages = Storages()
        let code = """
            bool cu = createTag("%IX0.0.0", false);
            bool cd = createTag("%IX0.0.1", false);
            bool r  = createTag("%IX0.0.2", false);
            bool ld = createTag("%IX0.0.3", false);
            ctud myCTUD = createXgiCTUD(2000us, $cu, $cd, $r, $ld);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTR simple test`` () =
        let storages = Storages()
        let code = """
            bool cd = createTag("%IX0.0.0", false);
            bool res = createTag("%IX0.0.1", false);
            ctr myCTR = createXgiCTR(2000us, $cd, $res);
            //int x7 = createTag("%QX0.1", 0);
            //$x7 := $myCTR.CV;
            $myCTR.RST := $cd;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


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
            ctu myCTU = createXgiCTU(2000us, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $xx7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTD with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cu1  = true;
            bool cu2  = false;
            bool cu3  = false;
            bool res0 = false;
            bool res1 = false;
            bool res2 = false;

            bool xx7 = false;
            ctd myCTD = createXgiCTD(2000us, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $xx7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

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
            ctr myCTR = createXgiCTR(2000us, ($cd1 && $cd2) || $cd3, ($res0 || $res1) && $res2 );
            $xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

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
            ctr myCTR = createXgiCTR(2000us, ($cd1 && $cd2 || $cd3 || $cd4) && $cd3, ($res0 || $res1) && $res2 );
            $xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

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
                    2000us
                    , ($cu1 && $cu2) || $cu3 || $cu4
                    , $cd1 || $cd2 || $cd3 && $cd4
                    , $res0 || $res1 && $res2
                    , $load1 && $load2 ||$load3 || $load4
                    );
            //$xx7 := (($cd1 && $cd2) || $cd3 || ($res0 || $res1) && $res2) && $cd1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

type XgiFunctionTest() =
    inherit XgiTestClass()

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
        let xml = LsXGI.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml



//    [<Test>]
//    member __.``TIMER= Many1 AND RungIn Condition test`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
//                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 && $x14    );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml

//    [<Test>]
//    member __.``TIMER= Many2 AND RungIn Condition test`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                // 산전 limit : 가로로 31개
//                //let coilCellX = 31
//                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
//                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 &&

//                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 &&
//                $x08 &&
//                $x09 &&
//                $x10 &&
//                $x11 &&
//                //$x12 &&
//                //$x13 &&

//                $x14    );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml


//    [<Test>]
//    member __.``TIMER= Many1 OR RungIn Condition test`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14    );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml


//    [<Test>]
//    member __.``TIMER= Many2 OR RungIn Condition test`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

//                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

//                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14

//                );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml


//    [<Test>]
//    member __.``TIMER= Many And, OR RungIn Condition test`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)

//                );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml

//    [<Test>]
//    member __.``TIMER= Many And, OR RungIn Condition test2`` () =
//        let storages = Storages()
//        let code = codeForBits + """
//            ton myTon = createTON(2000us,
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                && $x00 &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                && $x00 &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
//                && $x00 &&
//                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
//                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)

//                );
//"""
//        let statements = parseCode storages code
//        let xml = LsXGI.generateXml storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml
