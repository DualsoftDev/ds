namespace Dual.Common.Core.FS

open System
open Microsoft.FSharp.Core

[<AutoOpen>]
module CollectionAlgorithm =
    // https://stackoverflow.com/questions/4495597/combinations-and-permutations-in-f
    /// 주어진 seq 에서 size 갯수크기의 combination 을 생성한다.
    /// combinations 2 [1..5] |> Array.ofSeq
    ///  [| [2; 1]; [3; 1]; [4; 1]; [5; 1];
    ///     [3; 2]; [4; 2]; [5; 2];
    ///     [4; 3]; [5; 3];
    ///     [5; 4]|]
    let combinations size xs =
        let rec combinationsHelper acc size xs =
            seq {
                match size, xs with
                | n, y::ys ->
                    if n > 0 then yield! combinationsHelper (y::acc) (n - 1) ys
                    if n >= 0 then yield! combinationsHelper acc n ys
                | 0, [] -> yield acc
                | _, [] -> () }
        combinationsHelper [] size (xs |> List.ofSeq)


    // https://stackoverflow.com/questions/286427/calculating-permutations-in-f
    /// [1..3] |> permutations |> Array.ofSeq;;
    /// ---> [|[1; 2; 3]; [1; 3; 2]; [2; 1; 3]; [2; 3; 1]; [3; 1; 2]; [3; 2; 1]|]
    let permutations xs =
        let rec permu xs taken =
            seq {
                if Set.count taken = List.length xs then
                    yield []
                else
                    for l in xs do
                        if not (Set.contains l taken) then
                            for perm in permu xs (Set.add l taken) do
                                yield l::perm }
        permu (xs |> List.ofSeq) Set.empty

    //// http://euler.synap.co.kr/prob_detail.php?id=24
    //// crashes on [1..10] |> permutations |> Array.ofSeq
    let permutations2 xs =
        let rec permu = function
            | []      -> [List.empty]
            | x :: xs -> List.collect (insertions x) (permu xs)
        and insertions x = function
            | []             -> [[x]]
            | (y :: ys) as xs -> (x::xs)::(List.map (fun x -> y::x) (insertions x ys))

        permu (xs |> List.ofSeq)
        |> List.sort


    let crossProduct2 l1 l2 =
        seq {
            for el1 in l1 do
                for el2 in l2 do
                    yield el1, el2 }

    /// Seq of Seq 로 주어진 각 항목들에 대해서 cross product 수행
    /// e.g [[1; 2]; [3; ]; [7; 8; 9]]
    /// ==> [   [1; 3; 7]; [1; 3; 8]; [1; 3; 9]
    ///         [2; 3; 7]; [2; 3; 8]; [2; 3; 9] ]
    // https://stackoverflow.com/questions/32263241/cartesian-product-in-c-sharp-using-enumeratos
    let rec crossProduct xss =
        if xss |> Seq.isEmpty then
            [ List.empty ]
        else
            let head = xss |> Seq.head
            let tailCross = crossProduct(xss |> Seq.skip 1)

            [
                for h in head do
                    for ts in tailCross do
                        yield h::ts
            ]

    /// Seq of Seq 로 주어진 각 항목들에 대해서 cross product 수행.  이때, empty sequence 는 무시
    /// e.g [[1; 2]; []; [7; 8; 9]]
    /// ==> [   [1; 7]; [1; 8]; [1; 9]
    ///         [2; 7]; [2; 8]; [2; 9] ]
    let rec crossProductIgnoringEmpty xss =
        if xss |> Seq.isEmpty then
            [ List.empty ]
        else
            let sets = xss |> Seq.filter (Seq.isEmpty >> not)
            let head = sets |> Seq.head
            let tailCross = crossProduct(sets |> Seq.skip 1)

            [
                for h in head do
                    for ts in tailCross do
                        yield h::ts
            ]

    /// <summary>
    /// 주어진 sequence 를 unsort/random shuffle/scramble 한다.
    /// http://www.fssnip.net/16/title/Sequence-Random-Permutation
    /// </summary>
    /// <param name="xs"></param>
    let scramble xs =
        let rnd = new System.Random()
        let rec scramble2 xs =
            /// Removes an element from a sequence.
            let remove n xs = xs |> Seq.filter (fun x -> x <> n)

            seq {
                let x = xs |> Seq.item (rnd.Next(0, xs |> Seq.length))
                yield x
                let sqn' = remove x xs
                if not (sqn' |> Seq.isEmpty) then
                    yield! scramble2 sqn'
            }
        scramble2 xs

    // Example:
    //scramble ['1' .. '9'] |> Seq.toList
    //scramble [1..9] |> Seq.toList
    //scramble ['a' .. 'z'] |> Seq.toList


    /// <summary>
    /// http://www.fssnip.net/2n/title/Sequnsort
    /// </summary>
    /// <param name="xs"></param>
    let unsort xs =
            //let rand = System.Random(Seed=0)
            let rand = System.Random()
            xs
            |> Seq.map (fun x -> rand.Next(),x)
            |> Seq.cache
            |> Seq.sortBy fst
            |> Seq.map snd



    /// <summary>
    /// 주어진 sequence 를 계속 circular population 한다.
    /// http://www.fssnip.net/1N/title/Function-to-generate-circular-infinite-sequence-from-a-list
    /// </summary>
    /// <param name="lst"></param>
    let generateCircularSeqObsolete xs =
        let rec next () =
            seq {
                for x in xs do
                    yield x
                yield! next()
            }
        next()
    /// 주어진 sequence 를 계속 circular population 한다.
    // https://riptutorial.com/fsharp/example/18041/infinite-repeating-sequences
    let generateCircularSeq xs = seq { while true do yield! xs}
    //generateCircularSeq [1..3] |> Seq.take 10 |> List.ofSeq
    //val it: int list = [1; 2; 3; 1; 2; 3; 1; 2; 3; 1]


    /// <summary>
    /// http://www.fssnip.net/1R/title/Take-every-Nth-element-of-sequence
    /// </summary>
    /// <param name="n"></param>
    /// <param name="seq"></param>
    let everyNth n xs =
        xs |> Seq.mapi (fun i el -> el, i)              // Add index to element
           |> Seq.filter (fun (el, i) -> i % n = n - 1) // Take every nth element
           |> Seq.map fst                               // Drop index from the result
    //> [1..100] |> everyNth 11;;
    //val it: seq<int> = seq [11; 22; 33; 44; ...]


    // C# : EmConsecutive.ToConsecutiveGroups
    // http://vaskir.blogspot.com/2013/09/grouping-consecutive-integers-in-f-and.html
    /// [1; 2; 4; 5; 6; 9] |> groupConsecutive ==> [[1;2]; [4;5;6]; [9]]
    let groupConsecutive xs =
        List.foldBack (fun x acc ->
            match acc, x with
            | [], _ -> [[x]]
            | (h :: t) :: rest, x when h - x <= 1 -> (x :: h :: t) :: rest
            | acc, x -> [x] :: acc) xs []


    /// m 과 n 사이의 수를 random 으로 발생
    let randomSequence m n =
        let m = defaultArg m 1
        let n = defaultArg n 100
        seq {
            let rng = new Random()
            while true do
                yield rng.Next(m,n)
        }

    //let rec last = function
    //| hd :: [] -> hd
    //| hd :: tl -> last tl
    //| _ -> failwithlog "Empty list."

    module private TestMe =
        let a = randomSequence None (Some 7) |> Seq.take 10 |> List.ofSeq   // e.g [5; 3; 1; 1; 1; 6; 3; 3; 6; 2]

