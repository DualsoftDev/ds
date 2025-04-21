namespace T.Comparison
open T

open Dual.Common.UnitTest.FS


open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open System


type XgxExpEqualityTest(xgx:HwCPU) =
    inherit XgxTestBaseClass(xgx)

    member x.``Assignment simple test`` () =
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 sum = $nn1 + $nn2;

"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``Assignment test`` () =
        let code = generateInt16VariableDeclarations 1 8 + """
            bool b1 = false;
            bool b2 = true;
            bool b3 = $nn1 > $nn2;
            bool b4 = $b1 <> $b2;
            bool b5 = $b1 == $b2;
            bool b6 = false;
            $b6 = $nn1 > $nn2;
            bool b7 = $nn1 > 3s;

            int16 sum = $nn1 + $nn2;
            bool b8 = $nn1 + $nn2 > 3s;

"""
        code |> x.TestCode (getFuncName()) |> ignore


    member x.``Comparision, Arithmetic, OR test`` () =
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            bool result = false;

            $result = $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``Comparision, Arithmetic, OR test2`` () =
        let code = generateInt16VariableDeclarations 1 8 + """
            bool cond1 = false;
            int16 sum = 0s;
            bool result = false;

            $result = $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``Expression equality generation test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            bool result1 = false;
            bool result2 = false;
            bool result3 = false;
            bool result4 = false;

            $result1 =
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x01             // 여기 다름
                ;

            $result2 =
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;

            $result3 =
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x01             // 여기 다름
                ;

            $result4 =
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member __.``Expression equality test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 8 + """
            bool cond1 = false;
            int16 sum = 0s;
            bool result = false;
"""
        let exprCode = "$result = $cond1 && $nn1 + $nn2 * $nn3 > 2s || $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;"
        let statements_ = parseCodeForWindows storages code
        let expr1 = parseExpression4UnitTest storages exprCode :?> Expression<bool>
        let expr2 = parseExpression4UnitTest storages exprCode :?> Expression<bool>
        expr1.IsEqual expr2 |> ShouldBeTrue

    member x.``XOR test`` () =
        let code = """
            bool b1 = false;
            bool b2 = false;
            bool b3 = false;
            $b3 = $b1 <> $b2;
"""
        code |> x.TestCode (getFuncName()) |> ignore


//[<Collection("SerialXgxExpEqualityTest")>]
type XgiExpEqualityTest() =
    inherit XgxExpEqualityTest(XGI)

    [<Test>] member __.``Assignment simple test`` () = base.``Assignment simple test``()
    [<Test>] member __.``Assignment test`` () = base.``Assignment test``()
    [<Test>] member __.``Comparision, Arithmetic, OR test`` () = base.``Comparision, Arithmetic, OR test``()
    [<Test>] member __.``Comparision, Arithmetic, OR test2`` () = base.``Comparision, Arithmetic, OR test2``()
    [<Test>] member __.``Expression equality generation test`` () = base.``Expression equality generation test``()
    [<Test>] member __.``Expression equality test`` () = base.``Expression equality test``()
    [<Test>] member __.``XOR test`` () = base.``XOR test``()


type XgkExpEqualityTest() =
    inherit XgxExpEqualityTest(XGK)
    [<Test>] member __.``Assignment simple test`` () = base.``Assignment simple test``()
    [<Test>] member __.``Assignment test`` () = base.``Assignment test``()
    [<Test>] member __.``Comparision, Arithmetic, OR test`` () = base.``Comparision, Arithmetic, OR test``()
    [<Test>] member __.``Comparision, Arithmetic, OR test2`` () = base.``Comparision, Arithmetic, OR test2``()
    [<Test>] member __.``Expression equality generation test`` () = base.``Expression equality generation test``()
    [<Test>] member __.``Expression equality test`` () = base.``Expression equality test``()
    [<Test>] member __.``XOR test`` () = base.``XOR test``()



