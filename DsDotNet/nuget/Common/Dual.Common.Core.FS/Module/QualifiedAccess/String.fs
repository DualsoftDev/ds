namespace Dual.Common.Core.FS

open System


[<RequireQualifiedAccess>]
module String =
    let dos2unix (str:string) =
        str.Replace("\r\n", "\n")


    let private q = "'"
    let private qq = "\""

    let surround prefix suffix s = sprintf "%s%s%s" prefix s suffix
    let escapeQuote(s:string) = s.Replace("'", @"\'")
    //let quote q = surround q q
    let singleQuote = surround q q
    let doubleQuote = surround qq qq
    let roundParen = surround "(" ")"
    let squareParen = surround "[" "]"
    let xmlComment = surround "<!--" "-->"

    let removeNewline msg:string =
        Text.RegularExpressions.Regex.Replace(msg, "[\\r\\n]*$", "")

    let split (seperators:char array) (splitOption:StringSplitOptions) (text:string) =
        text.Split(seperators, splitOption)
    let splitByLines (text:string) = text.SplitByLine()
    let splibBy (seperator:char) (text:string) = text.SplitBy(seperator)

    /// 주어진 someString 이 non-null any 이면 이 값을 그대로 반환하고, null or empty 이면 defaultString 값을 반환
    let defaultValue (defaultString:string) (someString:string) =
        if String.IsNullOrEmpty(someString) then defaultString else someString

    let any str = not <| String.IsNullOrEmpty(str)

    let toCharArray (s:string) = s.ToCharArray()
    let toUpper (s:string) = s.ToUpper()
    let toLower (s:string) = s.ToLower()
    let toOption(s:string) = if String.IsNullOrEmpty(s) then None else Some s

    let orElse = defaultValue
    let orElseWith f x = if String.IsNullOrEmpty x then f() else x
