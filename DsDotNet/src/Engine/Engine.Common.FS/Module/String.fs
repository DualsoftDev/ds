namespace Engine.Common.FS

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<AutoOpen>]
module StringExtModule =
    let joinLines xs = xs |> String.concat "\r\n"
    let joinWith (xs:string seq) separator = String.Join(separator, xs)
    let notNullAny x = x <> null && x <> ""
    let isNullOrEmpty x = isNull x || x = ""
    let ofNotNullAny xs = xs |> Seq.filter notNullAny

[<Extension>]
type StringExt =
    [<Extension>] static member SplitByLine(x:string
                    , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions) =
                        x.Split([|'\r'; '\n'|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:string
                    , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions) =
                        x.Split([|separator|], splitOption)
    [<Extension>] static member SplitBy(x:string, separator:char
                    , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions) =
                        x.Split([|separator|], splitOption)

    [<Extension>] static member Split(x:string, separator:string) = x.Split([|separator|], StringSplitOptions.None)

    [<Extension>] static member JoinLines xs = joinLines xs
    [<Extension>] static member JoinWith(xs:string seq, separator) = joinWith xs separator
    [<Extension>] static member IsNullOrEmpty(x:string) = isNullOrEmpty x
    [<Extension>] static member NonNullAny(x:string) = notNullAny x
    [<Extension>] static member OfNotNullAny(xs:string seq) = ofNotNullAny xs
    [<Extension>] static member EncloseWith(x:string, wrapper:string) = $"{wrapper}{x}{wrapper}"
    [<Extension>] static member EncloseWith2(x:string, start:string, end_:string) = $"{start}{x}{end_}"

