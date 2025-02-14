namespace Dual.Common.Core.FS

open System
open System.Linq
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<AutoOpen>]
module StringExtModule =
    let joinLines xs = xs |> String.concat "\r\n"
    let joinWith separator (xs:string seq) = String.Join(separator, xs)
    let notNullAny x = x <> null && x <> ""
    let isNullOrEmpty x = isNull x || x = ""
    let ofNotNullAny xs = xs |> Seq.filter notNullAny

[<Extension>]
type StringExt =
    [<Extension>]
    static member SplitByLine(
            x:string
            , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions
    ) =
        x.Split([|'\r'; '\n'|], splitOption)

    [<Extension>]
    static member SplitBy(
        x:string
        , separator:string
        , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions
    ) =
        x.Split([|separator|], splitOption)

    [<Extension>]
    static member SplitBy(
        x:string
        , separator:char
        , [<Optional; DefaultParameterValue(StringSplitOptions.RemoveEmptyEntries)>] splitOption:StringSplitOptions
    ) =
        x.Split([|separator|], splitOption)

    [<Extension>] static member Split(x:string, separator:string) = x.Split([|separator|], StringSplitOptions.None)

    [<Extension>] static member JoinLines x = joinLines x
    [<Extension>] static member JoinWith(xs:string seq, separator) = joinWith separator xs
    [<Extension>] static member IsNullOrEmpty(x:string) = isNullOrEmpty x
    /// String.IsNullOrEmpty().  Name space 충돌 회피용
    [<Extension>] static member IsNullOrEmptyFs(x:string) = isNullOrEmpty x
    [<Extension>] static member NonNullAny(x:string) = notNullAny x
    [<Extension>] static member OfNotNullAny(xs:string seq) = ofNotNullAny xs
    [<Extension>] static member DefaultValue(x:string, coverValue:string) = if isNullOrEmpty x then coverValue else x

    /// NonNull Any 인 값 선택
    [<Extension>] static member OrElse(x, coverValue) = StringExt.DefaultValue(x, coverValue)
    /// NonNull Any 인 값 선택
    [<Extension>] static member OrElseWith(x, f) = if isNullOrEmpty x then f() else x

    [<Extension>] static member EncloseWith(x:string, wrapper:string) = $"{wrapper}{x}{wrapper}"
    [<Extension>] static member EncloseWith2(x:string, start:string, end_:string) = $"{start}{x}{end_}"

