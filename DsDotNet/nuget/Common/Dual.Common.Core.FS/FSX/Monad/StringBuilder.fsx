// https://github.com/fsharp/fslang-suggestions/issues/775

[<AutoOpen>]
module StringBufferOriginal =
    open System.Text

    type StringBuffer = StringBuilder -> unit

    type StringBufferBuilder () =
        member inline _.Yield (txt: string) = fun (b: StringBuilder) -> Printf.bprintf b "%s" txt
        member inline _.Yield (c: char) = fun (b: StringBuilder) -> Printf.bprintf b "%c" c
        member inline _.Yield (strings: #seq<string>) =
            fun (b: StringBuilder) -> for s in strings do Printf.bprintf b "%s\n" s
        member inline _.YieldFrom ([<InlineIfLambda>] f: StringBuffer) = f
        member inline _.Combine ([<InlineIfLambda>] f,[<InlineIfLambda>] g) = fun (b: StringBuilder) -> f b; g b
        member inline _.Delay ([<InlineIfLambda>] f) = fun (b: StringBuilder) -> (f()) b
        member inline _.Zero () = ignore

        member inline _.For (xs: 'a seq, [<InlineIfLambda>] f: 'a -> StringBuffer) =
            fun (b: StringBuilder) ->
                use e = xs.GetEnumerator ()
                while e.MoveNext() do
                    (f e.Current) b

        member inline _.While ([<InlineIfLambda>] p: unit -> bool, [<InlineIfLambda>] f: StringBuffer) =
            fun (b: StringBuilder) -> while p () do f b

        member inline _.Run ([<InlineIfLambda>] f: StringBuffer) =
            let b = StringBuilder()
            do f b
            b.ToString()

    let stringBuffer = new StringBufferBuilder ()

    type StringBufferBuilder with
      member inline __.Yield (b: byte) = fun (sb: StringBuilder) -> Printf.bprintf sb "%02x " b


let fakeBin s = stringBuffer {
    for c in s do if c < '5' then '0' else '1'
}
fakeBin "0123456789" |> printfn "%s"














[<AutoOpen>]
module StringBuffer =
    open System.Text

    type StringBuffer = StringBuilder -> unit

    type StringBufferBuilder (?separator: string) =
        member _.Yield (txt: string) = fun (b: StringBuilder) -> Printf.bprintf b "%s" txt
        member _.Yield (c: char) = fun (b: StringBuilder) -> Printf.bprintf b "%c" c
        member _.Yield (strings: #seq<string>) =
            let separator = defaultArg separator ""
            fun (b: StringBuilder) ->
                let mutable first = true
                for s in strings do
                    if not first then Printf.bprintf b "%s" separator
                    Printf.bprintf b "%s" s
                    first <- false
        member _.YieldFrom (f: StringBuffer) = f
        member _.Combine (f: StringBuffer, g: StringBuffer) =
            let separator = defaultArg separator ""
            fun (b: StringBuilder) ->
                f b
                if not (separator = "") then Printf.bprintf b "%s" separator
                g b
        member _.Delay (f: unit -> StringBuffer) = fun (b: StringBuilder) -> (f()) b
        member _.Zero () = ignore

        member _.For (xs: 'a seq, f: 'a -> StringBuffer) =
            let separator = defaultArg separator ""
            fun (b: StringBuilder) ->
                let mutable first = true
                use e = xs.GetEnumerator ()
                while e.MoveNext() do
                    if not first then Printf.bprintf b "%s" separator
                    (f e.Current) b
                    first <- false

        member _.While (p: unit -> bool, f: StringBuffer) =
            fun (b: StringBuilder) -> while p () do f b

        member _.Run (f: StringBuffer) =
            let b = StringBuilder()
            do f b
            b.ToString()

    let stringBuffer = new StringBufferBuilder ()
    let stringBufferWithSeparator separator = new StringBufferBuilder (separator = separator)

    type StringBufferBuilder with
        member _.Yield (b: byte) = fun (sb: StringBuilder) -> Printf.bprintf sb "%02x " b



// Usage example
let fakeBin s = stringBuffer {
    for c in s do if c < '5' then '0' else '1'
}
fakeBin "0123456789" |> printfn "%s"

// Example with separator
let fakeBinWithSeparator s = stringBufferWithSeparator "," {
    for c in s do if c < '5' then '0' else '1'
}
fakeBinWithSeparator "0123456789" |> printfn "%s"



stringBufferWithSeparator "|" {
    yield "hello"
    yield "world"
    if true then
        yield "!!"

    yield ["a"; "b"; "c"]
    for i in [1..10] do
        yield string i
}




