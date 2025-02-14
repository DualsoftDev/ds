namespace Dual.Common.Core.FS

open System

module BuilderImpl =
    /// Real world functional programming, pp. 344
    /// https://fsharpforfunandprofit.com/posts/computation-expressions-intro/
    //    type OptionBuilder() =
    //        member x.Bind(opt, f) =
    //            match opt with
    //                | Some(value) -> f(value)
    //                | _ -> None
    //            member x.Return(v) = Some(v)
    //
    //    let option = new OptionBuilder()



    // https://github.dev/ekonbenefits/FSharp.Interop.NullOptAble
    type OptionBuilder() =
        member __.Zero() = None

        member __.Return(x: 'T) = Some x

        member __.ReturnFrom(m: 'T option) = m
        member __.ReturnFrom(m: 'T Nullable) = Option.ofNullable m
        member __.ReturnFrom(m: 'T when 'T:null) = Option.ofObj m

        member __.Bind(m: 'T option, f) = Option.bind f m
        member __.Bind(m: 'T Nullable, f) = m |> Option.ofNullable |> Option.bind f
        member __.Bind(m: 'T when 'T:null, f) = m |> Option.ofObj |> Option.bind f

        member __.Combine (x, f) = Option.orElseWith f x
        member __.Delay(f: unit -> _) = f
        member __.Run(f) = f()

        member this.TryWith(delayedExpr, handler) =
            try this.Run(delayedExpr)
            with exn -> handler exn

        member this.TryFinally(delayedExpr, compensation) =
            try this.Run(delayedExpr)
            finally compensation()

        member this.Using(resource:#IDisposable, body) =
            this.TryFinally(this.Delay(fun ()->body resource), fun () -> match box resource with null -> () | _ -> resource.Dispose())



    type OptionNoneException(msg) =
        inherit System.Exception(msg)
        new () = OptionNoneException(null)

    let option2result = function
        | Some v -> Ok v
        | None -> Error (new OptionNoneException())
    let result2option = function
        | Ok v ->  Some v
        | Error x -> None

    /// Result<> 기반으로 수행되는 either monad builder.
    /// Binding 수행 함수들이 Success/Failure 의 Result<> type 을 반환할 때에 사용된다.
    type TrialBuilderBasedEither() =
        /// The exception handling uses the delayed type directly
        /// http://tryjoinads.org/docs/computations/monads.html
        member __.TryWith(f, handler) =
            try f() with e -> handler e

        /// The finalizer is defined similarly (but the body of the
        /// finalizer cannot be monadic - just a `unit -> unit` function)
        member m.TryFinally(f, finalizer) =
            try f()
            finally finalizer()

        /// Option binding
        member __.Bind(m, f) =
            match m with
            | Some s -> try f s with exn -> Error (exn)
            | None -> Error (new System.Exception("Option None Error"))

        /// Result/Either binding
        member __.Bind(m, f) =
            match m with
            | Ok s -> try f s with exn -> Error (exn)
            | Error x -> Error x

        member __.Return v = Ok v
        member __.ReturnFrom (m: Result<'S, 'F>) = m
        member __.ReturnFrom (o: Option<'T>) =
            match o with
            | Some v -> Ok v
            | None -> Error (new System.Exception("Option None Error"))
        member __.Delay(f) =
            printf "DELAY";
            try f() with exn -> Error (exn)
        member __.Zero() = Ok ()


    /// Option<> 기반으로 수행되는 maybe monad builder.
    /// Binding 수행 함수들이 Some/None 의 Option<> type 을 반환할 때에 사용된다.
    type TrialBuilderBasedOption() =
        /// The exception handling uses the delayed type directly
        /// http://tryjoinads.org/docs/computations/monads.html
        member __.TryWith(f, handler) =
            try f() with e -> handler e

        /// The finalizer is defined similarly (but the body of the
        /// finalizer cannot be monadic - just a `unit -> unit` function)
        member m.TryFinally(f, finalizer) =
            try f()
            finally finalizer()

        /// Option binding
        member __.Bind(m, f) =
            match m with
            | Some s -> try f s with exn -> None
            | None -> None

        /// Result/Either binding
        member __.Bind(m, f) =
            match m with
            | Ok s -> try f s with exn -> None
            | Error x -> None

        member __.Return v = Some v
        member __.ReturnFrom (o: Option<'T>) = o
        member __.ReturnFrom (m: Result<'S, 'F>) =
            match m with
            | Ok v -> Some v
            | Error ex -> None
        member __.Delay(f) =
            try f() with exn -> None
        member __.Zero() = Some ()


    let trialE = TrialBuilderBasedEither()
    let trialO = TrialBuilderBasedOption()

[<AutoOpen>]
module MaybeBuilder =
    /// Option computation expression builder.
    ///
    /// - option {} 형태로 사용.  let!, do!, return, return!
    let option = BuilderImpl.OptionBuilder()
