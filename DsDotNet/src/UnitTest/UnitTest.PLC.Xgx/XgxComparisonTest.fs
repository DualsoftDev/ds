namespace T
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS

type XgxComparisonTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``COMP int8 test`` () =
        let storages = Storages()
        let code = """
            int8 nn1 = 1y;              // SINT type 으로 변환됨
            int8 nn2 = 2y;
            uint8 unn1 = 1uy;
            uint8 unn2 = 2uy;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 = $nn2;
            //bool b4 = $nn1 == $nn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 = $unn2;
            //bool ub4 = $unn1 == $unn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``COMP int16 test`` () =
        let storages = Storages()
        let code = """
            int16 nn1 = 1s;             // INT type 으로 변환됨
            int16 nn2 = 2s;
            uint16 unn1 = 1us;
            uint16 unn2 = 2us;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 = $nn2;
            //bool b4 = $nn1 == $nn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 = $unn2;
            //bool ub4 = $unn1 == $unn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``COMP int32 test`` () =
        let storages = Storages()
        let code = """
            int32 nn1 = 1;             // DINT type 으로 변환됨
            int32 nn2 = 2;
            uint32 unn1 = 1u;
            uint32 unn2 = 2u;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 = $nn2;
            //bool b4 = $nn1 == $nn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 = $unn2;
            //bool ub4 = $unn1 == $unn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``COMP int64 test`` () =
        let storages = Storages()
        let code = """
            int64 nn1 = 1L;             // LINT type 으로 변환되어야 함.  XGK 에서는 LINT 가 지원되지 않음.
            int64 nn2 = 2L;
            uint64 unn1 = 1UL;
            uint64 unn2 = 2UL;
            bool b1 = $nn1 > $nn2;
            bool b2 = $nn1 < $nn2;
            bool b3 = $nn1 = $nn2;
            //bool b4 = $nn1 == $nn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool b5 = $nn1 != $nn2;     // 확장 notation. "<>"
            bool b6 = $nn1 <> $nn2;
            bool b7 = $nn1 >= $nn2;
            bool b8 = $nn1 <= $nn2;

            bool ub1 = $unn1 > $unn2;
            bool ub2 = $unn1 < $unn2;
            bool ub3 = $unn1 = $unn2;
            //bool ub4 = $unn1 == $unn2;   // equality check 에 "==" 는 지원 안함.  ":=" 사용부분과 헷갈림
            bool ub5 = $unn1 != $unn2;     // 확장 notation. "<>"
            bool ub6 = $unn1 <> $unn2;
            bool ub7 = $unn1 >= $unn2;
            bool ub8 = $unn1 <= $unn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let test =
            fun () -> 
                let xml = x.generateXmlForTest f storages (map withNoComment statements)
                x.saveTestResult f xml
        match xgx with
        | XGI -> test()
        | XGK -> test |> ShouldFailWithSubstringT "XGK does not support int64 types"
        | _ -> failwith "Not supported plc type"

    member x.``COMP double test`` () =
        let storages = Storages()
        let code = """
            double nn1 = 1.1;
            double nn2 = 2.2;
            double sum = 0.0;
            $sum := $nn1 + $nn2;
            double sub = $nn1 - $nn2;
            double mul = $nn1 * $nn2;
            double div = $nn1 / $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``COMP single test`` () =
        let storages = Storages()
        let code = """
            single nn1 = 1.1f;
            single nn2 = 2.2f;
            single sum = 0.0f;
            $sum := $nn1 + $nn2;
            single sub = $nn1 - $nn2;
            single mul = $nn1 * $nn2;
            single div = $nn1 / $nn2;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``COMP 3 items test`` () =
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

    member x.``COMP 7 items test`` () =
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

    member x.``COMP 8 items test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``COMP 10 items test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 10 + """

            int16 sum = 0s;
            $sum := $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8 + $nn9 + $nn10;
        """
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``DIV 3 items test`` () =
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
        x.saveTestResult f xml
    member x.``COMP MUL 3 items test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            $sum := $nn1 + $nn2 * $nn3 + $nn4 + $nn5 * $nn6 / $nn7 - $nn8;
        """
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Comparision, Arithmatic, AND test`` () =
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








//[<Collection("SerialXgxFunctionTest")>]
type XgiComparisonTest() =
    inherit XgxComparisonTest(XGI)

    [<Test>] member __.``COMP int8 test`` () = base.``COMP int8 test``()
    [<Test>] member __.``COMP int16 test`` () = base.``COMP int16 test``()
    [<Test>] member __.``COMP int32 test`` () = base.``COMP int32 test``()
    [<Test>] member __.``COMP int64 test`` () = base.``COMP int64 test``()
    //[<Test>] member __.``COMP single test`` () = base.``COMP single test``()
    //[<Test>] member __.``COMP double test`` () = base.``COMP double test``()
    //[<Test>] member __.``COMP 3 items test`` () = base.``COMP 3 items test``()
    //[<Test>] member __.``COMP 7 items test`` () = base.``COMP 7 items test``()
    //[<Test>] member __.``COMP 8 items test`` () = base.``COMP 8 items test``()
    //[<Test>] member __.``COMP 10 items test`` () = base.``COMP 10 items test``()
    //[<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()
    //[<Test>] member __.``COMP MUL 3 items test`` () = base.``COMP MUL 3 items test``()
    //[<Test>] member __.``Comparision, Arithmatic, AND test`` () = base.``Comparision, Arithmatic, AND test``()

type XgkComparisonTest() =
    inherit XgxComparisonTest(XGK)

    [<Test>] member __.``COMP int8 test`` () = base.``COMP int8 test``()
    [<Test>] member __.``COMP int16 test`` () = base.``COMP int16 test``()
    [<Test>] member __.``COMP int32 test`` () = base.``COMP int32 test``()
    [<Test>] member __.``COMP int64 test`` () = base.``COMP int64 test``()
    //[<Test>] member __.``COMP single test`` () = base.``COMP single test``()
    //[<Test>] member __.``COMP double test`` () = base.``COMP double test``()
    //[<Test>] member __.``COMP 3 items test`` () = base.``COMP 3 items test``()
    //[<Test>] member __.``COMP 7 items test`` () = base.``COMP 7 items test``()
    //[<Test>] member __.``COMP 8 items test`` () = base.``COMP 8 items test``()
    //[<Test>] member __.``COMP 10 items test`` () = base.``COMP 10 items test``()
    //[<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()
    //[<Test>] member __.``COMP MUL 3 items test`` () = base.``COMP MUL 3 items test``()
    //[<Test>] member __.``Comparision, Arithmatic, AND test`` () = base.``Comparision, Arithmatic, AND test``()




