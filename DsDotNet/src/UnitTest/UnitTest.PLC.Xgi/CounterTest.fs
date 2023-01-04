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
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTD simple test`` () =
        use _ = setRuntimeTarget XGI
        let storages = Storages()
        let code = """
            bool cd = createTag("%IX0.0.0", false);
            bool res = createTag("%IX0.0.1", false);
            ctd myCTD = createXgiCTD(2000us, $cd, $res);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTUD simple test`` () =
        let storages = Storages()
        let code = """
            bool cu = createTag("%IX0.0.0", false);
            bool cd = createTag("%IX0.0.1", false);
            bool res = createTag("%IX0.0.2", false);
            ctud myCTUD = createXgiCTUD(2000us, $cu, $cd, $res);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``Counter CTU with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cu1 = createTag("%IX0.0.0", false);
            bool cu2 = createTag("%IX0.0.1", false);
            bool cu3 = createTag("%IX0.0.2", false);
            bool res0 = createTag("%IX0.0.2", false);
            bool res1 = createTag("%IX0.0.2", false);
            bool res2 = createTag("%IX0.0.2", false);

            bool x7 = createTag("%IX0.0.7", false);
            ctu myCTU = createXgiCTU(2000us, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $x7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Counter CTD with conditional test`` () =
        let storages = Storages()
        let code = """
            bool cu1 = createTag("%IX0.0.0", false);
            bool cu2 = createTag("%IX0.0.1", false);
            bool cu3 = createTag("%IX0.0.2", false);
            bool res0 = createTag("%IX0.0.2", false);
            bool res1 = createTag("%IX0.0.2", false);
            bool res2 = createTag("%IX0.0.2", false);

            bool x7 = createTag("%IX0.0.7", false);
            ctd myCTD = createXgiCTD(2000us, ($cu1 && $cu2) || $cu3, ($res0 || $res1) && $res2 );
            $x7 := (($cu1 && $cu2) || $cu3 || ($res0 || $res1) && $res2) && $cu1;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
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
//        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
//        saveTestResult (get_current_function_name()) xml
