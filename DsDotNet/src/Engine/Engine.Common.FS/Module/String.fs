namespace Engine.Common.FS

open System
open System.Runtime.CompilerServices

[<Extension>]
type StringModule =
    [<Extension>] static member SplitByLine(x:string, splitOption:StringSplitOptions) = x.Split([|'\r'; '\n'|], splitOption)
    [<Extension>] static member SplitByLine(x:string) = x.SplitByLine(StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member SplitBy(x:string, separator:string, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:string) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member SplitBy(x:string, separator:char, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:char) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member JoinLines(xs:string seq) = String.Join("\r\n", xs)
    [<Extension>] static member JoinWith(xs:string seq, separator) = String.Join(separator, xs)


[<AutoOpen>]
module StringExtModule =
    let joinLines xs = xs |> String.concat "\r\n"



