#load "UntilSomeBuilder.fs"

open Old.Dual.Common.UntilSomeBuilder

let map1 = [ ("1","One"); ("2","Two") ] |> Map.ofList
let map2 = [ ("A","Alice"); ("B","Bob") ] |> Map.ofList
let map3 = [ ("CA","California"); ("NY","New York") ] |> Map.ofList

let multiLookup key = untilSome {
    return! map1.TryFind key
    return! map2.TryFind key
    return! map3.TryFind key
    }

multiLookup "None" |> printfn "Result for None is %A" 
multiLookup "A" |> printfn "Result for A is %A" 
multiLookup "CA" |> printfn "Result for CA is %A" 
multiLookup "X" |> printfn "Result for X is %A" 




