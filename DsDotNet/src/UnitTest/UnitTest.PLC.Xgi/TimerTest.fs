namespace T

open System.IO

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.FlatExpressionModule
open System.Globalization




type XgiTimerTest() =
    inherit XgiTestClass()


    [<Test>]
    member __.``Timer test`` () =
        let storages = Storages()
        let code = """
            bool myQBit0 = createTag("%QX0.1.0", false);
            bool x0 = createTag("%IX0.0.0", false);
            bool x1 = createTag("%IX0.0.1", false);
            bool x2 = createTag("%IX0.0.2", false);

            bool x7 = createTag("%IX0.0.7", false);
            ton myTon = createTON(2000us, $myQBit0);
            $x7 := ($x0 || $x1) && $x2;
"""
        let statements = parseCode storages code
        //storages.Count === 12
        //statements.Length === 2      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``TIMER= Many1 AND RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 && $x14    );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``TIMER= Many2 AND RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                // 산전 limit : 가로로 31개
                //let coilCellX = 31
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 && 

                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 &&
                $x08 &&
                $x09 &&
                $x10 &&
                $x11 &&
                //$x12 &&
                //$x13 &&

                $x14    );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``TIMER= Many1 OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14    );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``TIMER= Many2 OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14

                );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``TIMER= Many And, OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)

                );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``XX TIMER= Many And, OR RungIn Condition test2`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createTON(2000us,
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                && $x00 &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                && $x00 &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)
                && $x00 &&
                ($x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14)

                );
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
