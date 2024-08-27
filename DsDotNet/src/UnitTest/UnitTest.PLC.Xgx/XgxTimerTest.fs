namespace T.CounterTimer
open T


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open Xunit
open System




type XgxTimerTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Timer test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool myQBit0 = createTag("%QX0.1.0", false);
                bool x0 = createTag("%IX0.0.0", false);
                bool x1 = createTag("%IX0.0.1", false);
                bool x2 = createTag("%IX0.0.2", false);

                bool x7 = createTag("%IX0.0.7", false);
                ton myTon = createXgiTON(2000u, $myQBit0);
                $x7 = ($x0 || $x1) && $x2;
                """
            | XGK -> """
                bool myQBit0 = createTag("P0001A", false);
                bool x0 = createTag("P00001", false);
                bool x1 = createTag("P00002", false);
                bool x2 = createTag("P00003", false);

                bool x7 = createTag("P00004", false);
                ton myTon = createXgkTON(20u, $myQBit0);
                $x7 = ($x0 || $x1) && $x2;
                """
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        //storages.Count === 12
        //statements.Length === 2      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many And, OR RungIn Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
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
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many And, OR RungIn Condition test2`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
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
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many1 AND RungIn Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07
                && $x08 && $x09 && $x10 && $x11 && $x12 && $x13 && $x14    );
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many1 OR RungIn Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14    );
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many2 AND RungIn Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
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
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Many2 OR RungIn Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code = generateBitTagVariableDeclarations xgx 0 16 + $"""
            ton myTon = {ton}(2000u,
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14 ||

                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07
                || $x08 || $x09 || $x10 || $x11 || $x12 || $x13 || $x14

                );
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Not BOOLEAN ENABLE Condition test`` () =
        let storages = Storages()
        let ton = if xgx = XGI then "createXgiTON" else "createXgkTON"
        let code =
            $"""
                //double pi = 3.14;
                //bool b0 = !(2.1 == 6.1);
                //bool b1 = !($pi == 6.1);
                //bool b2 = true && !($pi == 6.2);
                //bool b3 = $pi > 6.23;
                //bool b4 = !($pi > 6.24);
                //ton myTon0 = {ton}(15000u, 3.14 > 6.25);
                ton myTon1 = {ton}(15000u, (3.14 + 2.0) > 6.25);
                //ton myTon2 = {ton}(15000u, $pi > 6.25);
            """;

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Not Condition test`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
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
            | XGK -> """
                bool ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo = createTag("P0000A", false);
                bool Clamp1_RET_I = createTag("P0000B", false);
                bool Clamp2_RET_I = createTag("P0000C", false);
                bool Clamp3_RET_I = createTag("P0000D", false);
                bool Clamp4_RET_I = createTag("P0000E", false);
                bool IOP_ClampOperation = createTag("P0000F", false);

                ton myTon = createXgkTON(15000u,
                    $ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo
                        && (!$Clamp1_RET_I || !$Clamp2_RET_I || !$Clamp3_RET_I || !$Clamp4_RET_I) && !$IOP_ClampOperation
                    );
                """
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``TIMER= Not Condition test 2`` () =
        let storages = Storages()
        let code =
            match xgx with
            | XGI -> """
                bool ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo = createTag("%IX1.0.0", false);
                bool Clamp1_RET_I = createTag("%IX1.0.1", false);
                bool Clamp2_RET_I = createTag("%IX1.0.2", false);
                bool Clamp3_RET_I = createTag("%IX1.0.3", false);
                bool Clamp4_RET_I = createTag("%IX1.0.4", false);
                bool IOP_ClampOperation = createTag("%IX1.0.5", false);

                ton TOUT3 = createXgiTON(15000u, $ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo && !(&&($Clamp1_RET_I, $Clamp2_RET_I, $Clamp3_RET_I, $Clamp4_RET_I)) && !($IOP_ClampOperation));
                """
            | XGK -> """
                bool ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo = createTag("P0000A", false);
                bool Clamp1_RET_I = createTag("P0000B", false);
                bool Clamp2_RET_I = createTag("P0000C", false);
                bool Clamp3_RET_I = createTag("P0000D", false);
                bool Clamp4_RET_I = createTag("P0000E", false);
                bool IOP_ClampOperation = createTag("P0000F", false);

                ton TOUT3 = createXgkTON(15000u, $ClampSystem_ClampOperation_Operation_AllClamps_RET_Memo && !(&&($Clamp1_RET_I, $Clamp2_RET_I, $Clamp3_RET_I, $Clamp4_RET_I)) && !($IOP_ClampOperation));
                """
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml





[<Collection("SeparatedTestGroup")>]
type XgiTimerTest() =
    inherit XgxTimerTest(XGI)
    [<Test>] member __.``Timer test`` () = base.``Timer test``()
    [<Test>] member __.``TIMER= Many And, OR RungIn Condition test`` () = base.``TIMER= Many And, OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many And, OR RungIn Condition test2`` () = base.``TIMER= Many And, OR RungIn Condition test2``()
    [<Test>] member __.``TIMER= Many1 AND RungIn Condition test`` () = base.``TIMER= Many1 AND RungIn Condition test``()
    [<Test>] member __.``TIMER= Many1 OR RungIn Condition test`` () = base.``TIMER= Many1 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many2 AND RungIn Condition test`` () = base.``TIMER= Many2 AND RungIn Condition test``()
    [<Test>] member __.``TIMER= Many2 OR RungIn Condition test`` () = base.``TIMER= Many2 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Not BOOLEAN ENABLE Condition test`` () = base.``TIMER= Not BOOLEAN ENABLE Condition test``()
    [<Test>] member __.``TIMER= Not Condition test`` () = base.``TIMER= Not Condition test``()
    [<Test>] member __.``TIMER= Not Condition test 2`` () = base.``TIMER= Not Condition test 2``()

[<Collection("SeparatedTestGroup")>]
type XgkTimerTest() =
    inherit XgxTimerTest(XGK)
    [<Test>] member __.``Timer test`` () = base.``Timer test``()
    [<Test>] member __.``X TIMER= Many And, OR RungIn Condition test`` () = base.``TIMER= Many And, OR RungIn Condition test``()
    [<Test>] member __.``X TIMER= Many And, OR RungIn Condition test2`` () = base.``TIMER= Many And, OR RungIn Condition test2``()
    [<Test>] member __.``TIMER= Many1 AND RungIn Condition test`` () = base.``TIMER= Many1 AND RungIn Condition test``()
    [<Test>] member __.``X TIMER= Many1 OR RungIn Condition test`` () = base.``TIMER= Many1 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Many2 AND RungIn Condition test`` () = base.``TIMER= Many2 AND RungIn Condition test``()
    [<Test>] member __.``X TIMER= Many2 OR RungIn Condition test`` () = base.``TIMER= Many2 OR RungIn Condition test``()
    [<Test>] member __.``TIMER= Not BOOLEAN ENABLE Condition test`` () = base.``TIMER= Not BOOLEAN ENABLE Condition test``()
    [<Test>] member __.``X TIMER= Not Condition test`` () = base.``TIMER= Not Condition test``()
    [<Test>] member __.``X TIMER= Not Condition test 2`` () = base.``TIMER= Not Condition test 2``()
