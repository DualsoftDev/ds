[<AutoOpen>]
module Engine.Common.FS.Prelude

open System

let dispose (x:#IDisposable) = if x <> null then x.Dispose()
let toString x = x.ToString()

/// x.ToText() 을 반환
let inline show x = (^T : (member ToText : unit->string) x)
/// x.ToText() 을 반환
let inline toText x = (^T : (member ToText : unit->string) x)


/// x.Name 을 반환
let inline name x = ( ^T: (member Name:string) x )

/// x.Value 을 반환
let inline value x = (^T : (member Value : 'v) x)

//#r "nuget: FSharpPlus"
//open FSharpPlus

//[1..10] |> sum


// https://github.com/fsharp/fsharp/blob/cb6cb5c410f537c81cf26825657ef3bb29a7e952/src/fsharp/FSharp.Core/printf.fs#L1645
let failwithf format =
    Printf.ksprintf failwith format


