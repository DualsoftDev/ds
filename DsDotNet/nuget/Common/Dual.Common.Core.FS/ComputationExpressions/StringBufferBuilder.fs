namespace Dual.Common.Core.FS

open System.Text
open System

/// String Buffer Builder
///
/// - null 문자이면 무시
// https://github.com/fsharp/fslang-suggestions/issues/775
[<AutoOpen>]
module StringBufferBuilder =
    type StringBuffer = StringBuilder -> unit

    type StringBufferBuilder (?separator: string) =
        member _.Yield (txt: string) = fun (b: StringBuilder) -> if not (isNull txt) then Printf.bprintf b "%s" txt
        member _.Yield (c: char) = fun (b: StringBuilder) -> if c <> '\u0000' then Printf.bprintf b "%c" c      // '\u0000'는 null 문자
        member _.Yield (strings: #seq<string>) =
            let separator = defaultArg separator ""
            fun (b: StringBuilder) ->
                let mutable first = true
                for s in strings do
                    if not (isNull s) then
                        if not first then Printf.bprintf b "%s" separator
                        Printf.bprintf b "%s" s
                        first <- false
        member _.YieldFrom (f: StringBuffer) = f
        member _.Combine (f: StringBuffer, g: StringBuffer) =
            let separator = defaultArg separator ""
            fun (b: StringBuilder) ->
                let before = b.Length
                f b
                let afterF = b.Length
                if afterF > before && not (String.IsNullOrEmpty separator) then
                    Printf.bprintf b "%s" separator
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
                    let currentBuffer = f e.Current
                    let before = b.Length
                    currentBuffer b
                    if b.Length > before then
                        first <- false

        member _.While (p: unit -> bool, f: StringBuffer) =
            fun (b: StringBuilder) -> while p () do f b

        member _.Run (f: StringBuffer) =
            let b = StringBuilder()
            do f b
            b.ToString()



    /// stringBuffer builder 계산식
    let stringBuffer = new StringBufferBuilder ()
    /// stringBuffer builder 계산식 with separator
    let stringBufferWithSeparator separator = new StringBufferBuilder (separator = separator)
    /// stringBuffer builder 계산식
    let sb = new StringBufferBuilder ()
    /// stringBuffer builder 계산식 with separator
    let sbs separator = new StringBufferBuilder (separator = separator)
    /// stringBuffer builder 계산식 with newline separator
    let sbn = new StringBufferBuilder (separator = Environment.NewLine)

    type StringBufferBuilder with
        member _.Yield (b: byte) = fun (sb: StringBuilder) -> Printf.bprintf sb "%02x " b



(*
stringBuffer {
    yield "hello"
    yield "world"
}
val it: string = "helloworld"


stringBufferWithSeparator "|" {
    yield "hello"
    yield "world"
    if true then
        yield "!!"

    yield ["a"; "b"; "c"]
    for i in [1..10] do
        yield string i
}
val it: string = "hello|world|!!|a|b|c|1|2|3|4|5|6|7|8|9|10"

sbs ":" { [1..10] >>- string }
val it: string = "1:2:3:4:5:6:7:8:9:10"

*)
