type Fruit = Apple | Pear | Orange

type BagItem = { fruit: Fruit; quantity: int }

let takeMore (previous: BagItem list) fruit =
    let toTakeThisTime =
        match previous with
        | bagItem :: otherBagItems -> bagItem.quantity + 1
        | [] -> 1
    { fruit = fruit; quantity = toTakeThisTime } :: previous

let inputs = [ Apple; Pear; Orange ]

([], inputs) ||> List.fold takeMore





let numbers = [1..3]

let mutable total = 0
for n in numbers do
    total <- total + n

printfn "%A" total   // 6


numbers
//|> List.fold (fun total n -> total + n) 0
|> List.fold (+) 0
|> printfn "%A"    // 6


/// initial state
let z0 = []
List.fold (fun z x -> x :: z) z0     [1..5]
//                            ^^     ^^^^^^
//                            z0 ==>   xs
// val it : int list = [5; 4; 3; 2; 1]

List.foldBack (fun x z -> x :: z) [1..5]     z0
//                                  xs   <== z0
//                                ^^^^^^ <== ^^
// val it : int list = [1; 2; 3; 4; 5]



fold (+) 0 [1..10]
foldRight (+) [1..10] 0

let xs = seq {1..10}
fold (+) 0 xs
//foldRight (+) xs 0

distinctBy (fun x -> x % 2) [1; 2; 3; 4; 5]

sortBy (fun x -> -x) [3; 1; 4; 1; 5; 9]


sortDescending [3; 1; 4; 1; 5; 9]
// result1 = [9; 5; 4; 3; 1; 1]

sortByDescending (fun x -> x % 3) [3; 1; 4; 1; 5; 9]
// val it: int list = [5; 1; 4; 1; 3; 9]

let result = scanBack (fun x acc -> x + acc) [1; 2; 3] 0        // [6; 5; 3; 0]
let result = scan (fun x acc -> x + acc) 0 [1; 2; 3]            // [0; 1; 3; 6]

let result1 = take 3 [1; 2; 3; 4; 5]          // result1 = [1; 2; 3]
let result2 = skip 2 [1; 2; 3; 4; 5]          // result2 = [3; 4; 5]
let result3 = takeWhile (fun x -> x < 4) [1; 2; 3; 4; 5]  // result3 = [1; 2; 3]
let result4 = skipWhile (fun x -> x < 4) [1; 2; 3; 4; 5]  // result4 = [4; 5]