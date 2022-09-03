namespace Engine.Common.FS

open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Async =
    /// 아무것도 하지 않는 Async unit
    let unit = async {()}

    /// Task<> -> Async<>
    // https://theburningmonk.com/2012/10/f-helper-functions-to-convert-between-asyncunit-and-task/
    let ofTask<'T> (task: Task<'T>) = 
        // rethrow exception from preceding task if it fauled
        let continuation (t : Task<'T>) : 'T =
            match t.IsFaulted with
            | true -> raise t.Exception
            | _ -> t.Result
        task.ContinueWith continuation |> Async.AwaitTask

    let toTask<'T> (work: Async<'T>) =
        Task.Run(fun () -> work |> Async.RunSynchronously)
        