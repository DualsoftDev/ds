namespace Dual.Common.Core.FS

open System

// https://github.dev/Acadian-Ambulance/Acadian.FSharp
[<AutoOpen>]
module AsyncOptionBuilderModule =
    type AsyncOptionBuilder() =
        member this.Bind (a, f) = async.Bind(a, f)
        member this.Bind (o, f) = async {
            match o with
            | Some x -> return! f x
            | None -> return None
        }
        member this.Return x = x |> Some |> async.Return
        member this.Return (_: unit) = this.Zero ()
        member this.ReturnFrom (x: Option<_>) = x |> async.Return
        member this.ReturnFrom (x: Async<Option<_>>) = async.ReturnFrom x
        member this.Zero () = None |> async.Return
        member this.Delay f = async.Delay f
        member this.Combine (x, y) = async {
            match! x with
            | Some a -> return Some a
            | None -> return! y
        }
        member this.Using (x, f) = async.Using (x, f)

        member this.TryWith (body: unit -> Async<Option<_>>, handler) =
            try this.ReturnFrom (body ())
            with e -> handler e

        member this.TryFinally (body: unit -> Async<Option<_>>, compensation) =
            try this.ReturnFrom (body ())
            finally compensation ()


    type AsyncResultBuilder() =
        member this.Bind (a, f) = async.Bind(a, f)
        member this.Bind (r, f) = async {
            match r with
            | Ok x -> return! f x
            | Error e -> return Error e
        }
        member this.Return x = x |> Ok |> async.Return
        member this.Return (_: unit) = this.Zero ()
        member this.ReturnFrom (x: Result<_,_>) = ResultBuilder().ReturnFrom x |> async.Return
        member this.ReturnFrom (x: Async<Result<_,_>>) = async.ReturnFrom x
        member this.Zero () = Ok () |> async.Return
        member this.Delay f = async.Delay f
        member this.Combine (x, y) = async {
            match! x with
            | Ok () -> return! y
            | Error e -> return Error e
        }
        member this.Using (x, f) = async.Using (x, f)

        member this.TryWith (body: unit -> Async<Result<_,_>>, handler) =
            try this.ReturnFrom (body ())
            with e -> handler e

        member this.TryFinally (body: unit -> Async<Result<_,_>>, compensation) =
            try this.ReturnFrom (body ())
            finally compensation ()

        member this.While (guard, body: unit -> Async<Result<_,_>>) =
            if guard() then
                this.Bind(body(), fun res ->
                    this.Bind(res, fun () ->
                        this.While (guard, body)
                    )
                )
            else
                this.Zero()

        member this.For (items: seq<_>, body: _ -> Async<Result<_,_>>) =
            this.Using(items.GetEnumerator(), fun it ->
                this.While(it.MoveNext, fun () -> body it.Current)
            )


    /// Async Option 모나드
    ///
    /// Combine returns the first Some value, so an `if` without an else that returns a Some value will return that value
    /// without executing the following code, while a None will return the result of the following code.
    let asyncOption = AsyncOptionBuilder()

    /// Async Result 모나드
    ///
    /// Combine returns upon encountering an Error value, so an `if` without an else that returns an Error value will return
    /// that value without executing the following code, while an Ok () will return the result of the following code.
    let asyncResult = AsyncResultBuilder()
