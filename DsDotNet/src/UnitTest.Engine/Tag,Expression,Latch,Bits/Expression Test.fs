namespace UnitTest.Engine


open Xunit
open Engine.Core
open Engine.Common.FS
open Xunit.Abstractions
open System
open System.Threading

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

            let cpu = new Cpu("dummy", new Model())
            let wait() = wait(cpu)
            let enqueue(bit, value) =
                cpu.Enqueue(bit, value)
                wait()


            let a1 = new TagE(cpu, null, "a1_test1")
            let a2 = new TagE(cpu, null, "a2_test1")
            let a3 = new TagE(cpu, null, "a3_test1")
            let xAnd = new And(cpu, "And1_test1", a1, a2, a3)

            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()


            xAnd.Value === false
            [a1; a2; a3] |> Seq.iter (fun x -> enqueue(x, true))
            xAnd.Value === true

            enqueue(a2, false)
            xAnd.Value === false

            enqueue(a2, true)
            xAnd.Value === true

            // And 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(xAnd, false)) |> ShouldFail
            xAnd.Value === true


        [<Fact>]
        member __.``Or test`` () =
            logInfo "============== Or test"
            init()

            let cpu = new Cpu("dummy", new Model())

            let a1 = new TagE(cpu, null, "a1_test2")
            let a2 = new TagE(cpu, null, "a2_test2")
            let a3 = new TagE(cpu, null, "a3_test2")
            let xOr = new Or(cpu, "Or1_test2", a1, a2, a3)

            let wait() = wait(cpu)
            let enqueue(bit, value) =
                cpu.Enqueue(bit, value)
                wait()
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            xOr.Value === false
            enqueue(a2, true)
            xOr.Value === true

            enqueue(a3, true)
            xOr.Value === true

            [a1; a2; a3] |> Seq.iter (fun x -> enqueue(x, false))
            xOr.Value === false

            enqueue(a1, true)
            xOr.Value === true

            // Or 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(xOr, false)) |> ShouldFail
            xOr.Value === true



        [<Fact>]
        member __.``Not test`` () =
            logInfo "============== Not test"
            init()

            let cpu = new Cpu("dummy", new Model())


            let a1 = new TagE(cpu, null, "a1_test3")
            let xNot = new Not(cpu, "Not1_test2", a1)

            let wait() = wait(cpu)
            let enqueue(bit, value) =
                cpu.Enqueue(bit, value)
                wait()
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            a1.Value === false
            xNot.Value === true

            enqueue(a1, true)
            xNot.Value === false


            // Not 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(xNot, true)) |> ShouldFail
            xNot.Value === false


        [<Fact>]
        member __.``복합 expression test`` () =
            logInfo "============== 복합 expression test"
            init()

            let cpu = new Cpu("dummy", new Model())

            // x = a && (b || ( c && !d))

            let a = new TagE(cpu, null, "a1_test4")
            let b = new TagE(cpu, null, "b1_test4")
            let c = new TagE(cpu, null, "c1_test4")
            let d = new TagE(cpu, null, "d1_test4")
            let ``!d`` = new Not(cpu, "!d1", d)
            let ``c&&!d`` = new And(cpu, "c&&!d1", c, ``!d``)
            let ``b || ( c && !d)`` = new Or(cpu, "b || ( c && !d1", b, ``c&&!d``)
            let x = new And(cpu, "a && (b || ( c && !d))", a, ``b || ( c && !d)``)


            let wait() = wait(cpu)
            let enqueue(bit, value) =
                cpu.Enqueue(bit, value)
                wait()
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()



            x.Value === false
            enqueue(a, true)
            enqueue(b, true)
            x.Value === true
            enqueue(b, false)
            x.Value === false

            enqueue(c, true)
            x.Value === true

            // And 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(x, true)) |> ShouldFail


        [<Fact>]
        member __.``(+Latch)복합 expression test`` () =
            logInfo "============== 복합(+Latch) expression test"
            init()

            let cpu = new Cpu("dummy", new Model())

            // x = a || latch
            // latch :
            //      Set = s↑
            //      Reset = r↑

            let a = new Flag(cpu, "a_test5")
            let s = new Flag(cpu, "s1_test5")
            let r = new Flag(cpu, "r1_test5")
            let latch = new Latch(cpu, "latch1_test5", s, r)
            let latch2 = new Latch(cpu, "latch2_test5", s, r)

            let x = new Or(cpu, "a||latch", a, latch)

            let wait() = wait(cpu)
            let enqueue(bit, value) =
                cpu.Enqueue(bit, value)
                wait()
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            x.Value === false

            enqueue(s, true)

            latch.Value === true
            x.Value === true

            enqueue(s, false)
            latch.Value === true
            x.Value === true

            // latch reset ON -> latch reset 및 Or expression OFF 확인
            enqueue(r, true)
            latch.Value === false
            x.Value === false

            ()