namespace Dual.Common

open System.Threading

[<AutoOpen>]
module TaskModule =
    /// Awaits dotnet Task, asynchronosely
    ///
    /// taskf: Task<'a> generating function.  Not Task<'a> itself.
    let awaitDotNetTask taskf =
        async {
            return! Async.AwaitTask (taskf())
        }

    /// Waits dotnet Task, synchornosly
    let waitDotNetTask taskf =
        awaitDotNetTask taskf |> Async.RunSynchronously


    /// 주어진 action f 를 주어진 시간 limitMilli 내에 수행.  실패하면 exception raise
    /// C# 구현은 EmTask.ExecuteWithTimeLimit 참고
    let withTimeLimit f (limitMilli:int) (description:string) =
        let cts = new CancellationTokenSource();
        cts.CancelAfter(limitMilli);
        let task = Async.StartAsTask(async{return f()}, cancellationToken=cts.Token)
        if not (task.Wait(limitMilli)) then
            failwithlogf "Timeout(%d ms) expired on %s." limitMilli description

        task.Result

    // http://www.fssnip.net/gu/title/Cooperative-cancellation-in-Async-workflows
    type Cancellable() =
        static member Do(f:CancellationToken->unit, ct:CancellationToken) =
            let comp = async { f(ct) }
            Async.Start(comp, ct)

        static member Do(act:System.Action<CancellationToken>, ct:CancellationToken) =
            let comp =
                let f(ct) = act.Invoke(ct)
                async { f(ct) }
            Async.Start(comp, ct)


            // A unique overload for method 'Do' could not be determined based on type information prior to this program point. A type annotation may be needed. Candidates:
            // static member Cancellable.Do : act:Action<CancellationToken> * ct:CancellationToken -> 'a,
            // static member Cancellable.Do : f:(CancellationToken -> unit) * ct:CancellationToken -> unit

            //        let f (ct:CancellationToken) = act.Invoke(ct)
            //        Cancellable.Do(f, ct)
