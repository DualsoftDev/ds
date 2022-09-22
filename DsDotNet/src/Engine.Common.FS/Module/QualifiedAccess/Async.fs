namespace Engine.Common.FS

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

