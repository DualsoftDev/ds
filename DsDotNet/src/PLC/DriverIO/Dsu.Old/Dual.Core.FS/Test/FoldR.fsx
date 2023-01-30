
let length xs = xs |> Seq.fold (fun _ n -> n + 1) 0
Seq.fol

let rec length = function
    | [] -> 0
    | x::xs -> 1 + length xs

let length xs = xs |> Seq.map (fun n -> 1) |> Seq.map (( *) 2)
let length = Seq.map (fun n -> 1) >> Seq.map (( *) 2)


let length = Seq.map (fun n -> 1) >> Seq.sum

let f = exp >> sqrt

let doubleSumError = (Seq.map (( *) 2) >>  Seq.sum) // error FS0030: Value restriction. The value 'doubleSumError' has been inferred to have generic type
let doubleSum a = (Seq.map (( *) 2) >>  Seq.sum) a
let doubleSum2 : seq<int> -> int = (Seq.map (( *) 2) >>  Seq.sum)

// https://www.youtube.com/watch?v=gw6wo6dIwy0
let argmax f xs = Seq.fold (fun acc x -> if f x > f acc then x else acc) (Seq.head xs) xs