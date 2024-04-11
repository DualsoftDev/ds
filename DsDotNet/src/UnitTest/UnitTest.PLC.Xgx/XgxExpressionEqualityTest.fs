namespace T
open Dual.UnitTest.Common.FS


open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS


type XgxExpEqualityTest(xgx:RuntimeTargetType) =
    inherit XgxTestBaseClass(xgx)

    member x.``Comparision, Arithmatic, OR test`` () =
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

                $result := $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``Comparision, Arithmatic, OR test2`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                bool cond1 = false;
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

                $result := $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )


    member __.``Expression equality test`` () =
        let storages = Storages()
        let code = """
            bool cond1 = false;
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
"""
        let exprCode = "$result := $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;"
        let statements_ = parseCode storages code
        let expr1 = parseExpression storages exprCode :?> Expression<bool>
        let expr2 = parseExpression storages exprCode :?> Expression<bool>
        expr1.IsEqual expr2 |> ShouldBeTrue



    member x.``Expression equality generation test`` () =
        let storages = Storages()
        let code = codeForBits + """
            bool result1 = false;
            bool result2 = false;
            bool result3 = false;
            bool result4 = false;

            $result1 :=
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x01             // 여기 다름
                ;

            $result2 :=
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;

            $result3 :=
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x01             // 여기 다름
                ;

            $result4 :=
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``XOR test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = """
                bool b1 = false;
                bool b2 = false;
                bool b3 = false;
                $b3 := $b1 <> $b2;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )




[<Collection("SerialXgxExpEqualityTest")>]
type XgiExpEqualityTest() =
    inherit XgxExpEqualityTest(XGI)

    [<Test>]
    member x.``Comparision, Arithmatic, OR test`` () = base.``Comparision, Arithmatic, OR test``()

    [<Test>]
    member x.``Comparision, Arithmatic, OR test2`` () = base.``Comparision, Arithmatic, OR test2``()


    [<Test>]
    member __.``Expression equality test`` () = base.``Expression equality test``()


    [<Test>]
    member x.``Expression equality generation test`` () = base.``Expression equality generation test``()


    [<Test>]
    member x.``XOR test`` () = base.``XOR test``()




