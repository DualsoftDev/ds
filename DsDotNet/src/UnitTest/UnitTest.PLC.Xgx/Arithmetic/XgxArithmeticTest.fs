namespace T.Arithmetic
open T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Base.CS
open Dual.Common.UnitTest.FS
open type Dual.Common.Base.CS.DcLogger

type XgxArithematicTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let enableTraceCache = DcLogger.EnableTrace

    [<SetUp>]
    member x.Setup () =
        // 이 test 는 trace 시간이 너무 걸리므로, 이 test 에 한해 disable
        DcLogger.EnableTrace <- false
    [<TearDown>]
    member __.TearDown () =
        DcLogger.EnableTrace <- enableTraceCache

    member x.``ADD 10 items test`` () =
        let code = generateInt16VariableDeclarations 1 10 + """

            int16 sum = 0s;
            $sum = $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8 + $nn9 + $nn10;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD 100 items test`` () =
        let numSum = 100
        let repeat = 1
        // "$nn1 + $nn2 + ... +$nn{count}"
        let sumText = [1..numSum] |> map (fun n -> $"$nn{n}") |> joinWith "+"
        let code =
            let sums =
                [
                    for i in [1..10] do
                        $"""
    int16 sum{i} = 0s;
    $sum{i} = {sumText};
    """
                ] |> joinLines
            generateInt16VariableDeclarations 1 numSum + sums

        code |> x.TestCode (getFuncName()) |> ignore


    member x.``ADD 3 items test`` () =
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 sum = 0s;
            $sum = $nn1 + $nn2 + $nn3;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD 7 items test`` () =
        let code =
            generateInt16VariableDeclarations 1 8 + """

            int16 sum = 0s;
            $sum = $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD 8 items test`` () =
        let storages = Storages()
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            $sum = $nn1 + $nn2 + $nn3 + $nn4 + $nn5 + $nn6 + $nn7 + $nn8;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD double test`` () =
        let code = """
            double nn1 = 1.1;
            double nn2 = 2.2;
            double sum = 0.0;
            $sum = $nn1 + $nn2;
            double sub = $nn1 - $nn2;
            double mul = $nn1 * $nn2;
            double div = $nn1 / $nn2;
"""
        code |> x.TestCode (getFuncName()) |> ignore


    member x.``ADD int16 test`` () =
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 sum = 0s;
            int16 sum2 = 0s;
            $sum = $nn1 + $nn2;
            $sum2 = $sum;

            uint16 unn1 = 1us;
            uint16 unn2 = 2us;
            uint16 usum = 0us;
            uint16 usum2 = 0us;
            $usum = $unn1 + $unn2;
            $usum2 = $usum;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD int32 test`` () =
        let code = """
            int nn1 = 1;
            int nn2 = 2;
            int sum = 0;
            $sum = $nn1 + $nn2;

            uint unn1 = 1u;
            uint unn2 = 2u;
            uint usum = 0u;
            $usum = $unn1 + $unn2;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD int64 test`` () =
        let storages = Storages()
        let code = """
            int64 nn1 = 1L;
            int64 nn2 = 2L;
            int64 sum = 0L;
            $sum = $nn1 + $nn2;
            int64 sub = $nn1 - $nn2;
            int64 mul = $nn1 * $nn2;
            int64 div = $nn1 / $nn2;

            uint64 unn1 = 1UL;
            uint64 unn2 = 2UL;
            uint64 usum = 0UL;
            $usum = $unn1 + $unn2;
            uint64 usub = $unn1 - $unn2;
            uint64 umul = $unn1 * $unn2;
            uint64 udiv = $unn1 / $unn2;

"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let test =
            fun () ->
                let xml = x.generateXmlForTest f storages (map withNoComment statements)
                x.saveTestResult f xml
        match xgx with
        | XGI -> test()
        | XGK -> test |> ShouldFailWithSubstringT "XGK does not support"
        | _ -> failwith "Not supported plc type"

    member x.``ADD int8 test`` () =
        let storages = Storages()
        let code = """
            int8 nn1 = 1y;
            int8 nn2 = 2y;
            int8 sum = 0y;
            int8 sum2 = 0y;
            $sum = $nn1 + $nn2;
            $sum2 = $sum;

            uint8 unn1 = 1uy;
            uint8 unn2 = 2uy;
            uint8 usum = 0uy;
            uint8 usum2 = 0uy;
            $usum = $unn1 + $unn2;
            $usum2 = $usum;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let test =
            fun () ->
                let xml = x.generateXmlForTest f storages (map withNoComment statements)
                x.saveTestResult f xml
        match xgx with
        | XGI -> test()
        | XGK -> test |> ShouldFailWithSubstringT "not supported in XGK"
        | _ -> failwith "Not supported plc type"

    member x.``ADD MUL 3 items test`` () =
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            $sum = $nn1 + $nn2 * $nn3 + $nn4 + $nn5 * $nn6 / $nn7 - $nn8;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD simple test`` () =
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 sum = 0s;
            $sum = $nn1 + $nn2;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``ADD single test`` () =
        let code = """
            single nn1 = 1.1f;
            single nn2 = 2.2f;
            single sum = 0.0f;
            $sum = $nn1 + $nn2;
            single sub = $nn1 - $nn2;
            single mul = $nn1 * $nn2;
            single div = $nn1 / $nn2;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic assign test`` () =
        let code =
            $"""
                double pi = 3.14;
                bool b0 = !(2.1 == 6.1);
                bool b1 = !($pi == 6.1);
                bool b2 = true && !($pi == 6.2);
                bool b3 = $pi > 6.23;
                bool b4 = !($pi > 6.24);
            """;

        code |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic assign test1`` () =
        "bool b0 = !(2.1 == 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test1`` () =
        "bool b0 = !(2.1 == 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test2`` () =
        "bool b0 = true && (2.1 == 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test3`` () =
        "bool b0 = false && (2.1 == 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test4`` () =
        "bool b0 = !(2.1 > 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test5`` () =
        "bool b0 = false && !(2.1 <= 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Arithmetic test6`` () =
        "bool b0 = false && !(2.1 == 6.1);"
        |> x.TestCode (getFuncName()) |> ignore

    member x.``Comparision, Arithmetic, AND test`` () =
        let code = generateInt16VariableDeclarations 1 8 + """
            int16 sum = 0s;
            bool result = false;

            $result = $nn1 + $nn2 * $nn3 > 2s && $nn4 + $nn5 * $nn6 / $nn7 - $nn8 > 5s;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``DIV 3 items test`` () =
        let code = """
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;

            int16 quotient = 0s;
            $quotient = $nn1 / $nn2 / $nn3;
        """
        code |> x.TestCode (getFuncName()) |> ignore








//[<Collection("SerialXgxFunctionTest")>]
type XgiArithematicTest() =
    inherit XgxArithematicTest(XGI)

    [<Test>] member __.``ADD 10 items test`` () = base.``ADD 10 items test``()
    [<Test>] member __.``ADD 100 items test`` () = base.``ADD 100 items test``()
    [<Test>] member __.``ADD 3 items test`` () = base.``ADD 3 items test``()
    [<Test>] member __.``ADD 7 items test`` () = base.``ADD 7 items test``()
    [<Test>] member __.``ADD 8 items test`` () = base.``ADD 8 items test``()
    [<Test>] member __.``ADD double test`` () = base.``ADD double test``()
    [<Test>] member __.``ADD int16 test`` () = base.``ADD int16 test``()
    [<Test>] member __.``ADD int32 test`` () = base.``ADD int32 test``()
    [<Test>] member __.``ADD int64 test`` () = base.``ADD int64 test``()
    [<Test>] member __.``ADD int8 test`` () = base.``ADD int8 test``()
    [<Test>] member __.``ADD MUL 3 items test`` () = base.``ADD MUL 3 items test``()
    [<Test>] member __.``ADD simple test`` () = base.``ADD simple test``()
    [<Test>] member __.``ADD single test`` () = base.``ADD single test``()
    [<Test>] member __.``Arithmetic assign test`` () = base.``Arithmetic assign test``()
    [<Test>] member __.``Arithmetic assign test1`` () = base.``Arithmetic assign test1``()
    [<Test>] member __.``Arithmetic test1`` () = base.``Arithmetic test1``()
    [<Test>] member __.``Arithmetic test2`` () = base.``Arithmetic test2``()
    [<Test>] member __.``Arithmetic test3`` () = base.``Arithmetic test3``()
    [<Test>] member __.``Arithmetic test4`` () = base.``Arithmetic test4``()
    [<Test>] member __.``Arithmetic test5`` () = base.``Arithmetic test5``()
    [<Test>] member __.``Arithmetic test6`` () = base.``Arithmetic test6``()
    [<Test>] member __.``Comparision, Arithmetic, AND test`` () = base.``Comparision, Arithmetic, AND test``()
    [<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()

type XgkArithematicTest() =
    inherit XgxArithematicTest(XGK)

    [<Test>] member __.``ADD 10 items test`` () = base.``ADD 10 items test``()
    [<Test>] member __.``ADD 100 items test`` () = base.``ADD 100 items test``()
    [<Test>] member __.``ADD 3 items test`` () = base.``ADD 3 items test``()
    [<Test>] member __.``ADD 7 items test`` () = base.``ADD 7 items test``()
    [<Test>] member __.``ADD 8 items test`` () = base.``ADD 8 items test``()
    [<Test>] member __.``ADD double test`` () = base.``ADD double test``()
    [<Test>] member __.``ADD int16 test`` () = base.``ADD int16 test``()
    [<Test>] member __.``ADD int32 test`` () = base.``ADD int32 test``()
    [<Test>] member __.``ADD int64 test`` () = base.``ADD int64 test``()
    [<Test>] member __.``ADD int8 test`` () = base.``ADD int8 test``()
    [<Test>] member __.``ADD MUL 3 items test`` () = base.``ADD MUL 3 items test``()
    [<Test>] member __.``ADD simple test`` () = base.``ADD simple test``()
    [<Test>] member __.``ADD single test`` () = base.``ADD single test``()
    [<Test>] member __.``Arithmetic assign test`` () = base.``Arithmetic assign test``()
    [<Test>] member __.``Arithmetic assign test1`` () = base.``Arithmetic assign test1``()
    [<Test>] member __.``Arithmetic test1`` () = base.``Arithmetic test1``()
    [<Test>] member __.``Arithmetic test2`` () = base.``Arithmetic test2``()
    [<Test>] member __.``Arithmetic test3`` () = base.``Arithmetic test3``()
    [<Test>] member __.``Arithmetic test4`` () = base.``Arithmetic test4``()
    [<Test>] member __.``Arithmetic test5`` () = base.``Arithmetic test5``()
    [<Test>] member __.``Arithmetic test6`` () = base.``Arithmetic test6``()
    [<Test>] member __.``Comparision, Arithmetic, AND test`` () = base.``Comparision, Arithmetic, AND test``()
    [<Test>] member __.``DIV 3 items test`` () = base.``DIV 3 items test``()




