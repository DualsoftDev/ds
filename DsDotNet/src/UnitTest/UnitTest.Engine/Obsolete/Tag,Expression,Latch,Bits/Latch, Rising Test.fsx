namespace T


open Engine.Core
open Engine.Runner
open Engine.Common.FS
open System
open NUnit.Framework

[<AutoOpen>]
module LatchTest =
    type LatchTests1() =
        inherit EngineTestBaseClass()
        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore


        [<Test>]
        member __.``Latch test`` () =
            task {
                logInfo "============== Latch test"
                Global.IsInUnitTest === true
                init()


                let cpu = new Cpu("dummy", new DsSystem("dummy", new Model()))

                let tSet = new TagE(cpu, null, "T1")
                let tReset = new TagE(cpu, null, "T2")
                let latch = new Latch(cpu, "Latch1", tSet, tReset)

                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()


                latch.Value === false
                do! enqueue(tSet, true)
                latch.Value === true

                do! enqueue(tReset, true)
                latch.Value === false

                // tSet 조건은 여전히 true 이므로, reset 만 clear 해도 latch ON 상태로 다시 변경된다.
                do! enqueue(tReset, false)
                latch.Value === true

                // set 조건만 false
                do! enqueue(tSet, false)
                latch.Value === true

                // reset 조건만 true
                do! enqueue(tReset, true)
                latch.Value === false

                // reset 이 살아 있으므로, set 시켜도 latch 안됨
                do! enqueue(tSet, true)
                latch.Value === false
            } |> Async.AwaitTask |> Async.RunSynchronously

        [<Test>]
        member __.``ResetLatch test`` () =
            task {
                let cpu = createDummyCpu()
                let going = Flag(cpu, "Going")
                let finish = Flag(cpu, "Finish")
                let notFinish = Not(finish)
                let rlBSet = And(cpu, "And_rlBSet", going, notFinish)
                let rlBReset = Flag(cpu, "Reset")
                let latch = Latch(cpu, "rlB", rlBSet, rlBReset)

                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.EnqueueAsync(bit, value)
                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                do! enqueue(going, true)
                rlBSet.Value === true
                latch.Value === true

                do! enqueue(rlBReset, true)
                latch.Value === false
                rlBSet.Value === true

                ()
            } |> Async.AwaitTask |> Async.RunSynchronously


        //[<Test>]
        //member __.``Obsolete Rising test`` () =
        //    logInfo "============== Rising test"
        //    init()
        //    let cpu = new Cpu("dummy", new Model())

        //    let t1 = new Tag(cpu, null, "T1")
        //    let t2 = new Tag(cpu, null, "T2")
        //    let ``t1↑`` = new Rising(cpu, "RisingT1", t1)
        //    let ``t2↑`` = new Rising(cpu, "RisingT2", t2)
        //    let ``t1↓`` = new Falling(cpu, "FallingT1", t1)
        //    let ``t2↓`` = new Falling(cpu, "FallingT2", t2)

        //    ``t1↑``.Value === false
        //    ``t2↑``.Value === false
        //    ``t1↓``.Value === false
        //    ``t2↓``.Value === false


        //    (fun () -> ``t1↑``.Value <- false) |> ShouldFail
        //    ``t1↑``.Value === false
        //    (fun () -> ``t1↓``.Value <- false) |> ShouldFail
        //    ``t1↓``.Value === false

        //    t1.Value <- true
        //    ``t1↑``.Value === true
        //    t1.Value <- false
        //    ``t1↓``.Value === false



        //    let ``↑&↓s`` = [``t1↑`` :> IBit; ``t2↑``; ``t1↓``; ``t2↓``]
        //    let ``↑counter`` =
        //        ``↑&↓s`` |> Seq.map (fun r -> (r, 0))
        //        |> Tuple.toDictionary
        //    let ``↓counter`` =
        //        ``↑&↓s`` |> Seq.map (fun r -> (r, 0))
        //        |> Tuple.toDictionary
        //    use _subs =
        //        Global.BitChangedSubject
        //            .Where(fun bc -> ``↑&↓s`` |> Seq.contains(bc.Bit))
        //            .Subscribe(fun bc ->
        //                let bit = bc.Bit
        //                logDebug $"RisingBit changed: [{bit}] = {bc.NewValue}"
        //                let map = if bc.NewValue then ``↑counter`` else ``↓counter``
        //                map[bit] <- map[bit] + 1
        //            )
        //    let reset = new Tag(cpu, null, "Reset")
        //    let latch = new Latch(cpu, "Latch1", ``t1↑``, reset)

        //    //``↑counter``[``t1↑``] === 0
        //    t1.Value <- true
        //    // --> t1.Value Rising 되는 순간에 t1Rising.Value 가 ON 되었다가 바로 OFF 된다.
        //    latch.Value === true
        //    ``t1↑``.Value === false
        //    //``↑counter``[``t1↑``] === 1

        //    (fun () -> latch.Value <- false) |> ShouldFail
        //    latch.Value === true

        //    t1.Value <- false
        //    //``↓counter``[``t1↓``] === 1

        //    ()


