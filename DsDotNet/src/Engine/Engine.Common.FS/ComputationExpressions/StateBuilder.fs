namespace Engine.Common.FS

open System
open System.IO

// https://tomasp.net/blog/2014/update-monads/


// https://dev.to/shimmer/the-state-monad-in-f-3ik0
// https://gist.github.com/jwosty/5338fce8a6691bbd9f6f
[<AutoOpen>]
module StateBuilderModule =
    type State<'s, 'a> = State of ('s -> ('a * 's))

    module State =
        let inline run state (State f) = f state
        let ret result = State(fun state -> (result, state))
        let bind binder stateful =
            State(fun state ->
                let result, state' = stateful |> run state
                binder result |> run state')
        let get = State(fun s -> s, s)
        let put newState = State(fun _ -> (), newState)
        let map f s = State(fun (state: 's) ->
            let x, state = run state s
            f x, state)

    /// The state monad passes around an explicit internal state that can be
    /// updated along the way. It enables the appearance of mutability in a purely
    /// functional context by hiding away the state when used with its proper operators
    /// (in StateBuilder()). In other words, you implicitly pass around an implicit
    /// state that gets transformed along its journey through pipelined code.
    type StateBuilder() =
        member this.Zero () = State(fun s -> (), s)
        member this.Return x = State(fun s -> x, s)
        member inline this.ReturnFrom (x: State<'s, 'a>) = x
        member this.Bind (x, f) : State<'s, 'b> =
            State(fun state ->
                let (result: 'a), state = State.run state x
                State.run state (f result))
        member this.Combine (x1: State<'s, 'a>, x2: State<'s, 'b>) =
            State(fun state ->
                let result, state = State.run state x1
                State.run state x2)
        member this.Delay f : State<'s, 'a> = f ()
        member this.For (seq, (f: 'a -> State<'s, 'b>)) =
            seq
            |> Seq.map f
            |> Seq.reduceBack (fun x1 x2 -> this.Combine (x1, x2))
        member this.While (f, x) =
            if f () then this.Combine (x, this.While (f, x))
            else this.Zero ()

    let state = new StateBuilder()

module private ShowSamples =
    let sumOfMultiplesOfThreeUptoTwenty =
        // Reads like imperative code, but it's actually chaining a series of transformations
        // on an invisible state value behind the scenes. No mutable state here.
        state {
            for i in 0..20 do
                if i % 3 = 0 then
                    let! s = State.get
                    do! State.put (s + i)
            return! State.get
        } |> State.run 0 |> fst
    let verify c = if not c then failwith "ERROR"
    verify ([ 3..3..20 ] |> List.sum = sumOfMultiplesOfThreeUptoTwenty)

    module SeqBased =
        /// Cons the input to the state
        let push x = State(fun (s: 's seq) -> (), Seq.append [x] s)
        /// Update and remove the return value from the top of the stack
        let pop = State(fun (s: 's seq) -> Seq.head s, Seq.tail s)
        /// Update the return value to the top of the stack
        let peek = State(fun (s: 's seq) -> Seq.head s, s)

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
        /// Cons the input to the state
        let push x = State(fun (s: 's list) -> (), x::s)
        /// Update and remove the return value from the top of the stack
        let pop = State(fun (s: 's list) ->
            match s with
            | [] -> failwith "Stack is empty"
            | h::t -> h, t)
        /// Update the return value to the top of the stack
        let peek = State(fun (s: 's list) ->
            match s with
            | [] -> failwith "Stack is empty"
            | h::t -> h, s)


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
        printfn "%A" (State.run stack comp)

        // Output: (5, Stack [7; 1])
        let stack2 = [5; 1]
        printfn "%A" (State.run stack2 comp)



    // Some utility types and functions for the next state monad example

    let splitString separatorChars (s: string) = s.Split separatorChars

    type FileType = File | Dir
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

    /// A table of replaceable I/O functions. Not to be confused with an IO monad.
    type IO = { copyFile: string * string -> unit; mkdir: string -> unit }

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


