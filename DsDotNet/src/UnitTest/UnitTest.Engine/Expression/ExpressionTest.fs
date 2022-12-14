namespace UnitTest.Engine.Expression

open System
open NUnit.Framework

open Engine.Core
open Engine.CodeGenCPU
open Engine.Parser.FS
open Engine.Common.FS
open UnitTest.Engine

[<AutoOpen>]
module TestModule =
    let evalExpr (text:string) = (parseExpression text).BoxedEvaluatedValue
    let v = ExpressionModule.literal
    let evaluate (exp:IExpression) = exp.BoxedEvaluatedValue
    let dq = "\""
    /// Parse And Serialize
    let pns (text:string) =
        let expr = parseExpression text
        expr.ToText(false)
    let toText (exp:IExpression) = exp.ToText(false)

    type ExpressionTest() =
        do Fixtures.SetUpTest()


        [<Test>]
        member __.``1 ExpressionValueUnit test`` () =


            (* 지원 value type :
                - bool, string,
                - dotnet numeric type
                    - (s)byte, (u)int16, (u)int32, (u)int64
                    - float, double
            *)

            v 1       |> evaluate === 1
            v 1       |> evaluate === 1
            v "hello" |> evaluate === "hello"
            v Math.PI |> evaluate === Math.PI
            v true    |> evaluate === true
            v false   |> evaluate === false
            v 3.14f   |> evaluate === 3.14f
            v 3.14    |> evaluate === 3.14

            v 1u   |> evaluate  === 1u
            v 1L   |> evaluate  === 1L
            v 1.0m |> evaluate  === 1.0m

            // primitive type (bool, int16, intXX, double, .., string)  을 제외한 나머지에 대한 value 는 생성 불가.
            (fun () -> v [1;2] |> ignore) |> ShouldFailWithSubstringT "Value Type Error"


        [<Test>]
        member __.``2 Tag test`` () =
            let t1 = PlcTag("1", 1)
            tag t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            tag t1 |> evaluate === 2
            t1.Value <- 3
            tag t1 |> evaluate === 3

            (t1 <== fAdd [v 3; v 4]).Do()
            t1.Value === 7

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)
            let tString = PlcTag("1", "1")
            (tString <== exp).Do()
            tString.Value === "helloworld"

            let t2 = PlcTag("2", 2)
            fAdd [tag t2; tag t2] |> evaluate === 4
            //함수 없는 Tag 배열 평가는 불가능
            (fun () -> v [t2;t2]   |> evaluate  === 1) |> ShouldFail

            tag (PlcTag("Two", "Two")) |> evaluate === "Two"

            fConcat([
                    tag <| PlcTag("Hello", "Hello, ")
                    tag <| PlcTag("World", "world!" )
                ]) |> evaluate === "Hello, world!"

            let tt1 = tag t1
            t1.Value <- 1
            let tt2 = PlcTag("t2", 2)

            let addTwoExpr = fAdd [ tt1; tag tt2 ]
            addTwoExpr |> evaluate  === 3
            t1.Value <- 10
            addTwoExpr |> evaluate  === 12



        [<Test>]
        member __.``3 Func test`` () =

            fAbs [v 13]                  |> evaluate === 13
            fAbs [v -13]                 |> evaluate === 13
            fAbs [v -13.0]               |> evaluate === 13.0
            fAdd [v 1; v 2]              |> evaluate === 3
            fSub [v 5; v 3]              |> evaluate === 2
            fMul [v 2; v 3]              |> evaluate === 6
            fDiv [v 3.0; v 2.0]          |> evaluate === 1.5
            fAdd [v 1; v 2; v 3]         |> evaluate === 6
            fAdd ([1..10]                |> List.map(v >> iexpr))   |> evaluate === 55
            fMul( [1..5]                 |> List.map(v >> iexpr))   |> evaluate === 120
            fSub [v 10; v 1; v 2]        |> evaluate === 7
            let xxx = fSub [fAdd [v 1.1; v 2.2]; v 3.3]

            fAbs [ fSub [fAdd [v 1.1; v 2.2]; v 3.3] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            fAbs [ fSub [fMul [v 1.1; v 2.0]; v 2.2] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            fConcat [v "Hello, "; v "world!"]|> evaluate === "Hello, world!"
            fMul [v 2; v 3] |> evaluate === 6
            fEqualString [v "Hello"; v "world"]    |> evaluate === false
            fEqualString [v "Hello"; v "Hello"]    |> evaluate === true
            fNotEqualString [v "Hello"; v "world"] |> evaluate === true
            fNotEqualString [v "Hello"; v "Hello"] |> evaluate === false
            fNotEqual [v 1; v 2]                   |> evaluate === true
            fNotEqual [v 2; v 2]                   |> evaluate === false
            fEqual [v 2; v 2]                      |> evaluate === true
            fEqual [v 2; v 2.0]                    |> evaluate=== true
            fEqual [v 2; v 2.0f]                   |> evaluate === true


            fGte [v 2; v 3]                        |> evaluate === false
            fGte [v 5; v 4]                        |> evaluate === true
            fLogicalNot [v true]                          |> evaluate  === false
            fLogicalNot [v false]                         |> evaluate  === true
            fLogicalAnd [v true; v false]                 |> evaluate === false
            fLogicalAnd [v true; v true]                  |> evaluate === true
            fLogicalAnd [v true; v true; v true; v false] |> evaluate === false
            fLogicalOr  [v true; v false]                 |> evaluate === true
            fLogicalOr  [v false;v false]                 |> evaluate === false
            fLogicalOr  [v true; v true; v true; v false] |> evaluate === true
            fShiftLeft [v 1; v 1]                  |> evaluate === 2
            fShiftLeft [v 2; v -1]                 |> evaluate === 0
            fShiftLeft [v 1; v 3]                  |> evaluate === 8
            fShiftRight [v 8; v 3]                 |> evaluate === 1

            let ex = fMul [v 2; v 3]
            fEqual [v 6; ex]                       |> evaluate === true
            fEqual [v 6; fMul [v 2; v 3]]           |> evaluate === true
            fAdd [v 1; v 2]                        |> evaluate === 3

        [<Test>]
        member __.``4 Composition test`` () =
            fMul [
                    tag <| PlcTag("t2", 2)
                    fAdd [v 1; v 2]
                    fAdd [v 4; v 5]
            ] |> evaluate === 54


            fMul [v 2; v 3; v 4] |> evaluate === 24

            (*
             (1<<2) * ((8>>3) + 4) * 5
             = 4 * (1+4) * 5
             = 100
            *)
            fMul [  fShiftLeft [v 1; v 2]   // 4
                    fAdd [
                        fShiftRight [v 8; v 3]   // 1
                        v 4
                        ]
                    v 5] |> evaluate === 100   // 4 * (1+4) * 5



        [<Test>]
        member __.``5 Statement test`` () =
            let expr = fMul [v 2; v 3; v 4]
            let target = PlcTag("target", 1)
            let targetExpr = tag target

            let stmt = Assign (expr, target)
            stmt.Do()
            targetExpr |> evaluate === 24

            (Assign (v 9, target)).Do()
            targetExpr |> evaluate === 9

            let source = PlcTag("source", 33)
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 33
            source.Value <- 44
            targetExpr |> evaluate  === 33
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 44

        [<Test>]
        member __.``6 Serialization test`` () =
            v 1         |> toText === "1"
            v "hello"   |> toText === "\"hello\""
            v Math.PI   |> toText === sprintf "%A" Math.PI
            v true      |> toText === "true"
            v false     |> toText === "false"
            v 3.14f     |> toText === sprintf "%A" 3.14f
            v 3.14      |> toText === "3.14"

            fMul [ v 2; v 3 ] |> toText === "2 * 3"
            fMul [ fAdd [v 1; v 2]; v 3 ] |> toText === "(1 + 2) * 3"
            fMul [ v 3; fAdd [v 1; v 2] ] |> toText === "3 * (1 + 2)"
            fAdd [ fMul [v 1; v 2]; v 3; ] |> toText === "1 * 2 + 3"

            fAdd [v 1; v 2; v 3 ] |> toText === "+(1, 2, 3)"
            fMul [ fAdd [v 1; v 2; v 3]; v 3 ] |> toText === "+(1, 2, 3) * 3"

            fMul [  v 2
                    fAdd [v 1; v 2]
                    fAdd [v 4; v 5]
            ] |> toText === "*(2, (1 + 2), (4 + 5))"
            fMul [v 2; fAdd[v 3; v 4]]|> toText  === "2 * (3 + 4)"

            fAdd [v 2; fMul [ v 5; v 6 ]; v 4]|> toText  === "+(2, (5 * 6), 4)"
            fAdd [
                fAdd [v 2; fMul [ v 5; v 6 ]];
                v 4
            ]|> toText  === "2 + 5 * 6 + 4"
            fMul [v 2; v 3; v 4] |> toText  === "*(2, 3, 4)"

            fAdd [ v 2
                   fMul [v 3; v 4] ] |> toText  === "2 + 3 * 4"
            fMul [ v 2
                   fAdd [v 3; v 4] ] |> toText  === "2 * (3 + 4)"
            fAdd [ v 2
                   fAdd [v 3; v 4] ] |> toText  === "2 + 3 + 4"

            fMul [  v 2
                    fAdd [v 1; v 2]
                    fAdd [v 4; v 5]
            ] |> toText === "*(2, (1 + 2), (4 + 5))"
            fMul [v 2; fAdd[v 3; v 4]]|> toText  === "2 * (3 + 4)"

            fMul [v 2; v 3; v 4]|> toText  === "*(2, 3, 4)"
            fMul [v 2; v 3; v 4]|> toText  === "*(2, 3, 4)"

            let t1 = PlcTag("t1", 1)
            let t2 = PlcTag("t2", 2)
            let tt1 = t1 |> tag
            let tt2 = t2 |> tag
            let addTwoExpr = fAdd [ tt1; tt2 ]
            addTwoExpr.ToText(false) === "%t1 + %t2"

            let sTag = PlcTag("address", "value")
            sTag.ToText() === "%address"
            let exprTag = tag sTag
            exprTag.ToText(false) === "%address"


            let expr = fMul [v 2; v 3; v 4]
            let target = PlcTag("target", 1)
            target.ToText() === "%target"

            let stmt = Assign (expr, target)
            stmt.ToText() === "%target := *(2, 3, 4)"


            "toBool(0)" |> pns === "toBool(0)"


        [<Test>]
        member __.``6 Operator Precedence test`` () =
            "2 + 3 + 4"         |> evalExpr === 9
            "2 + (3 + 4)"       |> evalExpr === 9
            "(2 + 3) + 4"       |> evalExpr === 9
            "2 * 3 + 4"         |> evalExpr === 10
            "(2 * 3) + 4"       |> evalExpr === 10
            "2 + 3 * 4"         |> evalExpr === 14
            "2 + (3 * 4)"       |> evalExpr === 14
            "2 * 3 + 4 * 5"     |> evalExpr === 26
            "(2 * 3) + (4 * 5)" |> evalExpr === 26


            "2 * (3 + 4)"       |> evalExpr === 14
            "(2 + 3) * (4 + 5)" |> evalExpr === 45
            "(2 + 3) * (4 * 5)" |> evalExpr === 100
            ()

        [<Test>]
        member __.``6 Text Serialization test`` () =
            "2 + 3 + 4"         |> pns === "2 + 3 + 4"
            "2 + (3 + 4)"       |> pns === "2 + 3 + 4"
            "(2 + 3) + 4"       |> pns === "2 + 3 + 4"
            "2 * 3 + 4"         |> pns === "2 * 3 + 4"
            "(2 * 3) + 4"       |> pns === "2 * 3 + 4"
            "2 + 3 * 4"         |> pns === "2 + 3 * 4"
            "2 + (3 * 4)"       |> pns === "2 + 3 * 4"
            "2 * 3 + 4 * 5"     |> pns === "2 * 3 + 4 * 5"
            "(2 * 3) + (4 * 5)" |> pns === "2 * 3 + 4 * 5"


            "2 * (3 + 4)"       |> pns === "2 * (3 + 4)"
            "(2 + 3) * (4 + 5)" |> pns === "(2 + 3) * (4 + 5)"
            "(2 + 3) * (4 * 5)" |> pns === "(2 + 3) * 4 * 5"
            ()



        [<Test>]
        member __.``7 Deserialize test`` () =
            let exprs =
                [
                    "1y + 2y"
                    "1uy + 2uy"
                    "1s + 2s"
                    "1us + 2us"
                    "1 + 2"
                    "1u + 2u"
                    "(1 + 2) * (3 + 4)"
                    "1.0 + 2.0"
                    "1.0f + 2.0f"
                    $"{dq}hello{dq} + {dq}world{dq}"
                    //"Int(1.0) + 2"
                ]

            for exp in exprs do
                pns exp === exp



        [<Test>]
        member __.``8 Operator test`` () =
            let t = fCastBool [v true]
            let f = fCastBool [v false]
            !! t |> evaluate === false
            !! f |> evaluate === true
            t <&&> t |> evaluate === true
            t <&&> f |> evaluate === false
            f <&&> t |> evaluate === false
            f <&&> f |> evaluate === false

            t <||> t |> evaluate === true
            t <||> f |> evaluate === true
            f <||> t |> evaluate === true
            f <||> f |> evaluate === false


            (t <||> f) <&&> t |> evaluate === true

            let target = PlcTag("bool", false)
            let targetExpr = tag target
            targetExpr |> evaluate === false

            let assignStatement = target <== t
            assignStatement.Do()
            targetExpr |> evaluate === true

            (target <== f).Do()
            targetExpr |> evaluate === false



        [<Test>]
        member __.``9 Tag type test`` () =
            let tags = [
                PlcTag("sbyte", 1y)     |> tag |> iexpr
                PlcTag("byte", 1uy)     |> tag |> iexpr
                PlcTag("int16", 1s)     |> tag |> iexpr
                PlcTag("uint16", 1us)   |> tag |> iexpr
                PlcTag("int32", 1)      |> tag |> iexpr
                PlcTag("uint32", 1u)    |> tag |> iexpr
                PlcTag("int64", 1L)     |> tag |> iexpr
                PlcTag("uint64", 1UL)   |> tag |> iexpr
                PlcTag("single", 1.0f)  |> tag |> iexpr
                PlcTag("double", 1.0)   |> tag |> iexpr
                PlcTag("char", '1')     |> tag |> iexpr
                PlcTag("string", "1")   |> tag |> iexpr
            ]
            let tagDic =
                [   for t in tags do
                        let inner = t.GetBoxedRawObject()
                        let name = (inner :?> INamed).Name
                        (name, t)
                ] |> Tuple.toDictionary
            let sbyte = tagDic["sbyte"]
            sbyte.DataType === typedefof<sbyte>
            sbyte.BoxedEvaluatedValue === 1y

            let variables = [
                StorageVariable("sbyte", 1y)    |> var |> iexpr
                StorageVariable("byte", 1uy)    |> var |> iexpr
                StorageVariable("int16", 1s)    |> var |> iexpr
                StorageVariable("uint16", 1us)  |> var |> iexpr
                StorageVariable("int32", 1)     |> var |> iexpr
                StorageVariable("uint32", 1u)   |> var |> iexpr
                StorageVariable("int64", 1L)    |> var |> iexpr
                StorageVariable("uint64", 1UL)  |> var |> iexpr
                StorageVariable("single", 1.0f) |> var |> iexpr
                StorageVariable("double", 1.0)  |> var |> iexpr
                StorageVariable("char", '1')    |> var |> iexpr
                StorageVariable("string", "1")  |> var |> iexpr
            ]
            let varDic =
                [   for v in variables do
                        let inner = v.GetBoxedRawObject()
                        let name = (inner :?> INamed).Name
                        (name, v)
                ] |> Tuple.toDictionary
            let sbyte = varDic["sbyte"]
            sbyte.DataType === typedefof<sbyte>
            sbyte.BoxedEvaluatedValue === 1y

            ()

        [<Test>]
        member __.``10 Parse test`` () =
            """  "hello, " + "world" """ |> evalExpr === "hello, world"

            "1 + 2" |> evalExpr === 3
            "+(1, 2, 3)" |> evalExpr === 6
            "+(1, *(2, 3))" |> evalExpr === 7
            "+(/(2, 1), 3)" |> evalExpr === 5


            //"Int(3.4) + 1 + 2 + (abs(%tag3))"


            "1.0 + 2.0" |> evalExpr === 3.0

            "+(2, 3, 4)" |> evalExpr === 9

            (fun () -> "1.0 + 2" |> evalExpr |> ignore )
            |> ShouldFailWithSubstringT "Type mismatch"
            (fun () -> "+(1.0, 2)" |> evalExpr |> ignore )
            |> ShouldFailWithSubstringT "Type mismatch"

            (fun () -> "\"hello\" + 2" |> evalExpr |> ignore )
            |> ShouldFailWithSubstringT "Type mismatch"

            "toInt(1.0) + 2" |> evalExpr === 3

            """  "hello, " + "world" """ |> evalExpr === "hello, world"

        [<Test>]
        member __.``11 Uncompilable test`` () =
            let t1 = PlcTag("1", 1)
            tag t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)

            let exp2 = fMul [ v 1; v 2; v 3 ]
            // Function-call should take arguments as an IExpression list.
            // let exp2 = mul [ 1; 2; 3 ]
            ()


