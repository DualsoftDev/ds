namespace Dual.Common.Core.FS

open System
open System.Diagnostics
// https://github.com/ekonbenefits/FSharp.Interop.NullOptAble/blob/master/src/FSharp.Interop.NullOptAble/TopLevelBuilders.fs
// https://ekonbenefits.github.io/FSharp.Interop.NullOptAble/RealWorldOperators.html
module ChooseSeqBuilderImpl =

    type GuardBuilder() =
        member __.Zero() = None

        member __.Return(x: 'T) = Some x

        member __.ReturnFrom(m: 'T option) = m
        member __.ReturnFrom(m: 'T Nullable) = Option.ofNullable m
        member __.ReturnFrom(m: 'T when 'T:null) = Option.ofObj m

        member __.Bind(m: 'T option, f) = Option.bind f m
        member __.Bind(m: 'T Nullable, f) = m |> Option.ofNullable |> Option.bind f
        member __.Bind(m: 'T when 'T:null, f) = m |> Option.ofObj |> Option.bind f

        member __.Delay(f: unit -> _) = f
        member __.Run(f) = f() |> ignore

        member __.TryWith(delayedExpr, handler) =
            try delayedExpr()
            with exn -> handler exn
        member __.TryFinally(delayedExpr, compensation) =
            try delayedExpr()
            finally compensation()
        member this.Using(resource:#IDisposable, body) =
            this.TryFinally(this.Delay(fun ()->body resource), fun () -> match box resource with null -> () | _ -> resource.Dispose())


    type NotNullSeq<'T> (source:'T seq) =
        interface Collections.Generic.IEnumerable<'T> with
            member __.GetEnumerator() =
                source
                |> Option.ofObj
                |> Option.map _.GetEnumerator()
                |> Option.defaultWith (fun () -> Seq.empty.GetEnumerator())
        interface Collections.IEnumerable with
            member __.GetEnumerator(): Collections.IEnumerator =
                source
                |> Option.ofObj
                |> Option.map (fun x -> (x :> Collections.IEnumerable).GetEnumerator())
                |> Option.defaultWith (fun ()-> (Seq.empty :> Collections.IEnumerable).GetEnumerator())

    module ChooseSeq =
        let forceRun delayedSeq = delayedSeq |> List.ofSeq :> 'T seq

    type private CombineOptimized<'T>() =
        let source = ResizeArray<'T seq> ()
        interface Collections.Generic.IEnumerable<'T> with
            member __.GetEnumerator() =
                source
                |> Seq.collect id
                |>  (fun x->x.GetEnumerator())
        interface Collections.IEnumerable with
            member __.GetEnumerator(): Collections.IEnumerator =
                source
                |> Seq.collect id
                |> fun x->(x :> Collections.IEnumerable).GetEnumerator()

        member __.AddRange(add:'T seq)=
            source.Add(add)

    type ChooseSeqBuilder() =
        member __.Zero<'T>() = Seq.empty<'T>

        member __.Yield(x: 'T) = Seq.singleton x

        member this.YieldFrom(m: 'T option) : 'T seq =
                m |> function | None -> this.Zero ()
                              | Some x -> this.Yield(x)

        member this.YieldFrom(m: 'T Nullable) : 'T seq =
                m |> Option.ofNullable
                  |> this.YieldFrom

        member this.YieldFrom(m: 'T when 'T:null) : 'T seq =
                    m |> Option.ofObj
                      |> this.YieldFrom

        member __.YieldFrom(m: 'T NotNullSeq) :'T seq =
            upcast m

        member __.YieldFrom(m: 'T list) :'T seq =
            upcast m

        member __.YieldFrom(m: 'T Set) :'T seq =
            upcast m

        member this.Bind(m: 'T option, f:'T->seq<'S>) : seq<'S> =
            match m with
                | Some x -> f x
                | None -> this.Zero<'S>()

        member this.Bind(m: 'T Nullable, f) =
            let m' = m |> Option.ofNullable
            this.Bind(m', f)

        member this.Bind(m: 'T when 'T:null, f) =
            let m' = m |> Option.ofObj
            this.Bind(m', f)

        member __.Combine(a:seq<'T>, b:seq<'T>) : seq<'T>=
            let list =
                match a with
                    | :? CombineOptimized<'T> as l -> l
                    | _ -> let l = CombineOptimized<'T>()
                           l.AddRange(a)
                           l
            list.AddRange(b)
            upcast list

        member __.Delay(f: unit -> _) = Seq.delay f

        member __.Run(f:seq<_>) = NotNullSeq f //makes this a delayed sequence by not running

        member this.While(guard, delayedExpr) =
            let mutable result = this.Zero()
            while guard() do
                result <- this.Combine(result,ChooseSeq.forceRun(delayedExpr))
            result

        member __.TryWith(delayedExpr, handler) =
            try ChooseSeq.forceRun(delayedExpr)
            with exn -> handler exn
        member __.TryFinally(delayedExpr, compensation) =
            try ChooseSeq.forceRun(delayedExpr)
            finally compensation()
        member this.Using(resource:#IDisposable, body) =
            this.TryFinally(this.Delay(fun ()->body resource), fun () -> match box resource with null -> () | _ -> resource.Dispose())

        member this.For(sequence:seq<_>, body) =
            this.Using(sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

[<AutoOpen>]
module ChooseSeqBuilder =
    /// Option 에 대한 guard
    ///
    /// - let!, do!
    let guard = ChooseSeqBuilderImpl.GuardBuilder()

    /// option + seq 두개의 computation builder 역할
    ///
    /// - for, let!, yield, yield!, do!
    let chooseSeq = ChooseSeqBuilderImpl.ChooseSeqBuilder()


module private ChooseSeqBuilderTestSample =
    // https://github.com/ekonbenefits/FSharp.Interop.NullOptAble/blob/master/tests/FSharp.Interop.NullOptAble.Tests/Option.fs
    let private do_guard_test() =
        guard {
            do! Some()
            printfn "-------------------- OK!!"
        }
        guard {
            do! None
            printfn "-------------------- No print!!"
        }

        let a =
            option {
                do! None
                return 1
            }


        let ``Basic Guard don't mutate`` () =
            let mutable test = false
            let x = Nullable<int>();
            let setTrue _ = test <- true
            guard {
                let! x' = x
                printfn "-------------------- This line will not be printed, below will not be executed!!"
                setTrue x'
            }

            assert(test = false)


        let ``Basic Guard do mutate`` () =
            let mutable test = false
            let x = Nullable<int>(3);
            let setTrue _ = test <- true
            guard {
                let! x' = x
                printfn "--------------------- OK"

                setTrue x'
            }

            assert(test = true)

        let ``guard loop test`` () =
            guard {
                // for not supported!
                //for x in [Some "a"; None; Some "b"]  do
                //    let! a = x
                //    printfn $"{a}"
                do! Some()
            }
        ()

    // https://github.com/ekonbenefits/FSharp.Interop.NullOptAble/blob/master/tests/FSharp.Interop.NullOptAble.Tests/RealWorld.fs
    let private do_chooseseq_test() =
        let successes =
            chooseSeq {
                for x in [Some "a"; None; Some "b"]  do
                    let! a = x
                    yield a
                yield! ["hello"; "world"]
            } |> Seq.toArray

        // val successes: string array = [|"a"; "b"; "hello"; "world"|]

        let numSome = successes |> Seq.length
        printfn $"total {numSome} Somes"

    ()
