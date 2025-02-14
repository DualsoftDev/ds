namespace Dual.Common.Core.FS

open System.Threading.Tasks
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module Async =
    /// 아무것도 하지 않는 Async unit
    let unit = async {()}

    /// Task<> -> Async<>
    /// Async.AwaitTask 와 동일한 역할을 하지만, exception 발생시, 해당 exception 을 logging.
    /// see EmLinq_Task.cs:: FireAndForget
    // https://theburningmonk.com/2012/10/f-helper-functions-to-convert-between-asyncunit-and-task/
    let ofTask<'T> (task: Task<'T>) : Async<'T> =
        task |> TaskHelper.withLogging |> Async.AwaitTask

    let toTask<'T> (work: Async<'T>) : Task<'T> =
        Task.Run(fun () -> work |> Async.RunSynchronously) |> TaskHelper.withLogging

    let startTask (task: Task) =
        task |> TaskHelper.withLoggingInternal |> ignore

    /// Apply an asynchronous transforming function to the result of an asynchronous computation.
    let inline bind f a = async {
        let! x = a
        return! f x
    }

    /// Apply a transforming function to the result of an asynchronous computation.
    let inline map f a = async {
        let! x = a
        return f x
    }

    let inline iter f a = async {
        let! x = a
        f x
    }

    /// Apply an asynchronous transforming function to the result of an asynchronous computation.
    /// Equivalent to `bind`.
    let inline mapAsync f a = bind f a

    /// Applies a side-effect function to the result of an async computation and returns the async.
    let inline tee f a = map (fun x -> f x; x) a

    // Return an asynchronous computation that will wait for the given task to complete.
    let inline AwaitPlainTask (task: System.Threading.Tasks.Task) =
        task |> Async.AwaitIAsyncResult |> Async.Ignore
