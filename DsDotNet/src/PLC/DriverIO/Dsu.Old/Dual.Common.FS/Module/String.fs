namespace Old.Dual.Common

open System

[<AutoOpen>]
module StringExtModule =

    type String with
        member x.SplitByLine(splitOption:StringSplitOptions) = x.Split([|'\r'; '\n'|], splitOption)
        member x.SplitByLine() = x.SplitByLine(StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:string, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:string) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:char, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:char) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)





