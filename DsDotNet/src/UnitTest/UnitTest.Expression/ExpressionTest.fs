namespace T.Expression
open Dual.UnitTest.Common.FS
open System
open NUnit.Framework

open Engine.Core
open Engine.CodeGenCPU
open Engine.Parser.FS
open Dual.Common.Core.FS
open T

[<AutoOpen>]
module Exp =
    let evalExpr storages (text:string) = (parseExpression4UnitTest storages text).BoxedEvaluatedValue
    let evalExpr2 (storages:Storages) (text:string) = (parseExpression4UnitTest storages text).BoxedEvaluatedValue
    let v = ExpressionModule.literal2expr
    let evaluate (exp:IExpression) = exp.BoxedEvaluatedValue
    let dq = "\""
    /// Parse And Serialize
    let pns storages (text:string) =
        let expr = parseExpression4UnitTest storages text
        expr.ToText()
    let pns2 (storages:Storages) (text:string) =
        let expr = parseExpression4UnitTest storages text
        expr.ToText()

    let toText (exp:IExpression) = exp.ToText()

    let toTimer (timerStatement:Statement) :Timer =
        match timerStatement with
        | DuTimer t -> t.Timer
        | _ -> failwithlog "not a timer statement"

    let toCounter (counterStatement:Statement) :Counter =
        match counterStatement with
        | DuCounter t -> t.Counter
        | _ -> failwithlog "not a counter statement"

    type ExpressionTest() =
        inherit ExpressionTestBaseClass()


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
            let storages = Storages()
            let t1 = createTag("1", "%M1.1", 1)
            var2expr t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            var2expr t1 |> evaluate === 2
            t1.Value <- 3
            var2expr t1 |> evaluate === 3

            (t1 <== fAdd [v 3; v 4]).Do()
            t1.Value === 7

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression4UnitTest storages :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)
            let tString = createTag("1", "%M1.1", "1")
            (tString <== exp).Do()
            tString.Value === "helloworld"

            let t2 = createTag("2", "%M1.1", 2)
            fAdd [var2expr t2; var2expr t2] |> evaluate === 4
            //함수 없는 Tag 배열 평가는 불가능
            (fun () -> v [t2;t2]   |> evaluate  === 1) |> ShouldFail

            var2expr (createTag("Two", "%M1.1", "Two")) |> evaluate === "Two"

            fConcat([
                    var2expr <| createTag("Hello", "%M1.1", "Hello, ")
                    var2expr <| createTag("World", "%M1.1", "world!" )
                ]) |> evaluate === "Hello, world!"

            let tt1 = var2expr t1
            t1.Value <- 1
            let tt2 = createTag("t2", "%M1.1", 2)

            let addTwoExpr = fAdd [ tt1; var2expr tt2 ]
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
            fAbs [ fSub [fAdd [v 1.1; v 2.2]; v 3.3] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            fAbs [ fSub [fMul [v 1.1; v 2.0]; v 2.2] ] |> evaluate :?> double <= 0.00001 |> ShouldBeTrue
            fConcat [v "Hello, "; v "world!"]|> evaluate === "Hello, world!"
            fMul [v 2; v 3] |> evaluate === 6
            fbEqualString [v "Hello"; v "world"]    |> evaluate === false
            fbEqualString [v "Hello"; v "Hello"]    |> evaluate === true
            fbNotEqualString [v "Hello"; v "world"] |> evaluate === true
            fbNotEqualString [v "Hello"; v "Hello"] |> evaluate === false
            fbNotEqual [v 1; v 2]                   |> evaluate === true
            fbNotEqual [v 2; v 2]                   |> evaluate === false
            fEqual [v 2; v 2]                      |> evaluate === true
            fEqual [v 2; v 2.0]                    |> evaluate=== true
            fEqual [v 2; v 2.0f]                   |> evaluate === true


            fbGte [v 2; v 3]                        |> evaluate === false
            fbGte [v 5; v 4]                        |> evaluate === true
            fbLogicalNot [v true]                          |> evaluate  === false
            fbLogicalNot [v false]                         |> evaluate  === true
            fbLogicalAnd [v true; v false]                 |> evaluate === false
            fbLogicalAnd [v true; v true]                  |> evaluate === true
            fbLogicalAnd [v true; v true; v true; v false] |> evaluate === false
            fbLogicalOr  [v true; v false]                 |> evaluate === true
            fbLogicalOr  [v false;v false]                 |> evaluate === false
            fbLogicalOr  [v true; v true; v true; v false] |> evaluate === true
            fShiftLeft [v 1; v 1]                  |> evaluate === 2
            fShiftLeft [v 2; v -1]                 |> evaluate === 0
            fShiftLeft [v 1; v 3]                  |> evaluate === 8
            fShiftRight [v 8; v 3]                 |> evaluate === 1

            let ex = fMul [v 2; v 3]
            fEqual [v 6; ex]                       |> evaluate === true
            fEqual [v 6; fMul [v 2; v 3]]           |> evaluate === true
            fAdd [v 1; v 2]                        |> evaluate === 3


        //pc 버전은 테스트 불가능
        //[<Test>]
        //member __.``31 Rising, Falling test`` () =
        //    let t = createTag("t2", "%M1.1", false)
            //fbFalling [var2expr t] |> evaluate |> ignore
            //fbRising [var2expr t] |> evaluate |> ignore

        [<Test>]
        member __.``4 Composition test`` () =
            fMul [
                    var2expr <| createTag("t2", "%M1.1", 2)
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
            let target = createTag("target", "%M1.1", 1)
            let targetExpr = var2expr target

            let stmt = DuAssign (None, expr, target)
            stmt.Do()
            targetExpr |> evaluate === 24

            (DuAssign (None, v 9, target)).Do()
            targetExpr |> evaluate === 9

            let source = createTag("source", "%M1.1", 33)
            DuAssign(None, var2expr source, target).Do()
            targetExpr |> evaluate === 33
            source.Value <- 44
            targetExpr |> evaluate  === 33
            DuAssign(None, var2expr source, target).Do()
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

            let t1 = createTag("t1", "%M1.1", 1)
            let t2 = createTag("t2", "%M1.1", 2)
            let tt1 = t1 |> var2expr
            let tt2 = t2 |> var2expr
            let addTwoExpr = fAdd [ tt1; tt2 ]
            addTwoExpr.ToText() === "$t1 + $t2"

            let sTag = createTag("address", "%M1.1", "value")
            sTag.ToText() === "$address"
            let exprTag = var2expr sTag
            exprTag.ToText() === "$address"


            let expr = fMul [v 2; v 3; v 4]
            let target = createTag("target", "%M1.1", 1)
            target.ToText() === "$target"

            let stmt = DuAssign (None, expr, target)
            stmt.ToText() === "$target = *(2, 3, 4);"


            let storages = Storages()
            "toBool(0)" |> pns storages === "toBool(0)"


        [<Test>]
        member __.``6 Operator Precedence test`` () =
            let storages = Storages()
            let evalExpr = evalExpr storages

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
            let storages = Storages()
            let pns = pns storages
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
            let storages = Storages()
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
                pns storages exp === exp



        [<Test>]
        member __.``8 Operator test`` () =
            let t = fCastBool [v true]
            let f = fCastBool [v false]
            !@ t |> evaluate === false
            !@ f |> evaluate === true
            t <&&> t |> evaluate === true
            t <&&> f |> evaluate === false
            f <&&> t |> evaluate === false
            f <&&> f |> evaluate === false

            t <||> t |> evaluate === true
            t <||> f |> evaluate === true
            f <||> t |> evaluate === true
            f <||> f |> evaluate === false


            (t <||> f) <&&> t |> evaluate === true

            let target = createTag("bool", "%M1.1", false)
            let targetExpr = var2expr target
            targetExpr |> evaluate === false

            let assignStatement = target <== t
            assignStatement.Do()
            targetExpr |> evaluate === true

            (target <== f).Do()
            targetExpr |> evaluate === false



        [<Test>]
        member __.``9 Tag type test`` () =
            let tags = [
                createTag("sbyte" , "%M1.1", 1y  )    |> var2expr |> iexpr
                createTag("byte"  , "%M1.1", 1uy )    |> var2expr |> iexpr
                createTag("int16" , "%M1.1", 1s  )    |> var2expr |> iexpr
                createTag("uint16", "%M1.1", 1us )    |> var2expr |> iexpr
                createTag("int32" , "%M1.1", 1   )    |> var2expr |> iexpr
                createTag("uint32", "%M1.1", 1u  )    |> var2expr |> iexpr
                createTag("int64" , "%M1.1", 1L  )    |> var2expr |> iexpr
                createTag("uint64", "%M1.1", 1UL )    |> var2expr |> iexpr
                createTag("single", "%M1.1", 1.0f)    |> var2expr |> iexpr
                createTag("double", "%M1.1", 1.0 )    |> var2expr |> iexpr
                createTag("char"  , "%M1.1", '1' )    |> var2expr |> iexpr
                createTag("string", "%M1.1", "1" )    |> var2expr |> iexpr
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

            let createParam (name, v) = {defaultStorageCreationParams(unbox v) (VariableTag.PcUserVariable|>int) with Name=name; System = Some sys}
            let variables = [
                Variable<byte>   (createParam("byte",   box 1uy))  |> var2expr |> iexpr
                Variable<char>   (createParam("char",   box '1'))  |> var2expr |> iexpr
                Variable<double> (createParam("double", box 0.1))  |> var2expr |> iexpr
                Variable<int16>  (createParam("int16",  box 1s))   |> var2expr |> iexpr
                Variable<int32>  (createParam("int32",  box 1))    |> var2expr |> iexpr
                Variable<int64>  (createParam("int64",  box 1L))   |> var2expr |> iexpr
                Variable<int8>   (createParam("sbyte",  box 1y))   |> var2expr |> iexpr
                Variable<single> (createParam("single", box 1.0f)) |> var2expr |> iexpr
                Variable<string> (createParam("string", box "1"))  |> var2expr |> iexpr
                Variable<uint16> (createParam("uint16", box 1us))  |> var2expr |> iexpr
                Variable<uint32> (createParam("uint32", box 1u))   |> var2expr |> iexpr
                Variable<uint64> (createParam("uint64", box 1UL))  |> var2expr |> iexpr
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
            let storages = Storages()
            let evalExpr = evalExpr storages
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
            let storages = Storages()
            let t1 = createTag("1", "%M1.1", 1)
            var2expr t1 |> evaluate === 1
            t1.Value <- 2

            // Invalid assignment: won't compile.  OK!
            // t1.Value <- 2.0

            let exp = $"{dq}hello{dq} + {dq}world{dq}" |> parseExpression4UnitTest storages :?> Expression<string>
            // Invalid assignment: won't compile.  OK!
            // (t1 <== exp)

            let exp2 = fMul [ v 1; v 2; v 3 ]
            // Function-call should take arguments as an IExpression list.
            // let exp2 = mul [ 1; 2; 3 ]
            ()


    type ExpressionObjectTest() =
        inherit ExpressionTestBaseClass()


        [<Test>]
        member __.``1 ExpressionReference test`` () =
            let t1 = createTag("1", "%M1.1", 1)

            (*
            let e1 = tag t1
            let e2 = tag t1
            let isEqual = e1 = e2   // won't compile: error FS0001: 'Expression<int>' 형식은 'equality' 제약 조건을 지원하지 않는 하나 이상의 구조적 요소 형식을 포함하는 레코드, 공용 구조체 또는 구조체이므로 'equality' 제약 조건을 지원하지 않습니다. 이 형식의 경우 같음 조건을 사용하지 말거나 형식에 'StructuralEquality' 특성을 추가하여 같음 조건을 지원하지 않는 필드 형식을 확인하세요.
            *)

            let e1 = var2expr t1 :> IExpression
            let e2 = var2expr t1 :> IExpression
            let isEqual = e1 = e2
            isEqual === false       // IExpression 참조로 접근할 때, equality check 가 불가능한 상태
            e1.IsEqual e2 === true
            ()

    type ExpressionFlattenTest() =
        inherit ExpressionTestBaseClass()


        [<Test>]
        member __.``1 ExpressionFlatten test`` () =
            //let expr =
            let t1 = createTag("1", "%M1.1", 1)

            (*
            let e1 = tag t1
            let e2 = tag t1
            let isEqual = e1 = e2   // won't compile: error FS0001: 'Expression<int>' 형식은 'equality' 제약 조건을 지원하지 않는 하나 이상의 구조적 요소 형식을 포함하는 레코드, 공용 구조체 또는 구조체이므로 'equality' 제약 조건을 지원하지 않습니다. 이 형식의 경우 같음 조건을 사용하지 말거나 형식에 'StructuralEquality' 특성을 추가하여 같음 조건을 지원하지 않는 필드 형식을 확인하세요.
            *)

            let e1 = var2expr t1 :> IExpression
            let e2 = var2expr t1 :> IExpression
            let isEqual = e1 = e2
            isEqual === false       // IExpression 참조로 접근할 때, equality check 가 불가능한 상태
            e1.IsEqual e2 === true
            ()

