namespace Dual.Common.Core.FS

open System
open System.IO

// https://dev.to/shimmer/the-state-monad-in-f-3ik0
// https://gist.github.com/jwosty/5338fce8a6691bbd9f6f

// https://tomasp.net/blog/2014/update-monads/

[<AutoOpen>]
module StateBuilderModule =
    type State<'Z, 'A> = State of ('Z -> 'A * 'Z)

    module State =
        let inline run z (State sf) = sf z
        let ret a = State(fun z -> a, z)

        (*
            ( a -> [b] ) -> [a] -> [b]
            ( a -> State<z, b> ) -> State<z, a> -> State<z, b>
         *)
        let bind f (State sf) = State(fun z -> let a, z' = sf z in run z' (f a))


        let get = State(fun z -> z, z)
        let put z = State(fun _ -> (), z)

        (*
            ( a -> b ) -> [a] -> [b]
            ( a -> b ) -> State<z, a> -> State<z, b>
         *)
        let map f (State sf) = State(fun z -> let a, z' = sf z in  f a, z')

        //let map1 f stateA =
        //    State(fun z ->
        //        let a, za = run z stateA
        //        f a, za)

        //let bind1 binder stateA =
        //    State(fun z ->
        //        let a, za = run z stateA
        //        let stateB = binder a
        //        run za stateB)

        //let bind2 f (State sf) =
        //    State(fun z ->
        //        let a, z' = sf z
        //        run z' (f a))

        //let bind3 f (State sf) =
        //    State(fun z ->
        //        let a, z' = sf z
        //        let (State g) = f a
        //        g z')




    /// The state monad passes around an explicit internal state that can be
    /// updated along the way. It enables the appearance of mutability in a purely
    /// functional context by hiding away the state when used with its proper operators
    /// (in StateBuilder()). In other words, you implicitly pass around an implicit
    /// state that gets transformed along its journey through pipelined code.
    type StateBuilder() =
        member _.Zero() = State(fun z -> (), z)
        member _.Return a = State(fun z -> a, z)
        member inline _.ReturnFrom(state: State<'Z, 'A>) = state
        member _.Bind(state, f) : State<'Z, 'A> =
            State(fun z ->
                let (b:'B), z' = State.run z state
                State.run z' (f b))
        member _.Combine(state1: State<'Z, 'A>, state2: State<'Z, 'B>) =
            State(fun z ->
                let a_, z' = State.run z state1
                State.run z' state2)
        member _.Delay f : State<'Z, 'A> = f ()
        member x.For(seq, (f: 'A -> State<'Z, 'B>)) =
            seq
            |> Seq.map f
            |> Seq.reduceBack (fun state1 state2 -> x.Combine (state1, state2))
        member x.While (f, state) =
            if f() then x.Combine(state, x.While (f, state))
            else x.Zero()

    /// State 모나드
    let state = new StateBuilder()

module private ShowSamples =
    let verify c = if not c then failwith "ERROR"
    let verifyM (message:string) condition =
        if not condition then
            failwith message

    let sumOfMultiplesOfThreeUptoTwenty =
        // Reads like imperative code, but it's actually chaining a series of transformations
        // on an invisible state value behind the scenes. No mutable state here.
        state {
            for i in 0..20 do
                if i % 3 = 0 then
                    let! z = State.get
                    do! State.put (z + i)
            return! State.get
        } |> State.run 0 |> fst
    verify ( sumOfMultiplesOfThreeUptoTwenty = ([ 3..3..20 ] |> List.sum) )

    module SeqBased =
        /// Cons the input to the state.  'Z = 's seq
        let push x = State(fun (xs: 's seq) -> (), Seq.append [x] xs)
        /// Update and remove the return value from the top of the stack
        let pop = State(fun (xs: 's seq) -> Seq.head xs, Seq.tail xs)
        /// Update the return value to the top of the stack
        let peek = State(fun (xs: 's seq) -> Seq.head xs, xs)

        module Seq =
            /// Create a safe lazy infinite sequence of random integers using a System.Random
            let fromRandom (randGen: Random) =
                let generator = fun () -> Some(randGen.Next(), ())
                Seq.unfold generator () |> Seq.cache

        let someRandom =
            state {
                // Pull the first random number off the stack
                let! rand1 = pop
                // Pull another random number
                let! rand2 = pop
                // Do some math on them and return the result
                return (rand1 % 4) + (rand2 % 4)
            } |> State.run (Seq.fromRandom (new Random()))

    module ListBased =
        /// Cons the input to the state.  'Z = 's list
        let push x = State(fun z -> (), x::z)
        /// Update and remove the return value from the top of the stack
        let pop = State(fun z ->
            match z with
            | [] -> failwith "Stack is empty"
            | h::t -> h, t)
        /// Update the return value to the top of the stack
        let peek = State(fun z ->
            match z with
            | [] -> failwith "Stack is empty"
            | h::t_ -> h, z)


        let comp =
            state {
                let! a = pop
                if a = 5 then
                    do! push 7
                else
                    do! push 3
                    do! push 8
                return a
            }
        // Output: (9, Stack [8; 3; 0; 2; 1; 0])
        let stack = [9; 0; 2; 1; 0]
        let a, z = State.run stack comp
        verify (a = 9)
        verify (z = [8; 3; 0; 2; 1; 0])

        // Output: (5, Stack [7; 1])
        let stack2 = [5; 1]
        let a2, z2 = State.run stack2 comp
        verify (a2 = 5)
        verify (z2 = [7; 1])

    module TurtleGraphic =
        type Coord = int*int
        let withHistory =
            let up n : State<Coord list, Coord> =
                State(fun (xs: Coord list) ->
                    match xs with
                    | (x, y)::t -> (x, y+n), (x, y+n)::xs
                    | [] -> failwith "ERROR")
            let down n : State<Coord list, Coord> =
                State(fun (xs: Coord list) ->
                    match xs with
                    | (x, y)::t -> (x, y-n), (x, y-n)::xs
                    | [] -> failwith "ERROR")
            let right n : State<Coord list, Coord> =
                State(fun (xs: Coord list) ->
                    match xs with
                    | (x, y)::t -> (x+n, y), (x+n, y)::xs
                    | [] -> failwith "ERROR")
            let left n : State<Coord list, Coord> =
                State(fun (xs: Coord list) ->
                    match xs with
                    | (x, y)::t -> (x-n, y), (x-n, y)::xs
                    | [] -> failwith "ERROR")
            let get = State(fun (xs: Coord list) -> xs|> List.head, xs)
            let put x = State(fun xs -> (), x::xs)


            let graphic =
                state {
                    let! c = get
                    verify(c = (0, 0))

                    let! c = up 1
                    verify(c = (0, 1))

                    let! c = right 3
                    verify(c = (3, 1))

                    let! c = down 1
                    verify(c = (3, 0))

                    let! c = left 3
                    verify(c = (0, 0))

                    do! put (10, 10)
                    let! c = get
                    verify(c = (10, 10))

                    let! c = up 3
                    verify(c = (10, 13))

                    return! State.get
                } |> State.run [ 0, 0 ] |> fst |> List.rev
            verify (graphic = [(0, 0); (0, 1); (3, 1); (3, 0); (0, 0); (10, 10); (10, 13)])
            graphic


        let withoutHistory =
            let up n : State<Coord, Coord> =
                State(fun ((x, y): Coord) -> let c=(x, y+n) in (c, c))
            let down n : State<Coord, Coord> =
                State(fun ((x, y): Coord) -> let c=(x, y-n) in (c, c))
            let right n : State<Coord, Coord> =
                State(fun ((x, y): Coord) -> let c=(x+n, y) in (c, c))
            let left n : State<Coord, Coord> =
                State(fun ((x, y): Coord) -> let c=(x-n, y) in (c, c))
            let get = State(fun ((x, y): Coord) -> let c=(x, y) in (c, c))
            let put ((x, y): Coord) = State(fun z -> (), (x, y))


            let graphic =
                state {
                    let! c = get
                    verify(c = (0, 0))

                    let! c = up 1
                    verify(c = (0, 1))

                    let! c = right 3
                    verify(c = (3, 1))

                    let! c = down 1
                    verify(c = (3, 0))

                    let! c = left 3
                    verify(c = (0, 0))

                    do! put (10, 10)
                    let! c = get
                    verify(c = (10, 10))

                    let! c = up 3
                    verify(c = (10, 13))

                    return! get
                } |> State.run (0, 0) |> fst
            verify (graphic = (10, 13))
            graphic


    module private showFileSample =
        type FileType = File | Dir
        /// A table of replaceable I/O functions. Not to be confused with an IO monad.
        type IO = { copyFile: string * string -> unit; mkdir: string -> unit }

        let show_me_sample() =
            // Some utility types and functions for the next state monad example

            let splitString (separatorChars: char array) (s: string) = s.Split separatorChars
            /// Lazily returns every file and directory that has the given root path as a parent. Directories paths
            /// occur before their children.
            let rec getSystemEntriesRec path =
                seq { yield Dir, path
                      for subPath in Directory.GetDirectories path do
                        yield! getSystemEntriesRec subPath
                      yield! Directory.GetFiles path |> Seq.map (fun f -> File, f) }
            let relativePath root fullPath =
                let sep = [|Path.DirectorySeparatorChar|]
                fullPath |> splitString sep |> Array.skip ((splitString sep root).Length) |> Path.Combine

            // We can use the state monad to implement dependency injection very readably and cleanly. The state is a
            // record of functions that we thread through transparently, so that when we want to call a special function,
            // we just have to get the current function table and call one of its defined functions.


            /// Copies a single file, relative to src, into a path relative to dst
            let copyFile fileType src dst =
                state {
                    // The state monad is like an underground stream running under all our functions using it.
                    // When we want to use a value, we fish for the state value at this point in the stream
                    // and then use it how we please, then putting it a changed version back if we so wish
                    // (though for the purpose of this example there's no need for ever setting the state).
                    let! io = State.get
                    match fileType with
                        | File -> io.copyFile (src, dst)
                        | Dir -> io.mkdir dst
                }

            /// Copies a list of files, relative to src, into a path relative to dst
            let copyFiles srcRoot dstRoot files =
                state {
                    // Get the absolute paths of each source and destination path
                    let fullSrcDstPaths = [
                        for (fileType, srcFullPath) in files do
                            fileType, srcFullPath, Path.Combine ([|dstRoot; relativePath srcRoot srcFullPath|])
                    ]
                    for (fileType, src, dst) in fullSrcDstPaths do
                        do! copyFile fileType src dst
                }

            /// Recursively copy everything in src to dst
            let copyRecursive src dst = getSystemEntriesRec src |> copyFiles src dst

            /// A table of IO functions that actually perform the IO
            let realIO = {
                copyFile = File.Copy
                mkdir = ignore << Directory.CreateDirectory }

            /// A table of mock IO functions that only log their inputs.
            let testIO = {
                copyFile = fun (src, dst) -> printfn "cp %s %s" src dst
                mkdir = fun dir -> printfn "mkdir %s" dir }

            let realCopyRecursive src dst =
                copyRecursive src dst |> State.run realIO |> ignore
            let testCopyRecursive src dst =
                copyRecursive src dst |> State.run testIO |> ignore

            let files = [Dir, "/foo"; Dir, "/foo/dir1"; Dir, "/foo/dir2"; File, "foo/file1"; File, "foo/file2"; File, "foo/dir1/hello"]
            files |> copyFiles "/foo" "/foo2" |> State.run testIO |> fst


