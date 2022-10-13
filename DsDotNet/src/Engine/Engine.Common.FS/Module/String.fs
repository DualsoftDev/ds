namespace Engine.Common.FS

open System
open System.Linq
open System.Runtime.CompilerServices

[<AutoOpen>]
module StringExtModule =
    let joinLines xs = xs |> String.concat "\r\n"
    let notNullAny(x:string) = x <> null && x <> ""
    let isNullOrEmpty(x:string) = isNull x || x = ""
    let ofNotNullAny (xs:string seq) = xs |> Seq.filter notNullAny

[<Extension>]
type StringModule =
    [<Extension>] static member SplitByLine(x:string, splitOption:StringSplitOptions) = x.Split([|'\r'; '\n'|], splitOption)
    [<Extension>] static member SplitByLine(x:string) = x.SplitByLine(StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member SplitBy(x:string, separator:string, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:string) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member SplitBy(x:string, separator:char, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:char) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
    [<Extension>] static member JoinLines xs = joinLines xs
    [<Extension>] static member JoinWith(xs:string seq, separator) = String.Join(separator, xs)
    [<Extension>] static member IsNullOrEmpty(x:string) = isNullOrEmpty x
    [<Extension>] static member NonNullAny(x:string) = notNullAny x
    [<Extension>] static member OfNotNullAny(xs:string seq) = ofNotNullAny xs
    





