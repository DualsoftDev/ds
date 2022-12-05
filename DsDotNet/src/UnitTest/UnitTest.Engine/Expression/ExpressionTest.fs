namespace UnitTest.Engine.Expression

open System
open NUnit.Framework

open Engine.Core
open Engine.CpuUnit
open Engine.Parser.FS
open Engine.Common.FS
open UnitTest.Engine

[<AutoOpen>]
module TestModule =
    let evalExpr (text:string) = (parseExpression text).BoxedEvaluatedValue
    let v = ExpressionModule.literal
    let evaluate (exp:IExpression) = exp.BoxedEvaluatedValue
    let dq = "\""

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
            let t1 = PlcTag.Create("1", 1)
            tag t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            tag t1 |> evaluate === 2
            t1.Value <- 3
            tag t1 |> evaluate === 3

            (t1 <== add [v 3; v 4]).Do()
            t1.Value === 7

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)
            let tString = PlcTag.Create("1", "1")
            (tString <== exp).Do()
            tString.Value === "helloworld"

            let t2 = PlcTag.Create("2", 2)
            add [tag t2; tag t2] |> evaluate === 4
            //함수 없는 Tag 배열 평가는 불가능
            (fun () -> v [t2;t2]   |> evaluate  === 1) |> ShouldFail

            tag (PlcTag.Create("Two", "Two")) |> evaluate === "Two"

            concat([
                    tag <| PlcTag.Create("Hello", "Hello, ")
                    tag <| PlcTag.Create("World", "world!" )
                ]) |> evaluate === "Hello, world!"

            let tt1 = tag t1
            t1.Value <- 1
            let tt2 = PlcTag.Create("t2", 2)

            let addTwoExpr = add [ tt1; tag tt2 ]
            addTwoExpr |> evaluate  === 3
            t1.Value <- 10
            addTwoExpr |> evaluate  === 12



        [<Test>]
        member __.``3 Func test`` () =

            abs [v 13]                  |> evaluate === 13
            abs [v -13]                 |> evaluate === 13
            abs [v -13.0]               |> evaluate === 13.0
            bitwiseXor [v 13; v 11]         |> evaluate === 6
            bitwiseAnd [v 2; v 3]           |> evaluate === 2
            bitwiseAnd [v 1; v 2; v 3; v 4] |> evaluate === 0
            bitwiseOr [v 1; v 2; v 3; v 4]  |> evaluate === 7
            bitwiseNot [v 65535]            |> evaluate === -65536
            add [v 1; v 2]              |> evaluate === 3
            sub [v 5; v 3]              |> evaluate === 2
            mul [v 2; v 3]              |> evaluate === 6
            div [v 3.0; v 2.0]          |> evaluate === 1.5
            add [v 1; v 2; v 3]         |> evaluate === 6
            add ([1..10]                |> List.map(v >> iexpr))   |> evaluate === 55
            mul( [1..5]                 |> List.map(v >> iexpr))   |> evaluate === 120
            sub [v 10; v 1; v 2]        |> evaluate === 7
            let xxx = sub [add [v 1.1; v 2.2]; v 3.3]

            abs [ sub [add [v 1.1; v 2.2]; v 3.3] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            abs [ sub [mul [v 1.1; v 2.0]; v 2.2] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            addString [v "Hello, "; v "world!"]|> evaluate === "Hello, world!"
            mul [v 2; v 3] |> evaluate === 6
            equalString [v "Hello"; v "world"]    |> evaluate === false
            equalString [v "Hello"; v "Hello"]    |> evaluate === true
            notEqualString [v "Hello"; v "world"] |> evaluate === true
            notEqualString [v "Hello"; v "Hello"] |> evaluate === false
            notEqual [v 1; v 2]                   |> evaluate === true
            notEqual [v 2; v 2]                   |> evaluate === false
            equal [v 2; v 2]                      |> evaluate === true
            equal [v 2; v 2.0]                    |> evaluate=== true
            equal [v 2; v 2.0f]                   |> evaluate === true


            gte [v 2; v 3]                        |> evaluate === false
            gte [v 5; v 4]                        |> evaluate === true
            noT [v true]                          |> evaluate  === false
            noT [v false]                         |> evaluate  === true
            anD [v true; v false]                 |> evaluate === false
            anD [v true; v true]                  |> evaluate === true
            anD [v true; v true; v true; v false] |> evaluate === false
            oR  [v true; v false]                 |> evaluate === true
            oR  [v false;v false]                 |> evaluate === false
            oR  [v true; v true; v true; v false] |> evaluate === true
            shiftLeft [v 1; v 1]                  |> evaluate === 2
            shiftLeft [v 2; v -1]                 |> evaluate === 0
            shiftLeft [v 1; v 3]                  |> evaluate === 8
            shiftRight [v 8; v 3]                 |> evaluate === 1

            let ex = mul [v 2; v 3]
            equal [v 6; ex]                       |> evaluate === true
            equal [v 6; mul [v 2; v 3]]           |> evaluate === true
            add [v 1; v 2]                        |> evaluate === 3

        [<Test>]
        member __.``4 Composition test`` () =
            mul [
                    tag <| PlcTag.Create("t2", 2)
                    add [v 1; v 2]
                    add [v 4; v 5]
            ] |> evaluate === 54


            mul [v 2; v 3; v 4] |> evaluate === 24

            (*
             (1<<2) * ((8>>3) + 4) * 5
             = 4 * (1+4) * 5
             = 100
            *)
            mul [   shiftLeft [v 1; v 2]   // 4
                    add [
                        shiftRight [v 8; v 3]   // 1
                        v 4
                        ]
                    v 5] |> evaluate === 100   // 4 * (1+4) * 5



        [<Test>]
        member __.``5 Statement test`` () =
            let expr = mul [v 2; v 3; v 4]
            let target = PlcTag.Create("target", 1)
            let targetExpr = tag target

            let stmt = Assign (expr, target)
            stmt.Do()
            targetExpr |> evaluate === 24

            (Assign (v 9, target)).Do()
            targetExpr |> evaluate === 9

            let source = PlcTag.Create("source", 33)
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 33
            source.Value <- 44
            targetExpr |> evaluate  === 33
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 44

        [<Test>]
        member __.``6 Serialization test`` () =
            let toText (exp:IExpression) = exp.ToText(false)

            v 1         |> toText === "1"
            v "hello"   |> toText === "\"hello\""
            v Math.PI   |> toText === sprintf "%A" Math.PI
            v true      |> toText === "true"
            v false     |> toText === "false"
            v 3.14f     |> toText === sprintf "%A" 3.14f
            v 3.14      |> toText === "3.14"

            mul [ v 2; v 3 ] |> toText === "2 * 3"
            mul [ add [v 1; v 2]; v 3 ] |> toText === "(1 + 2) * 3"
            mul [ v 3; add [v 1; v 2] ] |> toText === "3 * (1 + 2)"
            add [ mul [v 1; v 2]; v 3; ] |> toText === "(1 * 2) + 3"  //"1*2+3"

            add [v 1; v 2; v 3 ] |> toText === "+(1, 2, 3)"
            mul [ add [v 1; v 2; v 3]; v 3 ] |> toText === "+(1, 2, 3) * 3"

            mul [   v 2
                    add [v 1; v 2]
                    add [v 4; v 5]
            ] |> toText === "*(2, (1 + 2), (4 + 5))"
            mul [v 2; add[v 3; v 4]]|> toText  === "2 * (3 + 4)"

            add [v 2; mul [ v 5; v 6 ]; v 4]|> toText  === "+(2, (5 * 6), 4)"
            add [
                add [v 2; mul [ v 5; v 6 ]];
                v 4
            ]|> toText  === "(2 + (5 * 6)) + 4"  //"2+(5*6)+4)"
            mul [v 2; v 3; v 4]|> toText  === "*(2, 3, 4)"

        //    mul [   2
        //            add [1; 2]
        //            add [4; 5]
        //    ] |> toText === "*[2; +[1; 2]; +[4; 5]]"
        //    mul [2; add[3; 4]]|> toText  === "*[2; +[3; 4]]"

        //    mul [2; 3; 4]|> toText  === "*[2; 3; 4]"
        //    mul [2; 3; 4]|> toText  === "*[2; 3; 4]"

            let t1 = PlcTag.Create("t1", 1)
            let t2 = PlcTag.Create("t2", 2)
            let tt1 = t1 |> tag
            let tt2 = t2 |> tag
            let addTwoExpr = add [ tt1; tt2 ]
            addTwoExpr.ToText(false) === "%t1 + %t2"

            let sTag = PlcTag.Create("address", "value")
            sTag.ToText() === "%address"
            let exprTag = tag sTag
            exprTag.ToText(false) === "%address"


            let expr = mul [v 2; v 3; v 4]
            let target = PlcTag.Create("target", 1)
            target.ToText() === "%target"

            let stmt = Assign (expr, target)
            stmt.ToText() === "%target := *(2, 3, 4)"

        [<Test>]
        member __.``7 Deserialize test`` () =
            /// Parse And Serialize
            let pns (text:string) =
                let expr = parseExpression text
                expr.ToText(false)
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
            let t = cast_bool [v true]
            let f = cast_bool [v false]
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

            let target = PlcTag.Create("bool", false)
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
                PlcTag.Create("sbyte", 1y)     |> tag |> iexpr
                PlcTag.Create("byte", 1uy)     |> tag |> iexpr
                PlcTag.Create("int16", 1s)     |> tag |> iexpr
                PlcTag.Create("uint16", 1us)   |> tag |> iexpr
                PlcTag.Create("int32", 1)      |> tag |> iexpr
                PlcTag.Create("uint32", 1u)    |> tag |> iexpr
                PlcTag.Create("int64", 1L)     |> tag |> iexpr
                PlcTag.Create("uint64", 1UL)   |> tag |> iexpr
                PlcTag.Create("single", 1.0f)  |> tag |> iexpr
                PlcTag.Create("double", 1.0)   |> tag |> iexpr
                PlcTag.Create("char", '1')     |> tag |> iexpr
                PlcTag.Create("string", "1")   |> tag |> iexpr
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
            let t1 = PlcTag.Create("1", 1)
            tag t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)

            let exp2 = mul [ v 1; v 2; v 3 ]
            // Function-call should take arguments as an IExpression list.
            // let exp2 = mul [ 1; 2; 3 ]
            ()


