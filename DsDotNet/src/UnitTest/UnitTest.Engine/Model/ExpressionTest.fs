namespace UnitTest.Engine

open System
open System.Linq
open Engine.Core
open NUnit.Framework
open Engine.Cpu.TagModule
open Engine.Cpu
open Newtonsoft.Json
open Engine.Parser.FS.ExpressionParser
open Engine.Common.FS

[<AutoOpen>]
module ExpressionTestModule =

    type ExpressionTest() =
        do Fixtures.SetUpTest()

        let value = ExpressionModule.value
        let evaluate (exp:Expression<'T>) = exp.Evaluate()

        [<Test>]
        member __.``1 ExpressionValueUnit test`` () =


            //지원 value type : bool, int, single, double, string
            value 1       |> evaluate === 1
            value 1       |> evaluate === 1
            value "hello" |> evaluate === "hello"
            value Math.PI |> evaluate === Math.PI
            value true    |> evaluate === true
            value false   |> evaluate === false
            value 3.14f   |> evaluate === 3.14f
            value 3.14    |> evaluate === 3.14

            /////미지원 value type : uint, int64, ... 지원 기준외 등등
            (fun () -> value 1u   |> evaluate  === 1) |> ShouldFail
            (fun () -> value 1L   |> evaluate  === 1) |> ShouldFail
            (fun () -> value 1.0m |> evaluate  === 1) |> ShouldFail

            ////함수 없는 Value 배열 평가는 불가능
            (fun () -> value [1;2]   |> evaluate  === 1) |> ShouldFail

        [<Test>]
        member __.``2 ExpressionTagUnit test`` () =
            let t1 = PlcTag.Create("1", 1)
            tag t1 |> evaluate === 1
            t1.SetValue(2)

            tag t1 |> evaluate === 2
            t1.SetValue(3)
            tag t1 |> evaluate === 3

            let t2 = PlcTag.Create("2", 2)
            add [tag t2; tag t2] |> evaluate === 4
            //함수 없는 Tag 배열 평가는 불가능
            (fun () -> value [t2;t2]   |> evaluate  === 1) |> ShouldFail

            tag (PlcTag.Create("Two", "Two")) |> evaluate === "Two"

            concat([
                    (tag <| PlcTag.Create("Hello", "Hello, ")).Evaluate()
                    (tag <| PlcTag.Create("World", "world!" )).Evaluate()
                ]) |> evaluate === "Hello, world!"

            let tt1 = tag t1
            t1.SetValue( 1 )
            let tt2 = PlcTag.Create("t2", 2)

            let addTwoExpr = add [ tt1; tag tt2 ]
            addTwoExpr |> evaluate  === 3
            t1.SetValue( 10 )
            addTwoExpr |> evaluate  === 12



        [<Test>]
        member __.``3 ExpressionFuncUnit test`` () =

            abs[13]                          |> evaluate === 13
            abs[-13]                         |> evaluate === 13
            absDouble[-13.0]                 |> evaluate === 13.0
            xorBit[13; 11]                   |> evaluate === 6
            andBit[2; 3]                     |> evaluate === 2
            andBit[1; 2; 3; 4]               |> evaluate === 0
            orBit[1; 2; 3; 4]                |> evaluate === 7
            notBit[65535]                    |> evaluate === -65536
            add[1; 2]                        |> evaluate === 3
            sub[5; 3]                        |> evaluate === 2
            mul[2; 3]                        |> evaluate === 6
            divDouble[3.0; 2.0]              |> evaluate === 1.5
            add[1; 2; 3]                     |> evaluate === 6
            add([1..10]|>List.map(fun f->f)) |> evaluate === 55
            mul([1..5]|> List.map(fun f->f)) |> evaluate === 120
            sub[10; 1; 2]       |> evaluate === 7
            //Math.Abs((addDouble[1.1; 2.2] |> evaluate) - 3.3) <= 0.00001 |> ShouldBeTrue
            //Math.Abs((mulDouble[1.1; 2.0] |> evaluate) - 2.2) <= 0.00001 |> ShouldBeTrue
            addString["Hello, "; "world!"]|> evaluate === "Hello, world!"
            mul[2; 3] |> evaluate === 6
            equalString["Hello"; "world"]       |> evaluate === false
            equalString["Hello"; "Hello"]       |> evaluate === true
            notEqualString["Hello"; "world"]    |> evaluate === true
            notEqualString["Hello"; "Hello"]    |> evaluate === false
            notEqual[1; 2]                      |> evaluate === true
            notEqual[2; 2]                      |> evaluate === false
            equal[2; 2]                         |> evaluate === true
            equal[2; 2.0]                       |> evaluate=== true
            equal[2; 2.0f]                      |> evaluate === true


            gte [2; 3] |> evaluate === false
            gte [5; 4] |> evaluate === true
            noT [true] |> evaluate  === false
            noT [false] |> evaluate  === true
            anD [true; false] |> evaluate === false
            anD [true; true] |> evaluate === true
            anD [true; true; true; false] |> evaluate === false
            oR  [true; false] |> evaluate === true
            oR  [false; false] |> evaluate === false
            oR  [true; true; true; false] |> evaluate === true
            shiftLeft [1; 1] |> evaluate === 2
            shiftLeft [2; -1] |> evaluate === 0
            shiftLeft [1; 3] |> evaluate === 8
            shiftRight [8; 3] |> evaluate === 1

            let ex = mul [2; 3]
            equal [6; ex]                    |> evaluate === true
            equal [6; mul [2; 3]]            |> evaluate === true
            add [1; 2]     |> evaluate === 3

        [<Test>]
        member __.``4 ExpressionComposition test`` () =
            mul [
                    tag <| PlcTag.Create("t2", 2)
                    add [1; 2]
                    add [4; 5]
            ] |> evaluate === 54


            mul [2; 3; 4] |> evaluate === 24

            (*
             (1<<2) * ((8>>3) + 4) * 5
             = 4 * (1+4) * 5
             = 100
            *)
            mul [   shiftLeft [1; 2]   // 4
                    add [
                        shiftRight [8; 3]   // 1
                        4
                        ]
                    5] |> evaluate === 100   // 4 * (1+4) * 5



        [<Test>]
        member __.``5 Statement test`` () =
            let expr = mul [2; 3; 4]
            let target = PlcTag.Create("target", 1)
            let targetExpr = tag target

            let stmt = Assign (expr, target)
            stmt.Do()
            targetExpr |> evaluate === 24

            (Assign (value 9, target)).Do()
            targetExpr |> evaluate === 9

            let source = PlcTag.Create("source", 33)
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 33
            source.SetValue 44
            targetExpr |> evaluate  === 33
            Assign(tag source, target).Do()
            targetExpr |> evaluate === 44

        [<Test>]
        member __.``X 6 Serialization test`` () =
            let toText (exp:Expression<'T>) = exp.ToText()
            value 1         |> toText === "1"
            value "hello"   |> toText === "hello"
            value Math.PI   |> toText === Math.PI.ToString()
            value true      |> toText === "True"
            value false     |> toText === "False"
            value 3.14f     |> toText === "3.14"
            value 3.14      |> toText === "3.14"


            mul [   value 2
                    add [value 1; value 2]
                    add [value 4; value 5]
            ] |> toText === "*[2; +[1; 2]; +[4; 5]]"
            mul [2; add[3; 4]]|> toText  === "*[2; +[3; 4]]"

            mul [2; 3; 4]|> toText  === "*[2; 3; 4]"
            mul [2; 3; 4]|> toText  === "*[2; 3; 4]"

            let t1 = PlcTag.Create("t1", 1)
            let t2 = PlcTag.Create("t2", 2)
            let tt1 = t1 |> tag
            let tt2 = t2 |> tag

            let addTwoExpr = add [ tt1; tt2 ]
            addTwoExpr.ToText() === "+[(t1=1); (t2=2)]"


            let sTag = PlcTag.Create("address", "value")
            sTag.ToText() === "(address=value)"
            let exprTag = tag sTag
            exprTag.ToText() === "(address=value)"


            let expr = mul [2; 3; 4]
            let target = PlcTag.Create("target", 1)
            target.ToText() === "(target=1)"

            let stmt = Assign (expr, target)
            stmt.ToText() === "assign(*[2; 3; 4], (target=1))"

        [<Test>]
        member __.``X 7 Deserialize test`` () =
            ()
            let t2 = PlcTag.Create("t2", 2)
            let t1 = PlcTag.Create("t1", 1)
            let addTwoExpr = add [ t1;t2 ]
            //addTwoExpr.ToJsonText().ToExpression().ToJsonText() === addTwoExpr.ToJsonText()

            let expr = mul [   2
                               add [tag t1; tag t2]
                               add [4; 5]
                            ]
            let jsonSettings = JsonSerializerSettings(Formatting=Formatting.Indented)

            let json = JsonConvert.SerializeObject(expr, jsonSettings)
            let obj = JsonConvert.DeserializeObject<Expression<int>>(json, jsonSettings)

            ()

        //    expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()

        //    let expr = oR [false ;true ;false ]
        //    expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()

        //    let expr = add [10 ;12 ]
        //    expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()
        //    let target = PlcTag.Create("target", 1)


        //    let expr = mul [    2
        //                        expr
        //                        add [t1; t2]
        //                        add [1; 5]
        //            ]
        //    let a = expr |> evaluate

        //    let stmt = Assign (expr, target)
        //    stmt.ToJsonText().ToStatement().ToJsonText() === stmt.ToJsonText()


        [<Test>]
        member __.``8 ExpressionOperator test`` () =
            let t = Bool [true]
            let f = Bool [false]
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
        member __.``9 Expression Tag type test`` () =
            let rawTags = [
                PlcTag.Create("sbyte", 1y) |> box
                PlcTag.Create("byte", 1uy)
                PlcTag.Create("int16", 1s)
                PlcTag.Create("uint16", 1us)
                PlcTag.Create("int32", 1)
                PlcTag.Create("uint32", 1u)
                PlcTag.Create("int64", 1L)
                PlcTag.Create("uint64", 1UL)
                PlcTag.Create("single", 1.0f)
                PlcTag.Create("double", 1.0)
                PlcTag.Create("char", '1')
                PlcTag.Create("string", "1")
            ]
            let tags = rawTags |> List.map (createExpressionFromBoxedStorage)
            let tagDic =
                [   for t in tags do
                        let exp = t :?> IExpression
                        let inner = exp.GetBoxedRawObject()
                        let name = (inner :?> INamed).Name
                        (name, t)
                ] |> Tuple.toDictionary
            let sbyte = tagDic["sbyte"] :?> IExpression //:?> Terminal
            sbyte.Type === typedefof<sbyte>
            sbyte.BoxedEvaluatedValue === 1y

            let rawVariables = [
                StorageVariable("sbyte", 1y) |> box
                StorageVariable("byte", 1uy)
                StorageVariable("int16", 1s)
                StorageVariable("uint16", 1us)
                StorageVariable("int32", 1)
                StorageVariable("uint32", 1u)
                StorageVariable("int64", 1L)
                StorageVariable("uint64", 1UL)
                StorageVariable("single", 1.0f)
                StorageVariable("double", 1.0)
                StorageVariable("char", '1')
                StorageVariable("string", "1")
            ]
            let variables = rawVariables |> List.map (createExpressionFromBoxedStorage)
            let varDic =
                [   for t in variables do
                        let exp = t :?> IExpression
                        let inner = exp.GetBoxedRawObject()
                        let name = (inner :?> INamed).Name
                        (name, t)
                ] |> Tuple.toDictionary
            let sbyte = varDic["sbyte"] :?> IExpression //:?> Terminal
            sbyte.Type === typedefof<sbyte>
            sbyte.BoxedEvaluatedValue === 1y

            ()

        [<Test>]
        member __.``10 ExpressionParse test`` () =
            let evalExpr = parseExpression >> evaluateBoxedExpression


            //"Int(3.4) + 1 + 2 + (abs(%tag3))"
            "1 + 2" |> evalExpr === 3

            "1.0 + 2.0" |> evalExpr === 3.0

            (fun () -> "1.0 + 2" |> evalExpr |> ignore )
            |> ShouldFailWithSubstringT "Type mismatch"

            (fun () -> "\"hello\" + 2" |> evalExpr |> ignore )
            |> ShouldFailWithSubstringT "Type mismatch"

            "Int(1.0) + 2" |> evalExpr === 3

            """  "hello, " + "world" """ |> evalExpr === "hello, world"
