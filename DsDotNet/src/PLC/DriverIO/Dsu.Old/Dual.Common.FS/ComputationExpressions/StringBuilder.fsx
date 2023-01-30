

#load "StringBuilder.fs"

let bytes2hex (bytes : byte []) =
    stringBuilder {
        for byte in bytes -> sprintf "%02x" byte
    } |> build

printfn "%s" (bytes2hex [|(byte)2; (byte)55|])
//builds a string from four strings
stringBuilder {
        yield "one"
        yield "two"
        yield "three"
        yield "four"
    } |> build

