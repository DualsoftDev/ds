namespace Dual.Common.FS.LSIS
open System

[<AutoOpen>]
module StringExt =
    type String with
        member x.SplitByLine(splitOption:StringSplitOptions) = x.Split([|'\r'; '\n'|], splitOption)
        member x.SplitByLine() = x.SplitByLine(StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:string, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:string) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)
        member x.SplitBy(separator:char, splitOption:StringSplitOptions) = x.Split([|separator|], splitOption)
        member x.SplitBy(separator:char) = x.SplitBy(separator, StringSplitOptions.RemoveEmptyEntries)

    let split (text:string) (seperators:char array) (splitOption:StringSplitOptions) =
        text.Split(seperators, splitOption)
    let splitByLines (text:string) = text.SplitByLine()
    let splibBy(text:string) (seperator:char) = text.SplitBy(seperator)