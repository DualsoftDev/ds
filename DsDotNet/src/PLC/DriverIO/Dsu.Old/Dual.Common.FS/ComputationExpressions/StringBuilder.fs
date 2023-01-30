// http://www.fssnip.net/d5
// http://www.fssnip.net/d5/title/A-Computation-Expression-wrapper-for-StringBuilder
// http://stackoverflow.com/questions/18595597/is-using-a-stringbuilder-a-right-thing-to-do-in-f
module Old.Dual.Common.StringBuilder

open System

type FsStringBuilder = FsSb of (Text.StringBuilder -> unit)

let build (FsSb f) =
    let b = new Text.StringBuilder()
    do f b
    b.ToString ()

type StringBuilder () =
    let (!) = function FsSb f -> f
    member __.Yield (txt : string) = FsSb(fun b -> b.Append txt |> ignore)
    member __.Yield (c : char) = FsSb(fun b -> b.Append c |> ignore)
    member __.YieldFrom f = f : FsStringBuilder

    member __.Combine(f,g) = FsSb(fun b -> !f b; !g b)
    member __.Delay f = FsSb(fun b -> !(f ()) b) : FsStringBuilder
    member __.Zero () = FsSb(fun _ -> ())
    member __.For (xs : 'a seq, f : 'a -> FsStringBuilder) =
                    FsSb(fun b ->
                        let e = xs.GetEnumerator ()
                        while e.MoveNext() do
                            !(f e.Current) b)
    member __.While (p : unit -> bool, f : FsStringBuilder) =
                    FsSb(fun b -> while p () do !f b)

/// computation expression instance
let stringBuilder = new StringBuilder ()

//    // example
//    let bytes2hex (bytes : byte []) =
//        stringBuilder {
//            for byte in bytes -> sprintf "%02x" byte
//        } |> build
//
//    //builds a string from four strings
//    let s =
//        stringBuilder {
//            yield "one"
//            yield "two"
//            yield "three"
//            yield "four"
//        } |> build

