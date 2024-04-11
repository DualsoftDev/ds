namespace T


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS




type XgxTimerTest(xgx:RuntimeTargetType) =
    inherit XgxTestBaseClass(xgx)


    member x.``Timer test`` () =
        let storages = Storages()
        let code = """
            bool myQBit0 = createTag("%QX0.1.0", false);
            bool x0 = createTag("%IX0.0.0", false);
            bool x1 = createTag("%IX0.0.1", false);
            bool x2 = createTag("%IX0.0.2", false);

            bool x7 = createTag("%IX0.0.7", false);
            ton myTon = createXgiTON(2000u, $myQBit0);
            $x7 := ($x0 || $x1) && $x2;
"""
        let statements = parseCode storages code
        //storages.Count === 12
        //statements.Length === 2      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many1 AND RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 && $x14    );
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many2 AND RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
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
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``TIMER= Many1 OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14    );
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``TIMER= Many2 OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14

                );
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``TIMER= Many And, OR RungIn Condition test`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
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
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many And, OR RungIn Condition test2`` () =
        let storages = Storages()
        let code = codeForBits + """
            ton myTon = createXgiTON(2000u,
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
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``TIMER= Not Condition test`` () =
        let storages = Storages()
        let code = """
            bool ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo = createTag("%IX1.0.0", false);
            bool Clamp1_RET_I = createTag("%IX1.0.1", false);
            bool Clamp2_RET_I = createTag("%IX1.0.2", false);
            bool Clamp3_RET_I = createTag("%IX1.0.3", false);
            bool Clamp4_RET_I = createTag("%IX1.0.4", false);
            bool IOP_ClampOperation = createTag("%IX1.0.5", false);

            ton myTon = createXgiTON(15000u,
                $ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo
                    && (!$Clamp1_RET_I || !$Clamp2_RET_I || !$Clamp3_RET_I || !$Clamp4_RET_I) && !$IOP_ClampOperation
                );
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Not Condition test 2`` () =
        let storages = Storages()
        let code = """
            bool ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo = createTag("%IX1.0.0", false);
            bool Clamp1_RET_I = createTag("%IX1.0.1", false);
            bool Clamp2_RET_I = createTag("%IX1.0.2", false);
            bool Clamp3_RET_I = createTag("%IX1.0.3", false);
            bool Clamp4_RET_I = createTag("%IX1.0.4", false);
            bool IOP_ClampOperation = createTag("%IX1.0.5", false);

            ton TOUT3 = createWinTON(15000u, $ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo && !(&&($Clamp1_RET_I, $Clamp2_RET_I, $Clamp3_RET_I, $Clamp4_RET_I)) && !($IOP_ClampOperation));
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml





type XgiTimerTest() =
    inherit XgxTimerTest(XGI)


    [<Test>] member __.``Timer test`` () = base.``Timer test``()
    [<Test>] member __.``TIMER= Many1 AND RungIn Condition test`` () = base.``TIMER= Many1 AND RungIn Condition test``()
    [<Test>] member __.``TIMER= Many2 AND RungIn Condition test`` () = base.``TIMER= Many2 AND RungIn Condition test``()
    [<Test>] member __.``TIMER= Many1 OR RungIn Condition test`` () = base.``TIMER= Many1 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many2 OR RungIn Condition test`` () = base.``TIMER= Many2 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many And, OR RungIn Condition test`` () = base.``TIMER= Many And, OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many And, OR RungIn Condition test2`` () = base.``TIMER= Many And, OR RungIn Condition test2``()
    [<Test>] member __.``TIMER= Not Condition test`` () = base.``TIMER= Not Condition test``()
    [<Test>] member __.``TIMER= Not Condition test 2`` () = base.``TIMER= Not Condition test 2``()
