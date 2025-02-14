namespace Dual.Common.Core.FS

open System.Threading.Tasks
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module TaskHelper =
    // https://theburningmonk.com/2012/10/f-helper-functions-to-convert-between-asyncunit-and-task/
    let withLogging<'T> (task: Task<'T>) : Task<'T> =
        // rethrow exception from preceding task if it fauled
        let continuation (t : Task<'T>) : 'T =
            match t.IsFaulted with
            | true ->
                logError "%O" t.Exception.InnerException
                raise t.Exception
            | _ -> t.Result
        task.ContinueWith continuation

    let internal withLoggingInternal (task: Task) : Task =
        // rethrow exception from preceding task if it fauled
        let continuation (t : Task) =
            match t.IsFaulted with
            | true ->
                logError "%O" t.Exception.InnerException
                raise t.Exception
            | _ -> t
        task.ContinueWith continuation


    let fireAndForget (task: Task) = withLoggingInternal task |> ignore
    let toAsync<'T> (task: Task<'T>) : Async<'T> = Async.AwaitTask task

