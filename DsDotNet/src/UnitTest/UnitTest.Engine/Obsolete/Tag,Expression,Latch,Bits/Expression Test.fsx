namespace T


open Engine.Core
open Engine.Common.FS
open Engine.Runner
open System
open NUnit.Framework

[<AutoOpen>]
module ExpressionTest =
    type ExpressionTests1() =
        do Fixtures.SetUpTest()
        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore



        [<Test>]
        member __.``And test`` () =
            task {
                logInfo "============== And test"
                init()

                let cpu = createDummyCpu()
                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)


                let a1 = new TagE(cpu, null, "a1_test1")
                let a2 = new TagE(cpu, null, "a2_test1")
                let a3 = new TagE(cpu, null, "a3_test1")
                let xAnd = new And(cpu, "And1_test1", a1, a2, a3)

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()


                xAnd.Value === false
                for x in [a1; a2; a3] do
                    do! enqueue(x, true)                    
                xAnd.Value === true

                do! enqueue(a2, false)
                xAnd.Value === false

                do! enqueue(a2, true)
                xAnd.Value === true

                // And 의 값을 설정할 수 없어야 한다.
                (fun () -> enqueue(xAnd, false).Wait()) |> ShouldFail
                xAnd.Value === true
            } |> Async.AwaitTask |> Async.RunSynchronously



        [<Test>]
        member __.``Or test`` () =
            task {
                logInfo "============== Or test"
                init()

                let cpu = createDummyCpu()

                let a1 = new TagE(cpu, null, "a1_test2")
                let a2 = new TagE(cpu, null, "a2_test2")
                let a3 = new TagE(cpu, null, "a3_test2")
                let xOr = new Or(cpu, "Or1_test2", a1, a2, a3)

                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                xOr.Value === false
                do! enqueue(a2, true)
                xOr.Value === true

                do! enqueue(a3, true)
                xOr.Value === true

                for x in [a1; a2; a3] do
                    do! enqueue(x, false)                    
                xOr.Value === false

                do! enqueue(a1, true)
                xOr.Value === true

                // Or 의 값을 설정할 수 없어야 한다.
                (fun () -> enqueue(xOr, false).Wait()) |> ShouldFail
                xOr.Value === true
            } |> Async.AwaitTask |> Async.RunSynchronously



        [<Test>]
        member __.``Not test`` () =
            logInfo "============== Not test"
            init()

            let cpu = createDummyCpu()


            let a1 = new TagE(cpu, null, "a1_test3")
            let xNot = new Not(cpu, "Not1_test2", a1)

            let wait() = wait(cpu)
            let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            a1.Value === false
            xNot.Value === true

            enqueue(a1, true).Wait()
            xNot.Value === false


            // Not 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(xNot, true).Wait()) |> ShouldFail
            xNot.Value === false


        [<Test>]
        member __.``복합 expression test`` () =
            logInfo "============== 복합 expression test"
            init()

            let cpu = createDummyCpu()

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
            let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            task {
                x.Value === false
                do! enqueue(a, true)
                do! enqueue(b, true)
                x.Value === true
                do! enqueue(b, false)
                x.Value === false

                do! enqueue(c, true)
                x.Value === true
            } |> Async.AwaitTask |> Async.RunSynchronously



            // And 의 값을 설정할 수 없어야 한다.
            (fun () -> enqueue(x, true).Wait()) |> ShouldFail


        [<Test>]
        member __.``(+Latch)복합 expression test`` () =
            logInfo "============== 복합(+Latch) expression test"
            init()

            let cpu = createDummyCpu()

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
            let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            task {
                x.Value === false

                do! enqueue(s, true)

                latch.Value === true
                x.Value === true

                do! enqueue(s, false)
                latch.Value === true
                x.Value === true

                // latch reset ON -> latch reset 및 Or expression OFF 확인
                do! enqueue(r, true)
                latch.Value === false
                x.Value === false
            } |> Async.AwaitTask |> Async.RunSynchronously


            ()