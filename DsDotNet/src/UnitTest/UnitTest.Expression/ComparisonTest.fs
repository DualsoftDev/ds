namespace T.Expression
open Dual.UnitTest.Common.FS

open NUnit.Framework

open Engine.Parser.FS
open T
open Engine.Core
open System

[<AutoOpen>]
module ComparisionTestModule =

    type ComparisonTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``1 ">" test`` () =
            let storages = Storages()
            let trues =
                [
                    "2 > 1"
                    "2.0 > 1.0"
                    "2s > 1s"
                    "2us > 1us"
                    "2y > 1y"
                    "2uy > 1uy"
                    "2L > 1L"
                    "2UL > 1UL"

                    "2 >= 1"
                    "2.0 >= 1.0"
                    "2s >= 1s"
                    "2us >= 1us"
                    "2y >= 1y"
                    "2uy >= 1uy"
                    "2L >= 1L"
                    "2UL >= 1UL"

                    "-5 > -6"
                    "(2 + 3) * 2 > 5"
                    "(2 + 3) * 2 < 20"
                ]
            for t in trues do
                t |> evalExpr storages === true

            let falses =
                [
                    "2 < 1"
                    "2.0 < 1.0"
                    "2s < 1s"
                    "2us < 1us"
                    "2y < 1y"
                    "2uy < 1uy"
                    "2L < 1L"
                    "2UL < 1UL"

                    "2 <= 1"
                    "2.0 <= 1.0"
                    "2s <= 1s"
                    "2us <= 1us"
                    "2y <= 1y"
                    "2uy <= 1uy"
                    "2L <= 1L"
                    "2UL <= 1UL"

                    "-5 < -6"
                    "(2 + 3) * 2 < 5"
                    "(2 + 3) * 2 > 20"
                ]
            for t in falses do
                t |> evalExpr storages === false


            let typeMismatches =
                [
                    "2 > 1y"
                    "2.0 > 1"
                ]
            for f in typeMismatches do
                (fun () -> f |> parseExpression4UnitTest storages |> ignore) |> ShouldFailWithSubstringT "Type mismatch"


        [<Test>]
        member __.``1 "==" test`` () =
            let storages = Storages()
            let trues =
                [
                    "1 == 1"
                    "1s == 1s"
                    "1us == 1us"
                    "1y == 1y"
                    "1uy == 1uy"
                    "1L == 1L"
                    "1UL == 1UL"

                    "1 >= 1"
                    "1s >= 1s"
                    "1 <= 1"
                    "1s <= 1s"

                    $"{dq}hello{dq} == {dq}hello{dq}"

                    "(1 + 1) * 2 == 4"
                    "equal(1, (2 / 2), (3 / 3), 1)"
                ]
            for t in trues do
                t |> evalExpr storages === true

        [<Test>]
        member __.``1 "==" with different type test`` () =
            let storages = Storages()
            (* 구현을 위한 내부 함수 isEqual, gte 등은 type 과 상관없이 대소 비교 가능해야 하지만,
               실제 사용자가 사용할 때에는 type 이 같아야 한다. *)
            isEqual 1 1s === true
            isEqual 1u 1s === true
            isEqual 1 1.0 === true

            let fails =
                [
                    "1s == 1"
                    "1s == 1u"
                    "1us == 1.0"
                    "1s >= 1"
                ]
            for f in fails do
                (fun () -> f |> parseExpression4UnitTest storages |> ignore) |> ShouldFailWithSubstringT "Type mismatch"



    type ShiftTest() =
        inherit ExpressionTestBaseClass()

        [<Obsolete("1s <<< 2 = 4s 에 대해서 동작하지 않습니다.  수정 필요")>]
        [<Test>]
        member __.``1 ">>>" test`` () =
            let storages = Storages()
            let trues =
                [
                    "8 >>> 1 == 4"
                    "8s >>> 1 == 4s"
                    "8us >>> 1 == 4us"
                    "8L >>> 1 == 4L"
                    "8UL >>> 1 == 4UL"

                    "1 <<< 2 == 4"
                    "1s <<< 2 == 4s"
                    "1us <<< 2 == 4us"
                    "1L <<< 2 == 4L"
                    "1UL <<< 2 == 4UL"


                    "8 >> 1 == 4"
                    "8s >> 1 == 4s"
                    "8us >> 1 == 4us"
                    "8L >> 1 == 4L"
                    "8UL >> 1 == 4UL"

                    "1 << 2 == 4"
                    "1s << 2 == 4s"
                    "1us << 2 == 4us"
                    "1L << 2 == 4L"
                    "1UL << 2 == 4UL"


                ]
            for t in trues do
                t |> evalExpr storages === true
        [<Test>]
        member __.``1 Math test`` () =
            let storages = Storages()
            let trues =
                [
                    "sin(0) == 0.0"
                    "sin(.0) == 0.0"
                    "sin(0.) == 0.0"
                    "sin(0.f) == 0.0"
                    "sin(.0f) == 0.0"

                    "sin( 3.14 / 2.0 ) <= 1.0"
                    "sin( 3.14 / 2.0 ) >= 0.999"
                ]

            for t in trues do
                t |> evalExpr storages === true


        [<Test>]
        member __.``1 Bitwise operation test`` () =
            let storages = Storages()
            let trues =
                [
                    "8 &&& 255 == 8"
                    "8uy &&& 255uy == 8uy"
                    "24s &&& 16s == 16s"
                    "24u &&& 8u == 8u"

                    "1 ||| 2 ||| 4 == 7"
                    "1uy ||| 2uy == 3uy"

                    "2 ^^^ 3 == 1"

                    "~~~ 0y == -1y"
                    "~~~ 0uy == 255uy"
                    "~~~ 0s == -1s"
                    "~~~ 0us == 65535us"
                    "~~~ 0 == -1"
                    "~~~ 0u == 4294967295u"
                ]

            for t in trues do
                t |> evalExpr storages === true


        [<Test>]
        member __.``1 Logical And/Or/Not test`` () =
            let storages = Storages()
            let trues =
                [
                    "true != false"
                    "true <> false"

                    "true == true"
                    "false == false"
                    "! true == false"
                    "! false == true"
                    "!true == false"
                    "!false == true"
                    "!true == !true"


                    "true && true == true"
                    "true && false  == false"
                    "false && true == false"
                    "false && false == false"


                    "true || true == true"
                    "true || false  == true"
                    "false || true == true"
                    "false || false == false"

                    "(true || false) && true == true"
                    "(false || false) && true == false"

                    "2 > 3 == false"
                    "3 > 2 == true"

                    "3 > 2 == 10 > 9"

                    (* cast to boolean test *)
                    "toBool(0) == false"
                    "toBool(0.0) == false"
                    "toBool(0.0000000000001) == true"
                    "toBool(3) == true"
                    "toBool(3.0) == true"

                    "toInt(false) == 0"
                    "toInt(true) == 1"
                ]

            for t in trues do
                t |> evalExpr storages === true


            let fails =
                [
                    "0 & true"
                    "0 && true"
                    "0 &&& true"
                    "0 | true"
                    "0 || true"
                    "0 ||| true"
                    "1 == true"
                    "0 == false"
                ]
            for f in fails do
                (fun () -> f |> parseExpression4UnitTest storages |> ignore) |> ShouldFailWithSubstringT "Type mismatch"



        [<Test>]
        member __.``1 TypeCast test`` () =
            let storages = Storages()
            let trues =
                [
                    "(bool)0 == false"
                    "toBool(0) == false"
                    "toBool(0.0) == false"
                    "toBool(0.0000000000001) == true"
                    "toBool(3) == true"
                    "toBool(3.0) == true"

                    "toInt8(false) == 0y"
                    "toInt8(true) == 1y"
                    "toUInt8(false) == 0uy"
                    "toUInt8(true) == 1uy"

                    "toInt16(false) == 0s"
                    "toInt16(true) == 1s"
                    "toUInt16(false) == 0us"
                    "toUInt16(true) == 1us"

                    "toInt32(false) == 0"
                    "toInt32(true) == 1"
                    "toUInt32(false) == 0u"
                    "toUInt32(true) == 1u"

                    "toInt64(false) == 0L"
                    "toInt64(true) == 1L"
                    "toUInt64(false) == 0UL"
                    "toUInt64(true) == 1UL"


                    "toInt(false) == 0"
                    "toInt(true) == 1"


                    "(bool)0 && true == false"
                    "(bool)(2 + 3) == true"
                ]

            for t in trues do
                t |> evalExpr storages === true

            let fails =
                [
                    "(bool)(0 && true)"
                ]
            for f in fails do
                (fun () -> f |> parseExpression4UnitTest storages |> ignore) |> ShouldFailWithSubstringT "Type mismatch"

