namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework
open Engine.Cpu.TagModule
open Engine.Cpu

[<AutoOpen>]
module ExpressionTestModule =
    
    type ExpressionTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``1 ExpressionValueUnit test`` () =

            //지원 value type : bool, int, single, double, string
            1       |> evaluate === 1
            1       |> evaluate === 1
            "hello" |> evaluate === "hello"
            Math.PI |> evaluate === Math.PI
            true    |> evaluate === true
            false   |> evaluate === false
            3.14f   |> evaluate === 3.14f
            3.14    |> evaluate === 3.14

            /////미지원 value type : uint, int64, ... 지원 기준외 등등
            (fun () -> 1u   |> evaluate  === 1) |> ShouldFail
            (fun () -> 1L   |> evaluate  === 1) |> ShouldFail
            (fun () -> 1.0m |> evaluate  === 1) |> ShouldFail

            ////함수 없는 Value 배열 평가는 불가능
            (fun () -> [1;2]   |> evaluate  === 1) |> ShouldFail

        [<Test>]
        member __.``2 ExpressionTagUnit test`` () =
            let t1 = PlcTag.Create("1", 1)
            t1 |> evaluate === 1
            t1.SetValue(2)
            
            t1 |> evaluate === 2
            t1.SetValue(3)
            t1 |> evaluate === 3

            let t2 = PlcTag.Create("2", 2)
            add[t2;t2] |> evaluate === 4
            //함수 없는 Tag 배열 평가는 불가능
            (fun () -> [t2;t2]   |> evaluate  === 1) |> ShouldFail

            PlcTag.Create("Two", "Two") |> evaluate === "Two"
            
            addString([
                    PlcTag.Create("Hello", "Hello, ") 
                    PlcTag.Create("World", "world!" ) 
                ]) |> evaluate === "Hello, world!"

            let tt1 = t1 |> createTagExpr
            t1.SetValue( 1 )
            let tt2 = PlcTag.Create("t2", 2) 

            let addTwoExpr = Function("+", [ tt1; tt2 ])
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
            Function("+", [1; 2])     |> evaluate === 3

        [<Test>]
        member __.``4 ExpressionComposition test`` () =
            mul [   
                    0
                   // [2;2]
                    PlcTag.Create("t2", 2) 
                    add [1; 2] 
                    add [4; 5] 
            ] |> evaluate === 0


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

            let stmt = Assign (expr, target)
            stmt.Do()
            target |> evaluate === 24

            (Assign (createDataExpr 9, target)).Do()
            target |> evaluate === 9

            let source = PlcTag.Create("source", 33)
            Assign(createTagExpr source, target).Do()
            target |> evaluate === 33
            source.SetValue 44
            target |> evaluate  === 33
            Assign(createTagExpr source, target).Do()
            target |> evaluate === 44
          
        [<Test>]
        member __.``6 Serialization test`` () = 
            
            createDataExpr 1         |> ToText === "1"
            createDataExpr "hello"   |> ToText === "hello"
            createDataExpr Math.PI   |> ToText === Math.PI.ToString()
            createDataExpr true      |> ToText === "True"
            createDataExpr false     |> ToText === "False"
            createDataExpr 3.14f     |> ToText === "3.14"
            createDataExpr 3.14      |> ToText === "3.14"
            

            mul [   2
                    add [1; 2] 
                    add [4; 5] 
            ] |> ToText === "*[2; +[1; 2]; +[4; 5]]"
            mul [2; add[3; 4]]|> ToText  === "*[2; +[3; 4]]"

            mul [2; 3; 4]|> ToText  === "*[2; 3; 4]"
            mul [2; 3; 4]|> ToText  === "*[2; 3; 4]"

            let t1 = PlcTag.Create("t1", 1) 
            let t2 = PlcTag.Create("t2", 2) 
            let tt1 = t1 |> createTagExpr
            let tt2 = t2 |> createTagExpr

            let addTwoExpr = Function("+", [ tt1; tt2 ])
            addTwoExpr.ToText() === "+[(t1=1); (t2=2)]"


            let sTag = PlcTag.Create("address", "value")
            sTag.ToText() === "(address=value)"
            let exprTag = createTagExpr sTag
            exprTag.ToText() === "(address=value)"


            let expr = mul [2; 3; 4]
            let target = PlcTag.Create("target", 1)
            target.ToText() === "(target=1)"

            let stmt = Assign (expr, target)
            stmt.ToText() === "assign(*[2; 3; 4], (target=1))"

        [<Test>]
        member __.``7 Deserialize test`` () =

            let t2 = PlcTag.Create("t2", 2) 
            let t1 = PlcTag.Create("t1", 1) 
            let addTwoExpr = Function("+", [ t1;t2 ])
            addTwoExpr.ToJsonText().ToExpression().ToJsonText() === addTwoExpr.ToJsonText()

            let expr = mul [   2
                               add [t1; t2] 
                               add [4; 5] 
                            ]
            expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()

            let expr = oR [false ;true ;false ]
            expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()

            let expr = add [10 ;12 ]
            expr.ToJsonText().ToExpression().ToJsonText() === expr.ToJsonText()
            let target = PlcTag.Create("target", 1)
            

            let expr = mul [    2
                                expr
                                add [t1; t2] 
                                add [1; 5] 
                    ] 
            let a = expr |> evaluate

            let stmt = Assign (expr, target)
            stmt.ToJsonText().ToStatement().ToJsonText() === stmt.ToJsonText()


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
            target |> evaluate === false

            let assignStatement = target <== t
            assignStatement.Do()
            target |> evaluate === true

            (target <== f).Do()
            target |> evaluate === false

