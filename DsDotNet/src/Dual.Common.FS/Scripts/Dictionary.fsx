open System.Collections.Generic

let tryGetValue (dict:IDictionary<'k, 'v>) (k:'k) =
    match dict.TryGetValue(k) with
    | true, v -> Some(v)
    | _ -> None

let dict = Dictionary<int, string>()
dict.Add(1, "One")

let one = tryGetValue dict 1
let two = tryGetValue dict 2


