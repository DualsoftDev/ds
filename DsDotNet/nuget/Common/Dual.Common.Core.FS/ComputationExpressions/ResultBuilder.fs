namespace Dual.Common.Core.FS

open System

// https://github.com/Acadian-Ambulance/Acadian.FSharp
[<AutoOpen>]
module ResultBuilderModule =
    // https://fssnip.net/7UJ
    type ResultBuilder() =
        let orElseWith f x =
            match x with
            | Ok _ -> x
            | _ -> f()

        member this.Bind (x, f) = Result.bind f x
        member this.Return x = Ok x
        member this.ReturnFrom (x: Result<_,_>) = x
        member this.Zero () = Error "No result"
        member this.Delay f = f
        member this.Run f = f ()

        member __.Combine (x: Result<'T, 'Error>, f: unit -> Result<'T, 'Error>) = orElseWith f x

        member this.TryWith (body, handler) =
            try this.ReturnFrom (body ())
            with e -> handler e

        member this.TryFinally (body, compensation) =
            try this.ReturnFrom (body ())
            finally compensation ()

        member this.Using (disposable: #IDisposable, body) =
            let body' () = body disposable
            this.TryFinally(body', fun () -> disposable.Dispose())

        // https://fssnip.net/7UJ
        member __.While(guard, f) =
            if not (guard()) then
                Ok ()
            else
                do f() |> ignore
                __.While(guard, f)

        member __.For(sequence:seq<_>, body) =
            __.Using(sequence.GetEnumerator(), fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current)))


    /// Result<> type 모나드
    ///
    /// Combine returns upon encountering an Error value, so an `if` without an else that returns an Error value will return
    /// that value without executing the following code, while an Ok () will return the result of the following code.
    let result = ResultBuilder()
