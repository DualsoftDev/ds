namespace Engine.Common.FS

// https://github.com/Acadian-Ambulance/Acadian.FSharp
[<AutoOpen>]
module ResultBuilderModule =
    open System
    type ResultBuilder() =
        member this.Bind (x, f) = Result.bind f x
        member this.Return x = Ok x
        member this.ReturnFrom (x: Result<_,_>) = x
        member this.Zero () = Ok ()
        member this.Delay f = f
        member this.Run f = f ()
        member this.Combine (x: Result<unit, _>, f) = Result.bind f x

        member this.TryWith (body, handler) =
            try this.ReturnFrom (body ())
            with e -> handler e

        member this.TryFinally (body, compensation) =
            try this.ReturnFrom (body ())
            finally compensation ()

        member this.Using (disposable: #IDisposable, body) =
            let body' () = body disposable
            this.TryFinally(body', fun () -> disposable.Dispose())

        member this.While (guard, body) =
            if guard() then
                this.Bind(body(), fun () ->
                    this.While (guard, body)
                )
            else
                this.Zero()

        member this.For (items: seq<_>, body) =
            this.Using(items.GetEnumerator(), fun it ->
                this.While(it.MoveNext,
                    this.Delay(fun () -> body it.Current)
                )
            )



