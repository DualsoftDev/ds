namespace Common


open Engine.Common.FS
open System.Collections.Concurrent
open T
open System.Threading.Tasks
open System.Threading
open NUnit.Framework

[<AutoOpen>]
module QueueTestModule =
    type N(n) =
        member val number:int = n with get, set

    // http://www.fssnip.net/hv/title/Extending-async-with-await-on-tasks
    // Author:	Tomas Petricek
    //
    /// Implements an extension method that overloads the standard
    /// 'Bind' of the 'async' builder. The new overload awaits on 
    /// a standard .NET task
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t:Task<'T>, f:'T -> Async<'R>) : Async<'R>  = // for let!
            async.Bind(Async.AwaitTask t, f)
        member x.Bind(t:Task, f:unit -> Async<unit>) : Async<unit>  = // for do!
            async.Bind(Async.AwaitTask t, f)




    type QueueTest() =
        do Fixtures.SetUpTest()


        [<Test>]
        member __.``Async computation expression extended`` () =
            task {
                do! Async.Sleep(1000)
                logDebug "Slept on task"
                do! Async.Sleep(1000)
                logDebug "Slept on task"
            } |> Task.fireAndForget

            // Exception will be catched
            //async {
            //    failwithlog "ERROR"
            //} |> Async.Start

            // Exception won't be catched
            //task {
            //    failwithlog "ERROR"
            //}

            async {
                let! x = Task.FromResult(30)
                x === 30
                do! Task.Delay(1000)
                logDebug "Slept"
                do! Task.Delay(1000)
                logDebug "Slept"
            } |> Async.RunSynchronously


        [<Test>]
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

        [<Test>]
        member __.``MailboxProcessorTest`` () =
            logInfo "============== MailboxProcessorTest"

            let mutable agent:MailboxProcessor<string> option = None
            agent <-
                MailboxProcessor<string>.Start(fun inbox ->
                    let rec loop() =
                        async {
                            let! msg = inbox.Receive()
                            logDebug $"{msg}"
                            if msg.ToUpper() = msg then
                                agent.Value.Post (msg.ToLower())

                            return! loop()
                        } 
                    loop()
                ) |> Some

            [| "hello"; "KWAK!"; "nice"; "TO"; "meet"; "you" |]
            |> Array.iter agent.Value.Post


        [<Test>]
        member __.``AsyncTest`` () =
            logInfo "============== AsyncTest"

            let asyncGetx x = task { return x }
            let asyncGetx2 x = Task.FromResult(x)

            let n1 = asyncGetx  1 |> Task.toAsync |> Async.RunSynchronously
            let n2 = asyncGetx2 2 |> Task.toAsync |> Async.RunSynchronously

            let y =
                task {
                    do! Task.Delay(100)
                    failwithlog "Uncaught Exception 1"
                //} |> Task.withLogging |> Async.AwaitTask |> Async.Start
                //} |> Async.ofTask |> Async.RunSynchronously
                //} |> Async.AwaitTask |> Async.RunSynchronously
                } |> Task.fireAndForget


            task {
                do! Task.Delay(100)
                failwithlog "Uncaught Exception 2"
            } |> Async.startTask


            let kkk =
                task {
                    do! Task.Delay(200)
                    failwithlog "Uncaught Exception 3"
                    return 1
                //} |> Async.ofTask |> Async.StartAsTask |> ignore
                } |> Task.withLogging
                //} |> Task.withLogging |> Async.AwaitTask |> Async.RunSynchronously


            task {
                do! Task.Delay(200)
                failwithlog "Uncaught Exception 4"
                return 1
            //} |> Async.ofTask |> Async.StartAsTask |> ignore
            //} |> Task.withLogging //|> Task.toAsync //|> Async.StartAsTask
            } |> Task.fireAndForget

            let n =
                task {
                    do! Task.Delay(100)
                    //failwithlog "Uncaught Exception 5"
                    return 1
                //} |> Async.ofTask |> Async.RunSynchronously
                } |> Task.withLogging |> Async.AwaitTask |> Async.RunSynchronously

            logDebug "Done with %A" n
            Thread.Sleep(1000)