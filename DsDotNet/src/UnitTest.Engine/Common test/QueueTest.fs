namespace Common


open Xunit
open Engine
open Engine.Core
open System.Linq
open Engine.Common.FS
open Xunit.Abstractions
open System.Collections.Concurrent
open UnitTest.Engine

[<AutoOpen>]
module QueueTestModule =
    type N(n) =
        member val number:int = n with get, set

    type QueueTest(output1:ITestOutputHelper) =

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``QueueTest`` () =
            logInfo "============== QueueTest"
            let q = new ConcurrentQueue<N>()
            [1..10] |> Seq.iter (N >> q.Enqueue)

            for n in q do
                logDebug $"{n.number}"


            for n in q do
                n.number <- n.number * 10
            for n in q do
                logDebug $"{n.number}"
