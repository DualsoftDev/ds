namespace UnitTest.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System

[<AutoOpen>]
module ExpressionTest =
    type ExpressionTests1(output1:ITestOutputHelper) =
        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``And test`` () =
            logInfo "============== And test"
            init()

            let cpu = new Cpu("dummy", [||], new Model())

            let a1 = new Tag(cpu, null, "a1_test1")
            let a2 = new Tag(cpu, null, "a2_test1")
            let a3 = new Tag(cpu, null, "a3_test1")
            let xAnd = new And(cpu, "And1_test1", a1, a2, a3)

            xAnd.Value === false
            [a1; a2; a3] |> Seq.iter (fun x -> x.Value <- true)
            xAnd.Value === true

            a2.Value <- false
            xAnd.Value === false

            a2.Value <- true
            xAnd.Value === true

            // And 의 값을 설정할 수 없어야 한다.
            (fun () -> xAnd.Value <- false) |> ShouldFail
            xAnd.Value === true


        [<Fact>]
        member __.``Or test`` () =
            logInfo "============== Or test"
            init()

            let cpu = new Cpu("dummy", [||], new Model())

            let a1 = new Tag(cpu, null, "a1_test2")
            let a2 = new Tag(cpu, null, "a2_test2")
            let a3 = new Tag(cpu, null, "a3_test2")
            let xOr = new Or(cpu, "Or1_test2", a1, a2, a3)

            xOr.Value === false
            a2.Value <- true
            xOr.Value === true

            a3.Value <- true
            xOr.Value === true

            [a1; a2; a3] |> Seq.iter (fun x -> x.Value <- false)
            xOr.Value === false

            a1.Value <- true
            xOr.Value === true

            // Or 의 값을 설정할 수 없어야 한다.
            (fun () -> xOr.Value <- false) |> ShouldFail
            xOr.Value === true



        [<Fact>]
        member __.``Not test`` () =
            logInfo "============== Not test"
            init()

            let cpu = new Cpu("dummy", [||], new Model())

            let a1 = new Tag(cpu, null, "a1_test3")
            let xNot = new Not(cpu, "Not1_test2", a1)

            a1.Value === false
            xNot.Value === true

            a1.Value <- true
            xNot.Value === false


            // Not 의 값을 설정할 수 없어야 한다.
            (fun () -> xNot.Value <- true) |> ShouldFail
            xNot.Value === false


        [<Fact>]
        member __.``복합 expression test`` () =
            logInfo "============== 복합 expression test"
            init()

            let cpu = new Cpu("dummy", [||], new Model())

            // x = a && (b || ( c && !d))

            let a = new Tag(cpu, null, "a1_test4")
            let b = new Tag(cpu, null, "b1_test4")
            let c = new Tag(cpu, null, "c1_test4")
            let d = new Tag(cpu, null, "d1_test4")
            let ``!d`` = new Not(cpu, "!d1", d)
            let ``c&&!d`` = new And(cpu, "c&&!d1", c, ``!d``)
            let ``b || ( c && !d)`` = new Or(cpu, "b || ( c && !d1", b, ``c&&!d``)
            let x = new And(cpu, "a && (b || ( c && !d))", a, ``b || ( c && !d)``)

            x.Value === false
            a.Value <- true
            b.Value <- true
            x.Value === true
            b.Value <- false
            x.Value === false

            c.Value <- true
            x.Value === true

            // And 의 값을 설정할 수 없어야 한다.
            (fun () -> x.Value <- true) |> ShouldFail


        [<Fact>]
        member __.``(+Latch)복합 expression test`` () =
            logInfo "============== 복합(+Latch) expression test"
            init()

            let cpu = new Cpu("dummy", [||], new Model())

            // x = a || latch
            // latch :
            //      Set = s↑
            //      Reset = r↑

            let a = new Flag(cpu, "a_test5")
            let s = new Flag(cpu, "s1_test5")
            let r = new Flag(cpu, "r1_test5")
            let ``s↑`` = new Rising(cpu, "s↑", s)
            let ``r↑`` = new Rising(cpu, "r↑", r)
            let latch = new Latch(cpu, "latch1_test5", ``s↑``, ``r↑``)

            let x = new Or(cpu, "a||latch", a, latch)

            x.Value === false

            s.Value <- true
            latch.Value === true
            x.Value === true

            s.Value <- false
            latch.Value === true
            x.Value === true

            // latch reset ON -> latch reset 및 Or expression OFF 확인
            r.Value <- true
            latch.Value === false
            x.Value === false

            ()